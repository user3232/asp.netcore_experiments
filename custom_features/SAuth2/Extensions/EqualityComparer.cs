#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using Debug = System.Diagnostics.Debug;
using System.Linq;

namespace SAuth2.Extensions
{

  public class EqualityComparer<T> : IEqualityComparer<T>
  {
    public List<Func<T,T,bool>> ProjectedEqualisers {get; private set;}
    public List<Func<T,int>>    ProjectedHashers {get; private set;}

    public EqualityComparer(
      List<Func<T,T,bool>> ProjectedEqualisers,
      List<Func<T,int>>    ProjectedHashers
    )
    {
      this.ProjectedEqualisers = ProjectedEqualisers;
      this.ProjectedHashers    = ProjectedHashers;
    }

    public bool Equals([AllowNull] T x, [AllowNull] T y)
    {
      if(x == null && y == null) return true;
      else if(x == null || y == null) return false;
      else return ProjectedEqualisers.All(equaliser => equaliser(x,y));
    }

    public int GetHashCode([DisallowNull] T obj)
      => ProjectedHashers.Select(hash => hash(obj)).Aggregate(func: (acc, h) => acc ^ h);
    
  }


  public static class EqualsAndGetHashCode
  {
    public static Func<T,T,bool> ReturnEquals<T, T1>(
      this Func<T,T1> fromProjection
    ) => (T x, T y) =>
      {
        var px = fromProjection(x);
        var py = fromProjection(y);
        if(px == null && py == null) return true;
        else if(px == null || py == null) return false;
        else return px.Equals(py);
      };

    public static Func<T,int> ReturnGetHashCode<T, T1>(
      this Func<T,T1> fromProjection
    ) => obj => fromProjection(obj)?.GetHashCode() ?? default(int);

    public static Func<T,T,bool> BindToEquals<T, T1>(
      this Func<T,T,bool> toThis,
      Func<T,T1> projection
    ) => (x, y) => toThis(x, y) && projection.ReturnEquals()(x, y);

    public static Func<T,int> BindToHash<T, T1>(
      this Func<T,int> toThis,
      Func<T,T1> projection
    ) => obj => toThis(obj) ^ projection.ReturnGetHashCode()(obj);
  }



  public static class EqualityComparer
  {
    
    public static EqualityComparer<T> ReturnEqualityComparer<T,T1>(
      this Func<T,T1> fromProjection
    ) => new EqualityComparer<T>(
      new List<Func<T,T,bool>>(){fromProjection.ReturnEquals()}, 
      new List<Func<T,int>>(){fromProjection.ReturnGetHashCode()}
    );

    public static EqualityComparer<T> BindMutablyToEqualityComparer<T,T1>(
      this EqualityComparer<T> toThis,
      Func<T,T1> projection
    ) 
    {
      toThis.ProjectedEqualisers.Add(projection.ReturnEquals());
      toThis.ProjectedHashers.Add(projection.ReturnGetHashCode());
      return toThis;
    }

    public static EqualityComparer<T> BindImmutablyToEqualityComparer<T,T1>(
      this EqualityComparer<T> toThis,
      Func<T,T1> projection
    ) => new EqualityComparer<T>(
      new List<Func<T,T,bool>>(toThis.ProjectedEqualisers){projection.ReturnEquals()}, 
      new List<Func<T,int>>(toThis.ProjectedHashers){projection.ReturnGetHashCode()}
    );

    


    private class TestE 
    {
      public string a = ""; 
      public int    b = 0; 
      public double c = 0;
    }

    private static void Test()
    {
      var x = new TestE {a = "yo", b = 2, c = 33.3};
      var y = new TestE {a = "yo", b = 3, c = 33.3};
      var z = new{a = "yo", b = 3, c = 33.3};

      EqualityComparer<TestE> eq1 = EqualityComparer
        .ReturnEqualityComparer((TestE t) => t.a)
        .BindImmutablyToEqualityComparer((TestE t) => t.b)
        .BindImmutablyToEqualityComparer((TestE t) => t.c);
      EqualityComparer<TestE> eq2 = EqualityComparer
        .ReturnEqualityComparer((TestE t) => t.a)
        .BindMutablyToEqualityComparer((TestE t) => t.c);

      Debug.Assert(eq1.Equals(x,y) == false);
      Debug.Assert(eq2.Equals(x,y) == true);


    }
    
  }

}
#nullable restore