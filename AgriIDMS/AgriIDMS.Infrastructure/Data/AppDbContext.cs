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
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options){}
        public DbSet<RefreshToken> RefreshTokens { get; set; }
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
        }
    }
}
