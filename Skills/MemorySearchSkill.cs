using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

public class MemorySearchSkill
{
  private const double DefaultRelevance = 0.0;
  private const int DefaultLimit = 1;
  private readonly string collection;
  private readonly ISemanticTextMemory memory;
  
  public MemorySearchSkill(ISemanticTextMemory memory, string collection)
  {
    this.memory = memory;
    this.collection = collection;
  }

  [SKFunction, SKName("RecallMemories"), Description("Semantic search and return up to N memories related to the input text.")]
  public async Task<string> RecallAsync(
    [Description("The input text to find related memories for")] string input, 
    [DefaultValue(0)][Description("The relevance score, from 0.0 to 1.0, where 1.0 means perfect match")] double? relevance, 
    [DefaultValue(1)][Description("The maximum number of relevant memories to recall")] int? limit, 
    ILogger? logger,
    CancellationToken cancellationToken = default
  ) 
  {
      logger ??= NullLogger.Instance;
      relevance ??= DefaultRelevance;
      limit ??= DefaultLimit;

      logger.LogDebug("Searching memories in collection '{collection}', relevance '{relevance}'", collection, relevance);

      // Search memory
      List<MemoryQueryResult> memories = await this.memory
          .SearchAsync(collection, input, limit.Value, relevance.Value, cancellationToken: cancellationToken)
          .ToListAsync()
          .ConfigureAwait(false);

      if (memories.Count == 0)
      {
        logger.LogWarning("Memories not found in collection: {collection}", collection);
        return string.Empty;
      }

      logger.LogTrace("Done looking for memories in collection '{collection}')", collection);
      return limit == 1 ? memories[0].Metadata.Text : JsonSerializer.Serialize(memories.Select(x => x.Metadata.Text));
  }
}