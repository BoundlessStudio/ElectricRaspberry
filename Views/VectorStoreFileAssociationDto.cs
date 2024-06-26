public class VectorStoreFileAssociationDto
{
  public VectorStoreFileAssociationDto()
  {
    this.Files = new List<string>();
  }

  public List<string> Files { get; set; }
}