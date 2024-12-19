using Microsoft.EntityFrameworkCore;

namespace MeercatMonitor.Storage;

internal class StorageContext : DbContext
{
    public DbSet<Target> Target { get; set; }
    public DbSet<OnlineStatus> OnlineStatus { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=storage.sqlite");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>()
            .HasKey(x => x.TargetId);
        modelBuilder.Entity<Target>()
            .Property(r => r.TargetId)
            .ValueGeneratedOnAdd()
            .IsRequired();

        modelBuilder.Entity<OnlineStatus>()
            .HasKey(x => x.OnlineStatusId);
        modelBuilder.Entity<OnlineStatus>()
            .Property(x => x.TargetId)
            .IsRequired();
        modelBuilder.Entity<OnlineStatus>()
            .HasOne(x => x.Target)
            .WithMany(x => x.OnlineStatus)
            .HasForeignKey(x => x.TargetId);
    }
}
