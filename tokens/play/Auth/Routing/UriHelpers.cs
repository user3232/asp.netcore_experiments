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

  public static class UriHelpers
  {

    public static Func<string, string> IdentityFunction = (x) => x;
    public static Func<string, string> UnescapeFuncUri = Uri.UnescapeDataString;
    public static Func<string, string> EscapeFuncUri = Uri.EscapeDataString;
    public static Func<string, string> UnescapeFuncHttp = HttpUtility.UrlDecode;
    public static Func<string, string> EscapeFuncHttp = HttpUtility.UrlEncode;
    public static Func<string, string> UnescapeFuncWeb = WebUtility.UrlDecode;
    public static Func<string, string> EscapeFuncWeb = WebUtility.UrlEncode;
    public static Func<string, NameValueCollection> QueryStringParser = HttpUtility.ParseQueryString;


    public static Dictionary<string, IEnumerable<string>> ToUnescapedQuery(
      Uri fromUri
    )
    {
      return ToUnescapedQuery(
        fromUriString: fromUri.Query, 
        withKeyValueUnescapeMap: UnescapeFuncUri
      );
    }
    public static Dictionary<string, IEnumerable<string>> ToUnescapedQuery(
      string fromUriString,
      Func<string, string> withKeyValueUnescapeMap
    )
    {
      var encodedQuery = HttpUtility.ParseQueryString(
        query: fromUriString, 
        encoding: Encoding.UTF8
      );
      return ToQuery(
        fromNameValueCollection: encodedQuery, 
        withKeyValueMap: withKeyValueUnescapeMap
      );
    }


    public static string ToUriQueryString(
      Dictionary<string,IEnumerable<string>> fromUnescapedQuery,
      Func<string, string> withKeyValueEscapeMap
    )
    {
      var components = fromUnescapedQuery
      .Select(                                        // key and values escaping
        kv => (
          Key: withKeyValueEscapeMap(kv.Key), 
          Value: kv.Value.Select(selector: withKeyValueEscapeMap)
        )
      ) 
      .SelectMany(                                    // flatting
        kvEnc => kvEnc.Value.Select(                  
          valueEnc => $"{kvEnc.Key}={valueEnc}"       // query string encoding
        )
      );

      return "?" + string.Join(separator: "&", values: components);
    }

    

    



    public static Dictionary<string, IEnumerable<string>> ToQuery(
      NameValueCollection fromNameValueCollection,
      Func<string, string> withKeyValueMap
    )
    {
      var dict = new Dictionary<string, IEnumerable<string>>();
      foreach (var key in fromNameValueCollection.AllKeys)
      {
        var keyDecoded = withKeyValueMap(key);
        var valuesDecoded = fromNameValueCollection.GetValues(name: key)
          .Select(selector: withKeyValueMap);
        dict.Add(key: keyDecoded, value: valuesDecoded);
      }
      return dict;
    }

    public static NameValueCollection  ToQueryNvc(
      Dictionary<string, IEnumerable<string>> fromQuery,
      Func<string, string> withKeyValueMap
    )
    {
      var nvc = new NameValueCollection();
      foreach (var kv in fromQuery)
      {
        var key = withKeyValueMap(kv.Key);
        foreach (var value in kv.Value)
        {
            nvc.Add(name: key, value: withKeyValueMap(value));
        }
      }
      return nvc;
    }





    public static Dictionary<string,IEnumerable<string>> ToQuery(
      Dictionary<string,IEnumerable<string>> fromQuery,
      Func<string, string> withKeyValueMap
    )
    {
      var dict = new Dictionary<string, IEnumerable<string>>(fromQuery.Count);
      
      var mappedQuery = fromQuery.Select(
        kv => KeyValuePair.Create(
          key: withKeyValueMap(kv.Key), 
          value: kv.Value.Select(selector: withKeyValueMap)
        )
      );
      
      return new Dictionary<string, IEnumerable<string>>(collection: mappedQuery);
    }

    public static NameValueCollection ToQueryNvc(
      NameValueCollection fromQueryNvc,
      Func<string, string> withKeyValueMap
    )
    {
      var mappedQuery = new NameValueCollection();
      foreach (var k in fromQueryNvc.AllKeys)
      {
        var kMapped = withKeyValueMap(k);
        foreach (var v in fromQueryNvc.GetValues(k))
        {
          mappedQuery.Add(kMapped, withKeyValueMap(v));
        }
      }
      return mappedQuery;
    }








    public static IEnumerable<string> ToPathSegments(
      string fromUriPath,
      Func<string, string> withSegmentMap
    )
    {
      return fromUriPath.Split(
        separator: "/", 
        options: StringSplitOptions.RemoveEmptyEntries
      ).Select(selector: withSegmentMap);
    }

    public static string ToUriPathString(
      IEnumerable<string> fromSegments,
      Func<string, string> withSegmentMap
    )
    {
      return "/" + 
        string.Join(
          separator: "/", 
          values: fromSegments.Select(withSegmentMap)
        );
    }

  }
}