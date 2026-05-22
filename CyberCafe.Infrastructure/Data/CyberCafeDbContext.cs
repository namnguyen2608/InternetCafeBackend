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
    }
}
