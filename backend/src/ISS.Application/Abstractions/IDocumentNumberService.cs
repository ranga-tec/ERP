namespace ISS.Application.Abstractions;

public interface IDocumentNumberService
{
    Task<string> NextAsync(string documentType, string prefix, CancellationToken cancellationToken = default);
}

