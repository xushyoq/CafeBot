using CafeBot.Core.Enums;

namespace CafeBot.TelegramBot.States;

public class UserStateData
{
    // Данные для создания заказа
    public DateTime? SelectedDate { get; set; }
    public TimeSlot? SelectedTimeSlot { get; set; }
    public int? SelectedRoomId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientPhone { get; set; }
    public int? GuestCount { get; set; }
    
    // Для добавления продуктов
    public int? SelectedCategoryId { get; set; }
    public int? SelectedProductId { get; set; }
    public decimal? Quantity { get; set; }
    
    // Текущий заказ
    public int? CurrentOrderId { get; set; }
    
    // Временное хранилище для корзины (до создания заказа)
    public List<OrderItemData> Cart { get; set; } = new();

    // Данные для администрирования - Управление сотрудниками
    public long? AdminEmployeeTelegramId { get; set; }
    public string? AdminEmployeeFirstName { get; set; }
    public string? AdminEmployeeLastName { get; set; }
    public string? AdminEmployeePhone { get; set; }
    public EmployeeRole? AdminEmployeeRole { get; set; }

    public void Clear()
    {
        SelectedDate = null;
        SelectedTimeSlot = null;
        SelectedRoomId = null;
        ClientName = null;
        ClientPhone = null;
        GuestCount = null;
        SelectedCategoryId = null;
        SelectedProductId = null;
        Quantity = null;
        CurrentOrderId = null;
        Cart.Clear();

        // Очистка данных администратора
        AdminEmployeeTelegramId = null;
        AdminEmployeeFirstName = null;
        AdminEmployeeLastName = null;
        AdminEmployeePhone = null;
        AdminEmployeeRole = null;
    }
}

public class OrderItemData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public ProductUnit Unit { get; set; }
    public decimal Price { get; set; }
    public decimal Subtotal { get; set; }
}