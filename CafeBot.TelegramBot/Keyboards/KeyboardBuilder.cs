using Telegram.Bot.Types.ReplyMarkups;
using CafeBot.Core.Enums; // Добавлено

namespace CafeBot.TelegramBot.Keyboards;

public static class KeyboardBuilder
{
    public static ReplyKeyboardMarkup MainMenuKeyboard(bool isAdmin = false)
    {
        var buttons = new List<List<KeyboardButton>>
        {
            new()
            {
                new KeyboardButton("🆕 Создать заказ"),
                new KeyboardButton("📝 Мои заказы")
            },
            new()
            {
                new KeyboardButton("🏠 Комнаты"),
                new KeyboardButton("ℹ️ Помощь")
            }
        };

        if (isAdmin)
        {
            buttons.Add(new List<KeyboardButton>
            {
                new KeyboardButton("🔧 Админ панель")
            });
        }

        return new ReplyKeyboardMarkup(buttons)
        {
            ResizeKeyboard = true
        };
    }

    public static InlineKeyboardMarkup AdminMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👨‍💼 Управление сотрудниками", "admin_manage_employees")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📦 Управление продуктами", "admin_manage_products")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📁 Управление категориями", "admin_manage_categories")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏢 Управление комнатами", "admin_manage_rooms")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад в главное меню", "admin_back_to_main")
            }
        });
    }

    public static InlineKeyboardMarkup ManageEmployeesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Добавить сотрудника", "admin_add_employee")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒️ Просмотреть сотрудников", "admin_list_employees")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад в админ-панель", "admin_back_to_admin_menu")
            }
        });
    }

    public static InlineKeyboardMarkup EmployeeRolesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Официант", $"set_employee_role_{EmployeeRole.Waiter}"),
                InlineKeyboardButton.WithCallbackData("Админ", $"set_employee_role_{EmployeeRole.Admin}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Отмена", "admin_cancel")
            }
        });
    }

    public static InlineKeyboardMarkup YesNoKeyboard(string callbackPrefix, int entityId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Да", $"{callbackPrefix}_yes_{entityId}"),
                InlineKeyboardButton.WithCallbackData("❌ Нет", $"{callbackPrefix}_no_{entityId}")
            }
        });
    }


    public static InlineKeyboardMarkup DateSelectionKeyboard()
{
    var today = DateTime.UtcNow.Date; // Используем UTC!
    var tomorrow = today.AddDays(1);

    return new InlineKeyboardMarkup(new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData($"📅 Сегодня ({today:dd.MM})", $"date_{today:yyyy-MM-dd}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData($"📅 Завтра ({tomorrow:dd.MM})", $"date_{tomorrow:yyyy-MM-dd}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("❌ Отмена", "cancel")
        }
    });
}

    public static InlineKeyboardMarkup TimeSlotSelectionKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("☀️ День (12:00-16:00)", "timeslot_day")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🌙 Вечер (17:00-22:00)", "timeslot_evening")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_date")
            }
        });
    }

    public static InlineKeyboardMarkup BackButton()
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back")
        });
    }

    public static InlineKeyboardMarkup CancelButton()
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("❌ Отменить", "cancel")
        });
    }
}