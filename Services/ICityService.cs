using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface ICityService
{
    Task<List<string>> GetAllCitiesAsync();
    Task<List<string>> GetAvailableCitiesAsync();
    Task<bool> SetUserCityAsync(long userId, string city);
    Task<string?> GetUserCityAsync(long userId);
    Task<List<Product>> GetProductsByCityAsync(string city);
}