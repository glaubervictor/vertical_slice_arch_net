using ArchNet.Domain.Entities;
using ArchNet.Domain.Enums;
using FluentAssertions;

namespace ArchNet.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void User_Create_WithValidData_ReturnsUser()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);

        user.Name.Should().Be("Alice");
        user.Login.Should().Be("alice@qia.tech");
        user.PasswordHash.Should().Be("hash");
        user.Salt.Should().Be("salt");
        user.Role.Should().Be(UserRole.User);
        user.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void User_Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => User.Create("", "alice@qia.tech", "hash", "salt", UserRole.User);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_Create_WithEmptyLogin_ThrowsArgumentException()
    {
        var act = () => User.Create("Alice", "", "hash", "salt", UserRole.User);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void User_UpdateProfile_ChangesNameAndRole()
    {
        var user = User.Create("Alice", "alice@qia.tech", "hash", "salt", UserRole.User);

        user.UpdateProfile("Alice Smith", UserRole.Admin);

        user.Name.Should().Be("Alice Smith");
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void User_UpdatePassword_ChangesHashAndSalt()
    {
        var user = User.Create("Alice", "alice@qia.tech", "oldhash", "oldsalt", UserRole.User);

        user.UpdatePassword("newhash", "newsalt");

        user.PasswordHash.Should().Be("newhash");
        user.Salt.Should().Be("newsalt");
    }
}
