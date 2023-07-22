
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

public class AlexAgent : IAgent
{
  private readonly ILogger<AlexAgent> logger;
  private readonly IChatCompletion chatProvider;
  private readonly ChatRequestSettings settings;
  private readonly IHubContext<FeedHub> hub;
  private readonly ICommentTaskQueue queue;
  private readonly FeedsContext context;

  public AlexAgent(IOptions<OpenAIOptions> options, ILogger<AlexAgent> logger, IHubContext<FeedHub> hub, ICommentTaskQueue queue, FeedsContext context)
  {
    this.logger = logger;
    this.hub = hub;
    this.queue = queue;
    this.context = context;

    this.chatProvider = new OpenAIChatCompletion("gpt-4", options.Value.ApiKey, options.Value.OrgId);
    this.settings = new ChatRequestSettings()
    {
      MaxTokens = 4000,
      TopP = 0,
      Temperature = 0,
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
    var result = await GetChatResult(user, history, memories, ct);
    await UpdateResponse(user, placeholder, result, ct);
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
        if(sum < 4000)
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
      Type = "thinking",
      Timestamp = DateTimeOffset.UtcNow,
      Author = new AuthorRecord()
      {
        Id = "1be4d41cf63e43d2a1f55ba36e699960",
        IsBot = true,
        Name = "Alex",
        Mention = "@alex",
        Picture = "https://i.imgur.com/bYMhUJE.png",
      },
    };
    context.Comments.Add(model);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, ct);
    return model;
  }
  private async Task UpdateResponse(IAuthorizedUser user, CommentRecord model, string body, CancellationToken ct)
  {
    model.Body = body;
    model.Type = "comment";
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(model.FeedId!).SendAsync("onCommentsChanged", model.FeedId, ct);
  }

  private async Task<string> GetChatResult(IAuthorizedUser user, IEnumerable<CommentRecord> shortTerm, IEnumerable<CommentRecord> longTerm, CancellationToken ct) 
  {
    var instructions = new StringBuilder();
    instructions.AppendLine("You are Alex the illustration master. They specializes in creating stunning visuals that bring data to life, transforming complex information into digestible, engaging, and striking diagrams and charts. Their skills are invaluable for conveying ideas and insights in a compelling and accessible way.");
    instructions.AppendLine("An assistant to create the markdown for Diagrams or Charts based on data provided in the history.");
    instructions.AppendLine("For Diagrams create the Mermaid.js markdown code needed with the lang set to 'mermaid'.");
    instructions.AppendLine("For Charts create the Chart.js markdown code needed with the lang set to 'chart'.");

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