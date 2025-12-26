using CafeBot.Core.Entities;
using CafeBot.Core.Interfaces;
using CafeBot.Core.Enums;

namespace CafeBot.Application.Services;

public interface IProductService
{
    // Чтение
    Task<IEnumerable<Product>> GetAvailableProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<Product?> GetProductByIdAsync(int productId);
    Task<Product?> GetProductWithCategoryAsync(int productId);
    Task<IEnumerable<Product>> GetAllProductsForAdminAsync();
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();

    // CRUD операции с продуктами
    Task<Product> CreateProductAsync(int categoryId, string name, string? description, decimal price, ProductUnit unit, string? photoUrl, int displayOrder);
    Task<Product?> UpdateProductAsync(int productId, int? categoryId, string? name, string? description, decimal? price, ProductUnit? unit, string? photoUrl, int? displayOrder, bool? isAvailable);
    Task<bool> DeleteProductAsync(int productId);
    Task<bool> ToggleProductAvailabilityAsync(int productId);

    // CRUD операции с категориями
    Task<Category> CreateCategoryAsync(string name, int displayOrder);
    Task<Category?> UpdateCategoryAsync(int categoryId, string? name, int? displayOrder, bool? isActive);
    Task<bool> DeleteCategoryAsync(int categoryId);
    Task<Category?> GetCategoryByIdAsync(int categoryId);
}

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // Методы чтения
    public async Task<IEnumerable<Product>> GetAvailableProductsAsync()
    {
        return await _unitOfWork.Products.GetAvailableProductsAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _unitOfWork.Products.GetProductsByCategoryAsync(categoryId);
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _unitOfWork.Products.GetByIdAsync(productId);
    }

    public async Task<Product?> GetProductWithCategoryAsync(int productId)
    {
        return await _unitOfWork.Products.GetProductWithCategoryAsync(productId);
    }

    public async Task<IEnumerable<Product>> GetAllProductsForAdminAsync()
    {
        return await _unitOfWork.Products.GetAllProductsForAdminAsync();
    }

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _unitOfWork.Categories.GetActiveCategoriesAsync();
    }

    // CRUD операции с продуктами
    public async Task<Product> CreateProductAsync(int categoryId, string name, string? description, decimal price, ProductUnit unit, string? photoUrl, int displayOrder)
    {
        // Проверяем существование категории
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
        if (category == null)
        {
            throw new ArgumentException("Категория не найдена");
        }

        var product = new Product
        {
            CategoryId = categoryId,
            Name = name,
            Description = description,
            Price = price,
            Unit = unit,
            PhotoUrl = photoUrl,
            DisplayOrder = displayOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<Product?> UpdateProductAsync(int productId, int? categoryId, string? name, string? description, decimal? price, ProductUnit? unit, string? photoUrl, int? displayOrder, bool? isAvailable)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            return null;
        }

        // Проверяем категорию, если она указана
        if (categoryId.HasValue)
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(categoryId.Value);
            if (category == null)
            {
                throw new ArgumentException("Категория не найдена");
            }
            product.CategoryId = categoryId.Value;
        }

        if (!string.IsNullOrEmpty(name))
            product.Name = name;

        if (description != null)
            product.Description = description;

        if (price.HasValue)
            product.Price = price.Value;

        if (unit.HasValue)
            product.Unit = unit.Value;

        if (photoUrl != null)
            product.PhotoUrl = photoUrl;

        if (displayOrder.HasValue)
            product.DisplayOrder = displayOrder.Value;

        if (isAvailable.HasValue)
            product.IsAvailable = isAvailable.Value;

        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();

        return product;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            return false;
        }

        // Проверяем, используется ли продукт в активных заказах
        var hasActiveOrderItems = await _unitOfWork.OrderItems.HasProductInActiveOrdersAsync(productId);
        if (hasActiveOrderItems)
        {
            throw new InvalidOperationException("Нельзя удалить продукт, который используется в активных заказах. Сначала отмените или завершите все заказы с этим продуктом.");
        }

        // Soft delete вместо физического удаления
        product.IsDeleted = true;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleProductAvailabilityAsync(int productId)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(productId);
        if (product == null)
        {
            return false;
        }

        product.IsAvailable = !product.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Products.UpdateAsync(product);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    // CRUD операции с категориями
    public async Task<Category> CreateCategoryAsync(string name, int displayOrder)
    {
        var category = new Category
        {
            Name = name,
            DisplayOrder = displayOrder
        };

        await _unitOfWork.Categories.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return category;
    }

    public async Task<Category?> UpdateCategoryAsync(int categoryId, string? name, int? displayOrder, bool? isActive)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
        if (category == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(name))
            category.Name = name;

        if (displayOrder.HasValue)
            category.DisplayOrder = displayOrder.Value;

        if (isActive.HasValue)
            category.IsActive = isActive.Value;

        await _unitOfWork.Categories.UpdateAsync(category);
        await _unitOfWork.SaveChangesAsync();

        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
        if (category == null)
        {
            return false;
        }

        // Проверяем, есть ли продукты в этой категории
        var categoryWithProducts = await _unitOfWork.Categories.GetCategoryWithProductsAsync(categoryId);
        if (categoryWithProducts?.Products.Any() == true)
        {
            throw new InvalidOperationException("Нельзя удалить категорию, в которой есть продукты. Сначала удалите все продукты из этой категории.");
        }

        await _unitOfWork.Categories.DeleteAsync(category.Id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<Category?> GetCategoryByIdAsync(int categoryId)
    {
        return await _unitOfWork.Categories.GetByIdAsync(categoryId);
    }
}