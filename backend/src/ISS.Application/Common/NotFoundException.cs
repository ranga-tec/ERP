namespace ISS.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);

