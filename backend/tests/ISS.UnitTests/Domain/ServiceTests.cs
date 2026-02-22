using ISS.Domain.Common;
using ISS.Domain.Service;

namespace ISS.UnitTests.Domain;

public sealed class ServiceTests
{
    [Fact]
    public void ServiceJob_Transitions_Are_Validated()
    {
        var job = new ServiceJob("SJ0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, "Won't start");

        job.Start();
        Assert.Equal(ServiceJobStatus.InProgress, job.Status);

        job.Complete(DateTimeOffset.UtcNow);
        Assert.Equal(ServiceJobStatus.Completed, job.Status);

        job.Close();
        Assert.Equal(ServiceJobStatus.Closed, job.Status);

        Assert.Throws<DomainValidationException>(() => job.Cancel());
    }

    [Fact]
    public void MaterialRequisition_Post_Requires_Lines()
    {
        var mr = new MaterialRequisition("MR0001", Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => mr.Post());
        mr.AddLine(Guid.NewGuid(), 1m, batchNumber: "B1");
        mr.Post();
        Assert.Equal(MaterialRequisitionStatus.Posted, mr.Status);
    }
}

