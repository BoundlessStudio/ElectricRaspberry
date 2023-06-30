public static class AuthorConverter
{
  public static AuthorRecord ToRecord(this IAuthorizedUser user)
  {
    return new AuthorRecord()
    {
      Id = user.UserId,
      IsBot = false,
      Name = user.Name,
      Mention = user.Mention,
      Picture = user.Picture,
    };
  }

  public static AuthorDocument ToDocument(this AuthorRecord record)
  {
    return new AuthorDocument()
    {
      Name = record.Name,
      Mention = record.Mention,
      Picture = record.Picture,
    };
  }
}