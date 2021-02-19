using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable

namespace play.Auth.Diagnostics
{

  public static class ListDiag
  {
    public static string ListToString<T>(
      IEnumerable<T> list, 
      string indent = ""
    ) 
    {
      return ListToString(
        list: list, 
        indent: indent, 
        valueToString: (x) => x.ToString()
      );
    }
    public static string ListToString<T>(
      IEnumerable<T> list, 
      string indent,
      Func<T, string> valueToString
    ) 
    {
      return "\n" + indent 
      + string.Join(
        separator: "\n" + indent, 
        values: list.Select(valueToString)
      );
    }

  }

}