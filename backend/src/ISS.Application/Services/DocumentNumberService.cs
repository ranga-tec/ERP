using ISS.Application.Abstractions;
using ISS.Application.Persistence;
using ISS.Domain.Sequences;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ISS.Application.Services;

public sealed class DocumentNumberService(IIssDbContext dbContext) : IDocumentNumberService
{
    public async Task<string> NextAsync(string documentType, string prefix, CancellationToken cancellationToken = default)
    {
        documentType = documentType.Trim();
        prefix = prefix.Trim();

        await using var transaction = await dbContext.DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var sequence = await dbContext.DocumentSequences.SingleOrDefaultAsync(s => s.DocumentType == documentType, cancellationToken);
        if (sequence is null)
        {
            sequence = new DocumentSequence(documentType, prefix, nextNumber: 1);
            await dbContext.DocumentSequences.AddAsync(sequence, cancellationToken);
        }

        var number = sequence.Next();
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return number;
    }
}

