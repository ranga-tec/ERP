you can mark or remove fixed bugs here .

## Fixed

[FIXED] 1. Job order Responsible officer / supervisor must be a dropdown, not free typing.
   -> Dropdown of active system users showing name + roles, on both the job create and job
      edit forms, from a new `GET /api/service/jobs/responsible-officers`. That endpoint sits
      under service rather than admin/users (which is Admin-only) so Service and Sales can
      load it. Locked-out users are excluded. Still stored as a name string, so old typed
      values keep working and the edit form keeps them selectable. Reporting *by* officer
      would need a user-id column + migration - not done.

[FIXED] 2 + 3. Modal dialog should scale more vertically (`image copy.png`, `image copy 3.png`).
   -> One root cause, not two. The dropdown menu was absolutely positioned inside
      AppFormModal, which has `overflow-auto`, and an overflow ancestor clips absolutely
      positioned children. Making the dialog taller would only have moved the bug to longer
      forms, so the menu now renders in a portal on <body> with fixed positioning: no
      container can clip it, it follows the field on scroll/resize, and it flips above when
      there is no room below. AppFormModal also got `min-h-[18rem]` for short dialogs.
      Measured after: menu bottom 625 vs dialog bottom 594, fully on screen, all 5 options.

[FIXED] 4. Batch-tracked items should show available batch numbers like serials do
   (`image copy 5.png`).
   -> New AvailableBatchPicker lists batches actually in stock for the item + warehouse with
      quantities, rolls bin rows up per batch, and flags a batch as short when it holds less
      than the requested qty. Wired into the forms that *issue* from stock: MRN (reported),
      dispatches, direct dispatches, stock transfers. Deliberately not on receiving forms
      (direct purchase, customer return, stock adjustment) where the batch is new and typing
      it is correct. The text box stays, so the picker is additive.

[FIXED] 5. AOD should let you pick the job's MRN and load its requested items, like GRN
   loads from a PO.
   -> A draft AOD linked to a job now shows a "Load From MRN" panel listing that job's
      requisitions with line counts and status. Loading copies each requested line with its
      quantity, batch and serials. Guards: dispatch must be draft; the MRN must belong to the
      same job; items already present are skipped, so loading twice is harmless (the response
      reports added/skipped).
      NOT done: tracking how much of an MRN has already been dispatched across several AODs,
      which is what the GRN receipt plan does for POs. That is the larger follow-up.

[FIXED] 6. GRN PDF printed "Unit CostBatch" (`image copy 4.png`).
   -> The shared table cell helpers had no horizontal padding, so a right-aligned column sat
      flush against the left-aligned one beside it. Added PaddingRight to CellHeader and
      CellBody, which fixes every generated document rather than just the GRN. Verified by
      extracting text from a real GRN PDF: now "Unit Cost Batch".

[FIXED] 7. AOD load-stock panel takes too much space and is confusing (`image copy 6.png`).
   -> StockAvailabilityModal already existed and MRN already used it; the other document
      pages had never been switched. Direct dispatches, dispatches, stock adjustments, stock
      transfers and supplier returns now show a one-line row with a "Load stock" button that
      opens the same explorer in a dialog. The inventory On Hand page keeps it inline, since
      that is that page's whole purpose.

[FIXED] 8. Assistant accountant's petty cash cycle with head office.
   -> New `PettyCashRequest` aggregate in `ISS.Domain/Finance`, beside the fund and the IOU it sits
      between. Lines carry the category - Job Wise (names a job, which is what later lets the spend
      reach job cost), Emergency Operation, Transportation, or Custom with its own name. Head office
      approves each line on its own and may approve less than asked, then releases money per line;
      several lines funded by one bank transfer simply share a payment reference, which is how
      "funds received separately for each even in a single transfer" is represented.
      Sub-accounts needed no new balance machinery: `PettyCashTransaction` gained a nullable
      `PettyCashRequestLineId`, so a category's balance is the existing fund ledger filtered by
      line, and the fund total stays correct for free.
      Verified end to end: 11000 requested across all four categories, approved at 9000 with the
      custom line cut to 0, funded 4000+3000 on one reference and 2000 in two parts on another;
      fund balance moved 4300 -> 13300 and each sub-account matched. Guards confirmed: job-wise
      without a job, custom without a name, funding before approval, editing after submit,
      approving above the requested amount, and over-funding a line are all refused.
      Stage 2 closes the rest of the cycle - issuing, overhead spend, and settlement approval.
   -> `ServiceExpenseClaim.ServiceJobId` is now nullable, which is what finally lets transportation
      and emergency spend be recorded at all; before this the table simply had nowhere to put it.
      Job costing is untouched by that: `ServiceCostingService` matches on the job id, and null
      never equals a job, so overhead can never leak into a job's cost. All fourteen readers of the
      column were audited; the ones needing real handling were the job dashboard's pending count,
      the three cost-gate calls, daily-sheet attachment, and quotation conversion - a voucher with
      no job cannot become a quotation, because a quotation belongs to a job. The claim PDF also
      stopped printing a raw job id when the job was missing.
      Issuing: `IssueBillNumber` is captured at release, and `IssueDirectly` creates an already
      released IOU for cash handed over verbally, where the signed bill is the only record and so
      is mandatory. The IOU request form is unchanged, as asked.
      Settlement now has a head-office step: `SettlementApproved` (7) follows `Settled`, so the
      custodian saying it adds up and head office agreeing are two different facts.
      Both IOU releases and voucher settlements now carry the request line, so a category's
      sub-account is drawn down by what is spent from it, not just topped up by what is funded.
      Verified end to end: an overhead voucher with no job at all was created, charged to a funded
      Transportation category and settled for 800; a verbal IOU of 1500 was issued against a signed
      bill and charged to the Job Wise category, settled at 1200 and approved by head office. The
      sub-accounts moved 5000 -> 4200 and 4000 -> 2500 accordingly, and the job's costing endpoint
      still reported the overhead voucher absent. Guards confirmed: charging an unfunded category,
      charging a job-wise category from a voucher with no job, charging a category from an
      out-of-pocket voucher, and approving a settlement twice are all refused.

## Open

D:\VScode Projects\ISS\Bugs\image copy 26.png we implemented petty cache flow . in petty cache fund screen when request and recive money from head office check whay this 7 is typed .
  also check when request money for existing petty cache fund here cash movement brak down also needed to be updated when cash recieved .
  sfter sinse this project is getting conplex we need to have a skill with policies and standereds . so any codex or claude or gemini or agent
  working he has to follwo exactly and when they see somwhere its not up to the standered they have to fix is first place. we should have a common coding standeredm prefix standered UI standered etc. can you analyse
  and create one  ? UI standered is modeldialog box is a must for create edit and  should eliminate as much as possible adding header records first and adding details after creating the record but adding all in one model dialog. and just like in customer page create edit audit data system has to show all the audit daya of a record trough a  link D:\VScode Projects\ISS\Bugs\image copy 26.png. records cannot be deleted but inactive. 

   [PARTLY FIXED 2026-07-30] The two petty cash bugs above are done. The standards skill is NOT
   started - left deliberately, not forgotten.
   -> The "7" was PettyCashTransactionType.RequestFunding, added for head office funding and never
      given a label in transactionTypeLabel on finance/petty-cash/[id]/page.tsx, so the Type column
      printed the raw enum. Labelled it "Head Office Funding".
      Worse, and the real bug: type 7 was in no bucket of the Cash Movement Breakdown, so the table
      silently stopped tallying - it read 4300 against a true balance of 6300, with nothing on
      screen saying 2000 was missing. Added a "Received from head office" row, plus a catch-all
      "Other movements" row that appears only when non-zero, so a future transaction type that
      nobody buckets shows up instead of vanishing. Verified: breakdown reconciles to 6300 exactly.

   BEFORE BUILDING THE STANDARDS SKILL, read this. The standards are already violated in the
   existing code, so this is a document plus a remediation backlog, not just a document:
     - "records cannot be deleted, only inactive": 19 controllers hard-delete with
       dbContext.X.Remove(...), including Customers - the very page the screenshot holds up as the
       reference. 41 controllers expose HttpDelete.
     - "modal dialog for create/edit": 77 of 110 pages use AppFormModal, so roughly 33 do not.
     - "audit link on every record": only 12 pages carry AuditTrailButton.
     - "no header first, then details": the petty cash request flow does exactly this - create the
       request, then add category lines. It was built that way in July 2026 before this standard
       was stated, and is a violation to schedule, not an argument against the rule.
   The open question for whoever picks it up: standards only (rules for new work), or standards
   plus the audit above as a backlog. The client's "when they see somewhere its not up to the
   standard they have to fix it first place" implies the second, which is much the larger job -
   the hard-delete change alone touches 19 controllers and their screens.

not Urgernt: D:\VScode Projects\ISS\Bugs\image copy 22.png all the audit data of records in all forms should display like in this customer 


not urgent audit logs shows ID when click full audit details from customer form grid autid D:\VScode Projects\ISS\Bugs\image copy 23.png

Item master form grid need fixing . see jumbled items D:\VScode Projects\ISS\Bugs\image copy 24.png

