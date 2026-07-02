using Microsoft.EntityFrameworkCore;
using MHStore.Repositories.Entities;
using MHStore.Repositories.Enums;

namespace MHStore.Repositories.Data;

public class AppDbContext : DbContext
{
    private static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MainCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    private static readonly Guid SnackCategoryId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductChaRamId = Guid.Parse("33333333-3333-3333-3333-333333333331");
    private static readonly Guid ProductNemChuaId = Guid.Parse("33333333-3333-3333-3333-333333333332");
    private static readonly Guid ProductCaVienId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ProductChaRamImageId = Guid.Parse("66666666-6666-6666-6666-666666666661");
    private static readonly Guid ProductNemChuaImageId = Guid.Parse("66666666-6666-6666-6666-666666666662");
    private static readonly Guid ProductCaVienImageId = Guid.Parse("66666666-6666-6666-6666-666666666663");
    private static readonly Guid PendingOrderId = Guid.Parse("44444444-4444-4444-4444-444444444441");
    private static readonly Guid CompletedOrderId = Guid.Parse("44444444-4444-4444-4444-444444444442");
    private static readonly Guid PendingOrderChaRamItemId = Guid.Parse("77777777-7777-7777-7777-777777777771");
    private static readonly Guid PendingOrderCaVienItemId = Guid.Parse("77777777-7777-7777-7777-777777777772");
    private static readonly Guid CompletedOrderNemChuaItemId = Guid.Parse("77777777-7777-7777-7777-777777777773");
    private static readonly Guid CompletedOrderChaRamItemId = Guid.Parse("77777777-7777-7777-7777-777777777774");
    private static readonly Guid CompletedPaymentLogId = Guid.Parse("55555555-5555-5555-5555-555555555551");

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
        });

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = AdminId,
            Username = "admin",
            PasswordHash = "PBKDF2-SHA256.100000.TUhTdG9yZUFkbWluU2VlZA==.ACC1yxE9bTR835zC2Kuy+pf+C5xgL43C/BtSYOfKpb4=",
            FullName = "MHStore Admin",
            Role = "Admin",
            CreatedAt = new DateTime(2026, 6, 18, 0, 0, 0, DateTimeKind.Utc)
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = MainCategoryId, Name = "Món chính", Slug = "mon-chinh", Status = "Active" },
            new Category { Id = SnackCategoryId, Name = "Ăn vặt", Slug = "an-vat", Status = "Active" });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = ProductChaRamId,
                Name = "Chả ram tôm đất",
                Description = "Gói đông lạnh, chiên nhanh là giòn.",
                Price = 120000,
                ImageUrl = "https://images.unsplash.com/photo-1604908177522-0403f218842b?auto=format&fit=crop&w=900&q=80",
                Stock = 30,
                CategoryId = MainCategoryId,
                IsAvailable = true
            },
            new Product
            {
                Id = ProductNemChuaId,
                Name = "Nem chua rán",
                Description = "Hộp tiện lợi cho bữa ăn vặt tại nhà.",
                Price = 65000,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80",
                Stock = 40,
                CategoryId = SnackCategoryId,
                IsAvailable = true
            },
            new Product
            {
                Id = ProductCaVienId,
                Name = "Cá viên chiên",
                Description = "Đóng gói sẵn, phù hợp chiên hoặc thả lẩu.",
                Price = 45000,
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=900&q=80",
                Stock = 50,
                CategoryId = SnackCategoryId,
                IsAvailable = true
            });

        modelBuilder.Entity<ProductImage>().HasData(
            new ProductImage
            {
                Id = ProductChaRamImageId,
                ProductId = ProductChaRamId,
                ImageUrl = "https://images.unsplash.com/photo-1604908177522-0403f218842b?auto=format&fit=crop&w=900&q=80",
                SortOrder = 0
            },
            new ProductImage
            {
                Id = ProductNemChuaImageId,
                ProductId = ProductNemChuaId,
                ImageUrl = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=900&q=80",
                SortOrder = 0
            },
            new ProductImage
            {
                Id = ProductCaVienImageId,
                ProductId = ProductCaVienId,
                ImageUrl = "https://images.unsplash.com/photo-1546069901-ba9599a7e63c?auto=format&fit=crop&w=900&q=80",
                SortOrder = 0
            });

        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = PendingOrderId,
                OrderCode = "MH26063044444444",
                CustomerInfo = "{\"name\":\"Anh Huy\",\"phone\":\"0334140131\",\"address\":\"Quận 1, TP.HCM\",\"latitude\":null,\"longitude\":null,\"note\":\"Giao sau 18h\",\"addressReferenceId\":\"\"}",
                TotalPrice = 210000,
                OrderStatus = OrderStatus.PendingConfirmation,
                PaymentStatus = PaymentStatus.Pending,
                OrderChannel = OrderChannel.Website,
                PaymentMethod = PaymentMethod.Online,
                ReceiverName = "Anh Huy",
                ReceiverPhone = "0334140131",
                DeliveryAddress = "Quận 1, TP.HCM",
                Latitude = null,
                Longitude = null,
                AddressNote = "Giao sau 18h",
                AddressReferenceId = string.Empty,
                StockReleased = false,
                CreatedAt = new DateTime(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc)
            },
            new Order
            {
                Id = CompletedOrderId,
                OrderCode = "MH26062944444444",
                CustomerInfo = "{\"name\":\"Chị Minh\",\"phone\":\"0334140131\",\"address\":\"Quận Bình Thạnh, TP.HCM\",\"latitude\":null,\"longitude\":null,\"note\":\"Đã thanh toán SePay\",\"addressReferenceId\":\"\"}",
                TotalPrice = 250000,
                OrderStatus = OrderStatus.Completed,
                PaymentStatus = PaymentStatus.Paid,
                OrderChannel = OrderChannel.Website,
                PaymentMethod = PaymentMethod.Online,
                ReceiverName = "Chị Minh",
                ReceiverPhone = "0334140131",
                DeliveryAddress = "Quận Bình Thạnh, TP.HCM",
                Latitude = null,
                Longitude = null,
                AddressNote = "Đã thanh toán SePay",
                AddressReferenceId = string.Empty,
                StockReleased = false,
                CreatedAt = new DateTime(2026, 6, 29, 13, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem
            {
                Id = PendingOrderChaRamItemId,
                OrderId = PendingOrderId,
                ProductId = ProductChaRamId,
                ProductName = "Chả ram tôm đất",
                Quantity = 1,
                UnitPrice = 120000
            },
            new OrderItem
            {
                Id = PendingOrderCaVienItemId,
                OrderId = PendingOrderId,
                ProductId = ProductCaVienId,
                ProductName = "Cá viên chiên",
                Quantity = 2,
                UnitPrice = 45000
            },
            new OrderItem
            {
                Id = CompletedOrderNemChuaItemId,
                OrderId = CompletedOrderId,
                ProductId = ProductNemChuaId,
                ProductName = "Nem chua rán",
                Quantity = 2,
                UnitPrice = 65000
            },
            new OrderItem
            {
                Id = CompletedOrderChaRamItemId,
                OrderId = CompletedOrderId,
                ProductId = ProductChaRamId,
                ProductName = "Chả ram tôm đất",
                Quantity = 1,
                UnitPrice = 120000
            });

        modelBuilder.Entity<PaymentLog>().HasData(new PaymentLog
        {
            Id = CompletedPaymentLogId,
            OrderId = CompletedOrderId,
            TransactionId = "SEED-SEPAY-0001",
            Amount = 250000,
            Status = "Paid",
            RawData = "Seed payment log",
            CreatedAt = new DateTime(2026, 6, 29, 13, 0, 0, DateTimeKind.Utc)
        });
    }
}
