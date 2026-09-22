using ManTingEats.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ManTingEats.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50);
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(10, 2);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(e => e.TableNumber).HasMaxLength(20);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.VoidReason).HasMaxLength(200);

            entity.HasOne(e => e.CreatedByEmployee)
                .WithMany(e => e.CreatedOrders)
                .HasForeignKey(e => e.CreatedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.VoidedByEmployee)
                .WithMany()
                .HasForeignKey(e => e.VoidedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.Note).HasMaxLength(100);

            entity.HasOne(e => e.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.MenuItem)
                .WithMany(m => m.OrderItems)
                .HasForeignKey(e => e.MenuItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.Note).HasMaxLength(200);
        });
    }
}
