using Microsoft.EntityFrameworkCore;
using TicketingService.Models;

namespace TicketingService.DAL
{
    public class TicketingDbContext : DbContext
    {
        public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
        {
        }

        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<Gift> Gifts => Set<Gift>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Gift>(entity =>
            {
                entity.ToTable("Gifts");
                entity.HasKey(g => g.Id);
                entity.Property(g => g.Name).IsRequired().HasMaxLength(50);
                entity.Property(g => g.Description).IsRequired();
                entity.Property(g => g.Picture).IsRequired();
                entity.Property(g => g.Price).IsRequired();
            });

            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Carts");
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Quantity).IsRequired().HasDefaultValue(1);
                entity.Property(c => c.IsPurchased).HasDefaultValue(false);
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(c => c.Gift)
                    .WithMany()
                    .HasForeignKey(c => c.GiftID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(c => new { c.UserID, c.IsPurchased });
                entity.HasIndex(c => new { c.GiftID, c.IsPurchased });
                entity.HasIndex(c => new { c.UserID, c.GiftID, c.IsPurchased })
                    .IsUnique()
                    .HasFilter("[IsPurchased] = 0");
            });
        }
    }
}
