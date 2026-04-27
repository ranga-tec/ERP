# ISS Video Tutorial Pack

This folder turns the current ISS product and training docs into a practical video-production pack for:

- short marketing videos
- longer user-guidance tutorials
- repeatable screen recordings for demos, onboarding, and release showcases

Use this pack with the current ISS navigation in `frontend/src/components/Sidebar.tsx` and the operating guidance already documented in:

- `docs/user-manual.md`
- `docs/iss-tester-trainer-handbook.md`
- `docs/manual-uat-guide.md`

## Output Strategy

Create two videos for each top-level ISS section:

1. `Marketing cut`
   - length: `45-90 sec`
   - purpose: show business value and speed
   - style: fast, outcome-focused, minimum text, no deep data entry

2. `Guided tutorial`
   - length: `3-8 min`
   - purpose: teach a new operator how to use the section
   - style: slower, explicit, shows clicks, forms, status changes, and expected result

## Demo Baseline

Use the same demo setup across all videos so the story stays coherent.

- Role: `Admin` for the master recording pass
- Locale: `en-LK`
- Time zone: `Asia/Colombo`
- Base currency preference: `LKR`
- Seeded finance references: currencies, currency rates, payment types, taxes, tax conversions, reference forms

Recommended recurring demo values from the trainer handbook:

| Type | Value |
| --- | --- |
| Warehouse | `MAIN` |
| Supplier | `SUP1` |
| Customer | `CUS1` |
| Item | `SKU1 - Hydraulic Filter` |
| Receipt quantity | `10` |
| Receipt unit cost | `5` |
| Sales quantity | `4` |
| Sales unit price | `7` |

Add a few stable Sri Lanka-oriented reference values before recording:

- currency: `LKR`
- payment type: `MOBILE_PAYMENT`
- tax code examples: `VAT0`, `EXEMPT`, `VAT15`, `VAT15_INC`

## Episode Library

| Section | Marketing angle | Guidance focus | Core routes |
| --- | --- | --- | --- |
| Login + App Orientation | clean ERP entry, role-based access, fast navigation | sign in, sidebar, search, page layout, save/refresh patterns | `/login`, `/`, `/settings` |
| Overview | decision-ready dashboard | understand KPIs, panels, shortcuts | `/` |
| Master Data | one source of truth | setup order and ongoing maintenance | `/master-data/*` |
| Procurement | purchase-to-receipt control | PR/RFQ/PO/GRN/direct purchase/invoice/return flow | `/procurement/*` |
| Sales | quote-to-cash flow | quote/order/dispatch/invoice/return flow | `/sales/*` |
| Service | workshop and field-service execution | equipment, contract, job, estimate, claim, work order, handover | `/service/*` |
| Inventory | real-time stock visibility | on-hand, reorder, adjustment, transfer | `/inventory/*` |
| Finance | receivables, payables, settlements | accounts, AR, AP, payments, petty cash, credit/debit notes | `/finance/*` |
| Audit | traceability and accountability | audit log review and change tracking | `/audit-logs` |
| Reporting | operational intelligence | stock, aging, tax, service, sales, purchase, supplier, costing reports | `/reporting*` |
| Admin | control plane and rollout support | Excel import, notifications, users, settings | `/admin/*`, `/settings` |

## Recommended Recording Order

Record in this order so the data story compounds naturally:

1. Login + App Orientation
2. Master Data
3. Procurement
4. Inventory
5. Sales
6. Finance
7. Reporting
8. Service
9. Audit
10. Admin
11. Overview

Reason:

- Master Data creates the demo records
- Procurement creates stock and AP
- Inventory shows the stock effect
- Sales creates stock-out and AR
- Finance and Reporting then show the accounting and analytics effect
- Service can be recorded with the same item/customer context

## Recording Standards

Use the runbook in [recording-runbook.md](./recording-runbook.md) before recording any section.

Core standards:

- record at `1920x1080`
- use a clean browser profile
- keep browser zoom at `100%`
- hide bookmarks, chat popups, and desktop notifications
- use one consistent cursor speed and one consistent theme
- record actions silently first, then add voiceover in post

## Script Pack

Detailed section scripts are in [section-scripts.md](./section-scripts.md).
Voiceover-ready narration is in [voiceover-scripts.md](./voiceover-scripts.md).
Publishing metadata is in [publishing-copy.md](./publishing-copy.md).
Automation usage is in [automation-guide.md](./automation-guide.md).

Each section includes:

- marketing cut objective
- guided tutorial objective
- routes to open
- exact screen flow to record
- talking points for narration
- expected outcome to show before ending the clip

## Production Notes

- Keep every marketing clip centered on a business result, not a menu tour.
- Keep every guided tutorial centered on one operator job, not every button on the screen.
- When a page supports draft editing, show the real draft-save-post lifecycle instead of only showing the final posted screen.
- Where possible, end on a visible result:
  - saved row
  - posted document
  - changed stock balance
  - AP/AR entry
  - report value

## Next Use

If you want the next step automated, use this pack to produce:

- slide copy / YouTube descriptions
- a shot-by-shot checklist for an editor
- Playwright-driven demo flows for repeatable browser walkthroughs
