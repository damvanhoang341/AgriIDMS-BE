using AgriIDMS.Domain.Entities;
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

            entity.Property(x => x.Price)
                  .HasColumnType("decimal(18,2)");

            entity.Property(x => x.Unit)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(x => x.ImageUrl)
                  .HasMaxLength(500);

            entity.Property(x => x.Status)
                  .HasConversion<int>()
                  .IsRequired();

            entity.HasOne(x => x.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(x => x.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
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

    }
}