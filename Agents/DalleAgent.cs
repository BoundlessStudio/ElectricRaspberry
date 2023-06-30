using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ImageGeneration;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

public class DalleAgent : IAgent
{
  private readonly IServiceProvider serviceProvider;
  private readonly ILogger<DalleAgent> logger;
  private readonly IImageGeneration imageProvider;
  private readonly IHubContext<FeedHub> hub;
  private readonly HttpClient httpClient;
  
  public DalleAgent(IOptions<OpenAIOptions> options, ILogger<DalleAgent> logger, IHubContext<FeedHub> hub, IServiceProvider sp, HttpClient httpClient)
  {
    this.serviceProvider = sp;
    this.hub = hub;
    this.logger = logger;
    this.httpClient = httpClient;

    this.imageProvider = new OpenAIImageGeneration(options.Value.ApiKey, options.Value.OrgId);
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct)
  {
    var scope = this.serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetService<FeedsContext>() ?? throw new InvalidOperationException("Could not get FeedsContext from the scoped service provider");

    var model = new CommentRecord()
    {
      CommentId = Guid.NewGuid().ToString(),
      FeedId = comment.FeedId,
      Body = "Crafting an original masterpiece",
      Type = "drawing",
      Timestamp = DateTimeOffset.UtcNow,
      Author = new AuthorRecord()
      {
        Id = "81868d2e6dfa4882a7f3fecdbf4b7380",
        IsBot = true,
        Name = "Dalle",
        Mention = "@dalle",
        Picture = "https://i.imgur.com/8PhftEq.png",
      },
    };
    context.Comments.Add(model);
    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(comment.FeedId).SendAsync("onCommentsChanged", comment.FeedId, model.CommentId, ct);

    // TODO: Turn feed into image prompt

    var description = comment.Body.Replace("@dalle", "");
    var url = await imageProvider.GenerateImageAsync(description, 1024, 1024, ct);

    // TODO: download url to blob storage

    var body = $"![{description}]({url})";

    model.Type = "comment";
    model.Body = body;
    model.Tokens = GPT3Tokenizer.Encode(body).Count;
    model.Characters = body.Length;
    model.Characters  = body.Length;

    await context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(comment.FeedId).SendAsync("onCommentsChanged", comment.FeedId, model.CommentId, ct);
  }
}