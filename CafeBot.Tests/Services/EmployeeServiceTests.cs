using CafeBot.Application.Services;
using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CafeBot.Tests.Services;

public class EmployeeServiceTests : TestBase
{
    private readonly EmployeeService _employeeService;

    public EmployeeServiceTests()
    {
        _employeeService = new EmployeeService(_unitOfWork);
    }

    [Fact]
    public async Task GetEmployeeByTelegramIdAsync_ShouldReturnEmployee_WhenEmployeeExists()
    {
        // Arrange
        await SeedTestData();
        var telegramId = 123456789L;

        // Act
        var result = await _employeeService.GetEmployeeByTelegramIdAsync(telegramId);

        // Assert
        result.Should().NotBeNull();
        result!.TelegramId.Should().Be(telegramId);
        result.FirstName.Should().Be("Admin");
        result.Role.Should().Be(EmployeeRole.Admin);
    }

    [Fact]
    public async Task GetEmployeeByTelegramIdAsync_ShouldReturnNull_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var nonExistentTelegramId = 999999999L;

        // Act
        var result = await _employeeService.GetEmployeeByTelegramIdAsync(nonExistentTelegramId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllEmployeesAsync_ShouldReturnAllEmployees()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _employeeService.GetAllEmployeesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.FirstName == "Admin");
        result.Should().Contain(e => e.FirstName == "Waiter");
    }

    [Fact]
    public async Task CreateEmployeeAsync_ShouldCreateEmployeeSuccessfully()
    {
        // Arrange
        var telegramId = 111111111L;
        var firstName = "New";
        var lastName = "Employee";
        var phone = "+1111111111";
        var role = EmployeeRole.Waiter;

        // Act
        var result = await _employeeService.CreateEmployeeAsync(telegramId, firstName, lastName, phone, role);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.FirstName.Should().Be(firstName);
        result.LastName.Should().Be(lastName);
        result.Role.Should().Be(role);
        result.IsActive.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldUpdateEmployeeSuccessfully()
    {
        // Arrange
        await SeedTestData();
        var employeeId = 1;
        var firstName = "Updated Admin";
        var lastName = "User";
        var phone = "+9999999999";
        var role = EmployeeRole.Admin;
        var isActive = true;

        // Act
        var result = await _employeeService.UpdateEmployeeAsync(employeeId, firstName, lastName, phone, role, isActive);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be(firstName);
        result.Phone.Should().Be(phone);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateEmployeeAsync_ShouldReturnNull_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var nonExistentEmployeeId = 999;
        var firstName = "Non";
        var lastName = "Existent";
        var phone = "+9999999999";
        var role = EmployeeRole.Waiter;
        var isActive = true;

        // Act
        var result = await _employeeService.UpdateEmployeeAsync(nonExistentEmployeeId, firstName, lastName, phone, role, isActive);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_ShouldMarkEmployeeAsInactive()
    {
        // Arrange
        await SeedTestData();
        var employeeId = 1;

        // Act
        var result = await _employeeService.DeactivateEmployeeAsync(employeeId);

        // Assert
        result.Should().BeTrue();

        var deactivatedEmployee = await _employeeService.GetEmployeeByTelegramIdAsync(123456789);
        deactivatedEmployee!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task DeactivateEmployeeAsync_ShouldReturnFalse_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var nonExistentEmployeeId = 999;

        // Act
        var result = await _employeeService.DeactivateEmployeeAsync(nonExistentEmployeeId);

        // Assert
        result.Should().BeFalse();
    }
}
