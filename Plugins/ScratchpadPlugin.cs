using Microsoft.Extensions.Caching.Memory;
using Microsoft.SemanticKernel;
using System.ComponentModel;

public sealed class ScratchpadPlugin
{
  // private readonly

  public ScratchpadPlugin(IMemoryCache cache)
  {
  }

  [SKFunction, Description("Returns the Scratchpad. Use this function to get retrieve short term memories.")]
  public async Task<string> ScratchpadRead()
  {
    return string.Empty;
  }

  [SKFunction, Description("Write someting to Scratchpad. Use this function to write data to short term memory to be recalled later.")]
  public async Task ScratchpadWrite(
    [Description("The data to store in the cache")] string data
  )
  {
    // Store data in memory cache.
  }

}