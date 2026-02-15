using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class CatalogService : ICatalogService
{
    private readonly AppDbContext _context;

    public CatalogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        try
        {
            var products = await _context.Products
                .Where(p => p.StockQuantity > 0 && p.IsActive)
                .ToListAsync();

            Console.WriteLine($"📦 Loaded {products.Count} active products");
            return products;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting all products: {ex.Message}");
            return new List<Product>();
        }
    }

    public async Task<Product?> GetProductByIdForAdminAsync(int productId)
    {
        try
        {
            return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting product for admin: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(string categoryName, string? userCity = null)
    {
        try
        {
            var query = _context.Products
                .Where(p => p.Category == categoryName && p.StockQuantity > 0 && p.IsActive);

            if (!string.IsNullOrEmpty(userCity))
            {
                // ИСПРАВЛЕНО: Теперь учитывается город "Все"
                query = query.Where(p => p.City == userCity || p.City == "Все");
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return new List<Product>();
        }
    }

    public async Task<List<string>> GetCategoriesAsync(string? userCity = null)
    {
        try
        {
            var query = _context.Products
                .Where(p => p.StockQuantity > 0 && p.Category != null && p.IsActive);

            if (!string.IsNullOrEmpty(userCity))
            {
                // ИСПРАВЛЕНО: Категории также фильтруются с учетом города "Все"
                query = query.Where(p => p.City == userCity || p.City == "Все");
            }

            return await query.Select(p => p.Category!).Distinct().ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == productId && p.StockQuantity > 0 && p.IsActive);
    }

    public async Task<List<string>> GetAllCitiesAsync()
    {
        // Исключаем "Все" из списка выбора города для пользователя
        return await _context.Products
            .Where(p => !string.IsNullOrEmpty(p.City) && p.City != "Все")
            .Select(p => p.City!)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Product>> GetAllProductsForAdminAsync()
    {
        return await _context.Products.ToListAsync();
    }
    public async Task UpdateProductAsync(Product product)
    {
        try
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Product {product.Id} updated. Stock: {product.StockQuantity}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating product: {ex.Message}");
            throw;
        }
    }
}