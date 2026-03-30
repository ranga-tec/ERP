namespace ISS.Domain.Service;

public enum ServiceCoverageScope
{
    None = 0,
    InspectionOnly = 1,
    LaborOnly = 2,
    PartsOnly = 3,
    LaborAndParts = 4
}

public enum ServiceEntitlementSource
{
    None = 0,
    ManufacturerWarranty = 1,
    ServiceContract = 2
}

public enum CustomerBillingTreatment
{
    Billable = 0,
    PartiallyCovered = 1,
    CoveredNoCharge = 2
}

public static class ServiceEntitlementRules
{
    public static CustomerBillingTreatment ToBillingTreatment(ServiceCoverageScope coverage)
        => coverage switch
        {
            ServiceCoverageScope.None => CustomerBillingTreatment.Billable,
            ServiceCoverageScope.LaborAndParts => CustomerBillingTreatment.CoveredNoCharge,
            _ => CustomerBillingTreatment.PartiallyCovered
        };
}
