import type { ReactNode } from "react";
import Image from "next/image";
import { Card, Table } from "@/components/ui";

type LinkItem = { href: string; label: string };
type Row = { left: string; right: string };
type FlowStep = {
  title: string;
  input: string;
  output: string;
  check: string;
};

const quickLinks: LinkItem[] = [
  { href: "#start", label: "Start Here" },
  { href: "#master-data", label: "Master Data" },
  { href: "#procurement", label: "Procurement" },
  { href: "#inventory", label: "Inventory" },
  { href: "#sales", label: "Sales" },
  { href: "#service", label: "Service" },
  { href: "#finance", label: "Finance" },
  { href: "#admin", label: "Access & Notifications" },
  { href: "#reporting", label: "Reports" },
  { href: "#checks", label: "Testing Checks" },
];

const moduleRows: Row[] = [
  { left: "Overview", right: "Dashboard, KPIs, queues, and shortcuts." },
  { left: "Master Data", right: "Items, customers, suppliers, warehouses, taxes, currencies, payment types, and setup records." },
  { left: "Procurement", right: "Purchase requisitions, RFQs, purchase orders, goods receipts, supplier invoices, and supplier returns." },
  { left: "Inventory", right: "Stock availability, on-hand checks, reorder alerts, stock adjustments, and stock transfers." },
  { left: "Sales", right: "Quotes, orders, dispatches, direct dispatches, invoices, and customer returns." },
  { left: "Service", right: "Equipment units, jobs, daily sheets, work orders, materials, expenses, estimates, handovers, and closeout." },
  { left: "Finance", right: "AR, AP, payments, petty cash, IOUs, credit notes, debit notes, and allocations." },
  { left: "Reporting", right: "Stock, aging, tax, service, sales, purchase, supplier, and costing reports." },
  { left: "Admin", right: "Users, permissions, notifications, imports, and settings." },
  { left: "Audit Logs", right: "Evidence of who created, changed, approved, posted, or settled records." },
];

const masterDataRows: FlowStep[] = [
  {
    title: "Currencies",
    input: "Code, name, symbol, minor units, base currency flag, and exchange rates where needed.",
    output: "Currency can be used in invoices, payments, and reports.",
    check: "Only one active base currency should exist. Foreign currency transactions need an exchange rate.",
  },
  {
    title: "Items",
    input: "SKU, name, type, UoM, category, cost, tax, and tracking type.",
    output: "Item becomes selectable in purchase, sales, inventory, and service documents.",
    check: "Tracking type must be correct before transactions start. Use serial tracking for equipment units.",
  },
  {
    title: "Customers and suppliers",
    input: "Code, name, contact details, address, active status.",
    output: "Customer can be used in sales/service. Supplier can be used in procurement/AP.",
    check: "Do not duplicate codes. Use inactive status instead of deleting records already used by transactions.",
  },
  {
    title: "Warehouses and bins",
    input: "Warehouse code/name and optional bin, zone, rack, shelf details.",
    output: "Stock can be received, issued, counted, and transferred by location.",
    check: "Unassigned stock means earlier stock exists without a bin/rack.",
  },
];

const procurementRows: FlowStep[] = [
  {
    title: "Purchase requisition",
    input: "Required date, reason, item, quantity, and notes.",
    output: "Draft PR is created. Submit sends it for approval.",
    check: "Users with approve permission receive notifications. Approved PR can convert to PO.",
  },
  {
    title: "RFQ",
    input: "Supplier, requested items, quantities, and notes.",
    output: "RFQ is created and can be marked as sent.",
    check: "RFQ number and PDF are available.",
  },
  {
    title: "Purchase order",
    input: "Supplier, items, quantities, unit costs, taxes.",
    output: "Draft PO is created. Approval confirms the purchase.",
    check: "Creator is notified on approval. Approved PO can be used for goods receipt.",
  },
  {
    title: "Goods receipt",
    input: "PO, warehouse, received quantity, cost, batch, and serials where required.",
    output: "Posting increases stock and can create AP.",
    check: "Inventory availability, stock ledger, and supplier AP must update after posting.",
  },
  {
    title: "Supplier invoice and return",
    input: "Supplier invoice details or return item/quantity/reason.",
    output: "Posting supplier invoice updates AP. Posting return reduces stock and creates supplier credit note.",
    check: "AP, credit notes, stock, and PDFs match the posted document.",
  },
];

const inventoryRows: FlowStep[] = [
  {
    title: "Inventory availability",
    input: "Optional warehouse, item, batch, bin, or serial filters.",
    output: "Searchable stock table with quantity, cost, and value.",
    check: "Posted receipts increase stock. Posted dispatches/MRNs reduce stock.",
  },
  {
    title: "On hand",
    input: "Warehouse, item, and batch filters.",
    output: "Balance view by warehouse, item, batch, or combined view.",
    check: "Use this for exact quantity checks.",
  },
  {
    title: "Stock adjustment",
    input: "Warehouse, reason, item, and counted quantity.",
    output: "Posting records only the variance between system and counted quantity.",
    check: "Stock ledger shows the adjustment. Voided drafts do not affect stock.",
  },
  {
    title: "Stock transfer",
    input: "From warehouse, to warehouse, item, quantity, batch, serials.",
    output: "Posting reduces source warehouse and increases destination warehouse.",
    check: "Total company stock stays the same while warehouse balances change.",
  },
];

const salesRows: FlowStep[] = [
  {
    title: "Quote",
    input: "Customer, valid date, items/services, quantity, price, tax.",
    output: "Draft quote is created. Sending marks it as sent.",
    check: "Quote total and PDF are correct.",
  },
  {
    title: "Sales order",
    input: "Customer, item, quantity, price, tax.",
    output: "Draft order is created. Confirming accepts the order.",
    check: "Confirmed order can be dispatched.",
  },
  {
    title: "Dispatch or direct dispatch",
    input: "Customer/order/job, warehouse, item, quantity, batch/serials.",
    output: "Posting reduces stock. Serialized equipment can create equipment units.",
    check: "Stock decreases and equipment ownership is correct.",
  },
  {
    title: "Sales invoice",
    input: "Customer, source document or manual lines, quantity, price, tax.",
    output: "Posting creates AR.",
    check: "Customer AR, aging report, and invoice PDF are correct.",
  },
  {
    title: "Customer return",
    input: "Customer, optional invoice, item, quantity, reason.",
    output: "Posting returns stock and creates customer credit note.",
    check: "Stock increases and credit note is visible in finance.",
  },
];

const financeRows: FlowStep[] = [
  {
    title: "AR and AP",
    input: "Open Finance -> AR or AP and filter by outstanding only if needed.",
    output: "Shows customer receivables and supplier payables.",
    check: "Posted sales invoices create AR. Posted supplier documents create AP.",
  },
  {
    title: "Payments",
    input: "Direction, counterparty type, counterparty, payment type, currency, rate, amount, notes.",
    output: "Payment document is created and can be allocated.",
    check: "Allocation reduces AR/AP outstanding. Creator is notified when another user allocates.",
  },
  {
    title: "Credit notes",
    input: "Counterparty type, counterparty, amount, reason.",
    output: "Credit note is created and can be allocated to AR or AP.",
    check: "Remaining credit amount reduces after allocation.",
  },
  {
    title: "Debit notes",
    input: "Counterparty type, counterparty, amount, reason.",
    output: "Debit note is created.",
    check: "PDF opens and related counterparty balance is reviewed.",
  },
  {
    title: "Petty cash and IOUs",
    input: "Fund code/name/currency/opening balance, or IOU job/amount/purpose.",
    output: "Petty cash fund or IOU workflow is created.",
    check: "Approvers receive IOU notifications. Top-ups, adjustments, releases, and settlements update balances.",
  },
];

const serviceRows: FlowStep[] = [
  {
    title: "Equipment unit",
    input: "Item/model, serial number, customer, warranty and service details.",
    output: "Customer-owned equipment becomes selectable for jobs.",
    check: "Warranty and contract entitlement are correct before job creation.",
  },
  {
    title: "Job order",
    input: "Equipment, customer, job type, complaint, responsible officer, expected date.",
    output: "Job number is created and appears in service queues.",
    check: "Entitlement, customer, equipment, and status are correct.",
  },
  {
    title: "Daily field sheet",
    input: "Work date, planned work, completed work, pending/issues, site condition.",
    output: "Daily sheet card appears with staff, progress, material, expense, and IOU counts.",
    check: "Create one sheet per working day. Approve or reject sheets before closeout.",
  },
  {
    title: "Materials and MRN",
    input: "Service job, warehouse, item, quantity.",
    output: "Draft MRN is created. Posting issues stock to the job.",
    check: "Draft does not reduce stock. Posted MRN reduces stock and updates job cost.",
  },
  {
    title: "Billing and closeout",
    input: "Handover, estimate/invoice decision, cost review, closeout action.",
    output: "Job can be completed and closed when blockers are cleared.",
    check: "Closeout readiness must show no open blockers.",
  },
];

const adminRows: FlowStep[] = [
  {
    title: "Create user",
    input: "Company, email, password, display name, and roles.",
    output: "User can sign in with assigned broad roles.",
    check: "User sees only expected sidebar modules.",
  },
  {
    title: "Set permissions",
    input: "Open user access permissions and tick exact rights: view, create, edit, approve, post, allocate, settle.",
    output: "Effective permissions control menus and action buttons.",
    check: "Backend blocks unauthorized actions even if the user opens a URL manually.",
  },
  {
    title: "Notifications",
    input: "Submit, approve, post, allocate, release, or settle a workflow document.",
    output: "The correct user or approver group receives an in-app notification.",
    check: "Notification link opens the correct document and can be marked read.",
  },
];

const reportRows: Row[] = [
  { left: "Stock Ledger", right: "Check every receipt, issue, adjustment, and transfer." },
  { left: "Aging", right: "Check customer AR and supplier AP outstanding." },
  { left: "Tax Summary", right: "Check tax totals from posted documents." },
  { left: "Service KPIs", right: "Check service performance and job statistics." },
  { left: "Sales Analysis", right: "Check customer and item sales totals." },
  { left: "Purchase Analysis", right: "Check supplier and item purchase totals." },
  { left: "Supplier Performance", right: "Check supplier activity and delivery/purchase view." },
  { left: "Costing", right: "Check inventory value and cost." },
];

const checks: Row[] = [
  { left: "Create", right: "Document number is generated and document appears in the list." },
  { left: "Edit", right: "Draft document can be changed; posted document is locked or corrected through a proper transaction." },
  { left: "Permission", right: "Unauthorized user cannot view or perform restricted actions." },
  { left: "Approval", right: "Approver receives notification and status changes correctly." },
  { left: "Post", right: "Stock, AR, AP, reports, and audit update only after posting." },
  { left: "PDF", right: "PDF opens or downloads for the business document." },
  { left: "Audit", right: "Audit log shows the user action." },
  { left: "Report", right: "Related report matches the posted result." },
];

const endToEndStock = [
  "Create or confirm item, supplier, customer, warehouse, tax, and currency.",
  "Create and approve a purchase order for quantity 10.",
  "Create and post a GRN from the PO.",
  "Check inventory increased by 10.",
  "Create and confirm a sales order for quantity 4.",
  "Create and post dispatch.",
  "Check inventory reduced by 4.",
  "Create and post sales invoice.",
  "Check AR increased.",
  "Create incoming payment and allocate it to AR.",
  "Check AR outstanding reduced.",
  "Check stock ledger, aging, costing, PDFs, notifications, and audit logs.",
];

const endToEndService = [
  "Create or confirm equipment unit.",
  "Create job order and start the job.",
  "Create daily field sheet.",
  "Add daily staff and progress.",
  "Request IOU or expense claim if needed.",
  "Create and post MRN if parts are used.",
  "Create work order labour and approve it.",
  "Create estimate if customer approval is needed.",
  "Complete the job and create service handover.",
  "Review billing, costs, and closeout readiness.",
  "Clear blockers and close the job.",
];

function Section({ id, title, children }: { id: string; title: string; children: ReactNode }) {
  return (
    <section id={id} className="scroll-mt-20">
      <Card className="space-y-4 p-4">
        <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">{title}</h2>
        <div className="space-y-4 text-[14px] leading-6 text-[var(--foreground)]/90">{children}</div>
      </Card>
    </section>
  );
}

function ManualImage({ src, alt }: { src: string; alt: string }) {
  return (
    <Image
      src={src}
      alt={alt}
      width={1600}
      height={900}
      className="w-full rounded-lg border border-[var(--card-border)] bg-[var(--surface)] shadow-[var(--shadow-card)]"
    />
  );
}

function FlowTable({ rows }: { rows: FlowStep[] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-[var(--card-border)]">
      <Table>
        <thead>
          <tr>
            <th className="w-52 border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">Step</th>
            <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">What to input</th>
            <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">Output</th>
            <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">What to check</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.title}>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top font-semibold">{row.title}</td>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.input}</td>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.output}</td>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.check}</td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  );
}

function TwoColumnTable({ rows, left = "Area", right = "Purpose / check" }: { rows: Row[]; left?: string; right?: string }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-[var(--card-border)]">
      <Table>
        <thead>
          <tr>
            <th className="w-56 border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">{left}</th>
            <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">{right}</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.left}>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top font-semibold">{row.left}</td>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.right}</td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  );
}

function NumberedList({ items }: { items: string[] }) {
  return (
    <ol className="list-decimal space-y-1 pl-5">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ol>
  );
}

export default function HelpPage() {
  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--foreground)]">ISS ERP Help</h1>
          <p className="text-sm text-[var(--muted-foreground)]">
            Simple full-system tutorial: what to enter, what output to expect, and what to check.
          </p>
        </div>
        <div className="rounded-full border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-1 text-[12px] font-semibold text-[var(--muted-foreground)]">
          Full system guide
        </div>
      </div>

      <Card className="space-y-3 p-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">Start with this rule</h2>
          <p className="mt-1 text-[14px] leading-6 text-[var(--foreground)]/90">
            Draft documents are preparation. Posting, approval, confirmation, allocation, settlement, or closeout is what changes stock, AR, AP, reports, notifications, and audit logs.
          </p>
        </div>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-5">
          {quickLinks.map((link) => (
            <a
              key={link.href}
              href={link.href}
              className="rounded-md border border-[var(--input-border)] bg-[var(--surface)] px-3 py-2 text-[13px] font-semibold text-[var(--link)] shadow-[var(--shadow-control)] transition-colors hover:bg-[var(--surface-soft)]"
            >
              {link.label}
            </a>
          ))}
        </div>
      </Card>

      <Section id="start" title="1. Login, Dashboard, And Menu Map">
        <ManualImage src="/help/system/login-page.png" alt="Login page" />
        <p>Enter your email and password. After login, the dashboard opens and the sidebar shows only the modules you can access.</p>
        <ManualImage src="/help/system/dashboard.png" alt="Dashboard" />
        <TwoColumnTable rows={moduleRows} />
      </Section>

      <Section id="master-data" title="2. Master Data">
        <ManualImage src="/help/system/master-data-currencies.png" alt="Master data currencies" />
        <p>Set up master data before entering transactions. Recommended order: currencies, taxes, payment types, warehouses, UoMs, items, suppliers, customers, and reorder settings.</p>
        <FlowTable rows={masterDataRows} />
      </Section>

      <Section id="procurement" title="3. Procurement">
        <ManualImage src="/help/system/procurement-purchase-orders.png" alt="Procurement purchase orders" />
        <p>Procurement controls purchase requests, supplier quotations, purchase orders, receipts, supplier invoices, and supplier returns.</p>
        <FlowTable rows={procurementRows} />
      </Section>

      <Section id="inventory" title="4. Inventory">
        <p>Inventory screens are used to check stock and correct stock movement. Stock changes only after posting documents such as GRNs, dispatches, MRNs, adjustments, and transfers.</p>
        <FlowTable rows={inventoryRows} />
      </Section>

      <Section id="sales" title="5. Sales">
        <p>Sales controls quote to cash: quote, order, dispatch, invoice, payment, and return.</p>
        <FlowTable rows={salesRows} />
      </Section>

      <Section id="service" title="6. Service">
        <ManualImage src="/help/job-orders/01-jobs-list.png" alt="Service job list" />
        <p>Service manages customer equipment, job orders, daily work, labour, parts, expenses, handover, billing, costs, and closeout.</p>
        <FlowTable rows={serviceRows} />
        <ManualImage src="/help/job-orders/02-job-overview.png" alt="Service job overview" />
        <ManualImage src="/help/job-orders/04-daily-sheets.png" alt="Daily sheets" />
        <ManualImage src="/help/job-orders/08-billing.png" alt="Billing and closeout" />
      </Section>

      <Section id="finance" title="7. Finance">
        <ManualImage src="/help/system/finance-ar.png" alt="Accounts receivable" />
        <ManualImage src="/help/system/finance-ap.png" alt="Accounts payable" />
        <p>Finance controls AR, AP, payments, credit notes, debit notes, petty cash, IOUs, allocations, release, and settlement.</p>
        <FlowTable rows={financeRows} />
      </Section>

      <Section id="admin" title="8. Admin, Access Permissions, And Notifications">
        <ManualImage src="/help/system/admin-users.png" alt="Admin users" />
        <p>Admin users can create users, assign roles, and set exact permissions. Permissions control both visible actions and backend authorization.</p>
        <FlowTable rows={adminRows} />
      </Section>

      <Section id="reporting" title="9. Reporting">
        <ManualImage src="/help/system/reporting-costing.png" alt="Reporting costing" />
        <p>Reports are used to check the result of posted documents. Draft documents should not affect posted stock or finance reports.</p>
        <TwoColumnTable rows={reportRows} left="Report" right="What to check" />
      </Section>

      <Section id="end-to-end" title="10. End-To-End Training Examples">
        <div className="grid gap-4 lg:grid-cols-2">
          <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
            <h3 className="font-semibold">Stock purchase to customer payment</h3>
            <NumberedList items={endToEndStock} />
          </div>
          <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface-soft)] p-3">
            <h3 className="font-semibold">Service job from intake to closeout</h3>
            <NumberedList items={endToEndService} />
          </div>
        </div>
      </Section>

      <Section id="checks" title="11. What Testers Should Check">
        <TwoColumnTable rows={checks} left="Check" right="Pass condition" />
        <p>
          Trainer script: first set master data, then create draft documents, then post or approve them, then check stock, AR/AP, reports, notifications, PDFs, and audit logs.
        </p>
      </Section>
    </div>
  );
}
