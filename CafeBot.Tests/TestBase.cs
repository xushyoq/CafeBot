using CafeBot.Core.Entities;
using CafeBot.Core.Interfaces;
using CafeBot.Core.Enums;
using CafeBot.Application.Services;
using CafeBot.Infrastructure.Data;
using CafeBot.Infrastructure.Repositories;
using CafeBot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CafeBot.Tests;

public abstract class TestBase : IDisposable
{
    protected readonly ApplicationDbContext _dbContext;
    protected readonly IUnitOfWork _unitOfWork;

    protected TestBase()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _unitOfWork = new UnitOfWork(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    protected async Task SeedTestData()
    {
        // Создаем тестовые данные
        var adminEmployee = new Employee
        {
            Id = 1,
            FirstName = "Admin",
            LastName = "User",
            TelegramId = 123456789,
            Role = EmployeeRole.Admin,
            IsActive = true,
            Phone = "+1234567890",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var waiterEmployee = new Employee
        {
            Id = 2,
            FirstName = "Waiter",
            LastName = "User",
            TelegramId = 987654321,
            Role = EmployeeRole.Waiter,
            IsActive = true,
            Phone = "+0987654321",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var category = new Category
        {
            Id = 1,
            Name = "Еда",
            DisplayOrder = 1,
            IsActive = true
        };

        var product = new Product
        {
            Id = 1,
            Name = "Бургер",
            Description = "Вкусный бургер",
            Price = 10.99m,
            Unit = ProductUnit.Piece,
            DisplayOrder = 1,
            IsAvailable = true,
            IsDeleted = false,
            CategoryId = 1,
            Category = category
        };

        await _dbContext.Employees.AddRangeAsync(adminEmployee, waiterEmployee);
        await _dbContext.Categories.AddAsync(category);
        await _dbContext.Products.AddAsync(product);
        await _dbContext.SaveChangesAsync();
    }
}
