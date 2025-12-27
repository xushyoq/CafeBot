using CafeBot.Application.Services;
using CafeBot.Core.Enums;
using CafeBot.TelegramBot.Keyboards;
using CafeBot.TelegramBot.States;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace CafeBot.TelegramBot.Handlers;

public class EmployeeAdminHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly IUserStateManager _userStateManager;
    private readonly IEmployeeService _employeeService;

    public EmployeeAdminHandler(ITelegramBotClient botClient, IUserStateManager userStateManager, IEmployeeService employeeService)
    {
        _botClient = botClient;
        _userStateManager = userStateManager;
        _employeeService = employeeService;
    }

    public async Task ShowManageEmployeesMenu(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xodimlarni boshqarish:",
            replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task StartAddEmployeeFlow(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminAddingEmployeeTelegramId);
        _userStateManager.ClearStateData(userId); // Clear previous data for new employee
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Yangi xodimning Telegram ID sini kiriting (faqat raqamlar):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task RequestEmployeeFirstName(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xodimning ismini kiriting:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task RequestEmployeeLastName(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xodimning familiyasini kiriting:",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task RequestEmployeePhone(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xodimning telefon raqamini kiriting (masalan, +998901234567):",
            replyMarkup: new ForceReplyMarkup { Selective = true },
            cancellationToken: cancellationToken);
    }

    public async Task RequestEmployeeRole(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Xodim uchun rolni tanlang:",
            replyMarkup: KeyboardBuilder.EmployeeRolesKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task HandleSetEmployeeRoleCallback(long userId, long chatId, string callbackData, int messageId, CancellationToken cancellationToken)
    {
        var parts = callbackData.Split('_');
        if (parts.Length == 4 && Enum.TryParse<EmployeeRole>(parts[3], out var role))
        {
            var stateData = _userStateManager.GetStateData(userId);
            stateData.AdminEmployeeRole = role;

            // Confirm and create employee
            await ConfirmAddEmployee(userId, chatId, messageId, cancellationToken);
        }
    }

    public async Task ConfirmAddEmployee(long userId, long chatId, int messageId, CancellationToken cancellationToken)
    {
        var stateData = _userStateManager.GetStateData(userId);

        if (stateData.AdminEmployeeTelegramId.HasValue &&
            !string.IsNullOrEmpty(stateData.AdminEmployeeFirstName) &&
            !string.IsNullOrEmpty(stateData.AdminEmployeeLastName) &&
            !string.IsNullOrEmpty(stateData.AdminEmployeePhone) &&
            stateData.AdminEmployeeRole.HasValue)
        {
            var existingEmployee = await _employeeService.GetEmployeeByTelegramIdAsync(stateData.AdminEmployeeTelegramId.Value);
            if (existingEmployee != null)
            {
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId,
                    text: $"❌ Telegram ID {stateData.AdminEmployeeTelegramId.Value} bo'lgan xodim allaqachon mavjud.",
                    replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
                    cancellationToken: cancellationToken);
                _userStateManager.ClearState(userId);
                return;
            }

            var newEmployee = await _employeeService.CreateEmployeeAsync(
                stateData.AdminEmployeeTelegramId.Value,
                stateData.AdminEmployeeFirstName,
                stateData.AdminEmployeeLastName,
                stateData.AdminEmployeePhone,
                stateData.AdminEmployeeRole.Value
            );

            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: $"✅ Xodim {newEmployee.FirstName} {newEmployee.LastName} ({newEmployee.Role}) muvaffaqiyatli qo'shildi!",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
                cancellationToken: cancellationToken);

            _userStateManager.ClearState(userId);
        }
        else
        {
            await _botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: messageId,
                text: "Xodimni yaratishda xatolik. Barcha ma'lumotlar to'ldirilmagan.",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
        }
    }

    public async Task ShowEmployeeList(long chatId, CancellationToken cancellationToken)
    {
        var employees = await _employeeService.GetAllEmployeesAsync();

        if (!employees.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Hozirda ro'yxatdan o'tgan xodimlar yo'q.",
                replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        var employeeList = new System.Text.StringBuilder();
        employeeList.AppendLine("Xodimlar ro'yxati:");
        employeeList.AppendLine();

        foreach (var employee in employees)
        {
            employeeList.AppendLine($"👨‍💼 ID: {employee.Id}, Telegram ID: {employee.TelegramId}");
            employeeList.AppendLine($"  Ism: {employee.FirstName} {employee.LastName}");
            employeeList.AppendLine($"  Telefon: {employee.Phone}");
            employeeList.AppendLine($"  Rol: {employee.Role}");
            employeeList.AppendLine($"  Faol: {(employee.IsActive ? "✅ Ha" : "❌ Yo'q")}");
            employeeList.AppendLine();
        }

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: employeeList.ToString(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: KeyboardBuilder.ManageEmployeesKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task ShowStatisticsPeriodSelection(long chatId, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📊 Xodimlar statistikasini ko'rish uchun davrni tanlang:",
            replyMarkup: KeyboardBuilder.StatisticsPeriodKeyboard(),
            cancellationToken: cancellationToken);
    }

    public async Task HandleStatisticsPeriodCallback(long userId, long chatId, string callbackData, CancellationToken cancellationToken)
    {
        DateTime startDate, endDate;

        switch (callbackData)
        {
            case "stats_period_today":
                startDate = DateTime.Today.ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_week":
                startDate = DateTime.Today.AddDays(-7).ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_month":
                startDate = DateTime.Today.AddDays(-30).ToUniversalTime();
                endDate = DateTime.Today.AddDays(1).ToUniversalTime();
                break;
            case "stats_period_custom":
                await StartCustomPeriodSelection(chatId, userId, cancellationToken);
                return;
            default:
                return;
        }

        await ShowEmployeeStatistics(chatId, startDate, endDate, cancellationToken);
    }

    public async Task StartCustomPeriodSelection(long chatId, long userId, CancellationToken cancellationToken)
    {
        _userStateManager.SetState(userId, UserState.AdminSelectingStatisticsStartDate);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "📅 Davr BOSHLANISH sanasini DD.MM.YYYY formatda kiriting (masalan: 01.12.2025):",
            cancellationToken: cancellationToken);
    }

    public async Task HandleStatisticsStartDateInput(long chatId, long userId, string dateText, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(dateText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out var startDate))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Sana formati noto'g'ri. Sanani DD.MM.YYYY formatda kiriting (masalan: 01.12.2025):",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        stateData.AdminStatisticsStartDate = startDate.ToUniversalTime(); // Конвертация в UTC

        _userStateManager.SetState(userId, UserState.AdminSelectingStatisticsEndDate);
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"✅ Boshlanish sanasi: {startDate:dd.MM.yyyy}\n\n📅 Endi davr OXIR sanasini DD.MM.YYYY formatda kiriting:",
            cancellationToken: cancellationToken);
    }

    public async Task HandleStatisticsEndDateInput(long chatId, long userId, string dateText, CancellationToken cancellationToken)
    {
        if (!DateTime.TryParseExact(dateText, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.AssumeLocal, out var endDate))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Sana formati noto'g'ri. Sanani DD.MM.YYYY formatda kiriting (masalan: 31.12.2025):",
                cancellationToken: cancellationToken);
            return;
        }

        var stateData = _userStateManager.GetStateData(userId);
        if (!stateData.AdminStatisticsStartDate.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Xatolik: boshlanish sanasi topilmadi. Qaytadan boshlang.",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
            _userStateManager.ClearState(userId);
            return;
        }

        var startDateUtc = stateData.AdminStatisticsStartDate.Value;
        var startDateLocal = startDateUtc.ToLocalTime().Date; // Только дата без времени для корректного сравнения

        if (endDate.Date <= startDateLocal)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "❌ Oxir sanasi boshlanish sanasidan keyinroq bo'lishi kerak. Qaytadan urinib ko'ring:",
                cancellationToken: cancellationToken);
            return;
        }

        _userStateManager.ClearState(userId);
        await ShowEmployeeStatistics(chatId, startDateUtc, endDate.ToUniversalTime(), cancellationToken); // Конвертация в UTC
    }

    public async Task ShowEmployeeStatistics(long chatId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _employeeService.GetEmployeesStatisticsAsync(startDate, endDate);

            if (!statistics.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"📊 {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy} davri uchun statistika\n\n❌ Tanlangan davrda tugagan buyurtmalar yo'q.",
                    replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = $"📊 Xodimlar statistikasi\n📅 Davr: {startDate.ToLocalTime():dd.MM.yyyy} - {endDate.ToLocalTime():dd.MM.yyyy}\n\n";

            var sortedStats = statistics.OrderByDescending(s => s.TotalRevenue).ToList();

            for (int i = 0; i < sortedStats.Count; i++)
            {
                var stat = sortedStats[i];
                var medal = i switch
                {
                    0 => "🥇",
                    1 => "🥈",
                    2 => "🥉",
                    _ => "👤"
                };

                message += $"{medal} {stat.EmployeeName}\n";
                message += $"   📋 Buyurtmalar: {stat.OrdersCount}\n";
                message += $"   💰 Daromad: {stat.TotalRevenue:N0} so'm\n\n";
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Statistika olishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task ShowEmployeeCurrentStatus(long chatId, CancellationToken cancellationToken)
    {
        try
        {
            var statusList = await _employeeService.GetEmployeesCurrentStatusAsync();

            if (!statusList.Any())
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "👀 Ofitsiantlar holati\n\n❌ Faol ofitsiantlar yo'q.",
                    replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var message = "👀 Ofitsiantlar holati\n\n";

            foreach (var status in statusList.OrderBy(s => s.EmployeeName))
            {
                message += $"👨‍💼 {status.EmployeeName}\n";

                if (status.Status == "Svobod")
                {
                    message += $"   ✅ Bo'sh\n\n";
                }
                else
                {
                    message += $"   🔄 {status.Status}\n\n";
                }
            }

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message.Trim(),
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ Holat olishda xatolik: {ex.Message}",
                replyMarkup: KeyboardBuilder.AdminMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    public async Task HandleAdminCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From!.Id;
        var chatId = callbackQuery.Message!.Chat.Id;
        var data = callbackQuery.Data ?? string.Empty;

        switch (data)
        {
            case "admin_manage_employees":
                await ShowManageEmployeesMenu(chatId, cancellationToken);
                break;
            case "admin_add_employee":
                await StartAddEmployeeFlow(chatId, userId, cancellationToken);
                break;
            case "admin_list_employees":
                await ShowEmployeeList(chatId, cancellationToken);
                break;
            case "admin_employee_statistics":
                await ShowStatisticsPeriodSelection(chatId, cancellationToken);
                break;
            case "admin_employee_status":
                await ShowEmployeeCurrentStatus(chatId, cancellationToken);
                break;
            default:
                if (data.StartsWith("set_employee_role_"))
                {
                    await HandleSetEmployeeRoleCallback(userId, chatId, data, callbackQuery.Message.MessageId, cancellationToken);
                }
                else if (data.StartsWith("stats_period_"))
                {
                    await HandleStatisticsPeriodCallback(userId, chatId, data, cancellationToken);
                }
                break;
        }
    }

    public async Task HandleAdminTextMessageAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var chatId = message.Chat.Id;
        var messageText = message.Text ?? string.Empty;

        var currentState = _userStateManager.GetState(userId);
        var stateData = _userStateManager.GetStateData(userId);

        switch (currentState)
        {
            case UserState.AdminAddingEmployeeTelegramId:
                if (long.TryParse(messageText, out var telegramId))
                {
                    stateData.AdminEmployeeTelegramId = telegramId;
                    _userStateManager.SetState(userId, UserState.AdminAddingEmployeeFirstName);
                    await RequestEmployeeFirstName(chatId, cancellationToken);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "Iltimos, to'g'ri Telegram ID kiriting (faqat raqamlar).");
                }
                break;
            case UserState.AdminAddingEmployeeFirstName:
                stateData.AdminEmployeeFirstName = messageText;
                _userStateManager.SetState(userId, UserState.AdminAddingEmployeeLastName);
                await RequestEmployeeLastName(chatId, cancellationToken);
                break;
            case UserState.AdminAddingEmployeeLastName:
                stateData.AdminEmployeeLastName = messageText;
                _userStateManager.SetState(userId, UserState.AdminAddingEmployeePhone);
                await RequestEmployeePhone(chatId, cancellationToken);
                break;
            case UserState.AdminAddingEmployeePhone:
                stateData.AdminEmployeePhone = messageText;
                _userStateManager.SetState(userId, UserState.AdminSelectingEmployeeRole);
                await RequestEmployeeRole(chatId, cancellationToken);
                break;
            case UserState.AdminSelectingStatisticsStartDate:
                await HandleStatisticsStartDateInput(chatId, userId, messageText, cancellationToken);
                break;
            case UserState.AdminSelectingStatisticsEndDate:
                await HandleStatisticsEndDateInput(chatId, userId, messageText, cancellationToken);
                break;
        }
    }
}
