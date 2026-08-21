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

        if (dbContext.DbContext.Database.CurrentTransaction is not null)
        {
            // A caller may already hold a business-record lock. Reuse that transaction and
            // serialize this document type with a PostgreSQL transaction-scoped advisory lock;
            // starting a nested transaction is not supported by Npgsql.
            await dbContext.DbContext.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({documentType}))",
                cancellationToken);
            return await NextWithinTransactionAsync(documentType, prefix, cancellationToken);
        }

        var strategy = dbContext.DbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var number = await NextWithinTransactionAsync(documentType, prefix, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return number;
        });
    }

    private async Task<string> NextWithinTransactionAsync(
        string documentType,
        string prefix,
        CancellationToken cancellationToken)
    {
        var sequence = await dbContext.DocumentSequences
            .SingleOrDefaultAsync(s => s.DocumentType == documentType, cancellationToken);
        if (sequence is null)
        {
            sequence = new DocumentSequence(documentType, prefix, nextNumber: 1);
            await dbContext.DocumentSequences.AddAsync(sequence, cancellationToken);
        }

        var number = sequence.Next();
        await dbContext.SaveChangesAsync(cancellationToken);
        return number;
    }
}
