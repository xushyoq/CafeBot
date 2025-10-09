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