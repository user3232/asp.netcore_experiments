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

  public class PolicyManagerExample
  {

    public class User 
    {
      public string Name;
      public HashSet<string> Attributes;
      public static User New(string Name, HashSet<string> Attributes) {
        return new User() {
          Name = Name,
          Attributes = Attributes
        };
      }
      public static HashSet<string> HashSet(params string[] items) =>
        new HashSet<string>(items == null ? items : new string[]{});
    }

    public class AllowanceSum
    {
      public string UserName;
      public bool PariallyAllowed;
      public IEnumerable<string> RestrictedTo;
      public IEnumerable<string> RestrictionRequires;
      public bool IsFullyAllowed => 
        PariallyAllowed && RestrictedTo != null && RestrictedTo.Any() == false;
    }

    public class RequirementsRestrictions
    {
      public IEnumerable<string> Requirements;
      public IEnumerable<string> Restrictions;
    }

    public class AttributesRestrictions
    {
      public IEnumerable<string> Attributes;
      public IEnumerable<string> Restrictions;
    }

    public List<User> Users = new List<User>()
    {
      new User() { Name = "mike",     Attributes = new HashSet<string>() {"reader"} }, 
      new User() { Name = "tom",      Attributes = new HashSet<string>() {"reader", "writer"} }, 
      new User() { Name = "tim",      Attributes = new HashSet<string>() {"reader", "engeneer"} }, 
      new User() { Name = "alber",    Attributes = new HashSet<string>() {"admin"} }, 
      User.New (   Name:  "igor",     Attributes: User.HashSet(           "writer", "engeneer") ),
      User.New (   Name:  "ivan",     Attributes: User.HashSet(           "reader", "engeneer") ),
      User.New (   Name:  "ian",      Attributes: User.HashSet(           "writer", "engeneer") ),
      User.New (   Name:  "izod",     Attributes: User.HashSet(           "reader") ),
      User.New (   Name:  "ildefons", Attributes: User.HashSet(           "writer") ),
    }; 

    public bool Filter(string pass, string user, string path)
    {
      var varifiedUser = Users.FirstOrDefault(u => u.Name == pass);
      if(user == null) return false; // not verified pass.user
      if
      (
        pass != user                                          // asks for diffrent user
        && varifiedUser.Attributes.Contains("admin") == false // not admin
      )          
        return false;              

      return true;
    }


    public IEnumerable<string> ListAllowedPassUsers() =>
      Users
        .Where(u => u.Attributes.Contains("admin"))  // only admin allowed
          .Select(u => u.Name);
          
    public bool IsAuthorizedPassUserNeeded() => true;
    
    public IEnumerable<AllowanceSum> ListPartiallyAllowedPassUsers()
    {
      foreach(var dbUser in Users)
      {
        if(dbUser.Attributes.Contains("admin"))
        {
          yield return new AllowanceSum() {
            UserName = dbUser.Name,
            PariallyAllowed = true, 
            RestrictedTo = Enumerable.Empty<string>(),
            RestrictionRequires = Enumerable.Empty<string>(),
          };
        }
        else
        {
          yield return new AllowanceSum() {
            UserName = dbUser.Name,
            PariallyAllowed = true,
            RestrictedTo = new List<string>() {
              $"user parameter equall to ${dbUser.Name}",
              "No restrictions",
            },
            RestrictionRequires =  new List<string>() {
              "User to be Authorized", 
              "User to have admin attribute",
            }
          };
        }
      }
    }
    
    public AllowanceSum IsPassUserPartiallyAllowed(string user)
    {
      var dbUser = Users.FirstOrDefault(u => u.Name == user);

      if(dbUser != default(User) )
        if(dbUser.Attributes.Contains("admin"))
          return new AllowanceSum() {
            UserName = dbUser.Name,
            PariallyAllowed = true, 
            RestrictedTo = Enumerable.Empty<string>()
          };
        else
          return new AllowanceSum() {
            UserName = dbUser.Name,
            PariallyAllowed = true,
            RestrictedTo = new List<string>() {
              $"user parameter equall to ${user}",
              "No restrictions",
            },
            RestrictionRequires =  new List<string>() {
              "User to be Authorized", 
              "User to have admin attribute",
            }
          };
      else 
        return new AllowanceSum() {
          UserName = null,
          PariallyAllowed = false, 
          RestrictedTo = new List<string>() {
            "Call is not allowed",        // restr for unauthorized
            "user parameter must equall to authorized user name", 
                                          // restr for authorized
            "No restrictions",
          },
          RestrictionRequires = new List<string>() {
            "No requirements",
            "User to be Authorized", 
            "User to have admin attribute",
          }
        };
    }

    public RequirementsRestrictions ListReqirementRestriction() =>
      new RequirementsRestrictions() 
      {
        Requirements = new List<string>() 
        { 
          "No requirements",
          "User to be Authorized", 
          "User to have admin attribute",
        },
        Restrictions = new List<string>() 
        { 
          "Call is not allowed",
          "Call must have user parameter equall to authorized pass.user name",
          "No restrictions",
        },
      };

    public AttributesRestrictions ListRequiredPassUserAttributes() =>
      new AttributesRestrictions() 
      {
        Attributes = new List<string>() 
        { 
          "",
          "authorized", 
          "authorized;admin",
        },
        Restrictions = new List<string>() 
        { 
          "Call is not allowed",
          "Call must have user parameter equall to authorized pass.user name",
          "No restrictions",
        },
      };


    public static void TryThis() {
      //
      
    }
  }

}