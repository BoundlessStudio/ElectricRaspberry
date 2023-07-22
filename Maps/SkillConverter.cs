public static class SkillConverter
{
  public static SkillDocument ToDocument(this SkillRecord record)
  {
    return new SkillDocument()
    {
      SkillId = record.SkillId,
      Name = record.Name,
      Owner = record.Owner,
      Description = record.Description,
      Prompt = record.Prompt,
      Type = record.Type,
      TypeOf = record.TypeOf,
      Url = record.Url
    };
  }
}