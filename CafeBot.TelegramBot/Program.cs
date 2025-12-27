using CafeBot.Application.Services;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using CafeBot.Infrastructure.Repositories;
using CafeBot.TelegramBot.Bot;
using CafeBot.TelegramBot.Data;
using CafeBot.TelegramBot.Handlers;
using CafeBot.TelegramBot.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Получаем строку подключения
var connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] 
    ?? throw new Exception("Connection string not found");

// Добавляем DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Регистрируем UnitOfWork и Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Регистрируем Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Регистрируем State Manager (Singleton!)
builder.Services.AddSingleton<IUserStateManager, UserStateManager>();

// Регистрируем Handlers
builder.Services.AddScoped<CommandHandler>();
builder.Services.AddScoped<OrderFlowHandler>();
builder.Services.AddScoped<OrderListHandler>();
builder.Services.AddScoped<PaymentHandler>();
builder.Services.AddScoped<OrderManagementHandler>();
builder.Services.AddScoped<RoomHandler>();
builder.Services.AddScoped<AdminHandler>();
builder.Services.AddScoped<EmployeeAdminHandler>();
builder.Services.AddScoped<ProductAdminHandler>();
builder.Services.AddScoped<CategoryAdminHandler>();
builder.Services.AddScoped<RoomAdminHandler>();
builder.Services.AddScoped<BotUpdateHandler>(); // Добавлено

// Регистрируем Telegram Bot Client
var botToken = builder.Configuration["Telegram:BotToken"] 
    ?? throw new Exception("Telegram Bot Token not found");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// Регистрируем Bot Service
builder.Services.AddHostedService<BotBackgroundService>();

var host = builder.Build();

// Заполняем БД тестовыми данными при первом запуске
using (var scope = host.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbSeeder.SeedDataAsync(context);
}

Console.WriteLine("🤖 CafeBot ishga tushdi!");
Console.WriteLine("To'xtatish uchun Ctrl+C bosing...");

await host.RunAsync();