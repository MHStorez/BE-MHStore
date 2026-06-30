using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Entities;

namespace MHStore.Repositories.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<PaymentLog> PaymentLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Price).HasColumnName("price").HasPrecision(18, 2);
            entity.Property(e => e.ImageUrl).HasColumnName("image_url");
            entity.Property(e => e.Stock).HasColumnName("stock");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IsAvailable).HasColumnName("is_available");
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("ProductImages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ImageUrl).HasColumnName("image_url").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.HasIndex(e => e.ProductId);
            entity.HasOne(e => e.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderCode).HasColumnName("order_code").IsRequired().HasMaxLength(32);
            entity.Property(e => e.CustomerInfo).HasColumnName("customer_info").HasColumnType("jsonb");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price").HasPrecision(18, 2);
            entity.Property(e => e.OrderStatus).HasColumnName("order_status").HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.OrderChannel).HasColumnName("order_channel").HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.ReceiverName).HasColumnName("receiver_name").HasMaxLength(200);
            entity.Property(e => e.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(40);
            entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address").HasMaxLength(500);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
            entity.Property(e => e.AddressNote).HasColumnName("address_note").HasMaxLength(500);
            entity.Property(e => e.AddressReferenceId).HasColumnName("address_reference_id").HasMaxLength(120);
            entity.Property(e => e.StockReleased).HasColumnName("stock_released");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.OrderCode).IsUnique();
            entity.HasIndex(e => e.OrderStatus);
            entity.HasIndex(e => e.PaymentStatus);
            entity.HasIndex(e => e.OrderChannel);
            entity.HasIndex(e => e.PaymentMethod);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.HasOne(e => e.Order).WithMany(o => o.Items).HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<PaymentLog>(entity =>
        {
            entity.ToTable("PaymentLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasOne(e => e.Order).WithMany(o => o.PaymentLogs).HasForeignKey(e => e.OrderId);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(120);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(160);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").IsRequired();
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(e => e.FullName).HasColumnName("full_name").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.HasIndex(e => e.Username).IsUnique();

            entity.HasData(new User
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "admin",
                PasswordHash = "PBKDF2-SHA256.100000.TUhTdG9yZUFkbWluU2VlZA==.ACC1yxE9bTR835zC2Kuy+pf+C5xgL43C/BtSYOfKpb4=",
                FullName = "MHStore Admin",
                Role = "Admin",
                CreatedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
