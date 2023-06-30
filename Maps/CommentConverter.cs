public static class CommentConverter
{
  public static CommentDocument ToDocument(this CommentRecord model)
  {
    return new CommentDocument()
    {
      CommentId = model.CommentId,
      FeedId = model.FeedId,
      Body = model.Body,
      Tokens = model.Tokens,
      Type = model.Type ,
      Timestamp = model.Timestamp,
      Author = (model.Author is null) ?  AuthorDocument.Unknown : model.Author.ToDocument()
    };
  }
}