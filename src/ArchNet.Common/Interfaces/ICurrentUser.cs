namespace ArchNet.Common.Interfaces;

public interface ICurrentUser
{
    string Id { get; }
    string Name { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
}
