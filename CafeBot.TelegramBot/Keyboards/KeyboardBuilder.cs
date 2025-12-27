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
                new KeyboardButton("🆕 Buyurtma yaratish"),
                new KeyboardButton("📝 Mening buyurtmalarim")
            },
            new()
            {
                new KeyboardButton("🏠 Xonalar"),
                new KeyboardButton("ℹ️ Yordam")
            }
        };

        if (isAdmin)
        {
            buttons.Add(new List<KeyboardButton>
            {
                new KeyboardButton("🔧 Admin paneli")
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
                InlineKeyboardButton.WithCallbackData("👨‍💼 Xodimlarni boshqarish", "admin_manage_employees")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📊 Xodimlar statistikasi", "admin_employee_statistics")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👀 Ofitsiantlar holati", "admin_employee_status")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📦 Mahsulotlarni boshqarish", "admin_manage_products")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📁 Kategoriyalarni boshqarish", "admin_manage_categories")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏢 Xonalarni boshqarish", "admin_manage_rooms")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Asosiy menyuga orqaga", "admin_back_to_main")
            }
        });
    }

    public static InlineKeyboardMarkup ManageEmployeesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Xodim qo'shish", "admin_add_employee")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒️ Xodimlarni ko'rish", "admin_list_employees")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Admin paneliga orqaga", "admin_back_to_admin_menu")
            }
        });
    }

    public static InlineKeyboardMarkup EmployeeRolesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Ofitsiant", $"set_employee_role_{EmployeeRole.Waiter}"),
                InlineKeyboardButton.WithCallbackData("Admin", $"set_employee_role_{EmployeeRole.Admin}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Bekor qilish", "admin_cancel")
            }
        });
    }

    public static InlineKeyboardMarkup StatisticsPeriodKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 Bugun", "stats_period_today"),
                InlineKeyboardButton.WithCallbackData("📅 Hafta", "stats_period_week")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 Oy", "stats_period_month"),
                InlineKeyboardButton.WithCallbackData("📅 Sanalarni tanlash", "stats_period_custom")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", "admin_back_to_admin_menu")
            }
        });
    }

    public static InlineKeyboardMarkup YesNoKeyboard(string callbackPrefix, int entityId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Ha", $"{callbackPrefix}_yes_{entityId}"),
                InlineKeyboardButton.WithCallbackData("❌ Yo'q", $"{callbackPrefix}_no_{entityId}")
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
            InlineKeyboardButton.WithCallbackData($"📅 Bugun ({today:dd.MM})", $"date_{today:yyyy-MM-dd}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData($"📅 Ertaga ({tomorrow:dd.MM})", $"date_{tomorrow:yyyy-MM-dd}")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("❌ Bekor qilish", "cancel")
        }
    });
}

    public static InlineKeyboardMarkup TimeSlotSelectionKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("☀️ Kun (12:00-16:00)", "timeslot_day")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🌙 Kechqurun (17:00-22:00)", "timeslot_evening")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", "back_to_date")
            }
        });
    }

    public static InlineKeyboardMarkup BackButton()
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Orqaga", "back")
        });
    }

    public static InlineKeyboardMarkup CancelButton()
    {
        return new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("❌ Bekor qilish", "cancel")
        });
    }

    public static InlineKeyboardMarkup ManageProductsKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Mahsulot qo'shish", "admin_add_product")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒️ Mahsulotlarni ko'rish", "admin_list_products")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Admin paneliga orqaga", "admin_back_to_admin_menu")
            }
        });
    }

    public static InlineKeyboardMarkup ManageCategoriesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Kategoriya qo'shish", "admin_add_category")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒️ Kategoriyalarni ko'rish", "admin_list_categories")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Admin paneliga orqaga", "admin_back_to_admin_menu")
            }
        });
    }

    public static InlineKeyboardMarkup ProductUnitKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("dona (dona)", $"set_product_unit_{ProductUnit.Piece}"),
                InlineKeyboardButton.WithCallbackData("kg (kilogramm)", $"set_product_unit_{ProductUnit.Kg}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("g (gramm)", $"set_product_unit_{ProductUnit.Gram}"),
                InlineKeyboardButton.WithCallbackData("l (litr)", $"set_product_unit_{ProductUnit.Liter}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("ml (millilitr)", $"set_product_unit_{ProductUnit.Ml}"),
                InlineKeyboardButton.WithCallbackData("❌ Bekor qilish", "admin_cancel")
            }
        });
    }

    public static InlineKeyboardMarkup ManageRoomsKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("➕ Xona qo'shish", "admin_add_room")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🗒️ Xonalarni ko'rish", "admin_list_rooms")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Admin paneliga orqaga", "admin_back_to_admin_menu")
            }
        });
    }
}