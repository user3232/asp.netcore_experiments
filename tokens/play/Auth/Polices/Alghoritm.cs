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

  public static class FtrBld
  {
    public static Func<Tx, bool> And<Tx>(
      this IEnumerable<Func<Tx, bool>> ands
    ) => x => ands.All(and => and(x));

    public static Func<Tx, bool> Or<Tx>(
      this IEnumerable<Func<Tx,bool>> ors
    ) => x => ors.Any(or => or(x));
  }

  // [todo] Play with it moore when time available!!!
  // [done] Filter Nodes transformars
  // [done] Brunching
  // [done] Enumeration, searching, filtering
  // [todo] Currying
  // [todo] Invrses
  public static class FtrCont
  {
    public class Data {}
    public static void Example() {}


    public static ITreeFilter<Tx> TreeFilterOfBrunchesNotRequiring<Tx>(
      ITreeFilter<Tx> tree,
      ITreeFilter<Tx> requirement // e.g. particular user
                                  // will it work with not PrimaryFilter??
    )
    {
      var notNeedingBrunches = TreeFilterToListFilters(tree)
        .Where(
          list => EnumerateRecursively(list).Any(filter => filter == requirement)
        );
      return Simplify(ListFiltersToTreeFilter(tree, notNeedingBrunches));
    }

    public static IEnumerable<string> RoutesAvailableOnlyWithReq<Tx>(
      IEnumerable<(string Route, ITreeFilter<Tx> Filter)> routsFilters,
      ITreeFilter<Tx> requirement // e.g. particular user
    )
    {
      foreach(var (r, tree) in routsFilters)
      {
        if(
          TreeFilterToListFilters(tree).All(
            list => EnumerateRecursively(list).Select(
              li => li switch {
                PrimaryFilter<Tx> pf => pf,
                AndLFilter<Tx>    af => af.Origin,
                OrLFilter<Tx>     of => of.Origin,
                _                    => null
              }
            ).Contains(requirement)
          )
        )
        {
          yield return r;
        }
      }
    }

    public static ITreeFilter<Tx> ListFiltersToTreeFilter<Tx>(
      ITreeFilter<Tx> shape,              // must be able to contain 
                                          // any list of lists
      IEnumerable<IListFilter<Tx>> lists
    )
    {
      // copy tree
      // enum trees
      // zip enumed trees
      // filter out primitives form zipped enums
      // filter out primitives from copied tree
      // add list elements with origin to copied tree
      //   by mutating copied tree elements mapped
      //   from inverse of zipping
      // done!

      var copy = Copy(shape);

      // creating map:
      var originEnum = EnumerateRecursively(shape);
      var copyEnum = EnumerateRecursively(copy);
      var zip = originEnum.Zip(copyEnum);
      var dictAnds = originEnum.Where(x => x is AndTFilter<Tx>)
        .Zip(copyEnum.OfType<AndTFilter<Tx>>())
          .ToDictionary(
          keySelector: (oc) => oc.First,
          elementSelector: (oc) => oc.Second
        );
      var dictOrs = originEnum.Where(x => x is OrTFilter<Tx>)
        .Zip(copyEnum.OfType<OrTFilter<Tx>>())
          .ToDictionary(
          keySelector: (oc) => oc.First,
          elementSelector: (oc) => oc.Second
        );

      // cleaning from primitives:
      // var copyEnumOrs = copyEnum.OfType<OrTFilter<Tx>>();
      // var copyEnumAnds = copyEnum.Where(c => c is AndTFilter<Tx>);
      foreach (var or in copyEnum.OfType<OrTFilter<Tx>>())
      {
        var orWithoutPirmComponents = 
          or.OrComponents.Where(orc => ! (orc is PrimaryFilter<Tx>));
        if(or.OrComponents.SequenceEqual(orWithoutPirmComponents) == false)
          or.OrComponents = new HashSet<ITreeFilter<Tx>>(orWithoutPirmComponents);
      }
      foreach (var and in copyEnum.OfType<AndTFilter<Tx>>())
      {
        var andWithoutPirmComponents = 
          and.AndComponents.Where(andc => ! (andc is PrimaryFilter<Tx>));
        if(and.AndComponents.SequenceEqual(andWithoutPirmComponents) == false)
          and.AndComponents = new HashSet<ITreeFilter<Tx>>(andWithoutPirmComponents);
      }

      // adding all lists elements to copy without any primitives:
      foreach (var list in lists)
      {
        // var listEnumAnds = Enumerate(list).OfType<AndLFilter<Tx>>();
        foreach(var and in Enumerate(list).OfType<AndLFilter<Tx>>())
        {
          dictAnds[and.Origin].AndComponents = new HashSet<ITreeFilter<Tx>>(
            dictAnds[and.Origin].AndComponents.Concat(
              and.AndComponents.OfType<PrimaryFilter<Tx>>()
            )
          );
        }
        // var listEnumOrs = Enumerate(list).OfType<OrLFilter<Tx>>();
        foreach(var or in Enumerate(list).OfType<OrLFilter<Tx>>())
        {
          dictOrs[or.Origin].OrComponents = new HashSet<ITreeFilter<Tx>>(
            dictOrs[or.Origin].OrComponents.Concat(
              Singleton(or.OrComponent).OfType<PrimaryFilter<Tx>>()
            )
          );
        }
      }
      
      // clean the copy and return
      // return Simplify(copy);
      return copy;
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    

    public static ITreeFilter<Tx> Simplify<Tx>(ITreeFilter<Tx> tree)
    {
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return primFilter;
        case OrTFilter<Tx> orTreeFilter:
          if(IsSingleton(orTreeFilter.OrComponents)) 
            return Simplify(orTreeFilter.OrComponents.First());
          else
          {
            var simplified = orTreeFilter.OrComponents
              .Where(IsNotEmpty).Select(Simplify);
            if(orTreeFilter.OrComponents.SequenceEqual(simplified))
              return orTreeFilter;
            else
              return new OrTFilter<Tx>() {
                OrComponents = new HashSet<ITreeFilter<Tx>>(simplified),
                Filter = x => simplified.All(f => f.Filter(x))
              };
          }
        case AndTFilter<Tx> andTreeFilter:
          if(IsSingleton(andTreeFilter.AndComponents)) 
            return Simplify(andTreeFilter.AndComponents.First());
          else
          {
            var simplified = andTreeFilter.AndComponents
              .Where(IsNotEmpty).Select(Simplify);
            if(andTreeFilter.AndComponents.SequenceEqual(simplified))
              return andTreeFilter;
            else
              return new AndTFilter<Tx>() {
                AndComponents = new HashSet<ITreeFilter<Tx>>(simplified),
                Filter = x => simplified.All(f => f.Filter(x))
              };
          }
        default:
          return null;
      }
      static bool IsSingleton<T>(IEnumerable<T> xs) 
        => xs.Any() && xs.FirstOrDefault() != null && xs.Skip(1).Any() == false;
      static bool IsNotEmpty(ITreeFilter<Tx> tree)
        => tree switch {
          PrimaryFilter<Tx> primFilter    => true,
          OrTFilter<Tx>     orTreeFilter  => orTreeFilter.OrComponents.Any(),
          AndTFilter<Tx>    andTreeFilter => andTreeFilter.AndComponents.Any(),
          _ => true,
        };
    }

    public static ITreeFilter<Ty> Map<Tx, Ty>(
      ITreeFilter<Tx> tree,
      Func<ITreeFilter<Tx>, ITreeFilter<Ty>> map
    )
    {
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return map(primFilter);
        case OrTFilter<Tx> orTreeFilter:
          var orcs = new HashSet<ITreeFilter<Ty>>(
            orTreeFilter.OrComponents.Select(map)
          );
          return new OrTFilter<Ty>() {
            OrComponents = orcs,
            Filter = x => orcs.All(orc => orc.Filter(x))
          };
        case AndTFilter<Tx> andTreeFilter:
          var andcs = new HashSet<ITreeFilter<Ty>>(
            andTreeFilter.AndComponents.Select(map)
          );
          return new AndTFilter<Ty>() {
            AndComponents = andcs,
            Filter = x => andcs.All(andc => andc.Filter(x))
          };
        default:
          return null;
      }
    }

    public static IEnumerable<ITreeFilter<Tx>> Enumerate<Tx>(
      ITreeFilter<Tx> tree
    )
    {
      if(tree == null) return null;
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return Singleton(tree);
        case OrTFilter<Tx> orTreeFilter:
          return orTreeFilter.OrComponents;
        case AndTFilter<Tx> andTreeFilter:
          return andTreeFilter.AndComponents;
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static IEnumerable<IListFilter<Tx>> Enumerate<Tx>(
      IListFilter<Tx> tree
    )
    {
      if(tree == null) return null;
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return Singleton(tree);
        case OrLFilter<Tx> orListFilter:
          return Singleton(orListFilter.OrComponent);
        case AndLFilter<Tx> andListFilter:
          return andListFilter.AndComponents;
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static IEnumerable<ITreeFilter<Tx>> EnumerateRecursively<Tx>(
      ITreeFilter<Tx> tree
    )
    {
      if(tree == null) return null;
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return Singleton(tree);
        case OrTFilter<Tx> orTreeFilter:
          return orTreeFilter.OrComponents.SelectMany(EnumerateRecursively);
        case AndTFilter<Tx> andTreeFilter:
          return andTreeFilter.AndComponents.SelectMany(EnumerateRecursively);
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static IEnumerable<IListFilter<Tx>> EnumerateRecursively<Tx>(
      IListFilter<Tx> tree
    )
    {
      if(tree == null) return null;
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return Singleton(tree);
        case OrLFilter<Tx> orTreeFilter:
          return Singleton(orTreeFilter.OrComponent).SelectMany(EnumerateRecursively);
        case AndLFilter<Tx> andTreeFilter:
          return andTreeFilter.AndComponents.SelectMany(EnumerateRecursively);
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static ITreeFilter<Tx> Copy<Tx>(ITreeFilter<Tx> tree)
    {
      switch (tree)
      {
        case PrimaryFilter<Tx> primFilter:
          return new PrimaryFilter<Tx>(){Filter = primFilter.Filter};
        case OrTFilter<Tx> orTreeFilter:
          return new OrTFilter<Tx>() {
            OrComponents = new HashSet<ITreeFilter<Tx>>(
              orTreeFilter.OrComponents.Select(Copy)
            ),
            Filter = orTreeFilter.Filter
          };
        case AndTFilter<Tx> andTreeFilter:
          return new AndTFilter<Tx>() {
            AndComponents = new HashSet<ITreeFilter<Tx>>(
              andTreeFilter.AndComponents.Select(Copy)
            ),
            Filter = andTreeFilter.Filter
          };
        default:
          return null;
      }
    }

    public static ITreeFilter<Tx> ListFilterToTreeFilter<Tx>(
      IListFilter<Tx> list
    )
    {
      switch (list)
      {
        case PrimaryFilter<Tx> primListFilter:
          return primListFilter;
        case OrLFilter<Tx> orListFilter:
          return new OrTFilter<Tx>() {
            OrComponents = new HashSet<ITreeFilter<Tx>>() {
              ListFilterToTreeFilter(orListFilter.OrComponent)
            },
            Filter = orListFilter.Filter
          };
        case AndLFilter<Tx> andListFilter:
          return new AndTFilter<Tx>() {
            AndComponents = new HashSet<ITreeFilter<Tx>>(
              andListFilter.AndComponents.Select(ListFilterToTreeFilter)
            ),
            Filter = andListFilter.Filter
          };
        default:
          return null;
      }
    }

    public static IListFilter<Tx> PrimaryFiltersToListFilter<Tx>(
      IEnumerable<IPrimaryFilter<Tx>> list
    )
    {
      var andCmps = new HashSet<IListFilter<Tx>>(list.Select(
        pf => new PrimaryFilter<Tx>(){Filter = pf.Filter}
      ));
      return new AndLFilter<Tx>(){
        AndComponents = andCmps,
        Filter = x => andCmps.All(f => f.Filter(x))
      };
    }

    public static ITreeFilter<Tx> PrimaryFiltersToTreeFilter<Tx>(
      IEnumerable<IPrimaryFilter<Tx>> list
    )
    {
      var andCmps = new HashSet<ITreeFilter<Tx>>(list.Select(
        pf => new PrimaryFilter<Tx>(){Filter = pf.Filter}
      ));
      return new AndTFilter<Tx>(){
        AndComponents = andCmps,
        Filter = x => andCmps.All(f => f.Filter(x))
      };
    }

    public static IEnumerable<IPrimaryFilter<Tx>> ListFilterToPrimaryFilters<Tx>(
      IListFilter<Tx> list
    )
    {
      switch (list)
      {
        case PrimaryFilter<Tx> primaryFilter: 
          return Singleton(primaryFilter);
        case OrLFilter<Tx> orListFilter:
          return ListFilterToPrimaryFilters(orListFilter.OrComponent);
        case AndLFilter<Tx> andListFilter:
          return andListFilter.AndComponents.SelectMany(ListFilterToPrimaryFilters);
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static IEnumerable<IListFilter<Tx>> TreeFilterToListFilters<Tx>(
      ITreeFilter<Tx> tree
    )
    {
      switch (tree)
      {
        case PrimaryFilter<Tx> primaryFilter:
          return Singleton(primaryFilter);
        case OrTFilter<Tx> orFilter:
          return orFilter.OrComponents.SelectMany(  
                                        // maps single or
                                        // component to many
            orc => TreeFilterToListFilters<Tx>(orc).Select(
              orcc => 
                new OrLFilter<Tx>() {
                  Origin = orc,
                  OrComponent = orcc,
                  Filter = orcc.Filter
                }
            )
          );
        case AndTFilter<Tx> andFilter:
          return andFilter.AndComponents.Any() ? andFilter.AndComponents
            .Select(
              andCmp =>TreeFilterToListFilters<Tx>(andCmp)
            )
            .Aggregate(
              seed: Singleton(new AndLFilter<Tx>() {
                Origin = andFilter,
                AndComponents = new HashSet<IListFilter<Tx>>(),
                Filter = x => true,
              }),
              func: (prevCmps, cmps) =>
                prevCmps.SelectMany(
                  prevCmp => 
                    cmps.Select(
                      cmp => new AndLFilter<Tx>(){
                        Origin = prevCmp.Origin,
                        AndComponents = new HashSet<IListFilter<Tx>>(
                          prevCmp.AndComponents
                        ) { cmp },
                        Filter = x => prevCmp.Filter(x) && cmp.Filter(x)
                      }
                  )
                )
            ) : Enumerable.Empty<IListFilter<Tx>>();
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    public static IEnumerable<IEnumerable<IPrimaryFilter<Tx>>> TreeFilterToListOfPrimaryFilters<Tx>(
      ITreeFilter<Tx> tree
    )
    {
      switch (tree)
      {
        case PrimaryFilter<Tx> primary:
          return Singleton(Singleton<IPrimaryFilter<Tx>>(primary));
        case OrTFilter<Tx> or:
          return or.OrComponents.SelectMany(    
                                        // every or have possibilities
                                        // so or is exchanged to those
                                        // possibilites
            or => TreeFilterToListOfPrimaryFilters<Tx>(or)      
                                        // produces possibilities from or
          );
        case AndTFilter<Tx> and:
          var andCmpsBrunches = and.AndComponents
            .Select(andCmp => TreeFilterToListOfPrimaryFilters<Tx>(andCmp)); 
                                        // set of sets of brunches
                                        // (brunch is list of primary filters)
          return andCmpsBrunches.Any() ? 
            andCmpsBrunches.Aggregate(
              seed: Singleton(Enumerable.Empty<IPrimaryFilter<Tx>>()),
              func: (accumulatedBrunches, andCmpBrunches) => 
                accumulatedBrunches.SelectMany(                     
                                        // every acumulated 
                                        // possibility is exchanged
                                        // to many accumulated
                                        // possibilities
                  accumulatedBrunch => andCmpBrunches.Select(          
                    andCmpBrunch => accumulatedBrunch.Concat(andCmpBrunch) 
                                        // acc possibility and
                                        // new possibilities
                                        // creates many new acc 
                                        // possibilities
                  )
                )
            )
            : 
            Enumerable.Empty<IEnumerable<IPrimaryFilter<Tx>>>();       
                                        // no possibilities
                                        // nothing to product
                                        // combain
            
            
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    

    public static IEnumerable<IEnumerable<FtrNode2<Tx>>> TreeToLists<Tx>(
      FtrNode2<Tx> tree
    )
    {
      switch (tree.Type)
      {
        case FtrNode2<Tx>.FtrNodeType.Filter:
          return Singleton(Singleton(tree));
        case FtrNode2<Tx>.FtrNodeType.Ors:
          return tree.Descadents.SelectMany(
            fn => TreeToLists(fn)
          );
        case FtrNode2<Tx>.FtrNodeType.Ands:
          var xx = tree.Descadents.Select(fn => TreeToLists(fn));
          return xx.Any() ? xx.Aggregate(
            func: (listsAcc, lists) => 
              listsAcc.SelectMany(
                listAcc => lists.Select(
                  list => listAcc.Concat(list)
                )
              )
          ) : Enumerable.Empty<IEnumerable<FtrNode2<Tx>>>();
        default:
          return null;
      }
      IEnumerable<T> Singleton<T>(T element) { yield return element; }
    }

    // first list (brunch) containing filter 
    // or empty if tree does not contain specified filter
    public static IEnumerable<FtrNode2<Tx>> TreeToFirstList<Tx>(
      FtrNode2<Tx> tree, 
      Func<Tx, bool> containingFilter
    )
    {
      switch (tree.Type)
      {
        case FtrNode2<Tx>.FtrNodeType.Filter:
          return FilterToFirstList(tree, containingFilter);

        case FtrNode2<Tx>.FtrNodeType.Ands:
          return AndsToFirstList(tree, containingFilter);

        case FtrNode2<Tx>.FtrNodeType.Ors:
          return OrsToFirstList(tree, containingFilter);

        default:
          return null;
      }
      static IEnumerable<T> Singleton<T>(T x) { yield return x; }
      static IEnumerable<FtrNode2<Tx>> FilterToFirstList(
        FtrNode2<Tx> tree, 
        Func<Tx, bool> containingFilter
      )
      {
        if (tree.Filter == containingFilter)
          return new List<FtrNode2<Tx>>() { tree };
        else
          return Enumerable.Empty<FtrNode2<Tx>>();
      }
      static IEnumerable<FtrNode2<Tx>> AndsToFirstList(
        FtrNode2<Tx> tree, 
        Func<Tx, bool> containingFilter
      )
      {
        if (tree.Descadents.Any(fn => fn.Filter == containingFilter))
          return tree.Descadents;
        else
        {
          var (matchingAnd, list) = tree.Descadents.Select(
            and => (matchingAnd: and, list: TreeToFirstList(and, containingFilter))
          )
          .FirstOrDefault(x => x.list.Any());
          if (matchingAnd != null)
            return
                tree.Descadents.TakeWhile(and => and != matchingAnd)
                .Concat(list)
                .Concat(tree.Descadents.SkipWhile(and => and != matchingAnd).Skip(1));
          else
            return Enumerable.Empty<FtrNode2<Tx>>();
        }
      }
      static IEnumerable<FtrNode2<Tx>> OrsToFirstList(
        FtrNode2<Tx> tree, 
        Func<Tx, bool> containingFilter
      )
      {
        var firstOrNull = tree.Descadents
                    .FirstOrDefault(fn => fn.Filter == containingFilter);
        if (firstOrNull != null)
          return Singleton(firstOrNull);
        else
          return tree.Descadents
            .Select(x => TreeToFirstList(x, containingFilter))
            .FirstOrDefault(x => x.Any())
            ?? Enumerable.Empty<FtrNode2<Tx>>();
      }
    }

    

    public static IEnumerable<IFtrNode<Tx>> TreeToFirstList<Tx>(
      IFtrNode<Tx> tree, 
      Func<Tx, bool> containingFilter
    )
    {
      // trivial, root is specified filter
      switch (tree)
      {
        case FtrRawNode<Tx> raw:
          if(raw.Filter == containingFilter)
            return new List<IFtrNode<Tx>>(){raw};
          else
            return Enumerable.Empty<IFtrNode<Tx>>();

        case FtrAndsNode<Tx> ands:
          if(ands.Descadents().Any(and => and.Filter == containingFilter))
            return ands.Descadents();
          else 
          {
            var (matchingAnd, list) = ands.Descadents().Select(
              and => (matchingAnd: and, list: TreeToFirstList(and, containingFilter))
            )
            .FirstOrDefault(x => x.list.Any());
            if(matchingAnd != null)
              return 
                  ands.Descadents().TakeWhile(and => and != matchingAnd)
                  .Concat(list)
                  .Concat(ands.Descadents().SkipWhile(and => and != matchingAnd).Skip(1));
            else
              return Enumerable.Empty<IFtrNode<Tx>>();
          }
        
        case FtrOrsNode<Tx> ors:
          var firstOrNull = ors.Descadents()
            .FirstOrDefault(fn => fn.Filter == containingFilter);
          if(firstOrNull != null)
            return Singleton(firstOrNull);
          else 
            return ors.Descadents()
              .Select(x => TreeToFirstList(x, containingFilter))
              .FirstOrDefault(x => x.Any()) 
              ?? Enumerable.Empty<IFtrNode<Tx>>();
          
        default:
          return null;
      }
      static IEnumerable<T> Singleton<T>(T x) { yield return x; }
    }

  }




}