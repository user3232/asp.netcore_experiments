#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System;
using Debug = System.Diagnostics.Debug;
using System.Linq;

namespace SAuth2.Extensions.Funct
{

  public class EqualityComparerFunct<T> : IEqualityComparer<T>
  {
    public Func<T,T,bool> ProjectedEquals {get; private set;}
    public Func<T,int>    ProjectedGetHashCode {get; private set;}

    public EqualityComparerFunct(
      Func<T,T,bool> ProjectedEquals,
      Func<T,int>    ProjectedGetHashCode
    )
    {
      this.ProjectedEquals      = ProjectedEquals;
      this.ProjectedGetHashCode = ProjectedGetHashCode;
    }

    public bool Equals([AllowNull] T x, [AllowNull] T y)
    {
      if(x == null && y == null) return true;
      else if(x == null || y == null) return false;
      else return ProjectedEquals(x,y);
    }

    public int GetHashCode([DisallowNull] T obj)
      => ProjectedGetHashCode(obj);
    
  }

  



  public static class EqualityComparerFunct
  {

    public static EqualityComparerFunct<T> Add<T, T1>(
      this EqualityComparerFunct<T> toThis,
      Func<T,T1> projection
    ) => new EqualityComparerFunct<T>(
      toThis.ProjectedEquals.BindToEquals(projection), 
      toThis.ProjectedGetHashCode.BindToHash(projection)
    );


    public static EqualityComparerFunct<T> NewEqualityComparerFunct<T, T1>(
      this Func<T,T1> fromProjection
    ) => new EqualityComparerFunct<T>(
      fromProjection.ReturnEquals(), 
      fromProjection.ReturnGetHashCode()
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

      Func<TestE, string> projA = (TestE t) => t.a;
      Func<TestE, int>    projB = (TestE t) => t.b;
      Func<TestE, double> projC = (TestE t) => t.c;
      EqualityComparerFunct<TestE> eq1 = 
        EqualityComparerFunct.NewEqualityComparerFunct(projA).Add(projB).Add(projC);
      EqualityComparerFunct<TestE> eq2 = 
        EqualityComparerFunct.NewEqualityComparerFunct(projA).Add(projC);

      Debug.Assert(eq1.Equals(x,y) == false);
      Debug.Assert(eq2.Equals(x,y) == true);

    }

  }

}
#nullable restore