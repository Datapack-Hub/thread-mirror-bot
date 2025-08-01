using Microsoft.EntityFrameworkCore;

public class DphContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<MessageEntity> Messages { get; set; }
    public DbSet<ThreadEntity> Threads { get; set; }

    public string DbPath { get; set; }

    public DphContext(DbContextOptions<DphContext> options) : base(options) {}
}