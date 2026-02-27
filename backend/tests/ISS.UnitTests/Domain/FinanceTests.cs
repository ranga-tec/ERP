using ISS.Domain.Common;
using ISS.Domain.Finance;

namespace ISS.UnitTests.Domain;

public sealed class FinanceTests
{
    [Fact]
    public void Ar_Ap_ApplyPayment_Requires_Positive()
    {
        var ar = new AccountsReceivableEntry(Guid.NewGuid(), "INV", Guid.NewGuid(), 100m, DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => ar.ApplyPayment(0));
        ar.ApplyPayment(40m);
        Assert.Equal(60m, ar.Outstanding);

        var ap = new AccountsPayableEntry(Guid.NewGuid(), "GRN", Guid.NewGuid(), 50m, DateTimeOffset.UtcNow);
        Assert.Throws<DomainValidationException>(() => ap.ApplyPayment(-1));
        ap.ApplyPayment(10m);
        Assert.Equal(40m, ap.Outstanding);
    }

    [Fact]
    public void Payment_Can_Allocate_To_Entries()
    {
        var payment = new Payment("PAY0001", PaymentDirection.Incoming, CounterpartyType.Customer, Guid.NewGuid(), null, "USD", 1m, 100m, DateTimeOffset.UtcNow, null);
        payment.AllocateToAr(Guid.NewGuid(), 25m);
        Assert.Single(payment.Allocations);
    }
}
