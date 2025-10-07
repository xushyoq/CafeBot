using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class EmployeeRepository : BaseRepository<Employee>, IEmployeeRepository
{
    public EmployeeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Employee?> GetByTelegramIdAsync(long telegramId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(e => e.TelegramId == telegramId);
    }

    public async Task<IEnumerable<Employee>> GetActiveEmployeesAsync()
    {
        return await _dbSet
            .Where(e => e.IsActive)
            .OrderBy(e => e.FirstName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Employee>> GetByRoleAsync(EmployeeRole role)
    {
        return await _dbSet
            .Where(e => e.Role == role && e.IsActive)
            .OrderBy(e => e.FirstName)
            .ToListAsync();
    }
}