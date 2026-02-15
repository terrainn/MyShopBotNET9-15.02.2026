using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class CartCallbackHandler : ICallbackHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly CartService _cartService;
    private readonly UserService _userService;

    public CartCallbackHandler(ITelegramBotClient botClient, CartService cartService, UserService userService)
    {
        _botClient = botClient;
        _cartService = cartService;
        _userService = userService;
    }

    public bool CanHandle(string callbackData)
    {
        if (string.IsNullOrEmpty(callbackData)) return false;

        return callbackData == "main_menu" ||
               callbackData == "show_main_menu" ||
               callbackData == "show_cart" ||
               callbackData == "checkout" ||
               callbackData == "clear_cart" ||
               callbackData == "show_profile" || // ← ДОБАВИЛИ
               callbackData.StartsWith("cart_") ||
               callbackData.StartsWith("remove_");
    }

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        if (callback.Message == null || string.IsNullOrEmpty(callback.Data)) return;

        var data = callback.Data;
        Console.WriteLine($"🛒 Cart Process: {data} for user {user.Id}");

        try
        {
            if (data == "main_menu" || data == "show_main_menu")
            {
                await HandleMainMenuAsync(callback, user, ct);
            }
            else if (data == "show_cart")
            {
                await ShowCartAsync(callback, user, ct);
            }
            else if (data == "checkout")
            {
                await ShowCheckoutAsync(callback, user, ct);
            }
            else if (data == "clear_cart")
            {
                await HandleClearCartAsync(callback, user, ct);
            }
            else if (data == "show_profile") // ← ДОБАВИЛИ
            {
                await HandleProfileAsync(callback, user, ct);
            }
            else if (data.StartsWith("remove_"))
            {
                await HandleRemoveItemAsync(callback, user, data, ct);
            }

            // Всегда отвечаем на callback, чтобы убрать "часики"
            try { await _botClient.AnswerCallbackQueryAsync(callback.Id, cancellationToken: ct); } catch { }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in CartCallbackHandler: {ex.Message}");
        }
    }

    private async Task HandleMainMenuAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);

        // БЕЗОПАСНО: проверяем в базе каждый раз
        bool isAdmin = await _userService.IsUserAdminAsync(user.Id);

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "🏠 **Главное меню**\n\nВыберите интересующий вас раздел:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: isAdmin
                ? InlineKeyboards.GetAdminMainMenuKeyboard()
                : InlineKeyboards.GetMainMenuKeyboard(),
            cancellationToken: ct);
    }

    private async Task HandleProfileAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        await _userService.UpdateUserStateAsync(user.Id, BotState.Profile);

        var cityText = string.IsNullOrEmpty(user.City)
            ? "🌍 Город не выбран"
            : $"🏙️ Город: {user.City}";

        var usernameText = string.IsNullOrEmpty(user.Username)
            ? "👤 Username: не установлен"
            : $"👤 @{user.Username}";

        var profileText = $"👤 **Ваш профиль**\n\n" +
                         $"🆔 ID: {user.Id}\n" +
                         $"{usernameText}\n" +
                         $"👤 Имя: {user.FirstName ?? "Не указано"}\n" +
                         $"{cityText}\n" +
                         (user.IsAdmin ? "👑 **АДМИНИСТРАТОР**\n" : "") +
                         $"\n📅 Регистрация: {user.CreatedAt:dd.MM.yyyy}";

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: profileText,
            replyMarkup: ProfileKeyboards.GetProfileKeyboard(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }

    private async Task ShowCartAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        var cartItems = await _cartService.GetCartItemsAsync(user.Id);

        if (!cartItems.Any())
        {
            await _botClient.EditMessageTextAsync(
                chatId: callback.Message!.Chat.Id,
                messageId: callback.Message.MessageId,
                text: "🛒 **Ваша корзина пуста**",
                replyMarkup: InlineKeyboards.GetEmptyCartKeyboard(),
                cancellationToken: ct);
            return;
        }

        string text = "🛒 **Ваша корзина:**\n\n";
        decimal total = 0;

        foreach (var item in cartItems)
        {
            // Отладка
            Console.WriteLine($"🛒 Item: {item.Product.Name}, Gram={item.SelectedGram}, Quantity={item.Quantity}");
            Console.WriteLine($"   GramPrices: {string.Join(", ", item.Product.GramPrices.Select(kv => $"{kv.Key}:{kv.Value}"))}");

            var pricePerUnit = item.Product.GramPrices.ContainsKey(item.SelectedGram)
                ? item.Product.GramPrices[item.SelectedGram]
                : item.Product.Price;

            var itemTotal = pricePerUnit * item.Quantity;
            total += itemTotal;

            text += $"🔹 {item.Product.Name} | {item.SelectedGram}г | " +
                    $"{item.Quantity} × {pricePerUnit}₽ = {itemTotal}₽\n";
        }

        text += $"\n💰 **Итого: {total}₽**";

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: text,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: InlineKeyboards.GetCartKeyboard(true),
            cancellationToken: ct);
    }

    private async Task ShowCheckoutAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        var cartItems = await _cartService.GetCartItemsAsync(user.Id);
        if (!cartItems.Any())
        {
            await _botClient.AnswerCallbackQueryAsync(callback.Id, "❌ Корзина пуста", cancellationToken: ct);
            return;
        }

        // Устанавливаем состояние ожидания адреса/района
        await _userService.UpdateUserStateAsync(user.Id, BotState.WaitingForDistrict);

        await _botClient.EditMessageTextAsync(
            chatId: callback.Message!.Chat.Id,
            messageId: callback.Message.MessageId,
            text: "📦 **Оформление заказа**\n\nПожалуйста, введите ваш район текстом в ответном сообщении:",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("🔙 Назад в корзину", "show_cart")),
            cancellationToken: ct);
    }

    private async Task HandleClearCartAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        await _cartService.ClearCartAsync(user.Id);
        await ShowCartAsync(callback, user, ct);
    }

    private async Task HandleRemoveItemAsync(CallbackQuery callback, MyUser user, string data, CancellationToken ct)
    {
        if (int.TryParse(data.Replace("remove_", ""), out int productId))
        {
            await _cartService.RemoveFromCartAsync(user.Id, productId);
            await ShowCartAsync(callback, user, ct);
        }
    }
}