import type { ReactNode } from "react";
import Image from "next/image";
import { Card, Table } from "@/components/ui";

type LinkItem = {
  href: string;
  label: string;
};

type TableRow = {
  left: string;
  right: string;
};

const quickLinks: LinkItem[] = [
  { href: "#service-menu", label: "Service Menu" },
  { href: "#equipment-units", label: "Equipment Units" },
  { href: "#command-center", label: "Command Center" },
  { href: "#job-orders", label: "Job Orders" },
  { href: "#daily-sheets", label: "Daily Sheets" },
  { href: "#daily-labour", label: "Daily Labour" },
  { href: "#work-order-labour", label: "Work Order Labour" },
  { href: "#materials", label: "Materials" },
  { href: "#expenses", label: "Expenses" },
  { href: "#billing", label: "Billing" },
  { href: "#costs", label: "Costs" },
  { href: "#flow", label: "End-To-End Flow" },
];

const serviceAreas: TableRow[] = [
  {
    left: "Command Center",
    right: "Supervisor view of active jobs, overdue work, missing daily sheets, missing progress, finance blockers, billing queues, and closeout blockers.",
  },
  {
    left: "Dispatch Board",
    right: "Operational lane view for unassigned, assigned or active, waiting, and completed jobs.",
  },
  {
    left: "Technician Workbench",
    right: "Technician daily view for today's assignments, open daily sheets, and quick actions.",
  },
  {
    left: "Equipment Units",
    right: "Customer-owned machines or equipment that can receive service jobs.",
  },
  {
    left: "Service Contracts",
    right: "Contract coverage, billing entitlement, and service agreement information.",
  },
  {
    left: "Job Orders",
    right: "Main job record: intake, daily work, materials, expenses, billing, costs, and closeout.",
  },
  {
    left: "Technicians",
    right: "Technician master records and default labour rates.",
  },
  {
    left: "Job Sheets / Work Orders",
    right: "Billable work records and time entries used for labour costing and invoicing.",
  },
  {
    left: "MRN / Material Requisitions",
    right: "Stock issue documents used to consume spare parts or materials for jobs.",
  },
  {
    left: "Quotations / Estimates",
    right: "Customer quotation and change-order process.",
  },
  {
    left: "Service Taken / Handovers",
    right: "Customer handover, final service confirmation, and invoice conversion path.",
  },
  {
    left: "Quality Checks",
    right: "Inspection or QC records linked to service work.",
  },
];

const dailyLabourComparison = [
  {
    daily: "Shows who attended a daily field sheet.",
    workOrder: "Shows billable or costed labour time entries.",
  },
  {
    daily: "Used for daily job supervision.",
    workOrder: "Used for costing, approval, and customer billing.",
  },
  {
    daily: "Linked to a daily sheet.",
    workOrder: "Linked to a work order/job sheet and service job.",
  },
  {
    daily: 'Helps answer: "Who worked today?"',
    workOrder: 'Helps answer: "What labour cost or billing should be posted?"',
  },
  {
    daily: "Does not by itself create final billable labour.",
    workOrder: "Approved billable entries can feed invoices.",
  },
];

const commonQuestions: TableRow[] = [
  {
    left: "Should I create a daily sheet or a work order?",
    right: "Create a daily sheet for daily site/work record. Use a work order/job sheet for billable labour/time entries.",
  },
  {
    left: "Does daily staff labour create an invoice?",
    right: "No. It records attendance/work for the day. Invoice labour comes from approved billable job sheet/work-order time entries.",
  },
  {
    left: "Does planning a part reduce stock?",
    right: "No. Stock reduces only when an MRN is posted.",
  },
  {
    left: "Why is my IOU still visible after requesting it?",
    right: "That is correct. It stays visible so the requester and supervisor know it was sent and can track finance status.",
  },
  {
    left: "Why can't I close the job?",
    right: "Open Billing -> Closeout Readiness and clear the listed blockers.",
  },
  {
    left: "Why can't I edit the job header?",
    right: "The job may already be started, completed, invoiced, closed, or cancelled. Continue through operational tabs instead.",
  },
  {
    left: "Why does an expense claim show zero total?",
    right: "Open the claim detail and add expense lines.",
  },
  {
    left: "Where do I check job profit?",
    right: "Open the job Costs tab.",
  },
  {
    left: "Where do technicians work daily?",
    right: "Use Technician Workbench or the job Daily Work tab.",
  },
];

function Section({
  id,
  title,
  children,
}: {
  id: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <section id={id} className="scroll-mt-20">
      <Card className="space-y-3 p-4">
        <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">{title}</h2>
        <div className="space-y-3 text-[14px] leading-6 text-[var(--foreground)]/90">{children}</div>
      </Card>
    </section>
  );
}

function BulletList({ items }: { items: string[] }) {
  return (
    <ul className="list-disc space-y-1 pl-5">
      {items.map((item) => (
        <li key={item}>{item}</li>
      ))}
    </ul>
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

function TwoColumnTable({ rows }: { rows: TableRow[] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-[var(--card-border)]">
      <Table>
        <thead>
          <tr>
            <th className="w-56 border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">
              Area
            </th>
            <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">
              User purpose
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.left}>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top font-semibold">
                {row.left}
              </td>
              <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.right}</td>
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  );
}

export default function HelpPage() {
  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--foreground)]">Help</h1>
          <p className="text-sm text-[var(--muted-foreground)]">
            Service Job Section User Manual with screenshots and simple user guidance.
          </p>
        </div>
        <div className="rounded-full border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-1 text-[12px] font-semibold text-[var(--muted-foreground)]">
          Service module guide
        </div>
      </div>

      <Card className="space-y-3 p-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">
            Service Job Section User Manual
          </h2>
          <p className="mt-1 text-[14px] leading-6 text-[var(--foreground)]/90">
            This guide explains equipment units, command center, dispatch board, technician workbench, job
            orders, daily field sheets, job sheets/work orders, materials, expenses, estimates, service
            handover, billing, costs, files, and closeout.
          </p>
        </div>
        <div className="rounded-lg border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-[14px] font-medium text-[var(--foreground)]">
          Main rule: look at the list or status first, then open a form only when you need to add or edit
          something. Create and edit forms open in modal dialogs where possible so users do not lose the page
          they are working on.
        </div>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
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

      <Section id="service-menu" title="1. Main Service Menu Areas">
        <p>The Service module contains several screens. Each screen has a different purpose.</p>
        <TwoColumnTable rows={serviceAreas} />
      </Section>

      <Section id="equipment-units" title="2. Equipment Units">
        <p>Use Service -&gt; Equipment Units to register customer equipment before opening a job.</p>
        <p>An equipment unit normally contains:</p>
        <BulletList
          items={[
            "Serial number",
            "Linked item or machine model",
            "Customer",
            "Site/location information",
            "Warranty coverage where applicable",
          ]}
        />
        <p>
          When a job is opened, select the equipment unit first. The system uses the equipment unit to default
          or validate the customer and to check warranty or contract entitlement.
        </p>
        <p>Use equipment units when:</p>
        <BulletList
          items={[
            "A customer sends a machine for repair.",
            "A technician visits installed customer equipment.",
            "Warranty or service-contract coverage must be checked.",
          ]}
        />
      </Section>

      <Section id="command-center" title="3. Command Center">
        <p>Use Service -&gt; Command Center as the supervisor or coordinator first screen.</p>
        <p>The command center is not for entering job details. It is for seeing what needs attention.</p>
        <p>Use it to find:</p>
        <BulletList
          items={[
            "Active jobs",
            "Overdue jobs",
            "Jobs without today's daily sheet",
            "Jobs without today's progress update",
            "Pending daily sheets",
            "Pending IOUs and expense claims",
            "Billing-ready jobs",
            "Jobs blocked from closeout",
          ]}
        />
        <p>From command center cards or queue rows, open the related job or working area.</p>
      </Section>

      <Section id="dispatch-board" title="4. Dispatch Board">
        <p>Use Service -&gt; Dispatch Board to view jobs by operational lane.</p>
        <p>Typical lanes are:</p>
        <BulletList items={["Unassigned", "Assigned / Active", "Waiting", "Completed"]} />
        <p>
          Use this page when a coordinator needs to see which jobs are not assigned, which jobs are being
          worked on, and which jobs are waiting for parts, customer approval, supplier response, or another
          blocker.
        </p>
      </Section>

      <Section id="technician-workbench" title="5. Technician Workbench">
        <p>Use Service -&gt; Technician Workbench for technician daily work.</p>
        <p>It shows:</p>
        <BulletList
          items={[
            "Today's assignments",
            "Open daily sheets",
            "Active jobs",
            "Quick links for progress, material requests, IOU requests, and expenses",
          ]}
        />
        <p>Technicians should normally work from this screen or from the relevant job&apos;s Daily Work tab.</p>
      </Section>

      <Section id="job-orders" title="6. Job Orders List">
        <p>Go to Service -&gt; Job Orders.</p>
        <ManualImage src="/help/job-orders/01-jobs-list.png" alt="Job Orders list" />
        <p>The job list is the main place to open, view, or edit jobs.</p>
        <BulletList
          items={[
            "Click the job number or View to open the full job detail page.",
            "Click + New Job Order to create a new job in a modal dialog.",
            "Click Edit on an editable job to open the job header edit modal directly from the list.",
            "Jobs are normally editable while they are Draft, Open, or Reopened.",
            "Once execution starts, the job header is locked and users should continue through daily sheets, work orders, materials, expenses, handover, and billing.",
          ]}
        />
      </Section>

      <Section id="create-job-order" title="7. Create A New Job Order">
        <p>From the job list, click + New Job Order.</p>
        <p>Enter:</p>
        <BulletList
          items={[
            "Equipment unit",
            "Customer",
            "Job type: Service, Repair, PDI, Warranty, or Inspection",
            "Site/location",
            "Responsible officer",
            "Customer complaint or service requirement",
            "Job description and internal remarks if needed",
          ]}
        />
        <p>
          When the job is created, the system checks service contract and warranty entitlement. If contract or
          warranty data is added later, open the job and click Refresh Entitlement.
        </p>
      </Section>

      <Section id="job-overview" title="8. Job Overview">
        <p>Open a job to see the compact header, cockpit, and process timeline.</p>
        <ManualImage src="/help/job-orders/02-job-overview.png" alt="Job overview" />
        <p>The overview shows:</p>
        <BulletList
          items={[
            "Job number, status, type, equipment, customer, and responsible officer",
            "Main job actions such as Start, Complete, Close, Reopen, and Refresh Entitlement",
            "Job Cockpit summary cards",
            "Process Timeline from intake to closeout",
          ]}
        />
        <p>Use the process timeline to jump to the correct work area instead of scrolling through the full page.</p>
      </Section>

      <Section id="edit-job-header" title="9. Edit Job Header">
        <p>There are two ways to edit a job header:</p>
        <BulletList
          items={[
            "From Service -> Job Orders, click Edit in the row.",
            "From job detail Overview, click Edit Job.",
          ]}
        />
        <p>Both open the same edit modal.</p>
        <p>Use this only for intake/header information, such as:</p>
        <BulletList
          items={[
            "Equipment",
            "Customer",
            "Job type",
            "Expected dates",
            "Site/location",
            "Responsible officer",
            "Customer complaint",
            "Problem/intake note",
            "Internal remarks",
          ]}
        />
        <p>Do not use header editing to record daily work, parts, labour, or billing. Those belong in their own tabs.</p>
      </Section>

      <Section id="plan" title="10. Plan Job Operations">
        <p>Open the Plan tab.</p>
        <ManualImage src="/help/job-orders/03-plan.png" alt="Plan tab" />
        <p>Use this tab to plan major repair stages or sub-parts before doing the actual work.</p>
        <p>Examples:</p>
        <BulletList
          items={[
            "Diagnose hydraulic leak",
            "Remove and inspect pump",
            "Replace filter",
            "Test under load",
          ]}
        />
        <p>Important:</p>
        <BulletList
          items={[
            "Planning does not reduce stock.",
            "Planning does not create billable labour.",
            "Actual parts are issued through MRNs.",
            "Actual billable labour is entered through job sheets/work orders.",
          ]}
        />
      </Section>

      <Section id="daily-sheets" title="11. Daily Field Sheets">
        <p>Open Daily Work -&gt; Daily Sheets.</p>
        <ManualImage src="/help/job-orders/04-daily-sheets.png" alt="Daily sheets" />
        <p>A daily field sheet is the daily record of what happened on a job.</p>
        <p>Create one daily sheet for each working day.</p>
        <p>Each daily sheet can show:</p>
        <BulletList
          items={[
            "Date",
            "Work planned",
            "Work completed",
            "Work pending",
            "Site or weather condition",
            "Staff count",
            "Progress count",
            "Material/MRN count",
            "Return/damage count",
            "Expense count",
            "IOU count",
            "Approval status",
          ]}
        />
        <p>Use daily sheets for daily control and supervisor review.</p>
      </Section>

      <Section id="daily-labour" title="12. Daily Staff / Labour">
        <p>Open Daily Work -&gt; Staff / Labor.</p>
        <ManualImage src="/help/job-orders/05-daily-labor.png" alt="Daily labour" />
        <p>This area records who attended the job on a particular daily sheet.</p>
        <p>Use it for:</p>
        <BulletList
          items={[
            "Attendance",
            "Daily assignment",
            "What the person did that day",
            "Normal and overtime hours for daily tracking",
            "Supervisor review of who worked on site",
          ]}
        />
        <p>
          This is a daily operational record. It helps users understand who worked on a job on each day.
        </p>
      </Section>

      <Section id="work-order-labour" title="13. Job Sheets / Work Orders Labour">
        <p>Use Service -&gt; Job Sheets / Work Orders for billable labour, time entries, and job-sheet labour costing.</p>
        <p>This is different from daily staff/labour.</p>
        <div className="overflow-x-auto rounded-lg border border-[var(--card-border)]">
          <Table>
            <thead>
              <tr>
                <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">
                  Daily Staff / Labour
                </th>
                <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">
                  Job Sheets / Work Orders Labour
                </th>
              </tr>
            </thead>
            <tbody>
              {dailyLabourComparison.map((row) => (
                <tr key={row.daily}>
                  <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.daily}</td>
                  <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.workOrder}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
        <p>Simple example:</p>
        <BulletList
          items={[
            "Technician A attends the site today. Add Technician A in Daily Staff / Labor.",
            "Technician A performs 3 billable repair hours. Add a time entry in Job Sheets / Work Orders.",
            "The daily sheet shows attendance. The work order time entry supports costing and billing.",
          ]}
        />
        <p>Use both when both daily attendance and billable labour are required.</p>
      </Section>

      <Section id="daily-progress" title="14. Daily Progress">
        <p>Open Daily Work -&gt; Progress.</p>
        <p>Progress updates are recorded against a daily field sheet.</p>
        <p>Use progress updates to record:</p>
        <BulletList
          items={[
            "Work completed",
            "Work pending",
            "Problems found",
            "Additional parts required",
            "Additional labour required",
            "Customer instructions",
            "Site issues",
            "Technician notes",
            "Supervisor notes",
          ]}
        />
        <p>Progress updates help supervisors understand the current job situation without calling the technician.</p>
      </Section>

      <Section id="materials" title="15. Materials And MRNs">
        <p>Open the Materials tab.</p>
        <ManualImage src="/help/job-orders/06-materials.png" alt="Materials tab" />
        <p>Materials are handled through MRNs and material disposition.</p>
        <p>Tabs:</p>
        <BulletList items={["Issued MRNs", "Return Materials", "Damage Material"]} />
        <p>Use + New MRN to create a draft material requisition for the job. Then open the MRN document, add item lines, and post it.</p>
        <p>Important:</p>
        <BulletList
          items={[
            "Draft MRNs do not reduce stock.",
            "Posted MRNs reduce stock.",
            "Posted MRNs appear in the job under Issued MRNs.",
            "Unused, wrong, rejected, or damaged materials should be recorded through return/damage disposition.",
          ]}
        />
        <p>Use material disposition before job closeout so the system knows what happened to every issued item.</p>
      </Section>

      <Section id="expenses" title="16. IOUs And Expenses">
        <p>Open the Expenses tab.</p>
        <ManualImage src="/help/job-orders/07-expenses.png" alt="Expenses tab" />
        <p>There are three separate expense workflows.</p>
        <h3 className="text-base font-semibold text-[var(--foreground)]">IOU Advances</h3>
        <p>Use + Request IOU when a person needs a cash advance before expenses are finalized.</p>
        <p>Example:</p>
        <BulletList
          items={[
            "Technician needs cash for emergency job-related transport or a small purchase.",
            "The IOU is requested from the job.",
            "Finance approves, releases, and later settles the IOU.",
          ]}
        />
        <p>The requester is the signed-in system user. After creation, the IOU remains visible in the job IOU register.</p>
        <h3 className="text-base font-semibold text-[var(--foreground)]">Petty Cash Expenses</h3>
        <p>Use + Petty Cash Voucher when company petty cash was used for the job.</p>
        <p>Record:</p>
        <BulletList
          items={[
            "Daily sheet",
            "Voucher date",
            "Merchant/vendor",
            "Bill number issued by the accountant",
            "Payment handover method: cash handover, bank deposit, or other",
            "Notes",
          ]}
        />
        <h3 className="text-base font-semibold text-[var(--foreground)]">Out-Of-Pocket Claims</h3>
        <p>
          Use + Reimbursement Claim when an employee paid personally and needs reimbursement. The claim remains
          visible in the job expense register and follows finance approval and settlement.
        </p>
      </Section>

      <Section id="estimates" title="17. Service Estimates / Quotations">
        <p>Use Service -&gt; Quotations or the job Billing area to manage service estimates.</p>
        <p>Use estimates when the customer must approve a quoted repair or service amount before work continues.</p>
        <p>Estimate lines can include:</p>
        <BulletList items={["Parts", "Labour", "Billable expenses"]} />
        <p>
          Draft estimates can be edited. Once sent or approved, use change-order rules instead of silently
          overwriting approved scope.
        </p>
      </Section>

      <Section id="handover" title="18. Service Taken / Handover">
        <p>Use Service -&gt; Service Taken / service handover when the repair or service is handed back to the customer.</p>
        <p>The handover records:</p>
        <BulletList
          items={[
            "Handover date",
            "Customer acknowledgement",
            "Returned items or notes",
            "Post-service warranty if applicable",
            "Final service confirmation",
          ]}
        />
        <p>The handover is also part of the final invoice path where applicable.</p>
      </Section>

      <Section id="billing" title="19. Billing And Closeout">
        <p>Open the Billing tab.</p>
        <ManualImage src="/help/job-orders/08-billing.png" alt="Billing tab" />
        <p>Billing includes:</p>
        <BulletList
          items={[
            "Closeout readiness",
            "Warranty/billing entitlement",
            "Quotations and final invoices",
          ]}
        />
        <p>Closeout readiness tells users what is blocking job closure.</p>
        <p>Common blockers:</p>
        <BulletList
          items={[
            "Draft or submitted daily sheets",
            "Pending IOUs",
            "Pending expense claims",
            "Draft MRNs",
            "Open labour entries",
            "Unresolved material disposition",
            "Missing final invoice decision",
          ]}
        />
        <p>Clear the blockers before closing the job.</p>
      </Section>

      <Section id="costs" title="20. Costs">
        <p>Open the Costs tab.</p>
        <ManualImage src="/help/job-orders/09-costs.png" alt="Costs tab" />
        <p>The cost view shows:</p>
        <BulletList
          items={[
            "Actual cost",
            "Quoted revenue",
            "Posted invoice revenue",
            "Uninvoiced billable labour",
            "Material cost",
            "Direct purchase cost",
            "Approved labour cost",
            "Approved claim cost",
          ]}
        />
        <p>Use this tab before billing or closing to understand job profitability.</p>
      </Section>

      <Section id="files-notes" title="21. Files And Notes">
        <p>Open Files &amp; Notes.</p>
        <ManualImage src="/help/job-orders/10-files-notes.png" alt="Files and notes" />
        <p>Use this area for:</p>
        <BulletList
          items={[
            "Customer communication",
            "Internal comments",
            "Attachments",
            "Approval notes",
            "Supporting documents",
          ]}
        />
      </Section>

      <Section id="flow" title="22. Recommended End-To-End Job Flow">
        <NumberedList
          items={[
            "Create or confirm the equipment unit.",
            "Open the job order.",
            "Review entitlement or refresh entitlement if needed.",
            "Start the job.",
            "Plan operations if the work has multiple stages.",
            "Create a daily field sheet for each working day.",
            "Record daily staff and progress against the daily sheet.",
            "Issue materials through MRNs and post them.",
            "Record unused/damaged/rejected material disposition.",
            "Record IOUs and expenses where needed.",
            "Record billable labour through job sheets/work orders.",
            "Prepare estimate or change order if customer approval is needed.",
            "Complete the job when work is finished.",
            "Prepare service taken/handover.",
            "Review billing, invoices, costs, and closeout readiness.",
            "Clear all blockers.",
            "Close the job.",
          ]}
        />
      </Section>

      <Section id="questions" title="23. Common User Questions">
        <TwoColumnTable rows={commonQuestions} />
      </Section>
    </div>
  );
}
