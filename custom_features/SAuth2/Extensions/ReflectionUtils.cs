#nullable enable

using System.Linq;                // Enumerable
using System;                     // Func<...>
using System.Collections.Generic; // IEnumerable
using System.Reflection;          // BindingFlags

namespace SAuth2.Extensions
{

  public static class ReflectionUtils
  {

    public static string ObjectClassName(object o) 
      => o.GetType()?.FullName ?? "";

    public static string InstancesClassNames<T>(this IEnumerable<T> xs)
      where T : notnull
     => xs.StringifyPretty<T>(x => ObjectClassName(x));


    public static (bool, T) GetInstanceField<T>(this object instance, string fieldName)
    {
      var maybeFild = instance.GetType().GetField(
        name: fieldName,
        bindingAttr: 
          BindingFlags.GetField
          | BindingFlags.Instance 
          | BindingFlags.Public 
          | BindingFlags.NonPublic
          | BindingFlags.Static
      )?.GetValue(instance);

      if(maybeFild == null) return (false, default(T)!);
      if(maybeFild is T fildT) return (true, fildT);
      return (false, default(T)!);
    }

    public static (bool, T) GetInstanceProperty<T>(this object instance, string fieldName)
    {
      var maybeFild = instance.GetType().GetProperty(
        name: fieldName,
        bindingAttr: 
          BindingFlags.GetProperty
          | BindingFlags.Instance 
          | BindingFlags.Public 
          | BindingFlags.NonPublic
          | BindingFlags.Static
      )?.GetValue(instance);

      if(maybeFild == null) return (false, default(T)!);
      if(maybeFild is T fildT) return (true, fildT);
      return (false, default(T)!);
    }

  }

}

#nullable restore
