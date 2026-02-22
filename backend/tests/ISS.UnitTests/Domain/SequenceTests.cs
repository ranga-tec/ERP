using ISS.Domain.Sequences;

namespace ISS.UnitTests.Domain;

public sealed class SequenceTests
{
    [Fact]
    public void DocumentSequence_Increments()
    {
        var seq = new DocumentSequence("PO", "PO", nextNumber: 1);
        Assert.Equal("PO000001", seq.Peek());
        Assert.Equal("PO000001", seq.Next());
        Assert.Equal("PO000002", seq.Peek());
    }
}

