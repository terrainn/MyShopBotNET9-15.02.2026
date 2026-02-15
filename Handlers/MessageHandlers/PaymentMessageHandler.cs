using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Services;
using MyShopBotNET9.Handlers.Interfaces;
using MyShopBotNET9.Keyboards;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.MessageHandlers;

public class PaymentMessageHandler : IMessageHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly OrderService _orderService;
    private readonly UserService _userService;

    public PaymentMessageHandler(ITelegramBotClient botClient,
                               OrderService orderService,
                               UserService userService)
    {
        _botClient = botClient;
        _orderService = orderService;
        _userService = userService;
    }

    public bool CanHandle(string message, BotState userState)
    {
        // Этот хендлер обрабатывает текстовые сообщения при ожидании оплаты
        return userState == BotState.WaitingForPayment;
    }

    public async Task HandleAsync(Message message, MyUser user, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message.Text)) return;

        // Если пользователь пишет "оплатил" или подобное в тексте
        if (message.Text.ToLower().Contains("оплат") ||
            message.Text.ToLower().Contains("оплатил") ||
            message.Text.ToLower().Contains("paid"))
        {
            await HandlePaymentConfirmationAsync(message, user, ct);
        }
        else
        {
            // Напоминаем о кнопке подтверждения
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "💳 Для подтверждения оплаты нажмите кнопку '✅ Оплатил' в меню заказа",
                cancellationToken: ct);
        }
    }

    private async Task HandlePaymentConfirmationAsync(Message message, MyUser user, CancellationToken ct)
    {
        // Возвращаем в главное меню
        await _userService.UpdateUserStateAsync(user.Id, BotState.MainMenu);

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "✅ **Спасибо за уведомление!**\n\nАдминистратор получил информацию о вашей оплате и скоро обновит статус заказа.",
            replyMarkup: InlineKeyboards.GetMainMenuKeyboard(),
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
            cancellationToken: ct);
    }
}