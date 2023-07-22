public class CommentRecord 
{
  public string CommentId { get; set; } = Guid.NewGuid().ToString();
  public string Type { get; set; } = string.Empty;
  public string Body { get; set; } = string.Empty;
  public int Tokens { get; set; } = 0;
  public double Relevance { get; set; } = 0;
  public string FeedId { get; set; } = string.Empty;
  public virtual AuthorRecord Author { get; set; } = new AuthorRecord();
  public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public class AuthorRecord 
{
  public string Id { get; set; } = string.Empty;
  public bool IsBot { get; set; } = false;
  public string Name { get; set; }= string.Empty;
  public string Picture { get; set; } = string.Empty;
  public string Mention { get; set;}  = string.Empty;
}