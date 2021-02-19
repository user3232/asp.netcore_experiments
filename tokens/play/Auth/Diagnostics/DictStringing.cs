using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable

namespace play.Auth.Diagnostics
{

  public static class DictDiag
  {

    public static string DictToString<K, V>(
      Dictionary<K, IEnumerable<V>> dict, 
      string indent = ""
    ) 
    {
      return DictToString(
        dict: dict,
        indent: indent,
        keyToString: (x) => x.ToString(),
        valueToString: (xs) => string.Join(
          separator: ", ", 
          values: xs.Select(x => x.ToString())
        )
      );
    }
    public static string DictToString<K, V>(
      Dictionary<K, V> dict, 
      string indent = ""
    ) 
    {
      return DictToString(
        dict: dict,
        indent: indent,
        keyToString: (x) => x.ToString(),
        valueToString: (x) => x.ToString()
      );
    }
    public static string DictToString<K, V>(
      Dictionary<K, V> dict, 
      string indent,
      Func<K,string> keyToString,
      Func<V,string> valueToString
    ) 
    {
      return "\n" + indent 
      + string.Join(
        separator: "\n" + indent,
        values: dict.Select(
          kv => $"{keyToString(kv.Key)} = {valueToString(kv.Value)}"
        )
      );
    }

  }

}