using CafeBot.Core.Entities;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class ProductRepository : BaseRepository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetAvailableProductsAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => p.IsAvailable && !p.IsDeleted && p.Category.IsActive)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _dbSet
            .Where(p => p.CategoryId == categoryId && p.IsAvailable && !p.IsDeleted)
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Product?> GetProductWithCategoryAsync(int productId)
    {
        return await _dbSet
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<IEnumerable<Product>> GetAllProductsForAdminAsync()
    {
        return await _dbSet
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted) // Показываем все не удаленные продукты
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.DisplayOrder)
            .ToListAsync();
    }
}