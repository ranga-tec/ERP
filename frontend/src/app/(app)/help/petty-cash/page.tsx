import type { ReactNode } from "react";
import Link from "next/link";
import { Card, Table } from "@/components/ui";
import { company } from "@/lib/company";

type Scenario = {
  id: string;
  number: string;
  title: string;
  who: string;
  lede?: string;
  steps: ReactNode[];
  stop: ReactNode;
  warn?: ReactNode;
};

type Refusal = { message: string; meaning: string; fix: string };

const quickLinks = [
  { href: "#getting-money-in", label: "Getting money in" },
  { href: "#handing-cash-out", label: "Handing cash out" },
  { href: "#bills-and-settlement", label: "Bills and settlement" },
  { href: "#other-payments", label: "Other payments" },
  { href: "#refusals", label: "If it is refused" },
];

/** A screen path in the sidebar, e.g. Finance > Petty Cash Requests. */
function Nav({ children }: { children: ReactNode }) {
  return (
    <span className="whitespace-nowrap rounded bg-[var(--surface-soft)] px-1.5 py-0.5 font-mono text-[12px] text-[var(--foreground)]">
      {children}
    </span>
  );
}

/** A button exactly as it is labelled on screen. */
function Btn({ children }: { children: ReactNode }) {
  return (
    <span className="whitespace-nowrap rounded border border-[var(--input-border)] bg-[var(--surface)] px-1.5 py-0.5 text-[13px] font-semibold text-[var(--foreground)]">
      {children}
    </span>
  );
}

/** A field label on a form. */
function Field({ children }: { children: ReactNode }) {
  return <span className="font-mono text-[12px] text-[var(--muted-foreground)]">{children}</span>;
}

/** The word a confirmation box makes you type before it will let the action run. */
function Typed({ children }: { children: ReactNode }) {
  return (
    <span className="whitespace-nowrap rounded bg-amber-100 px-1.5 py-0.5 font-mono text-[12px] font-semibold tracking-wide text-amber-900 dark:bg-amber-950/50 dark:text-amber-300">
      {children}
    </span>
  );
}

const scenarios: Scenario[] = [
  {
    id: "getting-money-in",
    number: "1",
    title: "You need money from head office for the week",
    who: "Assistant accountant",
    lede: "One request covering several categories. Do not raise a separate request per category.",
    steps: [
      <>Go to <Nav>Finance › Petty Cash Requests</Nav> and click <Btn>+ New Request</Btn>.</>,
      <>Pick the fund the money should land in, set <Field>Needed by</Field> if it matters, then click <Btn>Create Request</Btn>. You land on the request page.</>,
      <>Click <Btn>+ Add Line</Btn> once per category: <Field>Job Wise</Field> (then pick the job order), <Field>Emergency Operation</Field>, <Field>Transportation</Field>, or <Field>Custom</Field> (then name it). Each line needs a reason and an amount.</>,
      <>Check the totals, click <Btn>Submit to Head Office</Btn>, and type <Typed>SUBMIT</Typed> to confirm.</>,
    ],
    stop: <>Status reads <strong>Submitted</strong> and head office has been notified. The lines can no longer be edited. Wait for their approval notification.</>,
  },
  {
    id: "approve-request",
    number: "2",
    title: "A request is waiting for you to approve",
    who: "Head office",
    lede: "You decide each category separately, and you may give less than was asked for.",
    steps: [
      <>Open the notification, or go to <Nav>Finance › Petty Cash Requests</Nav> and click the request number.</>,
      <>Click <Btn>Approve</Btn>. A panel lists every line with the requested amount already filled in.</>,
      <>Edit any line down. Enter <strong>0</strong> to give nothing for that category. You cannot enter more than was requested.</>,
      <>Check the &ldquo;Approving X of Y requested&rdquo; total, then confirm by typing <Typed>APPROVE</Typed>.</>,
    ],
    stop: <>Status reads <strong>Approved</strong>. <strong>No money has moved yet</strong> — approving and paying are two separate acts. Continue to scenario 3 to release it.</>,
  },
  {
    id: "release-funds",
    number: "3",
    title: "Paying an approved request — one transfer, several categories",
    who: "Head office",
    lede: "Release each line on its own, even when the bank transfer was a single payment.",
    steps: [
      <>On the request page, find the <Field>Category lines</Field> table. Each line has a <Btn>Release</Btn> button in the last column.</>,
      <>Click <Btn>Release</Btn> on the first line. The amount defaults to what is still outstanding; reduce it if you are paying in parts.</>,
      <>Put the bank transfer reference in <Field>Transfer / slip ref</Field>, then click <Btn>Confirm</Btn>.</>,
      <>Repeat for each remaining line, <strong>using the same transfer reference</strong> — it is filled in for you. That is how one payment covering four categories is recorded as four separate receipts.</>,
    ],
    stop: <>Status reads <strong>Funded</strong> once every line is fully paid, or <strong>Partially Funded</strong> if some is still to come. The float has gone up and each category shows a balance in the <Field>In sub-account</Field> column.</>,
  },
  {
    id: "handing-cash-out",
    number: "4",
    title: "Someone is at your desk asking for cash",
    who: "Assistant accountant",
    lede: "You write a slip from the printed book and record it. This is the everyday case.",
    steps: [
      <>Write the paper IOU slip as usual and get it signed.</>,
      <>Go to <Nav>Finance › Petty Cash Advances (IOU)</Nav> and click <Btn>+ Record IOU Slip</Btn>.</>,
      <>Type the number printed on the slip into <Field>I.O.U. No.</Field> — for example <strong>4001</strong>. That becomes the advance&apos;s number in the system; nothing else is generated.</>,
      <>Pick the person under <Field>Issued to</Field>, then enter the amount and the reason.</>,
      <>Click <Btn>Record IOU</Btn>.</>,
    ],
    stop: <>The advance appears as <strong>4001</strong>, status <strong>Cash Released</strong>, held by that person. The float has gone down. Nothing further until they come back to settle.</>,
    warn: <><strong>If it says the slip has already been entered</strong>, that number is already in the system. Find it in the list. Do not invent a number to get past the message — the slip book is the source.</>,
  },
  {
    id: "written-iou",
    number: "5",
    title: "A technician asks for an advance in the system, before any cash moves",
    who: "Technician, then assistant accountant",
    lede: "The written route. Use it when the request comes ahead of time rather than at the counter.",
    steps: [
      <><em>Technician:</em> <Nav>Finance › Petty Cash Advances (IOU)</Nav> → <Btn>+ New IOU</Btn>. Pick the job number, amount and purpose. It submits itself for approval.</>,
      <><em>Assistant accountant:</em> find the row in the list and click <Btn>Approve</Btn>.</>,
      <>Click <Btn>Release Cash</Btn>. Choose the fund, and put the printed slip number in <Field>Signed bill no.</Field> as you hand the money over.</>,
      <>Confirm by typing <Typed>RELEASE</Typed>.</>,
    ],
    stop: <>Status reads <strong>Cash Released</strong>. This one keeps its system number (<strong>IOU000012</strong>) because it existed before the slip did, with the slip number recorded alongside. Which number a row shows tells you which route it came in by.</>,
  },
  {
    id: "bills-and-settlement",
    number: "6",
    title: "Someone comes back with bills and change",
    who: "Assistant accountant, then head office",
    lede: "Two parts: get the bills in as vouchers, then record what was spent.",
    steps: [
      <>First raise the voucher for what they actually bought — scenario 7 — and link it to this advance. Do this <em>before</em> settling so the figures line up.</>,
      <>Go to <Nav>Finance › Petty Cash Advances (IOU)</Nav> and find the advance.</>,
      <>Click into <Field>Amount spent</Field>. It is pre-filled with the total of the vouchers you linked; change it only if the cash says otherwise.</>,
      <>If you type more than the vouchers cover, an amber warning shows how much will have no bill behind it. Read it, and fix the vouchers if you can.</>,
      <>Click <Btn>Settle / Account</Btn> and confirm with <Typed>SETTLE</Typed>. The unspent balance returns to the float automatically.</>,
      <><em>Head office:</em> check the figures and click <Btn>Approve Settlement</Btn>.</>,
    ],
    stop: <>Status reads <strong>Settlement Approved</strong> and the advance is closed for good. The <Field>Unaccounted</Field> column should read <strong>reconciled</strong>.</>,
    warn: <><strong>If Unaccounted shows an amber figure</strong>, that is cash declared as spent with no bill behind it. It has left the company and will never reach any job&apos;s cost. The system allows it deliberately — but it should be a decision, not an accident.</>,
  },
  {
    id: "vouchers",
    number: "7",
    title: "Entering the bills someone brought back",
    who: "Assistant accountant",
    lede: "This is what turns cash handed out into a real cost against a job.",
    steps: [
      <>Go to <Nav>Service › Expense Vouchers</Nav> and click <Btn>+ New Voucher</Btn>.</>,
      <>Pick the <Field>Job Order</Field>, or leave it on <Field>Not job related (overhead)</Field> for transport and general spend.</>,
      <>Set <Field>Funding source</Field> to <Field>Petty Cash Fund</Field>.</>,
      <>Under <Field>Funded by IOU advance</Field>, pick the advance the money came from. <strong>This is the step people skip</strong>, and skipping it is what leaves an advance looking unaccounted.</>,
      <>Under <Field>Charge to funded category</Field>, pick the category head office funded, so the sub-account draws down.</>,
      <>Click <Btn>Create Expense Voucher</Btn>, then <Btn>+ Add Line</Btn> for each bill — description, quantity, unit cost, and tick <Field>Billable to customer</Field> if the customer is being charged.</>,
      <>Attach the bill images in the <Field>Voucher Comments &amp; Attachments</Field> panel at the bottom.</>,
      <>Click <Btn>Submit Claim</Btn>, then <Btn>Approve</Btn> once finance has checked it.</>,
    ],
    stop: <>Once <strong>Approved</strong>, the voucher is in the job&apos;s cost. If this money came from an advance you already handed over, stop here — do <em>not</em> click Repay from Fund, or the cash leaves the box a second time.</>,
  },
  {
    id: "other-payments",
    number: "8",
    title: "You pay a taxi or a courier straight from the box",
    who: "Assistant accountant",
    lede: "Nobody owes anything here, so this is not an advance. The bill is the whole record.",
    steps: [
      <><Nav>Service › Expense Vouchers</Nav> → <Btn>+ New Voucher</Btn>.</>,
      <>Leave <Field>Job Order</Field> on <Field>Not job related (overhead)</Field> unless it genuinely belongs to one job.</>,
      <>Set <Field>Funding source</Field> to <Field>Petty Cash Fund</Field>. Put the receipt number in <Field>Receipt ref</Field> and the vendor in <Field>Merchant</Field>. Leave <Field>Funded by IOU advance</Field> empty.</>,
      <>Create it, add the line, attach the bill.</>,
      <><Btn>Submit Claim</Btn> → <Btn>Approve</Btn> → <Btn>Repay from Fund</Btn>, choosing the fund.</>,
    ],
    stop: <>Status <strong>Settled</strong>. The cash has come out of the box against a bill, and because there is no job it stays out of job costing — correct for overhead.</>,
  },
  {
    id: "out-of-pocket",
    number: "9",
    title: "Someone spent their own money and wants it back",
    who: "Anyone, then finance",
    steps: [
      <><Nav>Service › Expense Vouchers</Nav> → <Btn>+ New Voucher</Btn>.</>,
      <>Set <Field>Funding source</Field> to <Field>Out of Pocket</Field>. Pick the job if there is one.</>,
      <>Add the lines, attach the bills, click <Btn>Submit Claim</Btn>.</>,
      <>Finance: <Btn>Approve</Btn>, then <Btn>Repay Claimant</Btn> and choose how they were paid.</>,
    ],
    stop: <>Status <strong>Settled</strong>. An out-of-pocket voucher <em>cannot</em> be charged to a funded category — that is how overspend beyond an advance is recorded, and counting it against the category would make an overspent advance look reconciled.</>,
  },
  {
    id: "top-up",
    number: "10",
    title: "Topping the box up, outside any request",
    who: "Finance",
    steps: [
      <>Go to <Nav>Finance › Petty Cash Funds</Nav> and open the fund.</>,
      <>Use the top-up action for a straight replenishment, or the adjustment action to correct a counting error.</>,
    ],
    stop: <>The balance updates immediately. Money added this way carries <strong>no category</strong>, so it belongs to no sub-account — use a request when the money is for specific categories.</>,
  },
];

const refusals: Refusal[] = [
  { message: "IOU slip 4001 has already been entered", meaning: "That slip number is already in the system.", fix: "Find it in the list. Do not renumber the paper." },
  { message: "Only draft petty cash requests can be edited", meaning: "It has already been submitted.", fix: "Ask head office to reject it back, or raise another request." },
  { message: "Approved amount cannot exceed the … requested", meaning: "You typed more than was asked for.", fix: "Reduce it, or have them request the higher figure." },
  { message: "Funding … exceeds the … still outstanding", meaning: "That line is already fully or partly paid.", fix: "Check the Funded column and release only the remainder." },
  { message: "No money has been released for '…' yet", meaning: "You are charging to a category head office has not funded.", fix: "Release the money first, or pick another category." },
  { message: "'…' was funded for a different job order", meaning: "A Job Wise category only accepts that job's spend.", fix: "Pick the category funded for this job." },
  { message: "Only petty cash vouchers can be charged to a funded category", meaning: "The voucher is Out of Pocket.", fix: "Leave the category empty — this is overspend, and that is correct." },
  { message: "Petty cash fund does not have enough balance", meaning: "The box is short.", fix: "Top it up, or get a request funded first." },
];

function ScenarioCard({ scenario }: { scenario: Scenario }) {
  return (
    <section id={scenario.id} className="scroll-mt-20">
      <Card className="space-y-3 p-4">
        <div className="flex flex-wrap items-baseline gap-x-3 gap-y-1">
          <span className="font-mono text-[12px] font-semibold text-[var(--link)]">{scenario.number}</span>
          <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">{scenario.title}</h2>
          <span className="ml-auto font-mono text-[11px] uppercase tracking-wide text-[var(--muted-foreground)]">
            {scenario.who}
          </span>
        </div>

        {scenario.lede ? (
          <p className="text-[14px] leading-6 text-[var(--muted-foreground)]">{scenario.lede}</p>
        ) : null}

        <ol className="list-decimal space-y-2 pl-5 text-[14px] leading-6 text-[var(--foreground)]/90">
          {scenario.steps.map((step, index) => (
            <li key={index}>{step}</li>
          ))}
        </ol>

        <div className="rounded-md border border-[var(--card-border)] border-l-[3px] border-l-[var(--link)] bg-[var(--surface-soft)] px-3 py-2 text-[14px] leading-6">
          <span className="mr-2 font-mono text-[11px] uppercase tracking-wide text-[var(--link)]">Stop</span>
          {scenario.stop}
        </div>

        {scenario.warn ? (
          <div className="rounded-md border border-red-200 border-l-[3px] border-l-red-500 bg-red-50 px-3 py-2 text-[14px] leading-6 text-red-900 dark:border-red-900/40 dark:bg-red-950/40 dark:text-red-100">
            {scenario.warn}
          </div>
        ) : null}
      </Card>
    </section>
  );
}

export default function PettyCashHelpPage() {
  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight text-[var(--foreground)]">
            {company.shortName} Petty Cash Help
          </h1>
          <p className="text-sm text-[var(--muted-foreground)]">
            Every situation you will actually meet, with the screen, the button, and the point at which you stop.
          </p>
        </div>
        <Link
          href="/help"
          className="rounded-full border border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-1 text-[12px] font-semibold text-[var(--link)]"
        >
          ← Full system help
        </Link>
      </div>

      <Card className="space-y-3 p-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">Start with this rule</h2>
          <p className="mt-1 text-[14px] leading-6 text-[var(--foreground)]/90">
            An advance is not an expense. Handing someone 1,000 does not cost the company 1,000 — it moves 1,000 from
            the box into their pocket, and they owe it until a bill says what it bought. That is why job costing reads
            vouchers and never reads advances.
          </p>
          <p className="mt-2 text-[14px] leading-6 text-[var(--foreground)]/90">
            Where a confirmation box appears you must type the word it shows before the button turns on. That is
            deliberate: it means cash is about to move.
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

      {scenarios.map((scenario) => (
        <ScenarioCard key={scenario.id} scenario={scenario} />
      ))}

      <section id="refusals" className="scroll-mt-20">
        <Card className="space-y-4 p-4">
          <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">If something is refused, this is why</h2>
          <div className="overflow-x-auto rounded-lg border border-[var(--card-border)]">
            <Table>
              <thead>
                <tr>
                  <th className="w-72 border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">Message</th>
                  <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">What it means</th>
                  <th className="border-b border-[var(--card-border)] bg-[var(--surface-soft)] px-3 py-2 text-left text-[12px] font-semibold uppercase text-[var(--muted-foreground)]">What to do</th>
                </tr>
              </thead>
              <tbody>
                {refusals.map((row) => (
                  <tr key={row.message}>
                    <td className="border-b border-[var(--card-border)] px-3 py-2 align-top font-mono text-[12px]">{row.message}</td>
                    <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.meaning}</td>
                    <td className="border-b border-[var(--card-border)] px-3 py-2 align-top">{row.fix}</td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </div>
        </Card>
      </section>

      <Card className="space-y-2 p-4 text-[14px] leading-6 text-[var(--foreground)]/90">
        <h2 className="text-lg font-semibold tracking-tight text-[var(--foreground)]">Two things not to do</h2>
        <p>
          Do not click <Btn>Repay from Fund</Btn> on a voucher whose cash you already handed over as an advance — the
          money would leave the box twice.
        </p>
        <p>
          Do not create a job order called &ldquo;Workshop Expenses&rdquo; for overhead any more. Leave the job blank
          instead; a voucher with no job is overhead and correctly stays out of job costing.
        </p>
      </Card>
    </div>
  );
}
