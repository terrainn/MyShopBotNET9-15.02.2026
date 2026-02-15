using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class OrderMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly OrderService _orderService;
    private readonly UserService _userService;
    private readonly CartService _cartService;

    public OrderMessageHandler(ITelegramBotClient botClient, OrderService orderService,
        UserService userService, CartService cartService)
    {
        _botClient = botClient;
        _orderService = orderService;
        _userService = userService;
        _cartService = cartService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        return userState == BotState.WaitingForAddress || userState == BotState.WaitingForDistrict;
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        var address = message.Text;
        if (string.IsNullOrEmpty(address)) return;

        var cartItems = await _cartService.GetCartItemsAsync(user.Id);
        if (!cartItems.Any())
        {
            await _botClient.SendTextMessageAsync(message.Chat.Id, "❌ Ваша корзина пуста.");
            await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);
            return;
        }

        // 1. Создаем заказ (статус по умолчанию Pending)
        var order = await _orderService.CreateOrderAsync(user.Id, cartItems, address);

        // 2. Переводим в состояние ожидания оплаты
        await _userService.UpdateUserStateAsync(user.Id, BotState.WaitingForPayment);

        string paymentText = $"📜 **Заказ №{order.Id} сформирован**\n\n" +
                           $"💰 Сумма: {order.TotalAmount}₽\n" +
                           $"📍 Адрес: {address}\n\n" +
                           $"💳 **Реквизиты для оплаты:**\n" +
                           $"`2200770148697651` (Почта Банк)\n" +
                           $"Получатель: Анастасия П.\n\n" +
                           $"⚠️ Пожалуйста, совершите перевод и нажмите кнопку ниже:";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: paymentText,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            replyMarkup: InlineKeyboards.GetPaymentKeyboard(order.Id),
            cancellationToken: ct);
    }
}