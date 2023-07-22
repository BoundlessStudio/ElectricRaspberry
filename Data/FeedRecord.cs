
public class FeedRecord 
{
  public string FeedId { get; set; } = Guid.NewGuid().ToString();
  public string OwnerId { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Description  { get; set; } = string.Empty;
  public FeedAccess Access { get; set; } = FeedAccess.Private;
  public ICollection<SkillRecord> Skills { get; set; } = new List<SkillRecord>();
}

public enum FeedAccess : int
{
  Private = 0,
  Public = 1,
}