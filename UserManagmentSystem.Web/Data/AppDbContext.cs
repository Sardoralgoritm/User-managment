using Microsoft.EntityFrameworkCore;
using UserManagmentSystem.Web.Models.Entities;

namespace UserManagmentSystem.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(en =>
        {
            en.HasKey(e => e.Id);
            en.HasIndex(e => e.Email)
            .IsUnique();
        });
        base.OnModelCreating(modelBuilder);
    }
}
