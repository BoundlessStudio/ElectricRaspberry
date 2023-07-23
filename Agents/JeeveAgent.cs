using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;
using Microsoft.SemanticKernel.Memory;
using Newtonsoft.Json;
using OpenAI.Chat;

public class JeeveAgent : IAgent
{
  private readonly ILogger<JeeveAgent> logger;
  private readonly IHubContext<FeedHub> hub;
  private readonly ISemanticTextMemory memory;
  private readonly FeedsContext context;
  private readonly OpenAI.Chat.ChatEndpoint chatProvider;
  private readonly HttpClient httpClient;
  private readonly ITextChunkerService chunker;

  public JeeveAgent(
    IOptions<OpenAIOptions> aiOptions,
    ILogger<JeeveAgent> logger,
    IHubContext<FeedHub> hub,
    ISemanticTextMemory memory,
    IHttpClientFactory factory,
    ITextChunkerService chunker,
    FeedsContext context
  )
  {
    this.logger = logger;
    this.context = context;
    this.hub = hub;
    this.memory = memory;
    this.context = context;
    this.httpClient = factory.CreateClient();
    this.chunker = chunker;
    var api = new OpenAI.OpenAIClient(new OpenAI.OpenAIAuthentication(aiOptions.Value.ApiKey, aiOptions.Value.OrgId));
    this.chatProvider = api.ChatEndpoint;
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord model, CancellationToken ct)
  {
    var comment = await this.AddPlaceholder(model.FeedId, ct);
    var msg = await this.GetMessage(user.UserId, model.Body, ct);
    var name = msg.Function?.Name ?? string.Empty;
    var json = msg.Function?.Arguments.ToString() ?? string.Empty;
    switch (name)
    {
      case "Save":
        {
          var args = JsonConvert.DeserializeObject<JeeveSaveArg>(json) ?? new JeeveSaveArg();
          await this.Save(comment, args.Url, ct);
          break;
        }
      case "Recall":
      default:
        {
          var args = JsonConvert.DeserializeObject<JeeveRecallArg>(json) ?? new JeeveRecallArg();
          await this.Recall(comment, args.Text, args.Limit, args.Relevance, ct);
          break;
        }
    }
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
        Id = "fcf1e630f6fb406bbafea1ad5b7424b0",
        IsBot = true,
        Name = "Jeeve",
        Mention = "@jeeve",
        Picture = "https://i.imgur.com/EKrj5bE.png",
      },
    };
    context.Comments.Add(model);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, ct);
    return model;
  }

  private async Task UpdatePlaceholder(CommentRecord placeholder, CancellationToken ct)
  {
      await this.context.SaveChangesAsync(ct);
      await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task<Message> GetMessage(string userId, string body, CancellationToken ct)
  {
    var instructions = new StringBuilder();
    instructions.AppendLine("Your name is Jeeves and you are an assistant to store memories based on urls and retrieve memories based on embeddings.");

    var messages = new List<Message>
    {
        new Message(Role.System, instructions.ToString()),
        new Message(Role.User, body),
    };
    var functions = new List<Function>
    {
      new Function(
        "Save",
        "Save file to semantic memory",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["url"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The url of the file to create memories for."
              },
          },
          ["required"] = new JsonArray { "url" }
        }
      ),
      new Function(
        "Recall",
        "Search, recall or return up to N semantic memories related to the input text",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["text"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The input text to find related memories for."
              },
              ["relevance"] = new JsonObject
              {
                  ["type"] = "number",
                  ["description"] = "The relevance score, from 0.0 to 1.0, where 1.0 means perfect match",
              },
              ["limit"] = new JsonObject
              {
                  ["type"] = "integer",
                  ["description"] = "The maximum number of relevant memories to recall",
              },
          },
          ["required"] = new JsonArray { "text" }
        }
      )
    };
    var chatRequest = new ChatRequest(messages, functions: functions, user: userId, functionCall: "auto", model: "gpt-3.5-turbo-0613");
    var result = await this.chatProvider.GetCompletionAsync(chatRequest, ct);
    return result.FirstChoice.Message;
  }

  private async Task Save(CommentRecord placeholder, string url, CancellationToken ct)
  {
    placeholder.Type = "memorizing";
    await UpdatePlaceholder(placeholder, ct);

    var chunks = await this.GetChunks(url, ct);
    for (int i = 0; i < chunks.Count; i++)
    {
      await this.memory.SaveInformationAsync(placeholder.FeedId, text: $"{chunks[i]}", id: $"{url}#{i}", cancellationToken: ct);
    }

    placeholder.Body = $"Memorization complete! {chunks.Count} new memories added to the feed.";
    placeholder.Type = "memory";
    await UpdatePlaceholder(placeholder, ct);
  }

  private async Task Recall(CommentRecord placeholder, string text, int limit, double relevance, CancellationToken ct) 
  {
    placeholder.Type = "remembering";
    await UpdatePlaceholder(placeholder, ct);

    var memories = await this.memory.SearchAsync(placeholder.FeedId, text, limit, relevance, cancellationToken: ct).ToListAsync();
    foreach (var memory in memories)
    {
      var model = new CommentRecord()
      {
        CommentId = Guid.NewGuid().ToString(),
        FeedId = placeholder.FeedId,
        Body = memory.Metadata.Text,
        Type = "comment",
        Timestamp = DateTimeOffset.UtcNow,
        Relevance = memory.Relevance,
        Tokens = GPT3Tokenizer.Encode(memory.Metadata.Text).Count,
        Author = new AuthorRecord()
        {
          Id = "fcf1e630f6fb406bbafea1ad5b7424b0",
          IsBot = true,
          Name = "Jeeve",
          Mention = "@jeeve",
          Picture = "https://i.imgur.com/EKrj5bE.png",
        },
      };
      context.Comments.Add(model);
    }
    this.context.Comments.Remove(placeholder);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task<List<string>> GetChunks(string url, CancellationToken ct)
  {
    var response = await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, url), ct);
    response.EnsureSuccessStatusCode();
    var length = response.Content.Headers.ContentLength / 4;
    if(length.HasValue && length.Value > 200000000) throw new InvalidOperationException($"File {url} too large");
    switch (response.Content.Headers.ContentType?.MediaType)
    {
      case "text/plain":
        {
          var text = await response.Content.ReadAsStringAsync(ct);
          if(GPT3Tokenizer.Encode(text).Count > 1000) {
            return this.chunker.GetParagraphs(text).ToList();
          } else {
            return new List<string>() { text };
          }
        }
      case "text/markdown":
        {
          var text = await response.Content.ReadAsStringAsync(ct);
          if(GPT3Tokenizer.Encode(text).Count > 1000) {
            return this.chunker.GetParagraphs(text).ToList();
          } else {
            return new List<string>() { text };
          }
        }
      case "application/pdf":
        {
          var stream = await response.Content.ReadAsStreamAsync(ct);
          var pages = this.chunker.SplitPdf(stream).ToList();
          return this.chunker.GetParagraphs(pages).ToList();
        }
      case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
        {
          var stream = await response.Content.ReadAsStreamAsync(ct);
          return this.chunker.SplitWord(stream).ToList();
        }
      case "text/csv":
        {
          var text = await response.Content.ReadAsStringAsync(ct);
          if(GPT3Tokenizer.Encode(text).Count > 1000) {
            var lines = this.chunker.GetLines(text).ToList();
            return lines.Skip(1).Select(l => lines[0] + Environment.NewLine + l).ToList();
          } else {
            return new List<string>() { text };
          }
        }
      case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
        {
          var stream = await response.Content.ReadAsStreamAsync(ct);
          return this.chunker.SplitExcel(stream).ToList();
        }
      case "text/html": // TODO: puppeteer-sharp + browserless..?
      case "application/json":
      default:
        return new List<string>();
    }
  }
}