using Microsoft.EntityFrameworkCore;
using MyShopBotNET9.Models;

namespace MyShopBotNET9.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Конфигурация User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasMany(u => u.CartItems)
                  .WithOne(ci => ci.User)
                  .HasForeignKey(ci => ci.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(u => u.Orders)
                  .WithOne(o => o.User)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            // Убираем связь с CartItems здесь, она будет настроена в CartItem
        });

        // Конфигурация CartItem - ВАЖНО: исправляем проблему с ProductId1
        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(ci => ci.Id);

            // Настраиваем связь с Product
            entity.HasOne(ci => ci.Product)
                  .WithMany(p => p.CartItems) // ← Связываем с навигационным свойством Product.CartItems
                  .HasForeignKey(ci => ci.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ci => ci.User)
                  .WithMany(u => u.CartItems)
                  .HasForeignKey(ci => ci.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            entity.HasOne(o => o.User)
                  .WithMany(u => u.Orders)
                  .HasForeignKey(o => o.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(o => o.OrderItems)
                  .WithOne(oi => oi.Order)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Конфигурация OrderItem
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.Property(oi => oi.Price).HasColumnType("TEXT");
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId);
            entity.HasOne(oi => oi.Product)
                  .WithMany()
                  .HasForeignKey(oi => oi.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        // Конфигурация SupportMessage
        modelBuilder.Entity<SupportMessage>(entity =>
        {
            entity.HasKey(m => m.Id);

            entity.HasOne(m => m.Order)
                  .WithMany()
                  .HasForeignKey(m => m.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.OrderId);
            entity.HasIndex(m => m.SenderType);
            entity.HasIndex(m => m.IsReadByAdmin);
            entity.HasIndex(m => m.IsReadByClient);
        });
    }
}