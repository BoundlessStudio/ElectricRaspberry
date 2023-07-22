using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Skills.Core;
using Microsoft.SemanticKernel.Skills.MsGraph;
using Microsoft.SemanticKernel.Skills.Web;

public class FeedsContext : DbContext
{
  public FeedsContext(DbContextOptions<FeedsContext> options) : base(options) { }

  public DbSet<FeedRecord> Feeds => Set<FeedRecord>();
  public DbSet<AuthorRecord> Authors => Set<AuthorRecord>();
  public DbSet<CommentRecord> Comments => Set<CommentRecord>();
  public DbSet<SkillRecord> Skills => Set<SkillRecord>();

  override protected void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<FeedRecord>().ToTable("Feeds").HasKey(_ => _.FeedId);
    modelBuilder.Entity<FeedRecord>().Property(e => e.Access).HasConversion<string>();
    modelBuilder.Entity<FeedRecord>().HasMany(e => e.Skills);
    modelBuilder.Entity<CommentRecord>().ToTable("Comments").HasKey(_ => _.CommentId);
    modelBuilder.Entity<CommentRecord>().OwnsOne(c => c.Author);
    modelBuilder.Entity<SkillRecord>().ToTable("Skills").HasKey(_ => _.SkillId);

    modelBuilder.Entity<SkillRecord>().HasData(
      new SkillRecord() { Type = SkillType.Coded, SkillId = "16b59f4c263b40de97839143aea7ed0c", Name = "System.Time", TypeOf = nameof(TimeSkill), Owner = SkillRecord.SystemOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "eb26a105368142ddb04c74985335db4b", Name = "System.Math", TypeOf = nameof(LanguageCalculatorSkill), Owner = SkillRecord.SystemOwner  },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "9534e5423a2f4c0c82016b29f7c6f157", Name = "System.Memory", TypeOf = nameof(TextMemorySkill), Owner = SkillRecord.SystemOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "6878d0bb9c96400ea3ade85bf54ec362", Name = "System.Wait", TypeOf = nameof(WaitSkill), Owner = SkillRecord.SystemOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "59b8462108a84584a4820308aa2f89e7", Name = "Bing.Search", TypeOf = nameof(WebSearchEngineSkill), Owner = SkillRecord.SystemOwner }, // Switch to SkillRecord.MicrosoftOwner when Adding Google?
      new SkillRecord() { Type = SkillType.Coded, SkillId = "c3293570a98b40588739db905c10baf3", Name = "Microsoft.Calendar", TypeOf = nameof(CalendarSkill), Owner = SkillRecord.MicrosoftOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "73a7096a4f8040ea9f7b82d507be0cf2", Name = "Microsoft.Drive", TypeOf = nameof(CloudDriveSkill), Owner = SkillRecord.MicrosoftOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "d2321d025d75495da5cddcae4b2d37bc", Name = "Microsoft.Email", TypeOf = nameof(EmailSkill), Owner = SkillRecord.MicrosoftOwner },
      new SkillRecord() { Type = SkillType.Coded, SkillId = "c04d24b7b5704628b7287955acfbfa9c", Name = "Microsoft.Tasks", TypeOf = nameof(TaskListSkill), Owner = SkillRecord.MicrosoftOwner }
    );
  }
}