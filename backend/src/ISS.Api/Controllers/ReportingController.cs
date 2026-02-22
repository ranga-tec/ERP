using ISS.Api.Security;
using ISS.Application.Persistence;
using ISS.Application.Services;
using ISS.Domain.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISS.Api.Controllers;

[ApiController]
[Route("api/reporting")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Reporting}")]
public sealed class ReportingController(IIssDbContext dbContext, InventoryService inventoryService) : ControllerBase
{
    public sealed record DashboardDto(int OpenServiceJobs, decimal ArOutstanding, decimal ApOutstanding, int ReorderAlerts);

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken cancellationToken)
    {
        var openServiceJobs = await dbContext.ServiceJobs.AsNoTracking()
            .CountAsync(x => x.Status == ServiceJobStatus.Open || x.Status == ServiceJobStatus.InProgress, cancellationToken);

        var arOutstanding = await dbContext.AccountsReceivableEntries.AsNoTracking()
            .SumAsync(x => x.Outstanding, cancellationToken);

        var apOutstanding = await dbContext.AccountsPayableEntries.AsNoTracking()
            .SumAsync(x => x.Outstanding, cancellationToken);

        var settings = await dbContext.ReorderSettings.AsNoTracking().ToListAsync(cancellationToken);
        var alerts = 0;
        foreach (var s in settings)
        {
            var onHand = await inventoryService.GetOnHandAsync(s.WarehouseId, s.ItemId, null, cancellationToken);
            if (onHand <= s.ReorderPoint)
            {
                alerts++;
            }
        }

        return Ok(new DashboardDto(openServiceJobs, arOutstanding, apOutstanding, alerts));
    }
}

