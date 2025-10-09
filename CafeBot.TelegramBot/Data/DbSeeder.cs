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
            Console.WriteLine("База данных уже содержит данные. Пропускаем seed.");
            return;
        }

        Console.WriteLine("Заполняем базу данных тестовыми данными...");

        // Добавляем работника (для тестирования)
        // ВАЖНО: Замените 123456789 на ваш реальный Telegram ID!
        var employee = new Employee
        {
            TelegramId = 8094668102, // ← ЗАМЕНИТЕ НА ВАШ TELEGRAM ID!
            FirstName = "Админ",
            LastName = "Тестовый",
            Phone = "+998901234567",
            Role = EmployeeRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Employees.Add(employee);

        // Добавляем комнаты
        var rooms = new[]
        {
            new Room { Name = "VIP зал №1", Capacity = 10, Status = RoomStatus.Active, Description = "Большой VIP зал с караоке" },
            new Room { Name = "Малый зал", Capacity = 6, Status = RoomStatus.Active, Description = "Уютный зал для небольших компаний" },
            new Room { Name = "Караоке", Capacity = 8, Status = RoomStatus.Active, Description = "Зал с профессиональной караоке системой" },
            new Room { Name = "Семейный", Capacity = 12, Status = RoomStatus.Active, Description = "Просторный зал для семейных мероприятий" }
        };
        context.Rooms.AddRange(rooms);

        // Добавляем категории
        var categoryFood = new Category { Name = "Еда", DisplayOrder = 1, IsActive = true };
        var categoryDrinks = new Category { Name = "Напитки", DisplayOrder = 2, IsActive = true };
        context.Categories.AddRange(categoryFood, categoryDrinks);

        await context.SaveChangesAsync();

        // Добавляем продукты (еда)
        var products = new[]
        {
            new Product 
            { 
                CategoryId = categoryFood.Id, 
                Name = "Шашлык из баранины", 
                Price = 25000, 
                Unit = ProductUnit.Kg,
                Description = "Сочный шашлык из молодой баранины",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryFood.Id, 
                Name = "Шашлык из курицы", 
                Price = 18000, 
                Unit = ProductUnit.Kg,
                Description = "Нежный куриный шашлык",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryFood.Id, 
                Name = "Плов", 
                Price = 22000, 
                Unit = ProductUnit.Kg,
                Description = "Традиционный узбекский плов",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryFood.Id, 
                Name = "Салат Ачик-чучук", 
                Price = 15000, 
                Unit = ProductUnit.Gram,
                Description = "Свежий салат из помидоров и лука (300г)",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryFood.Id, 
                Name = "Лагман", 
                Price = 20000, 
                Unit = ProductUnit.Piece,
                Description = "Лагман с мясом и овощами",
                IsAvailable = true 
            },
            
            // Напитки
            new Product 
            { 
                CategoryId = categoryDrinks.Id, 
                Name = "Кола", 
                Price = 8000, 
                Unit = ProductUnit.Piece,
                Description = "Coca-Cola 0.5л",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryDrinks.Id, 
                Name = "Чай черный", 
                Price = 5000, 
                Unit = ProductUnit.Piece,
                Description = "Чайник черного чая",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryDrinks.Id, 
                Name = "Чай зеленый", 
                Price = 5000, 
                Unit = ProductUnit.Piece,
                Description = "Чайник зеленого чая",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryDrinks.Id, 
                Name = "Сок", 
                Price = 10000, 
                Unit = ProductUnit.Piece,
                Description = "Натуральный сок 1л",
                IsAvailable = true 
            },
            new Product 
            { 
                CategoryId = categoryDrinks.Id, 
                Name = "Вода минеральная", 
                Price = 4000, 
                Unit = ProductUnit.Piece,
                Description = "Минеральная вода 0.5л",
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