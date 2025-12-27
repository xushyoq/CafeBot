using CafeBot.Application.Services;
using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using FluentAssertions;
using Xunit;

namespace CafeBot.Tests.Services;

public class ProductServiceTests : TestBase
{
    private readonly IProductService _productService;

    public ProductServiceTests()
    {
        _productService = new ProductService(_unitOfWork);
    }

    [Fact]
    public async Task GetAvailableProductsAsync_ShouldReturnOnlyAvailableProducts()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _productService.GetAvailableProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Бургер");
        result.First().IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnProduct_WhenProductExists()
    {
        // Arrange
        await SeedTestData();
        var productId = 1;

        // Act
        var result = await _productService.GetProductByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Бургер");
        result.Id.Should().Be(productId);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = 999;

        // Act
        var result = await _productService.GetProductByIdAsync(nonExistentProductId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllProductsForAdminAsync_ShouldReturnAllProductsIncludingUnavailable()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _productService.GetAllProductsForAdminAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Бургер");
    }

    [Fact]
    public async Task GetActiveCategoriesAsync_ShouldReturnOnlyActiveCategories()
    {
        // Arrange
        await SeedTestData();

        // Act
        var result = await _productService.GetActiveCategoriesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Еда");
        result.First().IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProductAsync_ShouldCreateProductSuccessfully()
    {
        // Arrange
        await SeedTestData();
        var categoryId = 1;
        var name = "Новый продукт";
        var description = "Tavsif нового продукта";
        var price = 15.99m;
        var unit = ProductUnit.Piece;
        var photoUrl = "http://example.com/photo.jpg";
        var displayOrder = 2;

        // Act
        var result = await _productService.CreateProductAsync(categoryId, name, description, price, unit, photoUrl, displayOrder);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.Price.Should().Be(price);
        result.Unit.Should().Be(unit);
        result.PhotoUrl.Should().Be(photoUrl);
        result.DisplayOrder.Should().Be(displayOrder);
        result.IsAvailable.Should().BeTrue();
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldUpdateProductSuccessfully()
    {
        // Arrange
        await SeedTestData();
        var productId = 1;
        var newName = "Обновленный бургер";
        var newPrice = 12.99m;

        // Act
        var result = await _productService.UpdateProductAsync(productId, null, newName, null, newPrice, null, null, null, null);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(newName);
        result.Price.Should().Be(newPrice);
    }

    [Fact]
    public async Task UpdateProductAsync_ShouldReturnNull_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = 999;
        var newName = "Не существующий продукт";

        // Act
        var result = await _productService.UpdateProductAsync(nonExistentProductId, null, newName, null, null, null, null, null, null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldMarkProductAsDeleted()
    {
        // Arrange
        await SeedTestData();
        var productId = 1;

        // Act
        var result = await _productService.DeleteProductAsync(productId);

        // Assert
        result.Should().BeTrue();

        var deletedProduct = await _productService.GetProductByIdAsync(productId);
        deletedProduct!.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProductAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = 999;

        // Act
        var result = await _productService.DeleteProductAsync(nonExistentProductId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleProductAvailabilityAsync_ShouldToggleAvailability()
    {
        // Arrange
        await SeedTestData();
        var productId = 1;

        // Act
        var result = await _productService.ToggleProductAvailabilityAsync(productId);

        // Assert
        result.Should().BeTrue();

        var updatedProduct = await _productService.GetProductByIdAsync(productId);
        updatedProduct!.IsAvailable.Should().BeFalse();

        // Toggle again
        await _productService.ToggleProductAvailabilityAsync(productId);
        var toggledAgainProduct = await _productService.GetProductByIdAsync(productId);
        toggledAgainProduct!.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleProductAvailabilityAsync_ShouldReturnFalse_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentProductId = 999;

        // Act
        var result = await _productService.ToggleProductAvailabilityAsync(nonExistentProductId);

        // Assert
        result.Should().BeFalse();
    }
}
