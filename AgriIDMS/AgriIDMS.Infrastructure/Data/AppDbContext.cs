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

            builder.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens");
                e.HasIndex(x => x.Token).IsUnique();
                e.Property(x => x.Token).HasMaxLength(500).IsRequired();
                e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            });
        }
    }
}
