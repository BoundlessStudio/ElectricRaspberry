using Microsoft.EntityFrameworkCore;

public class FeedsContext : DbContext
{
    public FeedsContext(DbContextOptions<FeedsContext> options) : base(options) { }

    public DbSet<FeedRecord> Feeds => Set<FeedRecord>();
    public DbSet<AuthorRecord> Authors => Set<AuthorRecord>();
    public DbSet<CommentRecord> Comments => Set<CommentRecord>();

    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<FeedRecord>()
        .HasNoDiscriminator()
        .ToTable("Feeds")
        .HasKey(_ => _.FeedId);

       modelBuilder.Entity<FeedRecord>().Property(e => e.Access).HasConversion(
        v => v.ToString(),
        v => (FeedAccess)Enum.Parse(typeof(FeedAccess), v)
      );

      modelBuilder.Entity<CommentRecord>()
        .HasNoDiscriminator()
        .ToTable("Comments")
        .HasKey(_ => _.CommentId);

      modelBuilder.Entity<CommentRecord>().OwnsOne(c => c.Author);
    }
}