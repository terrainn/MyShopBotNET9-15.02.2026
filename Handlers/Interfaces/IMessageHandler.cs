using MyShopBotNET9.Models;
using Telegram.Bot.Types;
using MyUser = MyShopBotNET9.Models.User;

namespace MyShopBotNET9.Handlers.Interfaces;

public interface IMessageHandler
{
    bool CanHandle(string message, BotState userState);
    Task HandleAsync(Message message, MyUser user, CancellationToken ct);
}