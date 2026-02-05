using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AgriIDMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Token)
                      .IsRequired()
                      .HasMaxLength(500); // rất quan trọng

                entity.Property(x => x.UserId)
                      .IsRequired()
                      .HasMaxLength(450); // IdentityUser.Id

                entity.HasOne(x => x.User)
                      .WithMany()
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

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
                entity.HasMany(x => x.Products)
                      .WithOne(p => p.Category)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
