using CafeBot.Core.Entities;

namespace CafeBot.Core.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<IEnumerable<Category>> GetActiveCategoriesAsync();
    Task<Category?> GetCategoryWithProductsAsync(int categoryId);
}