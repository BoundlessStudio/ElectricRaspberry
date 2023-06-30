public static class FeedConverter
{
  public static FeedDocument ToDocument(this FeedRecord model)
  {
    return new FeedDocument()
    {
      FeedId = model.FeedId ?? string.Empty,
      Name = model.Name ?? string.Empty,
      Description = model.Description  ?? string.Empty,
      Access = model.Access,
    };
  }
}