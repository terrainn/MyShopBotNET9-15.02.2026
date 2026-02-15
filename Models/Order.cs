namespace MyShopBotNET9.Models;

public class Order
{
    public int Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime OrderDate
    {
        get => CreatedAt;
        set => CreatedAt = value;
    }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; set; }
    public string? Address { get; set; }
    public string? DeliveryPhotoUrl { get; set; } // ← НОВОЕ ПОЛЕ
    public string? DeliveryComment { get; set; }
    public int PublicOrderNumber => 20353 + Id;
    public List<OrderItem> OrderItems { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Completed,
    Cancelled
}