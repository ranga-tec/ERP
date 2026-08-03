# Petty-cash end-to-end test status

Last updated: 2026-07-30

This document records the current verification state for the petty-cash request and IOU workflow.
It is intentionally separate from the user guide: it records evidence, blockers, expected figures,
and the remaining test work.

## Completed

- Petty-cash IOU frontend changes were implemented:
  - IOU numbers link to the detail page.
  - List-level grid settlement was removed.
  - Bills, partial returns, and settlement are handled from one IOU detail page.
  - Staff selection is mandatory; an outsider requires a typed name.
- The first open IOU workflow bug was consolidated locally:
  - the duplicate direct-slip button was removed from the IOU page;
  - job staff create the request and finance releases from that same record;
  - funded Job Wise category, collecting employee and signed slip number are mandatory at release;
  - requester and collector are stored separately;
  - physical slip numbers are unique, displayed in the list and searchable;
  - release refuses a different fund/job/category and insufficient category balance.
- Petty-cash user-facing documentation was updated.
- Notification lifecycle was implemented and deployed:
  - submission notifies users with approval or funding permission;
  - approval notifies the requester;
  - each release notifies the requester;
  - the notification badge refreshes every 15 seconds.
- Frontend validation passed:
  - `npx tsc --noEmit`
  - `npx eslint "src/app/(app)" src/components src/lib`
- Backend validation passed:
  - `dotnet build src/ISS.Api/ISS.Api.csproj --no-restore`
  - 0 warnings and 0 errors for the backend build.
  - Final IOU consolidation build: 0 warnings and 0 errors.
  - Unit tests: 51 passed, 0 failed, including collector/slip release rules.
- Final frontend validation passed for the IOU consolidation:
  - full `tsc --noEmit` completed with no diagnostics;
  - targeted ESLint completed with no diagnostics;
  - targeted TypeScript/JSX transpilation and `git diff --check` passed.
- Railway deployment passed:
  - petty-cash UI deployment: `70f1e24a-fbf2-4495-90b3-d904dd6cfc28`
  - notification update deployment: `e50f3ed0-c2fe-46a9-864a-657c2f7b22d4`
  - IOU release consolidation: `0c1c3b8c-d6bb-48e7-ba2b-23695d75f04e`
  - IOU release/assistant settings 500 hotfix: `a1bbc150-73d6-4631-82aa-1cfc21e5f622`
  - the latest deployment reported `SUCCESS`; startup logs confirm
    `20260731045642_RepairMissingAssistantSettingsTables` was applied.
- The 2026-07-31 production hotfix:
  - replaced the IOU release query that PostgreSQL could not translate;
  - validates the selected category against the fund's current category balance;
  - repaired missing assistant settings tables idempotently;
  - passed a clean backend build, all 51 unit tests, migration-script generation, and
    `git diff --check`;
  - produced no 5xx responses in Railway HTTP logs during the post-deployment check.
- Local API health was verified at `/health` with response `Healthy` during the test attempt.

## Not yet completed

The following business flow has not yet been fully executed against the local working database:

1. Create a petty-cash request with four category lines.
2. Submit it and verify the head-office notification.
3. Approve each category amount and verify the approval notification.
4. Fund categories, including a partial funding case, and verify category and fund balances.
5. Approve a job IOU, then release it against its funded Job Wise category while recording a
   different collecting employee and a unique signed slip number.
6. Add two bills against the same IOU.
7. Record two partial cash returns.
8. Settle the IOU without entering a typed spent amount.
9. Add a bill after settlement but before head-office approval.
10. Approve the settlement and verify the hidden voucher reaches `Approved`.
11. Verify no `ExpenseSettlement` transaction is created for that hidden voucher.
12. Verify the return credits the original category, not only the overall fund.
13. Verify the final job-costing amount and all notification records.
14. Remove all test data and confirm the original fund balance and user count are restored.

## Test blockers encountered

- The integration test project could not start because Docker is unavailable on this machine. All
  51 integration tests failed during Testcontainers fixture initialization with
  `DockerUnavailableException`; they did not reach business assertions.
- The existing local `admin@local` password was rejected, so a temporary Development bootstrap-admin
  configuration was used to make the controlled local run possible. Confirm whether that temporary
  account exists and remove it after testing.
- The first browser session was stale and the frontend initially showed `ECONNREFUSED` while the API
  was down. The local API was subsequently started on `http://localhost:5257`; the browser flow still
  needs to be rerun in a fresh session.
- During the 2026-07-30 consolidation, foreground Build/Test/TypeScript commands repeatedly failed
  to return their output to the calling shell. Running the same commands as bounded hidden processes
  exposed their logs and completed successfully. This was a command-runner issue, not a compiler or
  test failure.

## Expected accounting figures

Use the known backend scenario from the handover as the primary exact-number test:

| Check | Expected |
| --- | ---: |
| Initial fund balance | `4300.00` |
| Request funding added | `3000.00` |
| IOU released | `3000.00` |
| Bills | `1200.00 + 500.00 = 1700.00` |
| Partial returns | `800.00 + 500.00 = 1300.00` |
| IOU outstanding after returns | `1700.00` |
| Claimed amount | `1700.00` |
| Unaccounted amount | `0.00` |
| Hidden voucher status after approval | `Approved` |
| Hidden voucher expense-settlement row | none |
| Fund after issue, before return | `2600.00` |
| Fund after returns | `4300.00` |

The request funding movement must be included when checking the fund before the IOU is issued. The
final cleanup must remove the test request, fund transactions, IOU, voucher, voucher lines, bills,
returns, notifications, and any temporary user in foreign-key order.

## Next execution steps

1. Confirm the local API is still listening on port `5257` and the frontend on port `3000`.
2. Start a fresh browser session and authenticate with a known admin account.
3. Capture baseline fund, request, IOU, voucher, notification, and user counts.
4. Execute the numbered business flow above, recording every response and balance.
5. Query the database/API directly after each state transition; do not rely only on displayed totals.
6. Check the notification page for the submit, approval, and release messages.
7. Run cleanup in foreign-key order.
8. Re-query the baseline entities and append actual-versus-expected results to this document.

## Current conclusion

The latest IOU release consolidation and the production 500 hotfix are implemented, code-validated,
and deployed under the IDs above. The full business-level end-to-end test is still open because
Docker blocked the automated integration suite and the fresh authenticated browser run was
interrupted. No end-to-end pass should be claimed until the authenticated browser workflow,
accounting figures and cleanup checks above are recorded.
