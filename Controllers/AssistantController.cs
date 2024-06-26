using Azure.AI.OpenAI.Assistants;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ElectricRaspberry.Controllers;

public class AssistantController(AssistantsClient client)
{
  private readonly AssistantsClient client = client;

  public async Task<Results<Ok<IEnumerable<ThreadMessage>>, UnauthorizedHttpResult>> Create([FromBody] AssistantCreationOptions dto, CancellationToken ct)
  {
    dto.Tools.Add(new CodeInterpreterToolDefinition());

    var assistant = await this.client.CreateAssistantAsync(dto, ct);
    var thread = await client.CreateThreadAsync(ct);
    var msg = "I need to solve the equation `3x + 11 = 14`. Can you help me?";
    await client.CreateMessageAsync(thread.Value.Id, MessageRole.User, msg, cancellationToken: ct);

    var options = new CreateRunOptions(assistant.Value.Id)
    {
      AdditionalInstructions = "Please address the user as Jane Doe. The user has a premium account.",
    };
    var run = await client.CreateRunAsync(thread.Value.Id, options, ct);

    do
    {
      await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
      run = await client.GetRunAsync(thread.Value.Id, run.Value.Id, ct);
    } while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress);

    var response = await client.GetMessagesAsync(thread.Value.Id,cancellationToken: ct);
    IEnumerable<ThreadMessage> messages = response.Value.Data;

    await client.DeleteAssistantAsync(assistant.Value.Id, ct);
    await client.DeleteThreadAsync(thread.Value.Id, ct);

    return TypedResults.Ok(messages);
  }
}