using CafeBot.Core.Entities;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.Infrastructure.Repositories;

public class PaymentRepository : BaseRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
    {
        return await _dbSet
            .Include(p => p.Order)
            .Include(p => p.ReceivedByEmployee)
            .FirstOrDefaultAsync(p => p.OrderId == orderId);
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByEmployeeAsync(int employeeId)
    {
        return await _dbSet
            .Include(p => p.Order)
            .Where(p => p.ReceivedByEmployeeId == employeeId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Payment>> GetPaymentsByDateAsync(DateTime date)
    {
        return await _dbSet
            .Include(p => p.Order)
            .Include(p => p.ReceivedByEmployee)
            .Where(p => p.PaidAt.Date == date.Date)
            .OrderBy(p => p.PaidAt)
            .ToListAsync();
    }
}