public static class Extensions
{
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
}