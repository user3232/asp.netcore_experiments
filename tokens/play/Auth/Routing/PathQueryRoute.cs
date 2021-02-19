using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Text.RegularExpressions; // Regex
using System.Collections.Specialized; // NameValueCollection
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility
using System.Text;                    // Encoding

namespace play.Auth.Routing
{


  public class PathQueryRoute
  {
    public string Raw { get; set; }
    public List<string> Path { get; set; }
    public Dictionary<string, string> Query { get; set; }

    public string Display()
    {
      return @$"
route: {Raw}
path:  {ListToString(Path, "  ")}
query: {DictToString(Query, "  ")}
";
    }

    public static PathQueryRoute Parse(string route)
    {
      // route = @"/api/get?id=1&search=yo"
      // pattern: "^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?",
      // //         12            3  4          5       6  7        8 9
      // 1 = protocol (http:)
      // 2 = protocol value (http)
      // 3 = hosty (//google.com)
      // 4 = host name (google.com)
      // 5 = path (/pub/ifet/uri/)
      // 6 = query (?id=1&search=yoyo)
      // 7 = query value (id=1&search=yoyo)
      // 8 = fragment (#anything)
      // 9 = fragment value (anything)
      var parseUrl = new Regex(
        pattern: @"^
          (?'proto'(?'protoval'[^:/?#]+):)?
          (?'host'//(?'hostname'[^/?#]*))?
          (?'path'[^?#]*)
          (?'query'\\?(?'queryval'[^#]*))?
          (?'fragment'#(?'fragmentname'.*))?
        ",
        options: RegexOptions.CultureInvariant
      );

      var parsedUrl = parseUrl.Match(input: route);
      var path = parsedUrl.Groups["path"].Value;
      var query = parsedUrl.Groups["queryval"].Value;

      var parsedUrlQuery = Regex.Matches(
        input: query,
        pattern: @"(?'separator'\?|\&)(?'name'[^=]+)=(?'val'[^&]+)",
        options: RegexOptions.CultureInvariant
      );


      var queryParams = new Dictionary<string, string>();
      // var queryParams = List<(string,string)>();
      // dictionary => create key if not presend => add val to values array
      foreach (Match match in parsedUrlQuery)
      {
        queryParams.Add(
          key: match.Groups["name"].Value,
          value: match.Groups["val"].Value
        );
      }

      var subPaths = path.Split(separator: "/");

      return new PathQueryRoute
      {
        Raw = route,
        Path = subPaths.ToList(),
        Query = queryParams
      };
    }

    public static string DictToString<K, V>(
      Dictionary<K, V> dict, 
      string indent = ""
    ) =>
      "\n" + indent + string.Join(
        separator: "\n" + indent,
        values: dict.Select(kv => $"{kv.Key.ToString()} = {kv.Value.ToString()}")
      );

    public static string ListToString<T>(
      IEnumerable<T> list, string indent = ""
    ) =>
      "\n" + indent + string.Join(separator: "\n" + indent, values: list);
  }

}