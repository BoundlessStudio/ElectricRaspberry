using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using OpenAI.Images;
using Polly;

public class LeonardoAgent : IAgent
{
  public readonly static AuthorRecord Author = new AuthorRecord()
  {
    Id = "81868d2e6dfa4882a7f3fecdbf4b7380",
    IsBot = true,
    Name = "Leonardo",
    Mention = "@Leonardo",
    Picture = "https://i.imgur.com/wKbKtP1.png",
  };

  private readonly ILogger<DalleAgent> logger;
  private readonly IServiceProvider serviceProvider;
  private readonly IChatCompletion chatProvider;
  private readonly ChatRequestSettings settings;
  private readonly IImageGeneration imageProvider;
  private readonly IFeedStorageService storageService;
  private readonly IHubContext<FeedHub> hub;
  private readonly HttpClient restClient;
  
  public LeonardoAgent(
    IOptions<OpenAIOptions> aiOptions, 
    IOptions<LeonardoOptions> leonardoOptions,
    ILogger<DalleAgent> logger, 
    IHubContext<FeedHub> hub,
    IFeedStorageService storageService,
    IServiceProvider sp,
    IHttpClientFactory factory)
  {
    this.serviceProvider = sp;
    this.hub = hub;
    this.logger = logger;
    this.storageService = storageService;
    this.restClient = factory.CreateClient();
    this.restClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    this.restClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", leonardoOptions.Value.ApiKey);

    this.chatProvider = new OpenAIChatCompletion("gpt-4", aiOptions.Value.ApiKey, aiOptions.Value.OrgId);
    this.settings = new ChatRequestSettings()
    {
      MaxTokens = 4000,
      TopP = 0,
      Temperature = 0.3,
      FrequencyPenalty = 0,
      PresencePenalty = 0,
      ResultsPerPrompt = 1,
    };
    this.imageProvider = new OpenAIImageGeneration(aiOptions.Value.ApiKey, aiOptions.Value.OrgId);
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct)
  {
    var scope = this.serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetService<FeedsContext>() ?? throw new InvalidOperationException("Could not get FeedsContext from the scoped service provider");

    var history = await GetHistory(context, comment.FeedId, ct);
    
    var model = new CommentRecord()
    {
      CommentId = Guid.NewGuid().ToString(),
      FeedId = comment.FeedId,
      Body = "",
      Type = "drawing",
      Timestamp = DateTimeOffset.UtcNow,
      Author = LeonardoAgent.Author,
    };
    context.Comments.Add(model);
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(comment.FeedId).SendAsync("onCommentsChanged", comment.FeedId, ct);

    var description = await GetChatResult(history, ct);
  
    var temporary = await GetImage(description);
    var permanent = await this.storageService.CopyFrom(temporary, model.FeedId, ct);
    
    var body = $"![Image]({permanent}) {description}";
    model.Body = body;
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    model.Type = "comment";
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(comment.FeedId).SendAsync("onCommentsChanged", comment.FeedId, ct);
  }

  private async Task<string> GetImage(string description)
  {
    if(description.ToLowerInvariant() == "none") 
      return "https://placehold.co/600x400?text=Not+Found";

    var request = new ImageGenerationRequest()
    {
      Prompt = description
    };

    var response = await this.restClient.PostAsJsonAsync("https://cloud.leonardo.ai/api/rest/v1/generations", request) ?? throw new InvalidOperationException("Error posting generation job");
    var json = await response.Content.ReadAsStringAsync();
    var generation = System.Text.Json.JsonSerializer.Deserialize<GenerationJob>(json) ?? throw new InvalidOperationException("Could prase the GenerationJob object");

    await Task.Delay(TimeSpan.FromSeconds(5));

    var retry = Policy<GeneratedImage?>
      .Handle<Exception>()
      .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
      
    var image = await retry.ExecuteAsync(async () => {
      var root = await this.restClient.GetFromJsonAsync<GenerationJobStatus>("https://cloud.leonardo.ai/api/rest/v1/generations/" + generation.Job.GenerationId);
      if(root is null) 
        throw new InvalidOperationException("failed to get status of job.");
      else if(root.Generations.Status != "COMPLETE") 
        throw new InvalidOperationException("incomplete job.");
      else
        return root?.Generations?.GeneratedImages?.FirstOrDefault();
    });

    return image?.Url ?? "https://placehold.co/600x400?text=Not+Found";
  }

  private static async Task<IList<CommentRecord>> GetHistory(FeedsContext context, string feedId, CancellationToken ct)
  {
    var comments = await context.Comments
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

  private async Task<string> GetChatResult(IEnumerable<CommentRecord> memories, CancellationToken ct) 
  {
    var instructions = "You are an assistant that generates image prompts for Leonardo.ai from chat history." +
    "The information for prompt will be spread through out the history with the goal being in the latest message." +
    "Aim for describing your thoughts and ideas in such depth that the they can easily be understood, without being too overwhelming." +
    "Return ONLY a short paragraph that describes the image from the intent based on the message history." +
    "If nothing is applicable, just response with 'NONE'";

    var history = this.chatProvider.CreateNewChat(instructions);
    foreach (var c in memories)
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