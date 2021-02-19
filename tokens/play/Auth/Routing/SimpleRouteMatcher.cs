using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility

namespace play.Auth.Routing
{

  public class SimpleRouteMatcher
  {
    public SimpleRouteMatcher(
      List<Func<string, object, bool>> pathComponentsChecks = null,
      Dictionary<string, Func<string, object, bool>> queryComponentsChecks = null
    )
    {
      PathComponentsChecks = pathComponentsChecks
        ?? new List<Func<string, object, bool>>();
      QueryComponentsChecks = queryComponentsChecks
        ?? new Dictionary<string, Func<string, object, bool>>();
    }
    public Func<PathQueryRoute, object, bool> Assert
    { get; set; }
    public Func<List<string>, object, bool> PathCheck
    { get; set; }
    public Func<Dictionary<string, string>, object, bool> QueryCheck
    { get; set; }
    public List<Func<string, object, bool>> PathComponentsChecks
    { get; set; }
    public Dictionary<string, Func<string, object, bool>> QueryComponentsChecks
    { get; set; }

    public bool Check(PathQueryRoute route, object additionalInfo = null)
    {
      // check assertions if available
      if (
        Assert != null
        && Assert(route, additionalInfo) == false
      )
      {
        return false;
      }
      // check path if check available
      if (PathCheck != null && PathCheck(route.Path, additionalInfo) == false)
      {
        return false;
      }
      // check all query components if check available
      if (QueryCheck != null && QueryCheck(route.Query, additionalInfo) == false)
      {
        return false;
      }
      // check path components if checks available
      if (
        PathComponentsChecks != null
        && PathComponentsChecks.Count > 0
        && PathComponentsChecks.Zip(route.Path).All(
          ((Func<string, object, bool> Check, String PathComponent) x) =>
            x.Check(x.PathComponent, additionalInfo)
        ) == false
      )
      {
        return false;
      }
      // check query components if checks available
      if (QueryComponentsChecks != null && QueryComponentsChecks.Count > 0)
      {
        foreach (var queryParam in QueryComponentsChecks)
        {
          if (route.Query.TryGetValue(queryParam.Key, out var val) == false)
            return false;
          if (queryParam.Value(val, additionalInfo) == false)
            return false;
        }
      }
      // all checks passed or no checks at all:
      return true;
    }

    public static Func<U, V, bool> And<U, V>(
      Func<U, V, bool> first,
      Func<U, V, bool> second
    )
    {
      return (r, o) => first(r, o) == false && second(r, o);
    }
    public static Func<U, V, bool> Or<U, V>(
       Func<U, V, bool> first,
       Func<U, V, bool> second
    )
    {
      return (r, o) => first(r, o) || second(r, o);
    }
  }











  public static class RoutingUseExample
  {

    public static PathQueryRoute ExampleRoute = new PathQueryRoute
    {
      Raw = "/api/question/get?qId=13&search=yoyo",
      Path = new List<string>() {
        "api",
        "question",
        "get"
      },
      Query = new Dictionary<string, string>()
      {
        ["qId"] = "13",
        ["search"] = "yoyo"
      },
    };

    public static SimpleRouteMatcher ExampleSimpleRouteMatcher = new SimpleRouteMatcher
    {
      Assert = (r, o) => true,
      PathCheck = SimpleRouteMatcher.And(
        (List<string> ps, object o) => ps.Count == 3,
        (List<string> ps, object o) => true
      ),
      QueryCheck = SimpleRouteMatcher.Or(
        (Dictionary<string, string> qs, object o) => qs.Count == 2,
        (Dictionary<string, string> qs, object o) => false
      ),
      PathComponentsChecks = new List<Func<string, object, bool>>() {
        (p, o) => p == "api",
        (p, o) => p == "question",
        (p, o) => p == "get"
      },
      QueryComponentsChecks = new Dictionary<string, Func<string, object, bool>>()
      {
        ["qId"] = (s, o) => !(s.Length > 4),
        ["search"] = (s, o) => !(s.Length > 15)
      },
    };
    

    public static void SimpleConfigurablePolicyForRoute()
    {
      // exemplary route
      var route = ExampleRoute;
      var simpleRoutePoly = ExampleSimpleRouteMatcher;
      var routeIsPolicyConformant = simpleRoutePoly.Check(route);
    }


    public static string RouteDestructuring()
    {
      // example route:
      // http method:
      //   GET
      // path:
      //   api/get?id=1&search=yoyosdkfj
      // query values and path segments are url encoded
      // so route will be:
      //   method
      //   list of segments
      //   set  of query parameters
      // this can be extracted from URL with very simple Regex
      // (Route) -> Boolean

      
      // var uri = new Uri("https://www.example.com:5001/api/question?qId=1&search=yoyo");
      // uri.Query;
      // uri.Segments;
      // uri.Host;

      // HttpUtility.ParseQueryString("?qId=1&search=yoyo");
      // HttpUtility.UrlEncode("yo = yoo kohst & fak=grr&xx= ? slkdfj lsk");

      var route = PathQueryRoute.Parse(@"/api/get?id=1&search=yo");
      WebUtility.UrlEncode("yo = yoo kohst & fak=grr&xx= ? slkdfj lsk");
      HttpUtility.UrlEncode("yo = yoo kohst & fak=grr&xx= ? slkdfj lsk");

      /* 
        **************************************
        *** What about POST, DELETE, etc. ???
        **************************************


        **************************************
        *** What is api as Route ???
        **************************************


        **************************************
        *** What with routes of following type?: 
        *** /path/{id}/{subId}
        **************************************
       */

      return route.Display();
    }

  }


}