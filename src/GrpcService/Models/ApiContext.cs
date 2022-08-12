using Microsoft.EntityFrameworkCore;

namespace GrpcService.Models
{
    public class ApiContext : DbContext
    {
        public ApiContext(DbContextOptions<ApiContext> options)
            : base(options)
        {
        }

        public virtual DbSet<User> Users { get; set; } = null!;

        public virtual DbSet<Coin> Coins { get; set; } = null!;

        public virtual DbSet<CoinTransaction> Transactions { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasData(
                    new User { Id = 1, Name = "boris", Rating = 5000 },
                    new User { Id = 2, Name = "maria", Rating = 1000 },
                    new User { Id = 3, Name = "oleg", Rating = 800 }
                );
        }
    }
}
