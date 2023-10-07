using Auth0.ManagementApi.Models;
using ElectricRaspberry.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.SkillDefinition;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ElectricRaspberry.Skills;

//public sealed class MemorySkill
//{
//  private readonly ISemanticTextMemory memory;
//  private readonly ILogger logger;

//  public MemorySkill(IKernel kernel)
//  {
//    this.memory = kernel.Memory;
//    this.logger = kernel.LoggerFactory.CreateLogger<MemorySkill>();
//  }

//  [SKFunction, Description("Search previous memories.")]
//  public async Task<string> SearchMemoryAsync([Description("The text to find related memory for.")] string input)
//  {
//    var collection = "scratchpad";
//    this.logger.LogDebug("Searching memories in collection '{0}', relevance '{1}'", collection, 0.7);

//    // Search memory
//    List<MemoryQueryResult> memories = await this.memory
//        .SearchAsync(collection, input, 1, 0.7)
//        .ToListAsync()
//        .ConfigureAwait(false);

//    if (memories.Count == 0)
//    {
//      logger.LogWarning("Memories not found in collection: {0}", collection);
//      return string.Empty;
//    }

//    logger.LogTrace("Done looking for memories in collection '{0}')", collection);
//    return memories.First().Metadata.Text;
//  }

//  [SKFunction, Description("Fetch a memory by key.")]
//  public async Task<string> FetchMemory([Description("The key to retrieve a specific memory.")] string key)
//  {
//    var collection = "scratchpad";
//    this.logger.LogDebug("Fetch memory '{0}' in collection '{1}'", key, collection);

//    // Retrieve memory
//    var memory = await this.memory.GetAsync(collection, key);
//    if (memory is null)
//    {
//      logger.LogWarning("Memories not found in collection: {0}", collection);
//      return $"Memory with key {key} dose not exist";
//    }

//    logger.LogTrace("Done looking for memories in collection '{0}')", collection);
//    return memory.Metadata.Text;
//  }

//  [SKFunction, Description("Store infomation to memory.")]
//  public async Task<string> StoreMemoryAsync([Description("The text to save"), MinLength(5)] string input)
//  {
//    this.logger.LogDebug("Saving memory to collection '{0}'", "scratchpad");

//    var key = Guid.NewGuid().ToString("N");
//    await this.memory.SaveInformationAsync("scratchpad", text: input, id: key).ConfigureAwait(false);
//    return $"Memory Key: {key}";
//  }
//}