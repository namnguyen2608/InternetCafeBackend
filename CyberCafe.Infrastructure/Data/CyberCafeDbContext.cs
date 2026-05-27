using CyberCafe.Core.Entities;
using CyberCafe.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace CyberCafe.Infrastructure.Data;

public class CyberCafeDbContext : DbContext
{
    public CyberCafeDbContext(DbContextOptions<CyberCafeDbContext> options)
        : base(options) { }

    // ── DbSets ──────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Computer> Computers => Set<Computer>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<FoodOrder> FoodOrders => Set<FoodOrder>();
    public DbSet<FoodOrderItem> FoodOrderItems => Set<FoodOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.HasIndex(u => u.Username)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.Role)
                  .HasConversion<string>()       // Store as "Admin", "Staff", "Customer"
                  .HasMaxLength(20);
        });

        // ── Wallet ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.HasKey(w => w.Id);

            entity.Property(w => w.Balance)
                  .HasColumnType("decimal(18,2)")
                  .HasDefaultValue(0m);

            // 1-to-1: User → Wallet
            entity.HasOne(w => w.User)
                  .WithOne(u => u.Wallet)
                  .HasForeignKey<Wallet>(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Zone ─────────────────────────────────────────────────────────────
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.HasKey(z => z.Id);

            entity.Property(z => z.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(z => z.PricePerHour)
                  .HasColumnType("decimal(18,2)");

            // Seed data
            entity.HasData(
                new Zone { Id = 1, Name = "Standard", PricePerHour = 10_000m },
                new Zone { Id = 2, Name = "VIP",      PricePerHour = 25_000m }
            );
        });

        // ── Computer ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Computer>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(c => c.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(c => c.Specs)
                  .HasMaxLength(500);

            // Optimistic concurrency token
            entity.Property(c => c.RowVersion)
                  .IsRowVersion();

            // Many-to-one: Computer → Zone
            entity.HasOne(c => c.Zone)
                  .WithMany(z => z.Computers)
                  .HasForeignKey(c => c.ZoneId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── GameSession ──────────────────────────────────────────────────────
        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(gs => gs.Id);

            entity.Property(gs => gs.StartTime)
                  .IsRequired();

            // EndTime is nullable — null means the session is still active
            entity.Property(gs => gs.EndTime)
                  .IsRequired(false);

            // Many-to-one: GameSession → User
            entity.HasOne(gs => gs.User)
                  .WithMany(u => u.GameSessions)
                  .HasForeignKey(gs => gs.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: GameSession → Computer
            entity.HasOne(gs => gs.Computer)
                  .WithMany(c => c.GameSessions)
                  .HasForeignKey(gs => gs.ComputerId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Transaction ──────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Amount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(t => t.Type)
                  .HasConversion<string>()
                  .HasMaxLength(10);

            entity.Property(t => t.Date)
                  .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(t => t.Description)
                  .HasMaxLength(255)
                  .IsRequired(false);

            // Many-to-one: Transaction → Wallet
            entity.HasOne(t => t.Wallet)
                  .WithMany(w => w.Transactions)
                  .HasForeignKey(t => t.WalletId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FoodItem ─────────────────────────────────────────────────────────
        modelBuilder.Entity<FoodItem>(entity =>
        {
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(f => f.Description)
                  .HasMaxLength(500)
                  .IsRequired(false);

            entity.Property(f => f.Price)
                  .HasColumnType("decimal(18,2)");

            entity.Property(f => f.Category)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(f => f.ImageUrl)
                  .HasMaxLength(500)
                  .IsRequired(false);

            // Seed data — sample menu
            entity.HasData(
                new FoodItem { Id = 1, Name = "Mì tôm Hảo Hảo",     Description = "Mì ăn liền vị tôm chua cay",  Price = 15_000m, Category = "Đồ ăn",   IsAvailable = true },
                new FoodItem { Id = 2, Name = "Bánh mì phô mai",      Description = "Bánh mì nướng phô mai bơ",    Price = 25_000m, Category = "Đồ ăn",   IsAvailable = true },
                new FoodItem { Id = 3, Name = "Xúc xích chiên",       Description = "Xúc xích chiên giòn",         Price = 20_000m, Category = "Đồ ăn",   IsAvailable = true },
                new FoodItem { Id = 4, Name = "Pepsi lon",            Description = "Nước ngọt Pepsi 330ml",       Price = 15_000m, Category = "Nước uống", IsAvailable = true },
                new FoodItem { Id = 5, Name = "Trà sữa trân châu",    Description = "Trà sữa trân châu đen 500ml", Price = 35_000m, Category = "Nước uống", IsAvailable = true },
                new FoodItem { Id = 6, Name = "Snack Oishi tôm chua", Description = "Snack tôm chua cay 40g",      Price = 10_000m, Category = "Snack",    IsAvailable = true }
            );
        });

        // ── FoodOrder ────────────────────────────────────────────────────────
        modelBuilder.Entity<FoodOrder>(entity =>
        {
            entity.HasKey(fo => fo.Id);

            entity.Property(fo => fo.TotalAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(fo => fo.OrderTime)
                  .IsRequired();

            entity.Property(fo => fo.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            // Many-to-one: FoodOrder → User
            entity.HasOne(fo => fo.User)
                  .WithMany()
                  .HasForeignKey(fo => fo.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── FoodOrderItem ────────────────────────────────────────────────────
        modelBuilder.Entity<FoodOrderItem>(entity =>
        {
            entity.HasKey(foi => foi.Id);

            entity.Property(foi => foi.UnitPrice)
                  .HasColumnType("decimal(18,2)");

            // Many-to-one: FoodOrderItem → FoodOrder
            entity.HasOne(foi => foi.FoodOrder)
                  .WithMany(fo => fo.FoodOrderItems)
                  .HasForeignKey(foi => foi.FoodOrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one: FoodOrderItem → FoodItem
            entity.HasOne(foi => foi.FoodItem)
                  .WithMany(fi => fi.FoodOrderItems)
                  .HasForeignKey(foi => foi.FoodItemId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
