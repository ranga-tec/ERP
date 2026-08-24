import type { ReactNode } from "react";
import Link from "next/link";
import { Card, Table } from "@/components/ui";
import { company } from "@/lib/company";

type Scenario = {
  id: string;
  number: string;
  title: string;
  who: string;
  lede: string;
  steps: ReactNode[];
  stop: ReactNode;
  warn?: ReactNode;
};

const quickLinks = [
  { href: "#replenishment", label: "Replenish fund" },
  { href: "#advances", label: "Issue advances" },
  { href: "#settlement", label: "Settle advances" },
  { href: "#expenses", label: "Record expenses" },
  { href: "#return", label: "Return cash" },
  { href: "#refusals", label: "Fix refusals" },
];

function Nav({ children }: { children: ReactNode }) {
  return <span className="whitespace-nowrap rounded bg-[var(--surface-soft)] px-1.5 py-0.5 font-mono text-[12px]">{children}</span>;
}

function Btn({ children }: { children: ReactNode }) {
  return <span className="whitespace-nowrap rounded border border-[var(--input-border)] bg-[var(--surface)] px-1.5 py-0.5 text-[13px] font-semibold">{children}</span>;
}

function Field({ children }: { children: ReactNode }) {
  return <span className="font-mono text-[12px] text-[var(--muted-foreground)]">{children}</span>;
}

function Typed({ children }: { children: ReactNode }) {
  return <span className="rounded bg-amber-100 px-1.5 py-0.5 font-mono text-[12px] font-semibold text-amber-900 dark:bg-amber-950/50 dark:text-amber-300">{children}</span>;
}

const scenarios: Scenario[] = [
  {
    id: "replenishment",
    number: "1",
    title: "Request a fund replenishment",
    who: "Workshop custodian",
    lede: "Replenishment restores the physical float. It does not create job or expense-category cash balances.",
    steps: [
      <>Reconcile the site: count <Field>Cash on hand</Field>, total <Field>Outstanding advances</Field>, and total accepted <Field>Reconciled expenses</Field>.</>,
      <>Go to <Nav>Finance › Petty Cash Replenishment</Nav> and click <Btn>+ New Replenishment</Btn>.</>,
      <>Select the fund, enter the three reconciliation figures and the <Field>Requested amount</Field>, then save.</>,
      <>Attach the cash count and voucher summary, click <Btn>Submit to Head Office</Btn>, and type <Typed>SUBMIT</Typed>.</>,
    ],
    stop: <>Status is <strong>Submitted</strong>. The request is locked and head office has the reconciliation snapshot.</>,
  },
  {
    id: "approve-replenishment",
    number: "2",
    title: "Approve and transfer replenishment money",
    who: "Head office",
    lede: "Approval authorizes the transfer; recording the receipt is the separate event that raises the fund balance.",
    steps: [
      <>Open the submitted replenishment and compare its accountability with the fund&apos;s <Field>Authorized float</Field>.</>,
      <>Enter one approved amount, not separate amounts by job or category, then click <Btn>Approve</Btn>.</>,
      <>After the bank/cash transfer occurs, enter its reference and amount under <Field>Record Fund Receipt</Field>.</>,
    ],
    stop: <>Status is <strong>Funded</strong>, or <strong>Partially Funded</strong> if another receipt is due. The ledger contains one fund-level replenishment movement per receipt.</>,
  },
  {
    id: "advances",
    number: "3",
    title: "Approve and release employee advances",
    who: "Requester, reviewer, approver, head office, custodian",
    lede: "IOU approval and fund replenishment are independent. An approved batch can be released whenever the selected fund has enough cash.",
    steps: [
      <><em>Requester:</em> create an IOU with the employee, job when applicable, purpose, amount, and expected settlement date.</>,
      <><em>Reviewer:</em> group submitted IOUs into a PCAB batch, select the fund and assigned approver, then send it for approval.</>,
      <><em>Assigned approver:</em> reduce any amount if necessary. Head office then approves the batch.</>,
      <><em>Custodian:</em> open each approved IOU, confirm the collector and signed slip number, then click <Btn>Release Cash</Btn>.</>,
    ],
    stop: <>Status is <strong>Cash Released</strong>. The employee owes the company until bills and returned cash fully account for the release.</>,
    warn: <>Release is blocked when the fund lacks cash, the amount exceeds its advance limit, the job authorization would be exceeded, or the collector has an overdue advance.</>,
  },
  {
    id: "settlement",
    number: "4",
    title: "Account for bills and returned change",
    who: "Custodian, then higher-level approver",
    lede: "Normal settlement requires released cash to equal accepted expenses plus returned cash.",
    steps: [
      <>Open the IOU and add every bill with its receipt number, amount, posting expense account, and billable flag.</>,
      <>Attach each bill image under that bill&apos;s <Field>Receipt File &amp; Line Evidence</Field>, then record each cash-return instalment.</>,
      <>Confirm <Field>Unaccounted</Field> is zero, then click <Btn>Settle / Account</Btn> and type <Typed>SETTLE</Typed>.</>,
      <><em>Head office:</em> review the expense accounts, job/cost impact, receipts, and returns, then click <Btn>Approve Settlement</Btn>.</>,
    ],
    stop: <>Status is <strong>Settlement Approved</strong>. Bills create the expense/job cost; the advance itself never does.</>,
    warn: <>If money remains unexplained, settlement is blocked. A higher-level approver may authorize the exact shortage after IOU evidence is attached; the system creates a linked voucher against the fund&apos;s configured shortage G/L account and cost centre.</>,
  },
  {
    id: "expenses",
    number: "5",
    title: "Record something already bought",
    who: "Employee or custodian, then finance",
    lede: "Use an expense voucher when spending already happened. Use an IOU only when cash is needed before the purchase.",
    steps: [
      <>Go to <Nav>Service › Expense Vouchers</Nav> and create a voucher.</>,
      <>Choose <Field>Petty Cash Fund</Field> or <Field>Out of Pocket</Field>, then select the job when the cost belongs to one.</>,
      <>For non-job petty-cash overhead, enter a <Field>Cost centre code</Field>.</>,
      <>Add every line with a posting <Field>Expense category/account</Field>, receipt reference, and line receipt file. If the receipt is missing, enter the reason and obtain separate approval before submission.</>,
    ],
    stop: <>The settled voucher records the actual expense, job/project, cost centre, expense account, and billable status without changing any cash category sub-ledger.</>,
  },
  {
    id: "controls",
    number: "6",
    title: "Configure the controls",
    who: "Finance or service management",
    lede: "Limits control spending directly; they do not earmark physical cash into categories.",
    steps: [
      <>Open <Nav>Finance › Petty Cash Funds</Nav> and set location, authorized float, direct transaction limit, advance limit, receipt policy, and overdue-advance rule.</>,
      <>Open a service job&apos;s Overview tab and set its <Field>Petty-cash authorization</Field> when management wants a job-specific ceiling.</>,
      <>On the fund, configure the settlement-shortage expense account, cost centre, cash-count frequency, and next count due date.</>,
      <>Record physical cash and supported vouchers on the fund page; a different authorized user approves or rejects the calculated variance.</>,
      <>Use 0 only when a particular ceiling is intentionally not configured.</>,
    ],
    stop: <>Head-office IOU approval now checks the job&apos;s actual petty-cash expense plus open advance commitments against that authorization.</>,
  },
  {
    id: "return",
    number: "7",
    title: "Return reconciled cash to head office",
    who: "Workshop custodian, then head office",
    lede: "PCRTN is now a physical fund return. It does not reverse job costs or expense categories.",
    steps: [
      <>Complete the cash count and make sure the amount is physically available in the fund.</>,
      <>Go to <Nav>Finance › Return Money to Head Office</Nav>, prepare one return with the fund and amount, and attach evidence.</>,
      <>Submit it. Head office enters the receipt/deposit reference after counting the cash.</>,
    ],
    stop: <>Status is <strong>Received</strong> and one fund-level return reduces the petty-cash ledger.</>,
  },
  {
    id: "history",
    number: "8",
    title: "Read pre-V2 category records",
    who: "Finance and auditors",
    lede: "Historical PCR lines, PCRTN lines, PCRAL reallocations, and category-linked ledger entries are retained for audit.",
    steps: [
      <>Open an older request or return marked <Field>Legacy category record</Field> to view its original lines and totals.</>,
      <>Use the ledger and audit trail to follow its historical category movements.</>,
    ],
    stop: <>Legacy data stays readable. New category funding, category returns, category reallocations, and direct IOU issue are disabled.</>,
  },
];

const refusals = [
  { message: "Settlement is blocked", meaning: "Released cash is not fully covered by accepted bills or returns.", fix: "Add support, return cash, or obtain a documented higher-level exception." },
  { message: "above the fund advance limit", meaning: "The approved IOU is larger than this fund permits.", fix: "Reduce it or change the fund control through authorized management." },
  { message: "overdue unsettled advance", meaning: "The collector already has past-due cash outstanding.", fix: "Settle the old advance before releasing another." },
  { message: "petty-cash authorization is", meaning: "Job actual plus committed petty cash would exceed its limit.", fix: "Reduce/reject the spend or authorize a justified higher job limit." },
  { message: "accountability exceed the authorized float", meaning: "The replenishment would put cash plus employee advances above the approved imprest.", fix: "Correct the reconciliation or reduce the receipt." },
  { message: "Select an expense category/account", meaning: "The expense has no posting classification.", fix: "Choose an active posting expense account on every line." },
  { message: "cost centre is required", meaning: "Non-job petty-cash overhead has no cost owner.", fix: "Enter the workshop/department cost centre." },
  { message: "Category reallocations are retired", meaning: "PCRAL is not part of V2 petty cash.", fix: "Control the job through its spending authorization and classify the actual expense." },
];

function ScenarioCard({ scenario }: { scenario: Scenario }) {
  return (
    <section id={scenario.id} className="scroll-mt-20">
      <Card className="space-y-3 p-4">
        <div className="flex flex-wrap items-baseline gap-3">
          <span className="font-mono text-xs font-semibold text-[var(--link)]">{scenario.number}</span>
          <h2 className="text-lg font-semibold">{scenario.title}</h2>
          <span className="ml-auto font-mono text-[11px] uppercase text-[var(--muted-foreground)]">{scenario.who}</span>
        </div>
        <p className="text-sm leading-6 text-[var(--muted-foreground)]">{scenario.lede}</p>
        <ol className="list-decimal space-y-2 pl-5 text-sm leading-6">{scenario.steps.map((step, index) => <li key={index}>{step}</li>)}</ol>
        <div className="rounded-md border border-l-[3px] border-[var(--card-border)] border-l-[var(--link)] bg-[var(--surface-soft)] px-3 py-2 text-sm leading-6"><strong className="mr-2 text-xs uppercase text-[var(--link)]">Stop</strong>{scenario.stop}</div>
        {scenario.warn ? <div className="rounded-md border border-l-[3px] border-amber-200 border-l-amber-500 bg-amber-50 px-3 py-2 text-sm leading-6 text-amber-900 dark:border-amber-900/40 dark:bg-amber-950/40 dark:text-amber-100">{scenario.warn}</div> : null}
      </Card>
    </section>
  );
}

export default function PettyCashHelpPage() {
  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end justify-between gap-2">
        <div><h1 className="text-2xl font-semibold">{company.shortName} Petty Cash V2 Help</h1><p className="text-sm text-[var(--muted-foreground)]">Fund custody, employee advances, and actual expense classification are separate controls.</p></div>
        <Link href="/help" className="rounded-full border border-[var(--card-border)] px-3 py-1 text-xs font-semibold text-[var(--link)]">← Full system help</Link>
      </div>
      <Card className="space-y-3 p-4">
        <h2 className="text-lg font-semibold">Start with this rule</h2>
        <p className="text-sm leading-6">The petty-cash fund says <strong>where cash is</strong>. The IOU says <strong>who owes it</strong>. The expense voucher says <strong>why it was spent</strong> and carries the job, cost centre, expense account, and billable flag.</p>
        <div className="grid gap-2 sm:grid-cols-2 lg:grid-cols-6">{quickLinks.map((link) => <a key={link.href} href={link.href} className="rounded-md border border-[var(--input-border)] px-3 py-2 text-sm font-semibold text-[var(--link)]">{link.label}</a>)}</div>
      </Card>
      {scenarios.map((scenario) => <ScenarioCard key={scenario.id} scenario={scenario} />)}
      <section id="refusals" className="scroll-mt-20"><Card className="space-y-3 p-4"><h2 className="text-lg font-semibold">If an action is refused</h2><div className="overflow-x-auto"><Table><thead><tr><th className="px-3 py-2 text-left">Message</th><th className="px-3 py-2 text-left">Meaning</th><th className="px-3 py-2 text-left">Action</th></tr></thead><tbody>{refusals.map((row) => <tr key={row.message}><td className="border-t px-3 py-2 font-mono text-xs">{row.message}</td><td className="border-t px-3 py-2">{row.meaning}</td><td className="border-t px-3 py-2">{row.fix}</td></tr>)}</tbody></Table></div></Card></section>
    </div>
  );
}
