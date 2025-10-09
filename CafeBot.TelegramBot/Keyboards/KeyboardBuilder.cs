using Telegram.Bot.Types.ReplyMarkups;

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