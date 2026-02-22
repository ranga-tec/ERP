namespace ISS.Api.Security;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Procurement = "Procurement";
    public const string Inventory = "Inventory";
    public const string Sales = "Sales";
    public const string Service = "Service";
    public const string Finance = "Finance";
    public const string Reporting = "Reporting";

    public static readonly string[] All =
    [
        Admin,
        Procurement,
        Inventory,
        Sales,
        Service,
        Finance,
        Reporting
    ];
}

