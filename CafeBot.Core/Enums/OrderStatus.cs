namespace CafeBot.Core.Enums;

public enum OrderStatus
{
    Created = 1,       // Создан
    Confirmed = 2,     // Подтвержден
    Active = 3,        // Активен (клиент в комнате)
    ReadyToPay = 4,    // Готов к оплате
    Paid = 5,          // Оплачен
    Completed = 6,     // Завершен
    Cancelled = 7      // Отменен
}