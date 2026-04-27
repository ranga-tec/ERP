# ISS Screen Recording Runbook

Use this runbook before recording any ISS tutorial or marketing clip.

## 1. Recording Goal

Every recording should do one of two jobs:

- `Sell the value`
  - short
  - visual
  - result-first

- `Teach the operator`
  - slower
  - task-based
  - explicit about what to click and what result to expect

Do not mix both styles in one cut.

## 2. Environment Setup

Before opening the app:

1. Close messaging apps, email popups, and system notifications.
2. Use a browser profile dedicated to demos.
3. Clear unrelated tabs.
4. Set the browser window to `1920x1080`.
5. Set zoom to `100%`.
6. Use the same theme and font scaling for all episodes.
7. Log in with a demo account that has stable access to the section you are recording.

For the ISS demo baseline:

- locale: `en-LK`
- time zone: `Asia/Colombo`
- preferred base currency: `LKR`
- use clean demo records such as `MAIN`, `SUP1`, `CUS1`, `SKU1`

## 3. Data Prep

Before recording process-heavy modules, verify:

- master data exists and is active
- the section opens without empty-state surprises
- seeded references exist:
  - currencies
  - currency rates
  - payment types
  - taxes
  - tax conversions
  - reference forms
- the demo scenario can run end to end:
  - receive stock
  - sell stock
  - review AR/AP
  - review reports

## 4. Recorder Operation

This is the practical screen-recording sequence.

1. Open the target page and wait until it is fully loaded.
2. Start screen recording only after the page is visually stable.
3. Leave `2-3 seconds` of stillness at the start of each take.
4. Move the cursor deliberately.
5. Single-click only once per action.
6. After navigation, wait briefly before the next action.
7. When typing, type cleanly and not too fast.
8. After saving or posting, pause long enough for the success state to be visible.
9. Leave `2-3 seconds` of stillness before stopping the recording.

Best practice:

- record the on-screen actions without narration first
- record narration separately as voiceover

This produces cleaner editing and makes retakes cheaper.

## 5. On-Screen Behavior Rules

During the recording:

- never hunt for the mouse pointer
- avoid fast zig-zag pointer movement
- do not scroll while talking about something else
- do not leave dropdowns half-open unless the narration is about the dropdown
- do not show failed attempts, retries, or typo corrections
- do not expose real credentials, personal email, or external notifications

## 6. Clip Structure

Use this structure for almost every clip:

1. `Context`
   - land on the page
   - show the module name clearly

2. `Action`
   - create, edit, post, allocate, or report

3. `Result`
   - show the saved row, posted status, updated balance, or report metric

4. `Close`
   - one sentence on why the result matters

## 7. Marketing Cut Rules

For marketing clips:

- show outcomes, not setup friction
- use fewer fields and fewer form steps
- prefer dashboards, posted documents, clear tables, and report summaries
- keep every segment visually different from the last one
- do not explain every button

Recommended structure:

1. business problem
2. fast product interaction
3. visible outcome
4. closing value statement

## 8. Guided Tutorial Rules

For longer tutorials:

- show the menu path before the action
- explain prerequisites first
- narrate why the user is doing the step
- call out expected system behavior
- mention the common failure mode if it matters

Good narration pattern:

- `Open`
- `Create`
- `Fill`
- `Save`
- `Post or approve`
- `Verify result`

## 9. Per-Take Checklist

Before pressing record:

- right module open
- correct data ready
- no private tabs visible
- no stale alerts on screen
- browser zoom correct
- menu expanded if needed

Before stopping the take:

- final state visible
- no modal blocking the result
- the audience can clearly see what changed

## 10. Editing Checklist

In the editor:

- trim dead time at the start and end
- keep cursor motion readable
- add zooms only where a form field or status change needs emphasis
- add callouts for:
  - route/path
  - role requirement
  - posted/approved state
  - stock or finance result
- remove loading waits where no information is changing

## 11. File Naming

Use consistent names:

- `iss-marketing-overview-v1.mp4`
- `iss-guide-master-data-v1.mp4`
- `iss-guide-procurement-grn-v1.mp4`
- `iss-guide-finance-payments-v1.mp4`

## 12. Suggested Batch Workflow

Record in batches:

1. Setup batch
   - login
   - orientation
   - master data

2. Transaction batch
   - procurement
   - inventory
   - sales
   - finance

3. Insight batch
   - reporting
   - audit
   - admin/settings

This reduces retakes caused by inconsistent demo data.
