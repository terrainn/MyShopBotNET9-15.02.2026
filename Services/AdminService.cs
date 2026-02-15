using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class AdminService
{
    private readonly AppDbContext _context;

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminStats> GetStatsAsync()
    {
        return new AdminStats
        {
            TotalUsers = await _context.Users.CountAsync(),
            TotalOrders = await _context.Orders.CountAsync(),
            TotalProducts = await _context.Products.CountAsync(),
            TotalRevenue = await _context.Orders
                .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount),
            PendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
            ActiveProducts = await _context.Products.CountAsync(p => p.IsActive && p.StockQuantity > 0)
        };
    }

    public async Task AddProductAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateProductAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteProductAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _context.Products.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int productId)
    {
        return await _context.Products.FindAsync(productId);
    }
    public async Task AddProductsAsync(List<Product> products)
    {
        _context.Products.AddRange(products);
        await _context.SaveChangesAsync();
    }
    public class AdminStats
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int ActiveProducts { get; set; }
    }

}