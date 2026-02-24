using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    //add
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptDetail> GoodsReceiptDetails => Set<GoodsReceiptDetail>();
    public DbSet<Lot> Lots => Set<Lot>();
    public DbSet<Box> Boxes => Set<Box>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Rack> Racks => Set<Rack>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<StockCheck> StockChecks => Set<StockCheck>();
    public DbSet<StockCheckDetail> StockCheckDetails => Set<StockCheckDetail>();
    public DbSet<InventoryRequest> InventoryRequest => Set<InventoryRequest>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ===================== RefreshToken =====================
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Token)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(x => x.UserId)
                  .IsRequired()
                  .HasMaxLength(450);

            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== ApplicationUser =====================
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName)
                  .HasMaxLength(150);

            entity.Property(x => x.Address)
                  .HasMaxLength(255);

            entity.Property(x => x.Gender)
                  .HasConversion<int>();

            entity.Property(x => x.Status)
                  .HasConversion<int>();
        });

        // ===================== Category =====================
        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(x => x.Description)
                  .HasMaxLength(500);

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();
        });

        // ===================== Product =====================
        builder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Description)
                  .HasMaxLength(1000);

            entity.Property(x => x.Price)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.Unit)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(x => x.ImageUrl)
                  .HasMaxLength(500);

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // =============================
            // Relationships
            // =============================

            entity.HasOne(x => x.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.GoodsReceiptDetails)
                  .WithOne(d => d.Product)
                  .HasForeignKey(d => d.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            // =============================
            // Index
            // =============================

            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Status);
        });

        // ===================== Notification =====================
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                  .IsRequired();

            entity.Property(x => x.Message)
                  .IsRequired()
                  .HasMaxLength(1000);

            entity.Property(x => x.CreatedAt)
                  .IsRequired();

            entity.Property(x => x.ReferenceType)
                  .HasMaxLength(100);

            entity.HasIndex(x => new { x.ReferenceType, x.ReferenceId });

            entity.HasMany(x => x.UserNotifications)
                  .WithOne(un => un.Notification)
                  .HasForeignKey(un => un.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== UserNotification =====================
        builder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("UserNotifications");

            entity.HasKey(x => new { x.UserId, x.NotificationId });

            entity.HasOne(x => x.User)
                  .WithMany(u => u.UserNotifications)
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Notification)
                  .WithMany(n => n.UserNotifications)
                  .HasForeignKey(x => x.NotificationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.IsRead)
                  .IsRequired();
        });

        //GoodsReceipt
        builder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("GoodsReceipts");

            // Primary Key
            entity.HasKey(x => x.Id);

            // Enum -> string (dễ đọc DB hơn int)
            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            // Decimal precision
            entity.Property(x => x.TotalEstimatedQuantity)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0);

            entity.Property(x => x.TotalActualQuantity)
                  .HasColumnType("decimal(18,2)");

            // Date config
            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.Property(x => x.ReceivedDate)
                  .IsRequired();

            // =============================
            // Relationships
            // =============================

            // Supplier (1 Supplier - many GoodsReceipt)
            entity.HasOne(x => x.Supplier)
                  .WithMany(s => s.GoodsReceipts)
                  .HasForeignKey(x => x.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Warehouse (1 Warehouse - many GoodsReceipt)
            entity.HasOne(x => x.Warehouse)
                  .WithMany(w => w.GoodsReceipts)
                  .HasForeignKey(x => x.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            // CreatedBy (User)
            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // ApprovedBy (User - optional)
            entity.HasOne(x => x.ApprovedUser)
                  .WithMany()
                  .HasForeignKey(x => x.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // Details (1 - many)
            entity.HasMany(x => x.Details)
                  .WithOne(d => d.GoodsReceipt)
                  .HasForeignKey(d => d.GoodsReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        //GoodsReceiptDetail
        builder.Entity<GoodsReceiptDetail>(entity =>
        {
            entity.ToTable("GoodsReceiptDetails");

            // Primary Key
            entity.HasKey(x => x.Id);

            // ==============================
            // Decimal precision
            // ==============================
            entity.Property(x => x.EstimatedQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.ActualQuantity)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.UnitPrice)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            // ==============================
            // Enum
            // ==============================
            entity.Property(x => x.QCResult)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.QCNote)
                  .HasMaxLength(500);

            // ==============================
            // Relationships
            // ==============================

            // GoodsReceipt (1 - many)
            entity.HasOne(x => x.GoodsReceipt)
                  .WithMany(r => r.Details)
                  .HasForeignKey(x => x.GoodsReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Product (1 - many)
            entity.HasOne(x => x.Product)
                  .WithMany(p => p.GoodsReceiptDetails)
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            // InspectedBy (User) (1 - many)
            entity.HasOne(x => x.InspectedUser)
                  .WithMany()
                  .HasForeignKey(x => x.InspectedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // Lot (1 - 1)
            entity.HasOne(x => x.Lot)
                  .WithOne(l => l.GoodsReceiptDetail)
                  .HasForeignKey<Lot>(l => l.GoodsReceiptDetailId)
                  .OnDelete(DeleteBehavior.Restrict);

            // ==============================
            // Index tối ưu query
            // ==============================

            entity.HasIndex(x => x.GoodsReceiptId);
            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.QCResult);
            entity.HasIndex(x => x.ExpiryDate);
        });

        //lot
        builder.Entity<Lot>(entity =>
        {
            entity.ToTable("Lots");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.LotCode)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(x => x.LotCode)
                  .IsUnique();

            entity.Property(x => x.TotalQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.RemainingQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.ExpiryDate)
                  .IsRequired();

            entity.Property(x => x.ReceivedDate)
                  .IsRequired();

            // =============================
            // Relationships
            // =============================

            // Product (1 - many)
            entity.HasOne(x => x.Product)
                  .WithMany()
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);

            // GoodsReceiptDetail (1 - 1)
            entity.HasOne(x => x.GoodsReceiptDetail)
                  .WithOne(d => d.Lot)
                  .HasForeignKey<Lot>(x => x.GoodsReceiptDetailId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Box (1 - many)
            entity.HasMany(x => x.Boxes)
                  .WithOne(b => b.Lot)
                  .HasForeignKey(b => b.LotId)
                  .OnDelete(DeleteBehavior.Cascade);

            // =============================
            // Index tối ưu FEFO
            // =============================

            entity.HasIndex(x => x.ProductId);
            entity.HasIndex(x => x.ExpiryDate);
            entity.HasIndex(x => x.Status);
        });

        //box
        builder.Entity<Box>(entity =>
        {
            entity.ToTable("Boxes");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.BoxCode)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(x => x.BoxCode)
                  .IsUnique();

            entity.Property(x => x.Weight)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.QRCode)
                  .HasMaxLength(300);

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // =============================
            // Relationships
            // =============================

            // Lot (1 - many)
            entity.HasOne(x => x.Lot)
                  .WithMany(l => l.Boxes)
                  .HasForeignKey(x => x.LotId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Slot (1 - many) - optional
            entity.HasOne(x => x.Slot)
                  .WithMany(s => s.Boxes)
                  .HasForeignKey(x => x.SlotId)
                  .OnDelete(DeleteBehavior.SetNull);

            // InventoryTransaction (1 - many)
            entity.HasMany(x => x.Transactions)
                  .WithOne(t => t.Box)
                  .HasForeignKey(t => t.BoxId)
                  .OnDelete(DeleteBehavior.Cascade);

            // =============================
            // Index tối ưu truy vấn
            // =============================

            entity.HasIndex(x => x.LotId);
            entity.HasIndex(x => x.SlotId);
            entity.HasIndex(x => x.Status);
        });

        //Supplier
        builder.Entity<Supplier>(entity =>
        {
            entity.ToTable("Suppliers");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.ContactPerson)
                  .HasMaxLength(150);

            entity.Property(x => x.Phone)
                  .HasMaxLength(20);

            entity.Property(x => x.Email)
                  .HasMaxLength(150);

            entity.Property(x => x.Address)
                  .HasMaxLength(500);

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // =============================
            // Relationships
            // =============================

            entity.HasMany(x => x.GoodsReceipts)
                  .WithOne(r => r.Supplier)
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // =============================
            // Index
            // =============================

            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Status);
        });

        //inventory transaction
        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.TransactionType)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.Quantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.ReferenceType)
                  .HasConversion<string>()
                  .HasMaxLength(50);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // =============================
            // Relationships
            // =============================

            // Box (1 - many)
            entity.HasOne(x => x.Box)
                  .WithMany(b => b.Transactions)
                  .HasForeignKey(x => x.BoxId)
                  .OnDelete(DeleteBehavior.Cascade);

            // CreatedUser (1 - many)
            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // FromSlot (optional)
            entity.HasOne<Slot>()
                  .WithMany()
                  .HasForeignKey(x => x.FromSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            // ToSlot (optional)
            entity.HasOne<Slot>()
                  .WithMany()
                  .HasForeignKey(x => x.ToSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.InventoryRequest)
              .WithMany(x => x.Transactions)
              .HasForeignKey(x => x.ReferenceRequestId)
              .OnDelete(DeleteBehavior.Restrict);
            // =============================
            // Index
            // =============================

            entity.HasIndex(x => x.BoxId);
            entity.HasIndex(x => x.TransactionType);
            entity.HasIndex(x => x.CreatedAt);
        });

        //InventoryRequest
        builder.Entity<InventoryRequest>(entity =>
        {
            entity.ToTable("InventoryRequest");

            entity.HasKey(x => x.RequestId);

            entity.Property(x => x.RequestType)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.ReferenceType)
                  .HasConversion<int>();

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .HasDefaultValue(InventoryRequestStatus.Pending);

            entity.Property(x => x.Reason)
                  .HasMaxLength(255);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ApprovedUser)
                  .WithMany()
                  .HasForeignKey(x => x.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        //Warehouse
        builder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouses");

            // Primary Key
            entity.HasKey(x => x.Id);

            // =============================
            // Properties
            // =============================

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Location)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(x => x.TitleWarehouse)
                  .HasConversion<string>()
                  .HasMaxLength(50)
                  .IsRequired();

            // =============================
            // Relationships
            // =============================

            // Warehouse - Zones (1 - many)
            entity.HasMany(x => x.Zones)
                  .WithOne(z => z.Warehouse)
                  .HasForeignKey(z => z.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Warehouse - GoodsReceipt (1 - many)
            entity.HasMany(x => x.GoodsReceipts)
                  .WithOne(r => r.Warehouse)
                  .HasForeignKey(r => r.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            // =============================
            // Index
            // =============================

            entity.HasIndex(x => x.Name);
        });

        //Zone
        builder.Entity<Zone>(entity =>
        {
            entity.ToTable("Zones");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            // Warehouse - Zone (1 - many)
            entity.HasOne(x => x.Warehouse)
                  .WithMany(w => w.Zones)
                  .HasForeignKey(x => x.WarehouseId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique Zone Name trong cùng 1 Warehouse
            entity.HasIndex(x => new { x.WarehouseId, x.Name })
                  .IsUnique();
        });

        //Rack
        builder.Entity<Rack>(entity =>
        {
            entity.ToTable("Racks");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            // Zone - Rack (1 - many)
            entity.HasOne(x => x.Zone)
                  .WithMany(z => z.Racks)
                  .HasForeignKey(x => x.ZoneId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique Rack Name trong cùng 1 Zone
            entity.HasIndex(x => new { x.ZoneId, x.Name })
                  .IsUnique();
        });

        //slot
        builder.Entity<Slot>(entity =>
        {
            entity.ToTable("Slots");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.QrCode)
                  .HasMaxLength(200);

            entity.Property(x => x.Capacity)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.CurrentCapacity)
                  .HasColumnType("decimal(18,2)");

            // Rack - Slot (1 - many)
            entity.HasOne(x => x.Rack)
                  .WithMany(r => r.Slots)
                  .HasForeignKey(x => x.RackId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Unique Code trong cùng Rack
            entity.HasIndex(x => new { x.RackId, x.Code })
                  .IsUnique();

            // Index tìm nhanh theo QR
            entity.HasIndex(x => x.QrCode);
        });

        // ===================== StockCheck =====================
        builder.Entity<StockCheck>(entity =>
        {
            entity.ToTable("StockChecks");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CheckType)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.SnapshotAt);

            entity.HasOne(x => x.Warehouse)
                  .WithMany()
                  .HasForeignKey(x => x.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ApprovedUser)
                  .WithMany()
                  .HasForeignKey(x => x.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Details)
                  .WithOne(d => d.StockCheck)
                  .HasForeignKey(d => d.StockCheckId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.WarehouseId);
            entity.HasIndex(x => x.Status);
        });

        // ===================== StockCheckDetail =====================
        builder.Entity<StockCheckDetail>(entity =>
        {
            entity.ToTable("StockCheckDetails");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SnapshotWeight)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.CurrentSystemWeight)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.CountedWeight)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.DifferenceWeight)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.VarianceType)
                  .HasConversion<int>();

            entity.HasOne(x => x.Box)
                  .WithMany()
                  .HasForeignKey(x => x.BoxId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.CountedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CountedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.StockCheckId);
            entity.HasIndex(x => x.BoxId);
        });
    }
}