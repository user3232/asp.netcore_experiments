using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Text.RegularExpressions; // Regex
using System.Collections.Specialized; // NameValueCollection
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility
using System.Text;                    // Encoding

using play.Auth.Routing;

namespace play.Auth.Polies
{

  public class ReqData
  {
    public string UserName = "mike";
    public string CallName = "api.show.question";
    public Dictionary<string, string> CallArgs = 
     new Dictionary<string, string>()
     {
       ["questionTitle"] = "How are you?"
     };
  }


  public static class RequestParsers
  {
    public static ReqData Parse(string user, Uri uri)
    {
      var r = RouteEx.From(uri.PathAndQuery);
      return new ReqData() 
      {
        UserName = user,
        CallName = string.Join('.', r.Path),
        CallArgs = r.Query.ToDictionary(
          kv => kv.Key, 
          kv => kv.Value.FirstOrDefault()
        )
      };
    }
  }

  public class ShowQuestionReqRespBlocks<TReqData, TFtrAddData, TCallAddData>
  {
    public string                             RouteTemplate = Rts.ShowQuestion;
    public Func<string, string, string>       Call = Apis.ShowQuestion;
    public Func<List<string>, string, bool>   Filter = Ftrs.IsRegisteredUser;


    public Func<ReqData, bool>                RouteMatches;
    public Func<ReqData, TReqData>            BindReqArgs;
    public Func<TFtrAddData>                  GetAdditionalFilterData;
    public Func<TCallAddData>                 GetAdditionalCallData;
    public Func<TReqData, string>             ApplyCall;
    public Func<TFtrAddData, TReqData, bool>  ApplyFilter;
  }


}