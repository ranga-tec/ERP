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

## Open
job invoice has to prepare befor pushing the stop button or after it 
can we  have  unit cost and selling price see by authorized people only using the priviladges section 