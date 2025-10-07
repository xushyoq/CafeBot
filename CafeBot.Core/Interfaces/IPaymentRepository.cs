using CafeBot.Core.Entities;

namespace CafeBot.Core.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
    Task<IEnumerable<Payment>> GetPaymentsByEmployeeAsync(int employeeId);
    Task<IEnumerable<Payment>> GetPaymentsByDateAsync(DateTime date);
}