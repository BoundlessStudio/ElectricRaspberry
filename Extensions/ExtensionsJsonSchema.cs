using ElectricRaspberry.Models;
using Json.Schema;
using Microsoft.Extensions.Options;
using OpenAI;

namespace ElectricRaspberry.Extensions;

public static class ExtensionsJsonSchema
{
  internal static BinaryData ToBinaryData(this JsonSchema schema)
  {
    return BinaryData.FromObjectAsJson(schema);
  }
}