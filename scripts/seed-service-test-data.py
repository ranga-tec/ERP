"""Seed service-module test data on a LOCAL environment.

Extends the section 4 baseline in docs/service-module-test-plan.md with the records the
later sections need: the three entitlement paths, batch and serial tracked parts, extra
technicians, and a few jobs so the dashboards are not empty.

Idempotent - re-running skips anything that already exists.

    python scripts/seed-service-test-data.py --email you@local --password 'Passw0rd1!'

NEVER point this at production. It creates demo customers, equipment and jobs, and posts
stock adjustments that create real inventory value.
"""

import argparse
import json
import sys
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone

ITEM_EQUIPMENT, ITEM_SPARE, ITEM_SERVICE = 1, 2, 3
TRACK_NONE, TRACK_SERIAL, TRACK_BATCH = 0, 1, 2
COVER_NONE, COVER_INSPECTION, COVER_LABOR, COVER_PARTS, COVER_LABOR_PARTS = 0, 1, 2, 3, 4
CONTRACT_ANNUAL = 0

now = datetime.now(timezone.utc)


class Api:
    def __init__(self, base, token):
        self.base, self.token = base, token

    def __call__(self, method, path, body=None):
        data = json.dumps(body).encode() if body is not None else None
        req = urllib.request.Request(self.base + path, data=data, method=method)
        req.add_header("authorization", f"Bearer {self.token}")
        req.add_header("content-type", "application/json")
        try:
            with urllib.request.urlopen(req, timeout=90) as r:
                raw = r.read().decode()
                return r.status, (json.loads(raw) if raw.strip() else None)
        except urllib.error.HTTPError as e:
            return e.code, e.read().decode()[:400]


def login(base, email, password):
    req = urllib.request.Request(
        base + "/api/auth/login",
        data=json.dumps({"email": email, "password": password}).encode(),
        method="POST",
    )
    req.add_header("content-type", "application/json")
    with urllib.request.urlopen(req, timeout=60) as r:
        return json.load(r)["token"]


def find(rows, key, value):
    return next((r for r in rows if str(r.get(key, "")).upper() == value.upper()), None)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--base", default="http://localhost:5257")
    p.add_argument("--email", required=True)
    p.add_argument("--password", required=True)
    args = p.parse_args()

    if "localhost" not in args.base and "127.0.0.1" not in args.base:
        sys.exit(f"Refusing to seed a non-local host: {args.base}")

    api = Api(args.base, login(args.base, args.email, args.password))

    def ensure(label, list_path, key, value, create_path, payload):
        rows = api("GET", list_path)[1] or []
        existing = find(rows, key, value)
        if existing:
            print(f"  = {label:<34} exists")
            return existing
        status, body = api("POST", create_path, payload)
        if status not in (200, 201):
            print(f"  ! {label:<34} FAILED {status}: {body}")
            return None
        print(f"  + {label:<34} created")
        return body if isinstance(body, dict) else find(api("GET", list_path)[1] or [], key, value)

    def ensure_user(email, display_name, roles, technician_code=None):
        users = api("GET", "/api/admin/users?take=500")[1] or []
        user = find(users, "email", email)
        if user:
            print(f"  = User {email:<28} exists")
            if sorted(user.get("roles") or []) != sorted(roles):
                status, body = api("PUT", f"/api/admin/users/{user['id']}/roles", {"roles": roles})
                if status != 200:
                    print(f"  ! User roles {email:<22} FAILED {status}: {body}")
        else:
            admin = find(users, "email", args.email)
            status, body = api("POST", "/api/admin/users", {
                "companyId": admin.get("companyId") if admin else None,
                "email": email,
                "password": "Demo@1234",
                "displayName": display_name,
                "roles": roles,
            })
            if status != 200:
                print(f"  ! User {email:<28} FAILED {status}: {body}")
                return None
            user = body
            print(f"  + User {email:<28} created")

        if technician_code and user:
            status, body = api("PUT", f"/api/admin/users/{user['id']}/technician", {
                "code": technician_code,
                "defaultCostRate": 18,
                "defaultBillingRate": 35,
                "phone": None,
                "notes": "Local service-module demo user",
                "isActive": True,
            })
            if status == 200:
                print(f"  = Technician profile {technician_code:<17} linked")
            else:
                print(f"  ! Technician profile {technician_code:<17} FAILED {status}: {body}")
        return user

    print("== prerequisites ==")
    ensure("UoM PCS", "/api/uoms", "code", "PCS", "/api/uoms", {"code": "PCS", "name": "Pieces"})
    wh = ensure("Warehouse MAIN", "/api/warehouses", "code", "MAIN", "/api/warehouses",
                {"code": "MAIN", "name": "Main Store", "address": "Nugegoda"})
    ensure("Category SUNDRIES", "/api/item-categories", "code", "SUNDRIES", "/api/item-categories",
           {"companyId": None, "code": "SUNDRIES", "name": "Sundries",
            "revenueAccountId": None, "expenseAccountId": None})
    sundries = find(api("GET", "/api/item-categories")[1] or [], "code", "SUNDRIES")

    print("== customers ==")
    cust1 = ensure("Customer CUS-SVC", "/api/customers", "code", "CUS-SVC", "/api/customers",
                   {"code": "CUS-SVC", "name": "Service Test Customer", "phone": None, "email": None, "address": None})
    cust2 = ensure("Customer CUS-SVC2", "/api/customers", "code", "CUS-SVC2", "/api/customers",
                   {"code": "CUS-SVC2", "name": "Second Service Customer", "phone": None, "email": None, "address": None})

    print("== items ==")
    specs = [
        ("EQ-GEN01",  "Generator Set",         ITEM_EQUIPMENT, TRACK_SERIAL, 0,   None),
        ("SP-FILT",   "Hydraulic Filter",      ITEM_SPARE,     TRACK_NONE,   500, None),
        ("SP-GREASE", "Multipurpose Grease",   ITEM_SPARE,     TRACK_NONE,   0,   (sundries or {}).get("id")),
        ("LAB-SVC",   "Service Labour",        ITEM_SERVICE,   TRACK_NONE,   0,   None),
        ("SP-BATCH",  "Engine Oil (batch)",    ITEM_SPARE,     TRACK_BATCH,  120, None),
        ("SP-SERIAL", "Control Board (serial)", ITEM_SPARE,    TRACK_SERIAL, 900, None),
    ]
    items = {}
    for sku, name, itype, track, cost, cat in specs:
        rec = ensure(f"Item {sku}", "/api/items", "sku", sku, "/api/items", {
            "companyId": None, "sku": sku, "name": name, "type": itype, "trackingType": track,
            "unitOfMeasure": "PCS", "brandId": None, "categoryId": cat, "subcategoryId": None,
            "barcode": None, "defaultUnitCost": cost, "revenueAccountId": None, "expenseAccountId": None,
        })
        if rec:
            items[sku] = rec

    print("== technicians ==")
    for code, name, cost, bill in (("TECH1", "Workshop Technician", 10, 25),
                                   ("TECH2", "Field Technician", 12, 30),
                                   ("TECH3", "Senior Technician", 18, 45)):
        ensure(f"Technician {code}", "/api/service/technicians", "code", code, "/api/service/technicians",
               {"code": code, "name": name, "defaultCostRate": cost, "defaultBillingRate": bill,
                "phone": None, "notes": None})

    print("== users and linked technician profiles ==")
    ensure_user("service.manager@local", "Local Service Manager", ["Service", "Reporting"])
    ensure_user("technician.one@local", "Local Technician One", ["Service"], "TECH-U1")
    ensure_user("technician.two@local", "Local Technician Two", ["Service"], "TECH-U2")
    ensure_user("technician.three@local", "Local Technician Three", ["Service"], "TECH-U3")

    print("== equipment units (entitlement matrix) ==")
    units_spec = [
        # serial,          customer, warrantyUntil,            coverage,            note
        ("SN-GEN-0001", cust1, None,                    None,                "no warranty -> Billable"),
        ("SN-GEN-0002", cust1, now + timedelta(days=180), COVER_LABOR_PARTS,  "in warranty -> CoveredNoCharge"),
        ("SN-GEN-0003", cust1, None,                    None,                "contract covered -> ServiceContract"),
        ("SN-GEN-0004", cust2, now + timedelta(days=90),  COVER_LABOR,       "labour only -> PartiallyCovered"),
        ("SN-GEN-0005", cust2, now - timedelta(days=30),  COVER_LABOR_PARTS, "warranty EXPIRED -> Billable"),
    ]
    existing_units = api("GET", "/api/service/equipment-units?includeInactive=true")[1] or []
    units = {}
    for serial, cust, warranty, coverage, note in units_spec:
        found = find(existing_units, "serialNumber", serial)
        if found:
            print(f"  = {('Unit ' + serial):<34} exists  ({note})")
            units[serial] = found
            continue
        st, body = api("POST", "/api/service/equipment-units", {
            "itemId": items["EQ-GEN01"]["id"], "serialNumber": serial, "customerId": cust["id"],
            "purchasedAt": (now - timedelta(days=200)).isoformat(),
            "warrantyUntil": warranty.isoformat() if warranty else None,
            "warrantyCoverage": coverage, "serviceIntervalDays": 90,
            "nextServiceDueAt": None, "nextRepairDueAt": None,
        })
        if st in (200, 201):
            units[serial] = body
            print(f"  + {('Unit ' + serial):<34} created ({note})")
        else:
            print(f"  ! {('Unit ' + serial):<34} FAILED {st}: {body}")

    print("== service contract ==")
    contracts = api("GET", "/api/service/contracts")[1] or []
    if any(c.get("equipmentUnitId") == units.get("SN-GEN-0003", {}).get("id") for c in contracts):
        print("  = contract for SN-GEN-0003             exists")
    elif units.get("SN-GEN-0003"):
        st, body = api("POST", "/api/service/contracts", {
            "customerId": cust1["id"], "equipmentUnitId": units["SN-GEN-0003"]["id"],
            "contractType": CONTRACT_ANNUAL, "coverage": COVER_LABOR_PARTS,
            "startDate": (now - timedelta(days=30)).isoformat(),
            "endDate": (now + timedelta(days=335)).isoformat(),
            "notes": "Annual maintenance contract for service testing",
        })
        print(f"  {'+' if st in (200,201) else '!'} contract for SN-GEN-0003             {st}"
              f"{'' if st in (200,201) else ' ' + str(body)}")

    print("== opening stock ==")
    onhand = api("GET", "/api/inventory/onhand")[1] or []
    have = {(r["itemId"], r.get("batchNumber")) for r in onhand if r["onHand"] > 0}
    wanted = [
        # sku, batch, qty, unit cost, serials
        ("SP-FILT",   None,          50,  500, None),
        ("SP-GREASE", None,          20,   80, None),
        ("SP-BATCH",  "LOT-2026-A",  40,  120, None),
        ("SP-BATCH",  "LOT-2026-B",  25,  120, None),
        ("SP-SERIAL", None,           4,  900, [f"CB-{n:04d}" for n in range(1, 5)]),
    ]
    todo = [w for w in wanted if items.get(w[0]) and (items[w[0]]["id"], w[1]) not in have]
    if not todo:
        print("  = stock already present")
    else:
        st, adj = api("POST", "/api/inventory/stock-adjustments",
                      {"warehouseId": wh["id"], "reason": "Service test data opening stock"})
        if st not in (200, 201):
            print(f"  ! adjustment FAILED {st}: {adj}")
        else:
            for sku, batch, qty, cost, serials in todo:
                s, b = api("POST", f"/api/inventory/stock-adjustments/{adj['id']}/lines", {
                    "itemId": items[sku]["id"], "countedQuantity": qty, "quantityDelta": None,
                    "unitCost": cost, "batchNumber": batch, "serials": serials,
                })
                label = f"{sku}{' ' + batch if batch else ''} qty {qty}"
                print(f"  {'+' if s in (200,201,204) else '!'} {label:<34} {s}{'' if s in (200,201,204) else ' ' + str(b)}")
            s, b = api("POST", f"/api/inventory/stock-adjustments/{adj['id']}/post")
            print(f"  {'+' if s in (200,201,204) else '!'} post adjustment                   {s}")

    print("== demo jobs (so dashboards are not empty) ==")
    existing_jobs = api("GET", "/api/service/jobs?take=200")[1] or []
    demo = [
        ("SN-GEN-0002", cust1, 3, "DEMO warranty repair - generator will not start", False),
        ("SN-GEN-0003", cust1, 0, "DEMO contract service - 500 hour maintenance", True),
        ("SN-GEN-0004", cust2, 1, "DEMO chargeable repair - hydraulic leak", False),
    ]
    if len(existing_jobs) >= 3:
        print(f"  = {len(existing_jobs)} job(s) already exist, skipping")
    else:
        for serial, cust, kind, problem, start in demo:
            unit = units.get(serial)
            if not unit:
                continue
            st, job = api("POST", "/api/service/jobs", {
                "equipmentUnitId": unit["id"], "customerId": cust["id"], "problemDescription": problem,
                "kind": kind, "estimatedStartAt": None, "expectedCompletionAt": (now + timedelta(days=5)).isoformat(),
                "siteLocation": "Workshop", "jobDescription": None, "customerComplaint": None,
                "internalRemarks": None, "responsibleOfficerName": None,
            })
            if st not in (200, 201):
                print(f"  ! job on {serial:<26} {st}: {job}")
                continue
            if start:
                api("POST", f"/api/service/jobs/{job['id']}/start")
            print(f"  + job {job['number']} on {serial:<18} {'InProgress' if start else 'Open'}")

    print("\n== summary ==")
    for label, path, key in (("customers", "/api/customers", "code"),
                             ("technicians", "/api/service/technicians", "code")):
        print(f"  {label:<12}", [r[key] for r in (api("GET", path)[1] or [])])
    print("  items       ", sorted(r["sku"] for r in (api("GET", "/api/items")[1] or [])))
    print("  equipment   ", [(u["serialNumber"], "warranty" if u["hasActiveWarranty"] else "-")
                             for u in (api("GET", "/api/service/equipment-units")[1] or [])])
    print("  jobs        ", [(j["number"], j["status"]) for j in (api("GET", "/api/service/jobs?take=200")[1] or [])])
    itm = {i["id"]: i["sku"] for i in (api("GET", "/api/items")[1] or [])}
    print("  on hand     ", [(itm.get(r["itemId"]), r.get("batchNumber"), r["onHand"])
                             for r in (api("GET", "/api/inventory/onhand")[1] or [])])


if __name__ == "__main__":
    main()
