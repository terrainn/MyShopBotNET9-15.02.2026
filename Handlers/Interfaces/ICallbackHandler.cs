using Telegram.Bot.Types;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.Interfaces;

public interface ICallbackHandler
{
    bool CanHandle(string callbackData);
    Task HandleAsync(CallbackQuery callback, MyUser user, CancellationToken ct);
}