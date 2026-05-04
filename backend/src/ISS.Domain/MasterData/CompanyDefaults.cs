namespace ISS.Domain.MasterData;

public static class CompanyDefaults
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid CComCompanyId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public const string DefaultCompanyCode = "ISS";
    public const string DefaultCompanyName = "ISS";
    public const string CComCompanyCode = "C-COM";
    public const string CComCompanyName = "C-COM";
}
