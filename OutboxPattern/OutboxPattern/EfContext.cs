using Microsoft.EntityFrameworkCore;

namespace OutboxPattern;

public class EfContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    public EfContext(DbContextOptions<EfContext> options) : base(options) { }
    
}