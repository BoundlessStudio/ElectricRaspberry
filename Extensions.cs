public static class Extensions
{
  public static int WordCount(this string str)
  {
    return str.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
  }
}