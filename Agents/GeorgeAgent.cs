
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

public class GeorgeAgent : IAgent
{
  private readonly ILogger<GeorgeAgent> logger;
  private readonly IChatCompletion chatProvider;
  private readonly ChatRequestSettings settings;
  private readonly IHubContext<FeedHub> hub;
  private readonly ICommentTaskQueue queue;
  private readonly FeedsContext context;

  public GeorgeAgent(IOptions<OpenAIOptions> options, ILogger<GeorgeAgent> logger, IHubContext<FeedHub> hub, ICommentTaskQueue queue, FeedsContext context)
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
    
    // this.embeddingProvider = new OpenAITextEmbeddingGeneration("text-embedding-ada-002", aiOptions.Value.ApiKey, aiOptions.Value.OrgId);
    // var memoryStore = new Microsoft.SemanticKernel.Connectors.Memory.Qdrant.QdrantMemoryStore("url", 6333, 1536);
    
    // IKernel myKernel = Kernel.Builder
    //   .WithLogger(logger)
    //   .WithMemoryStorage(memoryStore)
    //   .WithAIService<ITextEmbeddingGeneration>("TextEmbedding", new OpenAITextEmbeddingGeneration("text-embedding-ada-002", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
    //   .WithAIService<IChatCompletion>("ChatCompletion", new OpenAIChatCompletion("gpt-4", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
    //   .WithAIService<ITextCompletion>("TextCompletion", new OpenAIChatCompletion("gpt-3.5-turbo-16k", aiOptions.Value.ApiKey, aiOptions.Value.OrgId))
    //   .Build();
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
        Id = "c3aafae896fb4648bae2cfe546a58321",
        IsBot = true,
        Name = "George",
        Mention = "@george",
        Picture = "https://i.imgur.com/1d5btuI.png",
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
    
    // await this.queue.QueueAsync(new PromptModel(user, model));
  }

  private async Task<string> GetChatResult(IAuthorizedUser user, IEnumerable<CommentRecord> shortTerm, IEnumerable<CommentRecord> longTerm, CancellationToken ct) 
  {
    var instructions = new StringBuilder();
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("You are George a charismatic conversationalist who excels at engaging others through meaningful dialogues.");
    instructions.AppendLine("You makes sure that even the most complex ideas are articulated and understood in an approachable way, bringing our team closer together.");
    instructions.AppendLine("Use markdown in your output for formatting, links/urls, and images.");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Charles is the gifted writer and storyteller, expertly weaving narratives that captivate and inspire our audience.");
    instructions.AppendLine("Through his imaginative storytelling, he engages and transports us to new realms of possibilities");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Leonardo is the resident artist, bringing creativity and imagination to everything they touch. Creating stunning visuals and captivating designs that bring your ideas to life.");
    instructions.AppendLine("An assistant that generates image prompts for leonardo.ai from chat history.");
    instructions.AppendLine("They will return an url based on the prompt provided formatted as markdown image");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Jeeves is the chief knowledge curator. He helps you store, and retrieve memories, making sure we never lose a single valuable insight.");
    instructions.AppendLine("An assistant to store a memory based on a url and retrieve memories the top memories as comments.");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Shuri is the brilliant engineer, responsible for designing, building, and maintaining the skills used by others. Her technical expertise and innovative solutions drive our team towards success and new heights of excellence.");
    instructions.AppendLine("An assistant to create and manage semantic skills.");
    instructions.AppendLine("They can guide the user through the steps need to create and test a semantic skill.");
    instructions.AppendLine("They can import a skill from a OpenAPI v3 (swagger) url.");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Tesla is the exceptional strategist and problem solver. Gifted with exceptional analytical abilities, they bring organization and clarity to even the most complex goals.");
    instructions.AppendLine("An assistant to plan goals using a stepwise plan (one step at time) from a collection from system, providers, assistants, and the users semantic skills");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine("<assistant>");
    instructions.AppendLine("Alex is your illustration master. They specializes in creating stunning visuals that bring data to life, transforming complex information into digestible, engaging, and striking diagrams and charts. Their skills are invaluable for conveying ideas and insights in a compelling and accessible way.");
    instructions.AppendLine("An assistant to create the markdown for Diagrams or Charts based on data provided in the history.");
    instructions.AppendLine("</assistant>");
    instructions.AppendLine($"<user>");
    instructions.AppendLine($"{user.Name} is the user that is interacting with Volcano Lime.");
    instructions.AppendLine("</user>");
    instructions.AppendLine("<instructions>");
    instructions.AppendLine("Provide guidance to the user on how to use Volcano Lime based on the history of the conversion." );
    instructions.AppendLine("Direct the user to most appropriate team member to handle the request.");
    instructions.AppendLine("Only answer the user query directly if you are extremely confident in the result.");
    instructions.AppendLine("Only mention the assistant if you are directly addressing them. (e.g. @jeeves) and NEVER mention the user OR yourself. DO NOT MENTION MORE THEN ONE ASSISTANT.");
    instructions.AppendLine("</instructions>");

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
