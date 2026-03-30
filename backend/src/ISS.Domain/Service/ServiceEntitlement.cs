namespace ISS.Domain.Service;

using ISS.Domain.Common;

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

    public static bool CoversParts(ServiceCoverageScope coverage)
        => coverage is ServiceCoverageScope.PartsOnly or ServiceCoverageScope.LaborAndParts;

    public static bool CoversLabor(ServiceCoverageScope coverage)
        => coverage is ServiceCoverageScope.LaborOnly or ServiceCoverageScope.LaborAndParts;

    public static decimal ApplyEstimateUnitPrice(ServiceCoverageScope coverage, ServiceEstimateLineKind kind, decimal unitPrice)
    {
        Guard.NotNegative(unitPrice, nameof(unitPrice));

        return kind switch
        {
            ServiceEstimateLineKind.Part when CoversParts(coverage) => 0m,
            ServiceEstimateLineKind.Labor when CoversLabor(coverage) => 0m,
            _ => unitPrice
        };
    }
}
