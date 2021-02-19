using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable
using System.Text.RegularExpressions; // Regex
using System.Collections.Specialized; // NameValueCollection
using System.Net;                     // WebUtility
using System.Web;                     // HttpUtility
using System.Text;                    // Encoding

namespace play.Auth.Polies
{

  public static class Ftrs
  {
    public static bool IsRegisteredUser(
      List<string> registeredUsers,
      string user
    ) => registeredUsers.Any(registeredUser => registeredUser == user);

    public static bool IsSelectedUser(
      // List<User> regUsers,
      string equalTo,
      string user
    ) => user == equalTo;

    public static bool IsSelectedUser(
      // List<User> regUsers,
      List<string> equalTo,
      string user
    ) => equalTo.Any(u => u == user);

    public static bool IsInputShorterThan(
      int numberOfChars,                // for (-inf, 0] always false
      string input
    ) => numberOfChars >= 0             // positive -> check others,
                                        // negative -> false
      && (        
        string.IsNullOrEmpty(input)     //   shorter than any positive -> true
        || input.Length < numberOfChars //   shorter than specified    -> true
      );
    
    // IsInUserScope
    // IsOwner
    // IsInGroup


    // CanCreateGroup
    // CanAddUsersToGroups
    // CanAddGroups
  }

}