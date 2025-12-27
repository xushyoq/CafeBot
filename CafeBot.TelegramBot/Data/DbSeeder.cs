using CafeBot.Core.Entities;
using CafeBot.Core.Enums;
using CafeBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CafeBot.TelegramBot.Data;

public static class DbSeeder
{
    public static async Task SeedDataAsync(ApplicationDbContext context)
    {
        // Проверяем есть ли уже данные
        if (await context.Employees.AnyAsync() || await context.Rooms.AnyAsync())
        {
            Console.WriteLine("Ma'lumotlar bazasi allaqachon ma'lumotlarni o'z ichiga oladi. Seed o'tkazib yuboriladi.");
            return;
        }

        Console.WriteLine("Ma'lumotlar bazasini test ma'lumotlari bilan to'ldirish...");

        // Xodim qo'shamiz (test uchun)
        // MUHIM: 123456789 ni sizning haqiqiy Telegram ID ga o'zgartiring!
        var employee = new Employee
        {
            TelegramId = 8094668102, // ← SIZNING TELEGRAM ID GA O'ZGARTIRING!
            FirstName = "Admin",
            LastName = "Test",
            Phone = "+998901234567",
            Role = EmployeeRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);

        // Xonalarni qo'shamiz
        var rooms = new[]
        {
            new Room { Name = "VIP zal №1", Capacity = 10, Status = RoomStatus.Active, Description = "Karaoke bilan katta VIP zal" },
            new Room { Name = "Kichik zal", Capacity = 6, Status = RoomStatus.Active, Description = "Kichik kompaniyalar uchun shinam zal" },
            new Room { Name = "Karaoke", Capacity = 8, Status = RoomStatus.Active, Description = "Professional karaoke tizimi bilan zal" },
            new Room { Name = "Oila", Capacity = 12, Status = RoomStatus.Active, Description = "Oila tadbirlari uchun keng zal" }
        };
        context.Rooms.AddRange(rooms);

        // Kategoriyalarni qo'shamiz
        var categoryFood = new Category { Name = "Taomlar", DisplayOrder = 1, IsActive = true };
        var categoryDrinks = new Category { Name = "Ichimliklar", DisplayOrder = 2, IsActive = true };
        context.Categories.AddRange(categoryFood, categoryDrinks);

        await context.SaveChangesAsync();

        // Mahsulotlarni qo'shamiz (taomlar)
        var products = new[]
        {
            new Product
            {
                CategoryId = categoryFood.Id,
                Name = "Qo'zi go'shtidan shashlik",
                Price = 25000,
                Unit = ProductUnit.Kg,
                Description = "Yosh qo'zi go'shtidan suvli shashlik",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryFood.Id,
                Name = "Tovuq go'shtidan shashlik",
                Price = 18000,
                Unit = ProductUnit.Kg,
                Description = "Nazoratli tovuq go'shti shashligi",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryFood.Id,
                Name = "Palov",
                Price = 22000,
                Unit = ProductUnit.Kg,
                Description = "An'anaviy o'zbek palovi",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryFood.Id,
                Name = "Achichuk salat",
                Price = 15000,
                Unit = ProductUnit.Gram,
                Description = "Pomidor va piyozdan yangi salat (300g)",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryFood.Id,
                Name = "Lag'mon",
                Price = 20000,
                Unit = ProductUnit.Piece,
                Description = "Go'sht va sabzavot bilan lag'mon",
                IsAvailable = true
            },
            
            // Ichimliklar
            new Product
            {
                CategoryId = categoryDrinks.Id,
                Name = "Kola",
                Price = 8000,
                Unit = ProductUnit.Piece,
                Description = "Coca-Cola 0.5l",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryDrinks.Id,
                Name = "Qora choy",
                Price = 5000,
                Unit = ProductUnit.Piece,
                Description = "Qora choy uchun choynak",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryDrinks.Id,
                Name = "Yashil choy",
                Price = 5000,
                Unit = ProductUnit.Piece,
                Description = "Yashil choy uchun choynak",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryDrinks.Id,
                Name = "Sharbat",
                Price = 10000,
                Unit = ProductUnit.Piece,
                Description = "Tabiiy sharbat 1l",
                IsAvailable = true
            },
            new Product
            {
                CategoryId = categoryDrinks.Id,
                Name = "Mineral suv",
                Price = 4000,
                Unit = ProductUnit.Piece,
                Description = "Mineral suv 0.5l",
                IsAvailable = true
            }
        };
        
        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        Console.WriteLine("✅ Тестовые данные добавлены!");
        Console.WriteLine($"   - Работников: 1");
        Console.WriteLine($"   - Комнат: {rooms.Length}");
        Console.WriteLine($"   - Категорий: 2");
        Console.WriteLine($"   - Продуктов: {products.Length}");
    }
}