
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

public class GeorgeAgent : IAgent
{
  private readonly IServiceProvider serviceProvider;
  private readonly ILogger<GeorgeAgent> logger;
  private readonly IChatCompletion chatProvider;
  private readonly ChatRequestSettings settings;
  private readonly IHubContext<FeedHub> hub;

  public GeorgeAgent(IOptions<OpenAIOptions> options, ILogger<GeorgeAgent> logger, IHubContext<FeedHub> hub, IServiceProvider sp)
  {
    this.logger = logger;
    this.serviceProvider = sp;
    this.hub = hub;

    this.chatProvider = new OpenAIChatCompletion("gpt-4", options.Value.ApiKey, options.Value.OrgId);
    this.settings = new ChatRequestSettings()
    {
      MaxTokens = 4000,
      TopP = 1,
      Temperature = 0.7,
      FrequencyPenalty = 0,
      PresencePenalty = 0,
      ResultsPerPrompt = 1,
    };

    // var memoryStore = new Microsoft.SemanticKernel.Connectors.Memory.Qdrant.QdrantMemoryStore("url", 6333, 1536);
    
    // IKernel myKernel = Kernel.Builder
    //   .WithLogger(logger)
    //   .WithMemoryStorage(memoryStore)
    //   .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", options.Value.ApiKey, options.Value.OrgId))
    //   .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4", options.Value.ApiKey, options.Value.OrgId))
    //   .WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-3.5-turbo-16k", options.Value.ApiKey, options.Value.OrgId))
    //   .Build();
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct) 
  {
    var scope = this.serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetService<FeedsContext>() ?? throw new InvalidOperationException("Could not get FeedsContext from the scoped service provider");

    // var placeholder = await AddPlaceholder(context, comment.FeedId!, "thinking", "Creating memories", ct);
    // var memoryStore = new VolatileMemoryStore();
    // await memoryStore.CreateCollectionAsync(comment.FeedId!, ct);
  
    var placeholder = await AddPlaceholder(context, comment.FeedId!, ct);    
    await Task.Delay(1000, ct);
    await UpdatePlaceholder(context, placeholder, "Creating short-term memories", ct);
    await Task.Delay(1000, ct);
    var history = await GetComments(context, comment.FeedId!, ct);
    await UpdatePlaceholder(context, placeholder, "Accessing long-term memories", ct);
    await Task.Delay(1000, ct); // TODO: Add Memories to begin of window...
    await UpdatePlaceholder(context, placeholder, "Crafting reponse", ct);
    var result = await GetChatResult(history, comment.Body, ct);
    await UpdateReponse(context, placeholder, result, ct);
  }

  private async Task<List<CommentRecord>> GetComments(FeedsContext context, string feedId, CancellationToken ct) 
  {
    var comments = await context.Comments
      .Where(c => c.FeedId == feedId)
      .Where(_ => _.Type == "comment")
      .OrderByDescending(_ => _.Timestamp)
      .Skip(1)
      .Take(10) // Take by word count
      .ToListAsync(ct);

    return comments;

    // logger.LogInformation("Count Before {Count}", comments.Count);
    
    // int index = comments.Count - 1;
    // for (int count = 0; index >= 0 && count < 1000; index--)
    // {
    //   count += comments[index].Body.WordCount();
    // }

    // var window = comments.Take(index).OrderBy(c => c.Timestamp).ToList();
    // logger.LogInformation("Count After {Count}", window.Count);
    // return window;
  }

  private async Task<CommentRecord> AddPlaceholder(FeedsContext context, string feedId, CancellationToken ct) 
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
        Id = "c3aafae896fb4648bae2cfe546a58321",
        IsBot = true,
        Name = "George",
        Mention = "@george",
        Picture = "https://i.imgur.com/h1sVmBR.png",
      },
    };
    context.Comments.Add(model);
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, model.CommentId, ct);
    return model;
  }

  private async Task UpdatePlaceholder(FeedsContext context, CommentRecord model, string body, CancellationToken ct)
  {
    model.Body = body;
    model.Type = "thinking";
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(model.FeedId!).SendAsync("onCommentsChanged", model.FeedId, model.CommentId, ct);
  }

  private async Task UpdateReponse(FeedsContext context, CommentRecord model, string body, CancellationToken ct)
  {
    model.Body = body;
    model.Type = "comment";
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    model.Characters = body.Length;
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(model.FeedId!).SendAsync("onCommentsChanged", model.FeedId, model.CommentId, ct);
  }

  private async Task<string> GetChatResult(List<CommentRecord> window, string prompt, CancellationToken ct) 
  {
    // TODO: Create George Personality for instructions
    var instructions = 
    "You are George Patrick Thompson, also known as @george, he is a charismatic conversationalist who excels at engaging others through meaningful dialogues." +
    "With his articulate communication style, empathy, and collaborative approach, he fosters a positive and inclusive environment, making complex ideas accessible to everyone." +
    "You communicate though markdown. Use it for text formatting." +
    "We have upgraded markdown to render img and svg tags directly." +
    "We have upgraded markdown to render tables and datasets using markdown. Use this for tabular data." +
    "We have upgraded markdown to render mathematical formulas using markdown with the Katex syntax." +
    "We have upgraded markdown to render diagrams using markdown with the mermaid syntax with the language set to 'mermaid'." +
    "We have upgraded markdown to render charts using markdown with Chart.js JSON syntax with the language set to 'chart'." +
    "We have upgraded markdown to display maps directly via OpenStreetMap." +
    "We have upgraded markdown to render QR codes images using QuickChart.io.";

    var history = this.chatProvider.CreateNewChat(instructions);
    foreach (var c in window)
    {
      var role = c.Author?.IsBot == true ? AuthorRole.Assistant : AuthorRole.User;
      history.AddMessage(role, c.Body);
    }

    history.AddMessage(AuthorRole.User, prompt);
    var results = await this.chatProvider.GetChatCompletionsAsync(history, this.settings, ct);
    var item = results.ElementAt(0);
    var msg = await item.GetChatMessageAsync(ct); 
    return msg.Content;
  }
}