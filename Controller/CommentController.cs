using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.TextEmbedding;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

public class CommentController
{
  
  // TODO:
  // extract mentions and intents
  // If found run AI(s) =>
  // @george:
  // Extract comments for history (limited context window)
  // add memories based on prompt (limited context window)
  // add AI personality
  // add user info
  // add prompt
  // @dalle:
  // Prompt => Goal = (Drawing Skills -> Run Plan -> Add new comment with results)
  // @jeeves:
  // Store and retrieve memories: all memories are stored in the same collect id as feed id
  // @mary:
  // Setup intergrations: url to connection page based on selected provider => (ms, google, github, notion, etc.)
  // @tessla:
  // Prompt = Goal => Plan + Modifications
  // @shuri:
  // respond with away on holidays

  public async Task<Results<Ok<CommentDocument>, NotFound<string>>> Create(ClaimsPrincipal principal, ISecurityService security, ICommentTaskQueue queue, FeedsContext context, [FromBody]CommentCreateDocument dto)
  {
    var user = security.GetUser(principal);
    var feed = await context.Feeds.FindAsync(dto.FeedId);
    if (feed is null) return TypedResults.NotFound($"Feed {dto.FeedId} dose not exist");
    
    var model = new CommentRecord()
    {
        CommentId = Guid.NewGuid().ToString(),
        FeedId = feed.FeedId,
        Body = dto.Body,
        Type = dto.Type,
        Timestamp = DateTimeOffset.UtcNow,
        Tokens = GPT3Tokenizer.Encode(dto.Body).Count,
        Characters = dto.Body.Length,
        Author = user.ToRecord(),
    };
    await context.Comments.AddAsync(model);
    await context.SaveChangesAsync();

    var prompt = new PromptModel(user, model);
    await queue.QueueAsync(prompt);

    var document = model.ToDocument();
    return TypedResults.Ok(document);
  }

  public async Task<Results<Ok<IEnumerable<CommentDocument>>, NotFound>> List(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromRoute]string feed_id)
  {
    var user = security.GetUser(principal);
    var feed = await context.Feeds.FindAsync(feed_id);
    if(feed is null) return TypedResults.NotFound();
    if(feed.Access == FeedAccess.Private && feed.OwnerId != user.UserId) return TypedResults.NotFound();
    var query = await context.Comments.Where(_ => _.FeedId == feed_id).OrderBy(_ => _.Timestamp).ToListAsync();
    var collection = query .Select(_ => _.ToDocument()).AsEnumerable();
    return TypedResults.Ok(collection);
  }

  public async Task<Results<NoContent, NotFound>> Delete(ClaimsPrincipal principal, ISecurityService security, FeedsContext context, [FromRoute]string comment_id)
  {
    var user = security.GetUser(principal);
    var model = await context.Comments.FindAsync(comment_id);
    if(model is null) return TypedResults.NotFound();
     var feed = await context.Feeds.FindAsync(model.FeedId);
    if(feed is null) return TypedResults.NotFound();
    if(feed is null) return TypedResults.NotFound();
    if(feed.Access == FeedAccess.Private && feed.OwnerId != user.UserId) return TypedResults.NotFound();
    if(model.Author is null) return TypedResults.NotFound();
    if(feed.Access == FeedAccess.Public && model.Author.Id != user.UserId) return TypedResults.NotFound();
    context.Comments.Remove(model);
    await context.SaveChangesAsync();
    return TypedResults.NoContent();
  }
}