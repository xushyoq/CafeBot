using CafeBot.Core.Entities;

namespace CafeBot.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetAvailableProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
    Task<Product?> GetProductWithCategoryAsync(int productId);
}