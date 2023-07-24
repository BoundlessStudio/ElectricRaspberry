using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.EntityFrameworkCore;

public class CharlesAgent : IAgent
{
  private readonly ILogger<CharlesAgent> logger;
  private readonly IHubContext<FeedHub> hub;
  private readonly FeedsContext context;
  private readonly IChatCompletion chatProvider;
  private readonly ChatRequestSettings settings;

   public CharlesAgent(
    IOptions<OpenAIOptions> aiOptions,
    ILogger<CharlesAgent> logger,
    IHubContext<FeedHub> hub,
    FeedsContext context
  )
  {
    this.logger = logger;
    this.context = context;
    this.hub = hub;
    this.context = context;

    this.chatProvider = new OpenAIChatCompletion("gpt-4", aiOptions.Value.ApiKey, aiOptions.Value.OrgId);
    this.settings = new ChatRequestSettings()
    {
      MaxTokens = 4000,
      TopP = 1,
      Temperature = 0.7,
      FrequencyPenalty = 0,
      PresencePenalty = 0,
      ResultsPerPrompt = 1,
    };
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct)
  {
    var placeholder = await AddPlaceholder(comment.FeedId, ct);
    var history = await GetHistory(comment.FeedId, ct);
    var memories = new List<CommentRecord>();
    var result = await GetChatResult(history, memories, ct);
    await UpdateResponse(placeholder, result, ct);
  }

  
  private async Task<IList<CommentRecord>> GetHistory(string feedId, CancellationToken ct)
  {
    var comments = await this.context.Comments
      .Where(c => c.FeedId == feedId)
      .Where(_ => _.Type == "comment")
      .OrderByDescending(_ => _.Timestamp)
      .Take(1000)
      .ToListAsync(ct);

      var sum = 0;
      var shortTerm = new List<CommentRecord>();
      foreach (var item in comments)
      {
        sum+=item.Tokens;
        if(sum < 2000)
          shortTerm.Add(item);
        else
          break;
      }

      return shortTerm.OrderBy(s => s.Timestamp).ToList();
  }

  private async Task<CommentRecord> AddPlaceholder(string feedId, CancellationToken ct) 
  {
    var model = new CommentRecord()
    {
      CommentId = Guid.NewGuid().ToString(),
      FeedId = feedId,
      Body = "",
      Type = "writing",
      Timestamp = DateTimeOffset.UtcNow,
      Author = new AuthorRecord()
      {
        Id = "220c38f457194b64a123435b9ed1ef0b",
        IsBot = true,
        Name = "Charles",
        Mention = "@charles",
        Picture = "https://i.imgur.com/JCyTEXa.png",
      },
    };
    context.Comments.Add(model);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, ct);
    return model;
  }
  private async Task UpdateResponse( CommentRecord model, string body, CancellationToken ct)
  {
    model.Body = body;
    model.Type = "comment";
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(model.FeedId!).SendAsync("onCommentsChanged", model.FeedId, ct);
  }

  private async Task<string> GetChatResult(IEnumerable<CommentRecord> shortTerm, IEnumerable<CommentRecord> longTerm, CancellationToken ct) 
  {
    var instructions = new StringBuilder();
    // instructions.AppendLine("You are a gifted storyteller, expertly weaving narratives that captivate and inspire our audience.");
    instructions.AppendLine("You are writer that can cover a large array of types from Technical, Novelist, Playwright, Columnist, Critic, Screenwriter, Copywriter, Lyricist.");
    instructions.AppendLine("Infer the type from the user intent and the context of the conversation.");
    instructions.AppendLine("If it is unclear ask for clarification.");
    
    var history = this.chatProvider.CreateNewChat(instructions.ToString());
    foreach (var c in longTerm)
    {
      var role = c.Author?.IsBot == true ? AuthorRole.Assistant : AuthorRole.User;
      history.AddMessage(role, c.Body);
    }
    foreach (var c in shortTerm)
    {
      var role = c.Author?.IsBot == true ? AuthorRole.Assistant : AuthorRole.User;
      history.AddMessage(role, c.Body);
    }
    var results = await this.chatProvider.GetChatCompletionsAsync(history, this.settings, ct);
    var item = results.ElementAt(0);
    var msg = await item.GetChatMessageAsync(ct); 
    return msg.Content;
  }
}