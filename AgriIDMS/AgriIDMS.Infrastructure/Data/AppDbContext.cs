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
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<OrderAllocation> OrderAllocations => Set<OrderAllocation>();
    public DbSet<ExportReceipt> ExportReceipts => Set<ExportReceipt>();
    public DbSet<ExportDetail> ExportDetails => Set<ExportDetail>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
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

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(x => x.Description)
                  .HasMaxLength(1000);

            entity.Property(x => x.IsActive)
                  .HasDefaultValue(true);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Category (1 - N)
            entity.HasOne(x => x.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Product - Variants (1 - N)
            entity.HasMany(x => x.Variants)
                  .WithOne(v => v.Product)
                  .HasForeignKey(v => v.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.CategoryId);
        });

        // ===================== ProductVariant =====================
        builder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("ProductVariants");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Grade)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.Price)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.IsActive)
                  .HasDefaultValue(true);

            // Product (1 - N)
            entity.HasOne(x => x.Product)
                  .WithMany(p => p.Variants)
                  .HasForeignKey(x => x.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Variant - OrderDetail
            entity.HasMany(x => x.OrderDetails)
                  .WithOne(o => o.ProductVariant)
                  .HasForeignKey(o => o.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique: 1 Product chỉ có 1 Grade A
            entity.HasIndex(x => new { x.ProductId, x.Grade })
                  .IsUnique();
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

        // ============================== GOODS RECEIPT ==============================
        builder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("GoodsReceipts");

            entity.HasKey(x => x.Id);

            // ================= STATUS =================
            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();

            // ================= TRANSPORT =================
            entity.Property(x => x.VehicleNumber)
                  .HasMaxLength(20)
                  .IsRequired();

            entity.Property(x => x.DriverName)
                  .HasMaxLength(100);

            entity.Property(x => x.TransportCompany)
                  .HasMaxLength(150);

            // ================= WEIGHT =================
            entity.Property(x => x.GrossWeight)
                  .HasPrecision(18, 2);

            entity.Property(x => x.TareWeight)
                  .HasPrecision(18, 2);

            entity.Property(x => x.TotalEstimatedQuantity)
                  .HasPrecision(18, 2);

            entity.Property(x => x.TotalActualQuantity)
                  .HasPrecision(18, 2);

            // ================= DATES =================
            entity.Property(x => x.ReceivedDate)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.ApprovedAt);

            // ================= RELATIONSHIPS =================

            // Supplier (1 - N)
            entity.HasOne(x => x.Supplier)
                  .WithMany(s => s.GoodsReceipts)
                  .HasForeignKey(x => x.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Warehouse (1 - N)
            entity.HasOne(x => x.Warehouse)
                  .WithMany(w => w.GoodsReceipts)
                  .HasForeignKey(x => x.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);

            // CreatedUser
            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // ApprovedUser
            entity.HasOne(x => x.ApprovedUser)
                  .WithMany()
                  .HasForeignKey(x => x.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // GoodsReceipt (1 - N) GoodsReceiptDetail
            entity.HasMany(x => x.Details)
                  .WithOne(d => d.GoodsReceipt)
                  .HasForeignKey(d => d.GoodsReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ================= INDEX =================

            entity.HasIndex(x => x.SupplierId);
            entity.HasIndex(x => x.WarehouseId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.ReceivedDate);

            entity.HasIndex(x => new { x.WarehouseId, x.Status });
        });

        // ===================== GoodsReceiptDetail =====================
        builder.Entity<GoodsReceiptDetail>(entity =>
        {
            entity.ToTable("GoodsReceiptDetails");

            entity.HasKey(x => x.Id);

            // ================= QUANTITY =================

            entity.Property(x => x.EstimatedQuantity)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(x => x.ActualQuantity)
                  .HasPrecision(18, 2);

            entity.Property(x => x.UnitPrice)
                  .HasPrecision(18, 2)
                  .IsRequired();

            // ================= QC =================

            entity.Property(x => x.QCResult)
                  .HasConversion<int>()
                  .HasDefaultValue(QCResult.Pending)
                  .IsRequired();

            entity.Property(x => x.QCNote)
                  .HasMaxLength(500);

            entity.Property(x => x.InspectedBy)
                  .HasMaxLength(450);

            entity.Property(x => x.InspectedAt);

            // ================= RELATIONSHIPS =================

            // GoodsReceipt (1 - N)
            entity.HasOne(x => x.GoodsReceipt)
                  .WithMany(gr => gr.Details)
                  .HasForeignKey(x => x.GoodsReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Xoá phiếu => xoá chi tiết

            // ProductVariant (1 - N)
            entity.HasOne(x => x.ProductVariant)
                  .WithMany(pv => pv.GoodsReceiptDetails)
                  .HasForeignKey(x => x.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);
            // Không cho xoá Variant nếu đã nhập kho

            // InspectedUser
            entity.HasOne(x => x.InspectedUser)
                  .WithMany()
                  .HasForeignKey(x => x.InspectedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // Detail - Lot (1 - N)
            entity.HasMany(x => x.Lots)
                  .WithOne(l => l.GoodsReceiptDetail)
                  .HasForeignKey(l => l.GoodsReceiptDetailId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ================= INDEX =================

            entity.HasIndex(x => x.GoodsReceiptId);

            entity.HasIndex(x => x.ProductVariantId);

            // 1 phiếu không nên có trùng cùng 1 Variant
            entity.HasIndex(x => new { x.GoodsReceiptId, x.ProductVariantId })
                  .IsUnique();
        });

        // ============================== LOT ==============================
        builder.Entity<Lot>(entity =>
        {
            entity.ToTable("Lots");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.LotCode)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(x => x.LotCode)
                  .IsUnique();

            entity.Property(x => x.TotalQuantity)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(x => x.RemainingQuantity)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<int>() // lưu enum dạng int
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.ExpiryDate)
                  .IsRequired();

            entity.Property(x => x.ReceivedDate)
                  .IsRequired();

            // ================= RELATIONSHIPS =================

            // GoodsReceiptDetail (1 - N)
            entity.HasOne(x => x.GoodsReceiptDetail)
                  .WithMany(d => d.Lots)
                  .HasForeignKey(x => x.GoodsReceiptDetailId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Xóa detail → xóa luôn lot (đúng logic phiếu nhập nháp)

            // Box (1 - N)
            entity.HasMany(x => x.Boxes)
                  .WithOne(b => b.Lot)
                  .HasForeignKey(b => b.LotId)
                  .OnDelete(DeleteBehavior.Cascade);

            // ================= INDEX =================

            // Tối ưu query theo receipt detail
            entity.HasIndex(x => x.GoodsReceiptDetailId);

            // Tối ưu xuất kho FEFO (lọc theo status + hạn)
            entity.HasIndex(x => new { x.Status, x.ExpiryDate });

            // ================= CONSTRAINT =================

            entity.HasCheckConstraint(
                "CK_Lot_RemainingQuantity",
                "[RemainingQuantity] >= 0 AND [RemainingQuantity] <= [TotalQuantity]"
            );
        });

        //============================= box =============================
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

            entity.HasIndex(x => x.LotId);
            entity.HasIndex(x => x.SlotId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => new { x.LotId, x.Status, x.CreatedAt });
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


            entity.HasMany(x => x.GoodsReceipts)
                  .WithOne(r => r.Supplier)
                  .HasForeignKey(r => r.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.Status);
        });

        // ===================== InventoryTransaction =====================
        builder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("InventoryTransactions");

            entity.HasKey(x => x.Id);

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

            entity.HasIndex(x => x.BoxId);
            entity.HasIndex(x => x.TransactionType);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => new { x.ReferenceType, x.ReferenceRequestId });
        });

        //InventoryRequest
        builder.Entity<InventoryRequest>(entity =>
        {
            entity.ToTable("InventoryRequest");

            entity.HasKey(x => x.Id);

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

        //===================== Slot =====================
        builder.Entity<Slot>(entity =>
        {
            entity.ToTable("Slots");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(x => x.CurrentCapacity)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0);

            entity.Property(x => x.QrCode)
                  .HasMaxLength(200);

            entity.Property(x => x.Capacity)
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

        // ===================== Cart =====================
        builder.Entity<Cart>(entity =>
        {
            entity.ToTable("Carts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.UpdatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Mỗi user chỉ có 1 cart
            entity.HasIndex(x => x.UserId)
                  .IsUnique();

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.Items)
                  .WithOne(i => i.Cart)
                  .HasForeignKey(i => i.CartId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===================== CartItem =====================
        builder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.UnitPrice)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            // 1 sản phẩm chỉ xuất hiện 1 lần trong 1 cart
            entity.HasIndex(x => new { x.CartId, x.ProductVariantId })
                  .IsUnique();

            entity.HasOne(x => x.Cart)
                  .WithMany(c => c.Items)
                  .HasForeignKey(x => x.CartId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ProductVariant)
                  .WithMany()
                  .HasForeignKey(x => x.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

        });

        // ===================== Order =====================
        builder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.TotalAmount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne<ApplicationUser>()
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(x => x.Details)
                  .WithOne(d => d.Order)
                  .HasForeignKey(d => d.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.Status);
        });

        // ===================== OrderDetail =====================
        builder.Entity<OrderDetail>(entity =>
        {
            entity.ToTable("OrderDetails");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Quantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.UnitPrice)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();
            entity.Property(x => x.FulfilledQuantity)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.ShortageQuantity)
                  .HasColumnType("decimal(18,2)");

            entity.HasOne(x => x.Order)
                  .WithMany(o => o.Details)
                  .HasForeignKey(x => x.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ProductVariant)
                  .WithMany(v => v.OrderDetails)
                  .HasForeignKey(x => x.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.ProductVariantId);
        });

        // ===================== Payment =====================
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.PaymentStatus)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.TransactionCode)
                  .HasMaxLength(100);

            entity.Property(x => x.Amount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.PaidAt)
                  .IsRequired(false);

            entity.Property(x => x.CreatedAt)
                  .IsRequired();

            entity.HasOne(x => x.Order)
                  .WithMany(o => o.Payments)
                  .HasForeignKey(x => x.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.TransactionCode)
                  .IsUnique()
                  .HasFilter("[TransactionCode] IS NOT NULL");
        });

        // ===================== Refund =====================
        builder.Entity<Refund>(entity =>
        {
            entity.ToTable("Refunds");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Amount)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.RefundTransactionCode)
                  .HasMaxLength(100);

            entity.Property(x => x.CreatedAt)
                  .IsRequired();

            entity.Property(x => x.CompletedAt)
                  .IsRequired(false);

            entity.HasOne(x => x.Payment)
                  .WithMany(p => p.Refunds)
                  .HasForeignKey(x => x.PaymentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Complaint)
                  .WithMany(c => c.Refunds)
                  .HasForeignKey(x => x.ComplaintId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===================== OrderAllocation =====================
        builder.Entity<OrderAllocation>(entity =>
        {
            entity.ToTable("OrderAllocations");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ReservedQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.PickedQuantity)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.ReservedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(x => x.ExpiredAt)
                  .IsRequired(false);

            entity.HasCheckConstraint(
                "CK_OrderAllocation_ReservedQty_Positive",
                "[ReservedQuantity] > 0");

            entity.HasCheckConstraint(
                "CK_OrderAllocation_PickedQty_Valid",
                "[PickedQuantity] IS NULL OR [PickedQuantity] >= 0");

            // Order (1 - many)
            entity.HasOne(x => x.Order)
                  .WithMany(o => o.Allocations)
                  .HasForeignKey(x => x.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // OrderDetail (1 - many)
            entity.HasOne(x => x.OrderDetail)
                  .WithMany(o => o.Allocations)
                  .HasForeignKey(x => x.OrderDetailId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Box (1 - many)
            entity.HasOne(x => x.Box)
                  .WithMany(o => o.Allocations)
                  .HasForeignKey(x => x.BoxId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Query theo Order
            entity.HasIndex(x => x.OrderId);

            // Query theo Box (kiểm tra box đã reserve chưa)
            entity.HasIndex(x => x.BoxId);

            // Query theo trạng thái
            entity.HasIndex(x => x.Status);

            // Ngăn 1 box được reserve 2 lần cùng 1 order detail
            entity.HasIndex(x => new { x.OrderDetailId, x.BoxId })
                  .IsUnique();
        });

        // ===================== ExportReceipt =====================
        builder.Entity<ExportReceipt>(entity =>
        {
            entity.ToTable("ExportReceipts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ExportCode)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(x => x.ExportCode)
                  .IsUnique();

            entity.Property(x => x.Status)
                  .HasConversion<string>()
                  .HasMaxLength(30)
                  .IsRequired();

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Order (1 - many ExportReceipt nếu cho phép partial shipment)
            entity.HasOne(x => x.Order)
                  .WithMany()
                  .HasForeignKey(x => x.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // CreatedUser
            entity.HasOne(x => x.CreatedUser)
                  .WithMany()
                  .HasForeignKey(x => x.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // Details (1 - many)
            entity.HasMany(x => x.Details)
                  .WithOne(d => d.ExportReceipt)
                  .HasForeignKey(d => d.ExportReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CreatedAt);
        });

        // ===================== ExportDetail =====================
        builder.Entity<ExportDetail>(entity =>
        {
            entity.ToTable("ExportDetails");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.ActualQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.HasCheckConstraint(
                "CK_ExportDetail_ActualQty_Positive",
                "[ActualQuantity] > 0");

            // ExportReceipt (1 - many)
            entity.HasOne(x => x.ExportReceipt)
                  .WithMany(r => r.Details)
                  .HasForeignKey(x => x.ExportReceiptId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Box (1 - many)
            entity.HasOne(x => x.Box)
                  .WithMany()
                  .HasForeignKey(x => x.BoxId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ExportReceiptId);

            entity.HasIndex(x => x.BoxId);

            // Không cho 1 box xuất 2 lần trong cùng 1 phiếu
            entity.HasIndex(x => new { x.ExportReceiptId, x.BoxId })
                  .IsUnique();
        });

        // ============================= Complaint =============================
        builder.Entity<Complaint>(entity =>
        {
            entity.ToTable("Complaints");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Type)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();

            entity.Property(x => x.DamagedQuantity)
                  .HasColumnType("decimal(18,2)")
                  .IsRequired();

            entity.Property(x => x.Description)
                  .HasMaxLength(500);

            entity.Property(x => x.CustomerEvidenceUrl)
                  .HasMaxLength(500);

            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Order
            entity.HasOne(x => x.Order)
                  .WithMany()
                  .HasForeignKey(x => x.OrderId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Box
            entity.HasOne(x => x.Box)
                  .WithMany()
                  .HasForeignKey(x => x.BoxId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Verified user
            entity.HasOne(x => x.VerifiedUser)
                  .WithMany()
                  .HasForeignKey(x => x.VerifiedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.OrderId);
            entity.HasIndex(x => x.BoxId);
            entity.HasIndex(x => x.Status);
        });

        // ============================= Review =============================
        builder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");

            entity.HasKey(x => x.Id);

            // Rating
            entity.Property(x => x.Rating)
                  .IsRequired();

            // Giới hạn rating 1-5 (DB level constraint)
            entity.HasCheckConstraint("CK_Review_Rating", "[Rating] >= 1 AND [Rating] <= 5");

            // Comment
            entity.Property(x => x.Comment)
                  .HasMaxLength(1000);

            // IsApproved
            entity.Property(x => x.IsApproved)
                  .HasDefaultValue(false);

            // CreatedAt
            entity.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // ===== 1-1: Review - OrderDetail =====
            entity.HasOne(x => x.OrderDetail)
                  .WithOne(od => od.Review)
                  .HasForeignKey<Review>(x => x.OrderDetailId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.OrderDetailId)
                  .IsUnique();

            // ===== N-1: Review - ProductVariant =====
            entity.HasOne(x => x.ProductVariant)
                  .WithMany(v => v.Reviews)
                  .HasForeignKey(x => x.ProductVariantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.ProductVariantId);

            // Optional: index cho admin filter
            entity.HasIndex(x => x.IsApproved);
        });
    }
}