using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class CartMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly CartService _cartService;
    private readonly UserService _userService;

    public CartMessageHandler(ITelegramBotClient botClient, CartService cartService, UserService userService)
    {
        _botClient = botClient;
        _cartService = cartService;
        _userService = userService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        return message == "🛒 Корзина" || message == "/cart";
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        var cartItems = await _cartService.GetCartItemsAsync(user.Id);

        if (!cartItems.Any())
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id,
                "🛒 Ваша корзина пуста\n\nДобавьте товары из каталога!",
                replyMarkup: InlineKeyboards.GetEmptyCartKeyboard());
            return;
        }

        var total = await _cartService.GetCartTotalAsync(user.Id);
        var cartText = "🛒 **Ваша корзина**\n\n";

        foreach (var item in cartItems)
        {
            cartText += $"🎁 {item.Product.Name} - {item.Quantity} × {item.Product.Price}₽\n";
        }

        cartText += $"\n💵 **Итого: {total}₽**";

        await _botClient.SendTextMessageAsync(message.Chat.Id, cartText,
            replyMarkup: InlineKeyboards.GetCartKeyboard(total > 0),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
    }
}