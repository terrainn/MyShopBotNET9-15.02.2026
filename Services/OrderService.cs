using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Data;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    // В методе создания заказа OrderService.cs
    public async Task<Order> CreateOrderAsync(long userId, List<CartItem> cartItems, string address)
    {
        try
        {
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                TotalAmount = cartItems.Sum(ci => ci.Product.Price * ci.Quantity),
                Address = address
            };

            order.OrderItems = cartItems.Select(ci => new OrderItem
            {
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                Quantity = ci.Quantity,
                Price = ci.Product.Price
            }).ToList();

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Рассчитываем публичный номер (если начальный ID = 20354)
            var publicOrderNumber = 20353 + order.Id; // или просто order.Id, если сброс прошел

            Console.WriteLine($"✅ Заказ создан: ID={order.Id}, Публичный номер={publicOrderNumber}");

            return order;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating order: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Order>> GetUserOrdersAsync(long userId)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate) // Теперь это свойство существует
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting user orders: {ex.Message}");
            return new List<Order>();
        }
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        try
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting order by ID: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateOrderAsync(Order order)
    {
        try
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error updating order: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Order>> GetPendingOrdersAsync()
    {
        try
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Status == OrderStatus.Pending)
                .OrderBy(o => o.OrderDate) // Теперь это свойство существует
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting pending orders: {ex.Message}");
            return new List<Order>();
        }
    }

    public async Task<List<Order>> GetAllOrdersAsync()
    {
        try
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate) // Теперь это свойство существует
                .ToListAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error getting all orders: {ex.Message}");
            return new List<Order>();
        }
    }

    public async Task<bool> CancelOrderAsync(int orderId, long userId)
    {
        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order != null && order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cancelling order: {ex.Message}");
            return false;
        }
    }
}