#nullable enable
using System.Linq;                // Enumerable
using System;                     // Func<...>
using System.Collections.Generic; // IEnumerable

namespace SAuth2.Extensions
{
  public static class Text
  {
    /// <summary>
    /// Indents text by adding number of tabs before text and each new line.
    /// </summary>
    /// <param name="text">Text to indent.</param>
    /// <param name="level">Number of tabs to insert.</param>
    /// <returns>Indented text.</returns>
    public static string Indent(this string text, int level = 1)
      => new string('\t', level) + text.Replace("\n", "\n" + new string('\t', level));

    /// <summary>
    /// Repeats string number of times.
    /// </summary>
    /// <param name="text">Text to repeat.</param>
    /// <param name="times">Number of repetitions.</param>
    /// <returns>Repeated text.</returns>
    public static string Repeat(this string text, int times = 2)
      => string.Concat(Enumerable.Repeat(text, times));


    /// <summary>
    /// Stringifies enumerable elements using element ToString method.
    /// </summary>
    /// <param name="xs">Enumerable to stringify</param>
    /// <typeparam name="T">Objects</typeparam>
    /// <returns>String in format: [x1, x2, x3, ..., xN]</returns>
    public static string Stringify<T>(
      this IEnumerable<T> xs 
    ) => $"[{string.Join(", ", xs)}]";


    public static string StringifyPretty<T>(
      this IEnumerable<T> xs,
      Func<T,string> stringify
    ) => (xs == null || xs.Any() == false) ? "[]" :
      "[\n" 
      + string.Join(",\n", xs.Select(stringify)).Indent(1)
      + "\n]";
      
    public static string StringifyPretty<T>(
      this IEnumerable<T> xs
    ) => (xs == null || xs.Any() == false) ? "[]" :
      "[\n" 
      + string.Join(",\n", xs).Indent(1)
      + "\n]";

    /// <summary>
    /// Stringifies enumerable elements using provided function.
    /// </summary>
    /// <param name="xs">Enumerable to stringify</param>
    /// <param name="print">Function to stringify element</param>
    /// <typeparam name="T">Objects</typeparam>
    /// <returns>String in format: [x1, x2, x3, ..., xN]</returns>
    public static string Stringify<T>(
      this IEnumerable<T> xs, 
      Func<T,string> stringify
    ) => $"[{string.Join(", ", xs.Select(stringify))}]";

    
    public static string Lf(this string text) => text + "\n";
  }
}
#nullable restore