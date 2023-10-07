using ElectricRaspberry.Models;
using System.Security.Claims;

public static class Extensions
{

  internal static IUser GetUser(this ClaimsPrincipal principal)
  {
    var ns = "https://namespace.volcano-lime.com";

    var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.NewGuid().ToString();
    var lanauge = principal?.FindFirstValue($"{ns}/language") ?? "en";
    var ip = principal?.FindFirstValue($"{ns}/ip") ?? "0.0.0.0";
    var city = principal?.FindFirstValue($"{ns}/city") ?? "";
    var continent = principal?.FindFirstValue($"{ns}/continent") ?? "";
    var country = principal?.FindFirstValue($"{ns}/country") ?? "Canada";
    var latitude = principal?.FindFirstValue($"{ns}/latitude") ?? "0";
    var longitude = principal?.FindFirstValue($"{ns}/longitude") ?? "0";
    var timezone = principal?.FindFirstValue($"{ns}/timezone") ?? "UTC";
    var name = principal?.FindFirstValue($"{ns}/name") ?? "anonymous";
    var picture = principal?.FindFirstValue($"{ns}/picture") ?? "https://picsum.photos/64";

    return new User(id, name, lanauge, ip, city, continent, latitude, longitude, country, timezone, picture);
  }

  internal static int WordCount(this string str)
  {
    return str.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
  }

  internal static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
  {
      var result = new List<T>();
      await foreach (var item in source.ConfigureAwait(false))
      {
        result.Add(item);
      }

      return result;
  }

  internal static string GetName<T>(this T obj) where T : struct, Enum
  {
    return Enum.GetName(typeof(T), obj) ?? string.Empty;
  }

  internal static async ValueTask<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
  {
    var result = new List<T>();

    await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
    {
      result.Add(item);
    }

    return result;
  }
}