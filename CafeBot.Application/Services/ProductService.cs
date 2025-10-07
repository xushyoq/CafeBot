using CafeBot.Core.Entities;
using CafeBot.Core.Interfaces;

namespace CafeBot.Application.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAvailableProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<Product?> GetProductByIdAsync(int productId);
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
}

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProductService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

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

    public async Task<IEnumerable<Category>> GetActiveCategoriesAsync()
    {
        return await _unitOfWork.Categories.GetActiveCategoriesAsync();
    }
}