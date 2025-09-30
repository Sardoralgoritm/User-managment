using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagmentSystem.Web.Data;
using UserManagmentSystem.Web.Models;
using UserManagmentSystem.Web.Models.Entities;
using UserManagmentSystem.Web.Services;
using UserManagmentSystem.Web.Services.Interfaces;

namespace UserManagementSystem.Tests;

public class UserServiceTests
{
    private Mock<AppDbContext> _dbContextMock;
    private Mock<IPasswordService> _passwordServiceMock;
    private Mock<IEmailService> _emailServiceMock;
    private Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private Mock<ILogger<UserService>> _loggerMock;

    public UserServiceTests()
    {
        _dbContextMock = new Mock<AppDbContext>();
        _passwordServiceMock = new Mock<IPasswordService>();
        _emailServiceMock = new Mock<IEmailService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _loggerMock = new Mock<ILogger<UserService>>();
    }

    [Fact]
    public async Task RegisterUser_Success()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(x => x.HashPassword(It.IsAny<string>(), It.IsAny<string>())).Returns("hash");

        var service = new UserService(db, passwordService.Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.RegisterUserAsync(new RegisterViewModel
        {
            Name = "Test",
            Email = "test@test.com",
            Position = "Dev",
            Password = "pass"
        });

        Assert.True(result.Success);
        Assert.NotNull(await db.Users.FirstOrDefaultAsync(u => u.Email == "test@test.com"));
    }

    [Fact]
    public async Task RegisterUser_EmailExists_Fails()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "test@test.com", Name = "Existing" });
        await db.SaveChangesAsync();

        var service = new UserService(db, new Mock<IPasswordService>().Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.RegisterUserAsync(new RegisterViewModel { Email = "test@test.com" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginUser_EmailNotFound_Fails()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var service = new UserService(db, new Mock<IPasswordService>().Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.LogInUserAsync(new LoginViewModel { Email = "notfound@test.com" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginUser_WrongPassword_Fails()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        db.Users.Add(new User { Id = Guid.NewGuid(), Email = "test@test.com", PasswordHash = "hash" });
        await db.SaveChangesAsync();

        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var service = new UserService(db, passwordService.Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.LogInUserAsync(new LoginViewModel { Email = "test@test.com", Password = "wrong" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoginUser_BlockedUser_Fails()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = "hash",
            Status = Status.Blocked
        });
        await db.SaveChangesAsync();

        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var service = new UserService(db, passwordService.Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.LogInUserAsync(new LoginViewModel { Email = "test@test.com", Password = "pass" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task BlockUsers_Success()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        db.Users.AddRange(
            new User { Id = id1, Email = "user1@test.com", Status = Status.Active },
            new User { Id = id2, Email = "user2@test.com", Status = Status.Active }
        );

        await db.SaveChangesAsync();

        var service = new UserService(db, new Mock<IPasswordService>().Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.BlockUsersAsync(new List<Guid> { id1, id2 });

        Assert.True(result.Success);
        Assert.All(await db.Users.ToListAsync(), u => Assert.Equal(Status.Blocked, u.Status));
    }

    [Fact]
    public async Task BlockUsers_NullIds_Fails()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var service = new UserService(db, new Mock<IPasswordService>().Object, new Mock<IEmailService>().Object, new Mock<IHttpContextAccessor>().Object, new Mock<ILogger<UserService>>().Object);

        var result = await service.BlockUsersAsync(null);

        Assert.False(result.Success);
    }
}
