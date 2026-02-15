using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public interface ICatalogService
{
    Task<List<Product>> GetAllProductsAsync();
    Task<Product?> GetProductByIdAsync(int productId);
    Task<List<string>> GetCategoriesAsync(string? userCity = null);
    Task<List<Product>> GetProductsByCategoryAsync(string categoryName, string? userCity = null);
    Task<List<string>> GetAllCitiesAsync();
    Task<List<Product>> GetAllProductsForAdminAsync();
    Task<Product?> GetProductByIdForAdminAsync(int productId);
    Task UpdateProductAsync(Product product); // ← ДОБАВИЛИ ЭТОТ МЕТОД
}