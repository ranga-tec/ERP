using ISS.Domain.Common;

namespace ISS.Domain.MasterData;

public sealed class UnitConversion : AuditableEntity
{
    private UnitConversion() { }

    public UnitConversion(Guid fromUnitOfMeasureId, Guid toUnitOfMeasureId, decimal factor, string? notes)
    {
        if (fromUnitOfMeasureId == toUnitOfMeasureId)
        {
            throw new DomainValidationException("From and to UoM cannot be the same.");
        }

        FromUnitOfMeasureId = fromUnitOfMeasureId;
        ToUnitOfMeasureId = toUnitOfMeasureId;
        Factor = Guard.Positive(factor, nameof(Factor));
        Notes = NormalizeNotes(notes);
        IsActive = true;
    }

    public Guid FromUnitOfMeasureId { get; private set; }
    public UnitOfMeasure? FromUnitOfMeasure { get; private set; }

    public Guid ToUnitOfMeasureId { get; private set; }
    public UnitOfMeasure? ToUnitOfMeasure { get; private set; }

    public decimal Factor { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public decimal Convert(decimal quantity) => quantity * Factor;

    public void Update(Guid fromUnitOfMeasureId, Guid toUnitOfMeasureId, decimal factor, string? notes, bool isActive)
    {
        if (fromUnitOfMeasureId == toUnitOfMeasureId)
        {
            throw new DomainValidationException("From and to UoM cannot be the same.");
        }

        FromUnitOfMeasureId = fromUnitOfMeasureId;
        ToUnitOfMeasureId = toUnitOfMeasureId;
        Factor = Guard.Positive(factor, nameof(Factor));
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
