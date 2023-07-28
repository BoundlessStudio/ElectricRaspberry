using System.Text.Json.Nodes;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.Tokenizers;

// Write me a marketing slogan for my {{$BUSINESS}} in {{$CITY}} with a focus on {{$SPECIALTY}} we are without sacrificing quality 
// a function that generates marketing slogans
// @shuri I want to create a new skill that generates marketing slogans.

// @tesla Create Marketing Slogan for The BBQ Pit in London that specializes in Mustard sauce then email it to myself with the subject Marketing Slogan.

public class ShuriAgent : IAgent
{
  private readonly IHubContext<FeedHub> hub;
  private readonly FeedsContext context;
  private readonly OpenAI.Chat.ChatEndpoint chatProvider;

   public ShuriAgent(
    IOptions<OpenAIOptions> aiOptions,
    IHubContext<FeedHub> hub,
    FeedsContext context
  )
  {
    this.context = context;
    this.hub = hub;
    this.context = context;

    var api = new OpenAI.OpenAIClient(new OpenAI.OpenAIAuthentication(aiOptions.Value.ApiKey, aiOptions.Value.OrgId));
    this.chatProvider = api.ChatEndpoint;
  }

  public async Task InvokeAsync(IAuthorizedUser user, CommentRecord comment, CancellationToken ct)
  {
    var history = await GetHistory(comment.FeedId, ct);
    var placeholder = await this.AddPlaceholder(comment.FeedId, ct);
    var msg = await this.GetMessage(user.UserId, comment.FeedId, history, ct);
    var name = msg.Function?.Name ?? string.Empty;
    var json = msg.Function?.Arguments.ToString() ?? string.Empty;
    switch (name)
    {
      case "Create":
        {
          var args =  JsonSerializer.Deserialize<ShuriCreateArg>(json) ?? new();
          await this.Create(user, placeholder, args, ct);
          break;
        }
      case "Import":
        {
          var args = JsonSerializer.Deserialize<ShuriImportArg>(json) ?? new();
          await this.Import(placeholder, args, ct);
          break;
        }
      case "Add":
        {
          var args = JsonSerializer.Deserialize<ShuriAddArg>(json) ?? new();
          await this.Add(placeholder, args, ct);
          break;
        }
      case "Find":
        {
          var args = JsonSerializer.Deserialize<ShuriFindArg>(json) ?? new();
          await this.Find(placeholder, args, ct);
          break;
        }
      case "Enable":
        {
          var args = JsonSerializer.Deserialize<ShuriEnableArg>(json) ?? new();
          await this.Enable(placeholder, args, ct);
          break;
        }
      case "Disable":
        {
          var args = JsonSerializer.Deserialize<ShuriDisableArg>(json) ?? new();
          await this.Disable(placeholder, args, ct);
          break;
        }
      case "Delete":
        {
          var args = JsonSerializer.Deserialize<ShuriDeleteArg>(json) ?? new();
          await this.Delete(placeholder, args, ct);
          break;
        }
      default:
        {
          await UpdatePlaceholder(placeholder, msg.Content, ct);
          break;
        }
    }
  }

  private async Task Create(IAuthorizedUser user, CommentRecord placeholder, ShuriCreateArg arg, CancellationToken ct)
  {
    var model = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == placeholder.FeedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent Create Skill missing Feed: {placeholder.FeedId}");

    var skill = new SkillRecord()
    {
      SkillId = Guid.NewGuid().ToString(),
      Name = $"User.{arg.Name}",
      TypeOf = arg.Name,
      Description = arg.Description,
      Owner = user.UserId,
      Type = SkillType.Semantic,
    };
    this.context.Skills.Add(skill);
    await this.context.SaveChangesAsync(ct);

    model.Skills.Add(skill);
    await this.context.SaveChangesAsync(ct);

    placeholder.Type = "comment";
    placeholder.Body = $"Skill {skill.Name} ({skill.SkillId}) was created.";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Import(CommentRecord placeholder, ShuriImportArg arg, CancellationToken ct)
  {
    var model = await this.context.Skills.FindAsync(new[]{arg.Id}, cancellationToken: ct);
    if(model is null)
    {
      placeholder.Body = "Can't find the skill.";
    }
    else
    {
      model.Url = arg.Url;
      placeholder.Body = $"Functions imported to {model.Name}."; 
    }
    
    placeholder.Type = "comment";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Add(CommentRecord placeholder, ShuriAddArg arg, CancellationToken ct)
  {
    var model = await this.context.Skills.FindAsync(new[]{arg.Id}, cancellationToken: ct);
    if(model is null)
    {
      placeholder.Body = "Can't find the skill.";
    }
    else
    {
      model.Prompt = arg.Prompt;
      placeholder.Body = $"Function added to {model.Name}.";
    }
    
    placeholder.Type = "comment";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Find(CommentRecord placeholder, ShuriFindArg arg, CancellationToken ct)
  {
    var model = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == placeholder.FeedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent Find Skill missing Feed: {placeholder.FeedId}");
    
    var skills = model.Skills.Where(_ => _.Name.Contains(arg.Name, StringComparison.InvariantCultureIgnoreCase)).ToList();
    var builder = new StringBuilder();
    builder.AppendLine($"Skills found: {skills.Count}");
    foreach (var item in skills)
    {
      builder.AppendLine($"- {item.Name} ({item.SkillId})");
    }
    placeholder.Type = "comment";
    placeholder.Body = builder.ToString();

    await this.context.SaveChangesAsync(ct);
  
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Enable(CommentRecord placeholder, ShuriEnableArg arg, CancellationToken ct)
  {
    var feed = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == placeholder.FeedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent Delete Skill missing Feed: {placeholder.FeedId}");
    
    var skill = await this.context.Skills.FindAsync(new[]{arg.Id}, cancellationToken: ct);
    if(skill is null)
    {  
      placeholder.Body = "Can't find the skill.";
    }
    else
    {
      feed.Skills.Add(skill);
      placeholder.Body = $"Skill {skill.Name} was Enabled";
    }

    placeholder.Type = "comment";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Disable(CommentRecord placeholder, ShuriDisableArg arg, CancellationToken ct)
  {
    var feed = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == placeholder.FeedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent Delete Skill missing Feed: {placeholder.FeedId}");
    
    var skill = await this.context.Skills.FindAsync(new[]{arg.Id}, cancellationToken: ct);
    if(skill is null)
    {  
      placeholder.Body = "Can't find the skill.";
    }
    else
    {
      feed.Skills.Remove(skill);
      placeholder.Body = $"Skill {skill.Name} was Disabled";
    }

    placeholder.Type = "comment";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
  }

  private async Task Delete(CommentRecord placeholder, ShuriDeleteArg arg, CancellationToken ct)
  {
    var feed = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == placeholder.FeedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent Delete Skill missing Feed: {placeholder.FeedId}");
    
    var skill = await this.context.Skills.FindAsync(new[]{arg.Id}, cancellationToken: ct);
    if(skill is null)
    {  
      placeholder.Body = "Can't find the skill.";
    }
    else
    {
      feed.Skills.Remove(skill);
      context.Skills.Remove(skill);
      placeholder.Body = $"Skill {skill.Name} was Removed";
    }

    placeholder.Type = "comment";
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
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
        Id = "b5fc34fa66ca465d89cd67e3e96d2f5e",
        IsBot = true,
        Name = "Shuri",
        Mention = "@shuri",
        Picture = "https://i.imgur.com/C9xQbAy.png",
      },
    };
    context.Comments.Add(model);
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(feedId).SendAsync("onCommentsChanged", feedId, ct);
    return model;
  }

  private async Task UpdatePlaceholder(CommentRecord placeholder, string body, CancellationToken ct) 
  {
    placeholder.Type = "comment";
    placeholder.Body = body;
    placeholder.Tokens = GPT3Tokenizer.Encode(body).Count;
    await this.context.SaveChangesAsync(ct);
    await this.hub.Clients.Group(placeholder.FeedId).SendAsync("onCommentsChanged", placeholder.FeedId, ct);
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

  private async Task<Message> GetMessage(string userId, string feedId, IList<CommentRecord> comments, CancellationToken ct)
  {
    var instructions = new StringBuilder();
    instructions.AppendLine("You are an assistant to create, update, and delete semantic skills.");
    instructions.AppendLine("A skill can have many functions. A skill name should base TitleCase with no spaces and unique.");
    instructions.AppendLine("You can import functions from OpenAPI specs via a URL.");
    instructions.AppendLine("You add functions from user prompts.");
    instructions.AppendLine();
    instructions.AppendLine("If the user is creating a function manually they may need help.");
    instructions.AppendLine("Semantic functions are designed to be chain together to pass the output of one to input of another.");
    instructions.AppendLine("This optional input has a special token {{$INPUT}}");
    instructions.AppendLine("Semantic functions maybe also have other named parameters."); 
    instructions.AppendLine("They should be extracted from the chat history.");
    instructions.AppendLine("They follow the same format as the input. For example: {{$PARAM}}");
    instructions.AppendLine("The user can also invoke other functions by name (prefixed by the skill name). For example: SkillName.FunctionName");
    instructions.AppendLine("You goal is map the users intent to a function.");
    instructions.AppendLine("Failing to do that you can ask the user for more information.");
    instructions.AppendLine();
    var model = await context.Feeds.Include(f => f.Skills).FirstOrDefaultAsync(i => i.FeedId == feedId, cancellationToken: ct)
      ?? throw new NullReferenceException($"ShuriAgent GetMessage missing Feed: {feedId}");

    instructions.AppendLine("Skills:");
    instructions.AppendLine();
    foreach (var item in model.Skills)
    {
      instructions.AppendLine(item.Name);
    }

    var messages = new List<Message>
    {
      new Message(Role.System, instructions.ToString()),
    };

    foreach (var comment in comments)
    {
      if(comment.Author.IsBot)
        messages.Add(new Message(Role.Assistant, comment.Body));
      else
        messages.Add(new Message(Role.User, comment.Body));
    }
    var functions = new List<Function>
    {
      new Function(
        "Create",
        "Create a semantic skill",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["name"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The name of the skill."
              },
              ["description"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The description of the skill."
              },
          },
          ["required"] = new JsonArray { "name", "description" }
        }
      ),
      new Function(
        "Import",
        "Import functions from OpenAPI spec",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["id"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The id for the skill."
              },
              ["url"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The url of the swagger file to import."
              },
          },
          ["required"] = new JsonArray {  "id", "url"  }
        }
      ),
      new Function(
        "Add",
        "Add function from a prompt",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["id"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The id for the skill."
              },
              ["prompt"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The prompt to add as a semantic function."
              },
          },
          ["required"] = new JsonArray {  "id", "prompt"  }
        }
      ),
      new Function(
        "Find",
        "Find a skill and its functions based on the skill's name",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["name"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The name to find related skill for."
              }
          },
          ["required"] = new JsonArray { "name" }
        }
      ),
      new Function(
        "Enable",
        "Enable a skill in a feed based on its id",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["id"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The id of the skill to enable."
              }
          },
          ["required"] = new JsonArray { "id" }
        }
      ),
      new Function(
        "Disable",
        "Disable a skill in a feed based on its id",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["id"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The id of the skill to disable."
              }
          },
          ["required"] = new JsonArray { "id" }
        }
      ),
      new Function(
        "Delete",
        "Delete a skill based on its id",
        new JsonObject
        {
          ["type"] = "object",
          ["properties"] = new JsonObject
          {
              ["id"] = new JsonObject
              {
                  ["type"] = "string",
                  ["description"] = "The id of the skill to delete."
              }
          },
          ["required"] = new JsonArray { "id" }
        }
      )
    };
    var chatRequest = new ChatRequest(messages, functions: functions, user: userId, functionCall: "auto", model: "gpt-3.5-turbo-0613");
    var result = await this.chatProvider.GetCompletionAsync(chatRequest, ct);
    return result.FirstChoice.Message;
  }
}