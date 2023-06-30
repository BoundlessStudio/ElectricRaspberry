public interface IAgent
{
  Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct);
}
