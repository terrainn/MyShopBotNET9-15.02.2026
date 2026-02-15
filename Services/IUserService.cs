using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface IUserService
{
    Task<User> GetOrCreateUserAsync(long userId, string? firstName);
    Task UpdateUserStateAsync(long userId, BotState state);
    Task UpdateUserCityAsync(long userId, string city); // хглемхк мюгбюмхе
    Task<User?> GetUserAsync(long userId);
    Task<List<User>> GetAllUsersAsync();
    Task<User?> GetUserByChatId(long chatId); // днаюбкемн
}