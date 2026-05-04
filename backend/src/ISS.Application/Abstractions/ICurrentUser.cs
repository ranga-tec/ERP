namespace ISS.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? CompanyId { get; }
}
