using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class CityService : ICityService
{
    private readonly AppDbContext _context;

    public CityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>> GetAllCitiesAsync()
    {
        try
        {
            var cities = await _context.Products
                .Where(p => p.City != null)
                .Select(p => p.City!)
                .Distinct()
                .ToListAsync();

            if (!cities.Any())
            {
                return new List<string> { "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург" };
            }

            return cities;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting cities: {ex.Message}");
            return new List<string> { "Москва", "Санкт-Петербург", "Новосибирск", "Екатеринбург" };
        }
    }

    public async Task<List<string>> GetAvailableCitiesAsync()
    {
        // Просто вызываем GetAllCitiesAsync для совместимости
        return await GetAllCitiesAsync();
    }

    public async Task<bool> SetUserCityAsync(long userId, string city)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.City = city;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error setting user city: {ex.Message}");
            return false;
        }
    }

    public async Task<string?> GetUserCityAsync(long userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.City;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting user city: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Product>> GetProductsByCityAsync(string city)
    {
        try
        {
            return await _context.Products
                .Where(p => p.City == city && p.StockQuantity > 0 && p.IsActive)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting products by city: {ex.Message}");
            return new List<Product>();
        }
    }
}