using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class ReferenceForm : AuditableEntity
{
    private ReferenceForm() { }

    public ReferenceForm(string code, string name, string module, string? routeTemplate)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Module = Guard.NotNullOrWhiteSpace(module, nameof(Module), maxLength: 64);
        RouteTemplate = NormalizeRouteTemplate(routeTemplate);
        IsActive = true;
    }

    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Module { get; private set; } = null!;
    public string? RouteTemplate { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string code, string name, string module, string? routeTemplate, bool isActive)
    {
        Code = Guard.NotNullOrWhiteSpace(code, nameof(Code), maxLength: 32);
        Name = Guard.NotNullOrWhiteSpace(name, nameof(Name), maxLength: 128);
        Module = Guard.NotNullOrWhiteSpace(module, nameof(Module), maxLength: 64);
        RouteTemplate = NormalizeRouteTemplate(routeTemplate);
        IsActive = isActive;
    }

    private static string? NormalizeRouteTemplate(string? routeTemplate)
    {
        if (string.IsNullOrWhiteSpace(routeTemplate))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(routeTemplate, nameof(RouteTemplate), maxLength: 256);
    }
}
