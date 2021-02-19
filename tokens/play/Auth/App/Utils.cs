using System;                           // UriBuilder
                                        // Uri
using System.Collections.Generic;       // List, Dictionary
using System.Diagnostics.CodeAnalysis;  // AllowNullAttribute
                                        // DisallowNullAttribute


namespace play.Auth.App
{

  public class Equaliser<T> : EqualityComparer<T>
  {
    public Func<T,T,bool> Eq;
    public Func<T,int> Hash;
    public override bool Equals([AllowNull] T x, [AllowNull] T y)
    {
      if(x == null) return false;
      if(y == null) return false;
      return Eq(x,y);
    }
    public override int GetHashCode([DisallowNull] T obj) => Hash(obj);

    public static Equaliser<T> New<S>(
      Func<T,S> Select
    ) => new Equaliser<T>() {
        Eq = (x, y) => Select(x).Equals(Select(y)),
        Hash = obj => Select(obj).GetHashCode()
      };
    public static Equaliser<T> New<S1,S2>(
      Func<T,S1> Select1,
      Func<T,S2> Select2
    ) => new Equaliser<T>() {
        Eq = (x, y) => Select1(x).Equals(Select1(y)) 
                       && Select2(x).Equals(Select2(y)),
        Hash = obj => Select1(obj).GetHashCode() 
                      ^ Select2(obj).GetHashCode()
      };
    public static Equaliser<T> New<S1,S2,S3>(
      Func<T,S1> Select1,
      Func<T,S2> Select2,
      Func<T,S3> Select3
    ) => new Equaliser<T>() {
        Eq = (x, y) => Select1(x).Equals(Select1(y)) 
                       && Select2(x).Equals(Select2(y))
                       && Select3(x).Equals(Select3(y)),
        Hash = obj => Select1(obj).GetHashCode() 
                      ^ Select2(obj).GetHashCode()
                      ^ Select3(obj).GetHashCode()
      };
    public static Equaliser<T> New<S1,S2,S3,S4>(
      Func<T,S1> Select1,
      Func<T,S2> Select2,
      Func<T,S3> Select3,
      Func<T,S4> Select4
    ) => new Equaliser<T>() {
        Eq = (x, y) => Select1(x).Equals(Select1(y)) 
                       && Select2(x).Equals(Select2(y))
                       && Select3(x).Equals(Select3(y))
                       && Select4(x).Equals(Select4(y)),
        Hash = obj => Select1(obj).GetHashCode() 
                      ^ Select2(obj).GetHashCode()
                      ^ Select3(obj).GetHashCode()
                      ^ Select4(obj).GetHashCode()
      };
  }


  public class OwsomeClass
  {
    public string Name;
    public string LastName;
    public int    SSN;
    public Gender GenderE;
    public bool   SomeFlag;

    public enum Gender {male, female}
  }

  public static class EqualizerExample
  {
    public static void Example()
    {
      IEqualityComparer<OwsomeClass> eqComparer = 
        Equaliser<OwsomeClass>.New(
          x => x.Name,
          x => x.GenderE
        );

      var hashSet = new HashSet<OwsomeClass>(comparer: eqComparer);
    }
  }

}