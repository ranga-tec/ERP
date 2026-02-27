using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class TaxConversion : AuditableEntity
{
    private TaxConversion() { }

    public TaxConversion(Guid sourceTaxCodeId, Guid targetTaxCodeId, decimal multiplier, string? notes)
    {
        if (sourceTaxCodeId == targetTaxCodeId)
        {
            throw new DomainValidationException("Source and target tax codes cannot be the same.");
        }

        SourceTaxCodeId = sourceTaxCodeId;
        TargetTaxCodeId = targetTaxCodeId;
        Multiplier = Guard.Positive(multiplier, nameof(Multiplier));
        Notes = NormalizeNotes(notes);
        IsActive = true;
    }

    public Guid SourceTaxCodeId { get; private set; }
    public TaxCode? SourceTaxCode { get; private set; }

    public Guid TargetTaxCodeId { get; private set; }
    public TaxCode? TargetTaxCode { get; private set; }

    public decimal Multiplier { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public decimal Convert(decimal sourceTaxAmount) => sourceTaxAmount * Multiplier;

    public void Update(Guid sourceTaxCodeId, Guid targetTaxCodeId, decimal multiplier, string? notes, bool isActive)
    {
        if (sourceTaxCodeId == targetTaxCodeId)
        {
            throw new DomainValidationException("Source and target tax codes cannot be the same.");
        }

        SourceTaxCodeId = sourceTaxCodeId;
        TargetTaxCodeId = targetTaxCodeId;
        Multiplier = Guard.Positive(multiplier, nameof(Multiplier));
        Notes = NormalizeNotes(notes);
        IsActive = isActive;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        return Guard.NotNullOrWhiteSpace(notes, nameof(Notes), maxLength: 256);
    }
}
