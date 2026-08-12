using ArchNet.Common.Primitives;
using ArchNet.Domain.Enums;

namespace ArchNet.Domain.Entities;

public class User : EntityBase
{
    public string Name { get; private set; } = null!;
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Salt { get; private set; } = null!;
    public UserRole Role { get; private set; }

    private User() { }

    public static User Create(string name, string login, string passwordHash, string salt, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(login);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        return new User
        {
            Name = name,
            Login = login,
            PasswordHash = passwordHash,
            Salt = salt,
            Role = role
        };
    }

    public void UpdateProfile(string name, UserRole role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Role = role;
    }

    public void UpdatePassword(string passwordHash, string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);
        PasswordHash = passwordHash;
        Salt = salt;
    }
}
