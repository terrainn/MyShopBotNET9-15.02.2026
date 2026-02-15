using Telegram.Bot;
using Telegram.Bot.Types;
using MyShopBotNET9.Models;
using MyShopBotNET9.Data;
using MyShopBotNET9.Handlers.Interfaces;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.CallbackHandlers;

public class AdminOrderCallbackHandler : ICallbackHandler
{
    public bool CanHandle(string callbackData)
    {
        // Этот хендлер больше не обрабатывает никакие callback'и
        // Все админские операции по заказам теперь в AdminCallbackHandler
        return false;
    }

    public async Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct)
    {
        // Ничего не делаем, просто чтобы не было ошибок
        await Task.CompletedTask;
    }
}