public class PromptModel
{
  public PromptModel(IAuthorizedUser User, CommentRecord Model)
  {
    this.User = User;
    this.Model = Model;
  }

  public IAuthorizedUser User {get; set;}
  public CommentRecord Model {get; set;}

  public void Deconstruct(out IAuthorizedUser user, out CommentRecord model)
  {
    user = this.User;
    model = this.Model;
  }
}