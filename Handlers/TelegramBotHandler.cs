using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers;

public class TelegramBotHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly UserService _userService;
    private readonly IAdminStateService _adminStateService;
    private readonly IEnumerable<IMessageHandler> _messageHandlers;
    private readonly IEnumerable<ICallbackHandler> _callbackHandlers;

    public TelegramBotHandler(
        ITelegramBotClient botClient,
        UserService userService,
        IAdminStateService adminStateService,
        IEnumerable<IMessageHandler> messageHandlers,
        IEnumerable<ICallbackHandler> callbackHandlers)
    {
        _botClient = botClient;
        _userService = userService;
        _adminStateService = adminStateService;
        _messageHandlers = messageHandlers;
        _callbackHandlers = callbackHandlers;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken ct)
    {
        try
        {
            if (update.Message is { } message) await HandleMessageAsync(message, ct);
            else if (update.CallbackQuery is { } cb) await HandleCallbackQueryAsync(cb, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine("***********************************");
            Console.WriteLine($"❌ ОШИБКА ОБРАБОТКИ: {ex.Message}");
            Console.WriteLine($"🔍 ГДЕ ИМЕННО: {ex.StackTrace}");
            Console.WriteLine("***********************************");
        }
    }

    private async Task HandleMessageAsync(Message message, CancellationToken ct)
    {
        if (message.From == null) return;

        Console.WriteLine($"📥 Получено сообщение: '{message.Text}' от {message.From.Id}");

        var user = await _userService.GetOrCreateUserAsync(message.From.Id, message.From.Username ?? "User");

        bool isCurrentlyAdmin = await _userService.CheckAndUpdateAdminStatusAsync(user.Id);

        if (user.IsAdmin != isCurrentlyAdmin)
        {
            user.IsAdmin = isCurrentlyAdmin;
        }

        if (message.Text == "/start")
        {
            if (string.IsNullOrEmpty(user.City))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "👋 **Добро пожаловать в магазин!**\n\n📍 Для начала выберите ваш город:",
                    replyMarkup: CityKeyboards.GetCitiesKeyboard(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: ct);
            }
            else
            {
                await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "👋 Меню:",
                    replyMarkup: isCurrentlyAdmin
                        ? InlineKeyboards.GetAdminMainMenuKeyboard()
                        : InlineKeyboards.GetMainMenuKeyboard(),
                    cancellationToken: ct);
            }
            return;
        }

        var messageHandler = _messageHandlers.FirstOrDefault(h => h.CanHandle(message.Text ?? "", user.CurrentState));
        if (messageHandler != null)
        {
            Console.WriteLine($"✅ Найден обработчик: {messageHandler.GetType().Name}");
            await messageHandler.HandleAsync(message, user, ct);
        }
        else
        {
            Console.WriteLine($"❌ Обработчик не найден для: '{message.Text}'");
            if (string.IsNullOrEmpty(user.City))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "📍 **Сначала выберите город!**\n\nДля использования магазина нужно указать ваш город.",
                    replyMarkup: CityKeyboards.GetCitiesKeyboard(),
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                    cancellationToken: ct);
            }
        }
    }

    private async Task HandleCallbackQueryAsync(CallbackQuery callback, CancellationToken ct)
    {
        if (callback.From == null || callback.Message == null) return;

        Console.WriteLine($"🔄 Получен callback: '{callback.Data}' от {callback.From.Id}");

        var user = await _userService.GetUserAsync(callback.From.Id)
                   ?? await _userService.GetOrCreateUserAsync(callback.From.Id, callback.From.Username ?? "User");

        // ДЕБАГ: Выведем все callback-хендлеры
        Console.WriteLine($"🔍 Проверяем {_callbackHandlers.Count()} callback-хендлеров...");
        foreach (var handlerItem in _callbackHandlers)
        {
            var canHandle = handlerItem.CanHandle(callback.Data ?? "");
            Console.WriteLine($"   {handlerItem.GetType().Name}: {canHandle}");
        }

        var callbackHandler = _callbackHandlers.FirstOrDefault(h => h.CanHandle(callback.Data ?? ""));
        if (callbackHandler != null)
        {
            Console.WriteLine($"✅ Найден обработчик callback: {callbackHandler.GetType().Name}");
            await callbackHandler.HandleAsync(callback, user, ct);
        }
        else
        {
            Console.WriteLine($"❌ Callback-обработчик не найден для: '{callback.Data}'");
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "⚠️ Команда не распознана", cancellationToken: ct);
        }

        // Всегда отвечаем на callback, чтобы убрать "часики"
        try { await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct); } catch { }
    }

    private async Task HandleSupportRequestAsync(Message message, MyUser user, CancellationToken ct)
    {
        int orderId = _adminStateService.GetEditingProductId(user.Id) ?? 0;
        var admins = await _userService.GetAdminsAsync();
        string text = $"🆘 Поддержка (Заказ №{orderId})\nОт: {user.FirstName}\nТекст: {message.Text}";

        foreach (var admin in admins)
        {
            await _botClient.SendTextMessageAsync(admin.Id, text, cancellationToken: ct);
        }
        await _botClient.SendTextMessageAsync(message.Chat.Id, "✅ Отправлено", replyMarkup: InlineKeyboards.GetMainMenuKeyboard(), cancellationToken: ct);
        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);
    }
}