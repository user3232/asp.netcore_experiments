using Base = System;  
using Net = System.Net;  
using Cache = System.Net.Cache;  
using Io = System.IO;  

// This is commented example from:
// https://docs.microsoft.com/en-us/dotnet/framework/network-programming/how-to-set-cache-policy-for-a-request
namespace SAuth2.Sockets.Examples
{

  public static class CacheExample  
  {
    public static void UseCacheForOneDay(Base.Uri resource)  
    {  
      Cache.HttpRequestCachePolicy requestTimePolicy =      // Create a policy that allows items
        new Cache.HttpRequestCachePolicy(                   // in the cache to be used if they
          cacheAgeControl: Cache.HttpCacheAgeControl.MaxAge,// have been cached one day or less.
          ageOrFreshOrStale: Base.TimeSpan.FromDays(1)      // This is default time-based cache
        );                                                  // policy.

      Cache.HttpRequestCachePolicy requestLocationPolicy =  // Exemplary location policy
        new Cache.HttpRequestCachePolicy(
          level: Cache.HttpRequestCacheLevel.CacheIfAvailable
        );

      Net.WebRequest request = Net.WebRequest               // Factory web request
        .Create(requestUri: resource);  
        
      request.CachePolicy = requestTimePolicy;              // Set the policy for this request only.
      Net.HttpWebResponse response =                        // Request content.
        (Net.HttpWebResponse)request.GetResponse();  
        
      Base.Console.WriteLine(                               // Determine whether the response 
        "The response was retrieved from the cache : "      // was retrieved from the cache.
        + response.IsFromCache
      );  
      Io.Stream s = response.GetResponseStream ();          // Get response streams.
      Io.StreamReader reader = new Io.StreamReader(s);      // Apply StreamReader to response stream.
        
      Base.Console.WriteLine(reader.ReadToEnd());           // Display the requested resource.
      reader.Close ();                                      // Close stream reader.
      s.Close();                                            // Close response stream.
      response.Close();                                     // Close response.
    }  

    public static void Main(string[] args)                  // Call with:
    {                                                       // ./app "localhost:5000/test"
      if (args == null || args.Length != 1)                 // where some server is running
      {                                                     // on localhost port 5000  
        Base.Console.WriteLine (                            // with some defined route test .
          "You must specify the URI to retrieve."           // App command line argument must be
        );                                                  // valid uri.
        return;  
      }  
      UseCacheForOneDay(resource: new Base.Uri(args[0]));  
    }  
  }  

}