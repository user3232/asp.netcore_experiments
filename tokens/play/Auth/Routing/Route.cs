using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Text.RegularExpressions; // Regex
using System.Collections.Specialized; // NameValueCollection
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility
using System.Text;                    // Encoding
using static play.Auth.Diagnostics.DictDiag;
using static play.Auth.Diagnostics.ListDiag;


namespace play.Auth.Routing
{

  public class Route
  {
    public Uri Raw {get; set;}
    public string[] Path { get; set; }
    public Dictionary<string, IEnumerable<string>> Query { get; set; }

    public string ToUriPathQueryString() 
    {
      return UriHelpers.ToUriPathString(
        fromSegments: Path, 
        withSegmentMap: UriHelpers.EscapeFuncUri
      ) 
      + UriHelpers.ToUriQueryString(
        fromUnescapedQuery: Query, 
        withKeyValueEscapeMap: UriHelpers.EscapeFuncUri
      );
    }

    public string Display()
    {
      return $"route: {Raw}" + "\n"
           + $"path:  {ListToString(Path, "  ")}" + "\n"
           + $"query: {DictToString(Query, "  ")}" + "\n";
    }
  }


  public class RouteEx
  {

    public static Route From(string uriPathQueryString) 
    {
      var uri = new Uri(uriPathQueryString);
      
      return new Route() {
        Raw = uri,
        Path = uri.Segments.Select(selector: UriHelpers.UnescapeFuncUri).ToArray(),
        Query = UriHelpers.ToUnescapedQuery(
          fromUriString: uri.Query, 
          withKeyValueUnescapeMap: UriHelpers.UnescapeFuncUri
        )
      };
    }

    public static Route Parse(string route)
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


      var queryParams = new Dictionary<string, IEnumerable<string>>();
      // var queryParams = List<(string,string)>();
      // dictionary => create key if not presend => add val to values array
      foreach (Match match in parsedUrlQuery)
      {
        queryParams.Add(
          key: match.Groups["name"].Value,
          value: new string[] {match.Groups["val"].Value}
        );
      }

      var subPaths = path.Split(separator: "/");

      return new Route
      {
        Raw = new Uri(route),
        Path = subPaths.ToArray(),
        Query = queryParams
      };
    }
  }



  public static class RouteUsageExample
  {
    public static void Example()
    {
      var uri = new Uri("https://www.example.com:5001/api/question?qId=1&search=yoyo");

      var parsed = RouteEx.Parse(uri.OriginalString);
      var httpParsed = RouteEx.From(uri.OriginalString);

      var displayed = httpParsed.Display();
      var generatedEscaped = parsed.ToUriPathQueryString();
    }
  }


}