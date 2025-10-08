using CafeBot.Application.Services;
using CafeBot.Core.Interfaces;
using CafeBot.Infrastructure.Data;
using CafeBot.Infrastructure.Repositories;
using CafeBot.TelegramBot.Bot;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;

var builder = Host.CreateApplicationBuilder(args);

// Добавляем DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем UnitOfWork и Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Регистрируем Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Регистрируем Telegram Bot Client
var botToken = builder.Configuration["Telegram:BotToken"] 
    ?? throw new Exception("Telegram Bot Token not found in configuration");

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

// Регистрируем Bot Service
builder.Services.AddHostedService<BotBackgroundService>();

var host = builder.Build();

Console.WriteLine("🤖 CafeBot запущен!");
Console.WriteLine("Нажмите Ctrl+C для остановки...");

await host.RunAsync();