async (page) => {
  const routes = [
    "/service/command-center",
    "/service/dispatch-board",
    "/service/technician-workbench",
    "/service/equipment-units",
    "/service/contracts",
    "/service/jobs",
    "/service/technicians",
    "/service/estimates",
    "/service/expense-claims",
    "/service/work-orders",
    "/service/material-requisitions",
    "/service/quality-checks",
    "/service/handovers",
  ];
  const runtimeErrors = [];
  page.on("pageerror", (error) => runtimeErrors.push(error.message));

  const results = [];
  for (const route of routes) {
    const response = await page.goto(`http://127.0.0.1:3000${route}`, {
      waitUntil: "domcontentloaded",
    });
    await page.waitForTimeout(300);
    const body = (await page.locator("body").innerText()).replace(/\s+/g, " ");
    results.push({
      route,
      status: response?.status() ?? null,
      title: await page.title(),
      hasVisibleError: /Unhandled Runtime Error|Application error|Internal Server Error|Failed to fetch/i.test(body),
      sample: body.slice(0, 120),
    });
  }

  const jobsPayload = await page.evaluate(async () => {
    const response = await fetch("/api/backend/service/jobs?take=100");
    if (!response.ok) throw new Error(`Jobs API returned ${response.status}`);
    return response.json();
  });
  const jobs = Array.isArray(jobsPayload) ? jobsPayload : jobsPayload.items ?? [];
  const arithmeticFailures = [];
  const closeEnough = (left, right) => Math.abs(left - right) < 0.000001;
  const sum = (rows, selector) => rows.reduce((total, row) => total + selector(row), 0);

  for (const job of jobs) {
    const costing = await page.evaluate(async (jobId) => {
      const response = await fetch(`/api/backend/service/jobs/${jobId}/costing`);
      if (!response.ok) throw new Error(`Costing API returned ${response.status}`);
      return response.json();
    }, job.id);

    const checks = {
      materialConsumedCost: sum(costing.materialLines, (line) => line.quantity * line.unitCost),
      materialReturnedCredit: sum(costing.materialReturnLines, (line) => line.quantity * line.unitCost),
      directPurchaseCost: sum(
        costing.directPurchaseLines,
        (line) => line.quantity * line.unitPrice * (1 + line.taxPercent / 100),
      ),
      approvedLaborCost: sum(
        costing.laborLines.filter((line) => line.status === 2 || line.status === 4),
        (line) => line.hoursWorked * line.costRate,
      ),
      pendingLaborCost: sum(
        costing.laborLines.filter((line) => line.status === 1),
        (line) => line.hoursWorked * line.costRate,
      ),
      approvedExpenseClaimCost: sum(
        costing.expenseClaimLines.filter((line) => line.status === 2 || line.status === 4),
        (line) => line.quantity * line.unitCost,
      ),
      pendingExpenseClaimCost: sum(
        costing.expenseClaimLines.filter((line) => line.status === 1),
        (line) => line.quantity * line.unitCost,
      ),
    };
    checks.netMaterialCost = checks.materialConsumedCost - checks.materialReturnedCredit;
    checks.totalActualCost =
      checks.netMaterialCost
      + checks.directPurchaseCost
      + checks.approvedLaborCost
      + checks.approvedExpenseClaimCost;

    for (const [field, expected] of Object.entries(checks)) {
      if (!closeEnough(costing[field], expected)) {
        arithmeticFailures.push({ job: job.number, field, expected, actual: costing[field] });
      }
    }
  }

  let jobDetail = null;
  if (jobs.length > 0) {
    const response = await page.goto(`http://127.0.0.1:3000/service/jobs/${jobs[0].id}?tab=costs`, {
      waitUntil: "domcontentloaded",
    });
    await page.waitForTimeout(300);
    const body = await page.locator("body").innerText();
    jobDetail = {
      job: jobs[0].number,
      status: response?.status() ?? null,
      showsCosting: /Job Cost|Cost Breakdown|Materials/i.test(body),
      hasVisibleError: /Unhandled Runtime Error|Application error|Internal Server Error|Failed to fetch/i.test(body),
    };
  }

  return { results, runtimeErrors, reconciledJobCount: jobs.length, arithmeticFailures, jobDetail };
}
