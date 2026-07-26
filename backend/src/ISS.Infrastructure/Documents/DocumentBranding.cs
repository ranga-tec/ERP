using System.Reflection;

namespace ISS.Infrastructure.Documents;

/// <summary>
/// Single source of truth for the letterhead applied to every generated PDF.
/// Swap the values here (and <c>Assets/company-logo.png</c>) to rebrand all documents.
/// </summary>
internal static class DocumentBranding
{
    public const string CompanyName = "C-COM Equipment (Pvt) Ltd";
    public const string AddressLine = "64/10 Nawala Road, Nugegoda, Sri Lanka";
    public const string Phone = "(+94) 112812994";
    public const string Email = "info@c-com.lk";
    public const string Website = "www.c-com.lk";

    /// <summary>Logo orange.</summary>
    public const string Accent = "#F7941C";

    /// <summary>Logo charcoal.</summary>
    public const string Ink = "#231F20";

    public static string ContactLine => $"{Phone}  •  {Email}  •  {Website}";

    private const string LogoResourceName = "ISS.Infrastructure.Assets.CompanyLogo.png";

    private static readonly Lazy<byte[]> LazyLogo = new(LoadLogo, LazyThreadSafetyMode.ExecutionAndPublication);

    public static byte[] Logo => LazyLogo.Value;

    private static byte[] LoadLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(LogoResourceName)
            ?? throw new InvalidOperationException($"Embedded company logo '{LogoResourceName}' was not found.");

        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
