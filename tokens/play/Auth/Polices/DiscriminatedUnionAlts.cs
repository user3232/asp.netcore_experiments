using System;                         // UriBuilder
using System.Collections.Generic;     // List, Dictionary
using System.Linq;                    // IEnumerable


namespace play.Auth.Polies
{

    // What is the purpouse of Filter Node??
  //   => to traverse and identify filter components.

  // traverse
  // tag identify
  // filter function reference
  // descadents data reference
  public class FtrNode2<Tx>
  {
    public Func<Tx, bool> Filter;
    public IEnumerable<FtrNode2<Tx>> Descadents;
    public FtrNodeType Type;

    public enum FtrNodeType
    {
      Filter,
      Ands,
      Ors
    }

    public static FtrNode2<Tx> Raw(Func<Tx, bool> filter)
      => new FtrNode2<Tx>() {
        Filter = filter == null ? x => false : filter,
        Descadents = Enumerable.Empty<FtrNode2<Tx>>(),
        Type = FtrNodeType.Filter
      };
    public static FtrNode2<Tx> And(IEnumerable<FtrNode2<Tx>> ands)
      => ands == null ? 
        new FtrNode2<Tx>() {
          Filter = x => false,
          Descadents = Enumerable.Empty<FtrNode2<Tx>>(),
          Type = FtrNodeType.Ands
        }
        :
        new FtrNode2<Tx>() {
          Filter = x => ands.All(and => and.Filter(x)),
          Descadents = ands,
          Type = FtrNodeType.Ands
        };
    public static FtrNode2<Tx> Or(IEnumerable<FtrNode2<Tx>> ors)
      => ors == null ? 
        new FtrNode2<Tx>() {
          Filter = x => false,
          Descadents = Enumerable.Empty<FtrNode2<Tx>>(),
          Type = FtrNodeType.Ors
        }
        :
        new FtrNode2<Tx>() {
          Filter = x => ors.Any(or => or.Filter(x)),
          Descadents = ors,
          Type = FtrNodeType.Ors
        };
  }

  // traverse
  // typeof identify
  // filter dynamic dispatch
  // descadents dynamic dispatch
  public interface IFtrNode<Tx>
  {
    bool Filter(Tx x) => false;
    IEnumerable<IFtrNode<Tx>> Descadents()
      => Enumerable.Empty<IFtrNode<Tx>>();
  }

  public class FtrRawNode<Tx> : IFtrNode<Tx>
  {
    public Func<Tx, bool> AtomicFilter;

    public IEnumerable<IFtrNode<Tx>> Descadents()
      => Enumerable.Empty<IFtrNode<Tx>>();
    public bool Filter(Tx x) => AtomicFilter(x);
    public static FtrRawNode<Tx> New(Func<Tx, bool> filter)
      => filter == null ? 
        new FtrRawNode<Tx>(){AtomicFilter = (x) => false} 
        :
        new FtrRawNode<Tx>(){AtomicFilter = filter};
  }

  public class FtrAndsNode<Tx> : IFtrNode<Tx>
  {
    public Func<Tx, bool>             AtomicFilter;
    public IEnumerable<IFtrNode<Tx>>  Ands;

    public IEnumerable<IFtrNode<Tx>> Descadents() => Ands;
    public bool Filter(Tx x) => AtomicFilter(x);
    public static FtrAndsNode<Tx> New(IEnumerable<IFtrNode<Tx>> ands)
      => ands == null ? 
        new FtrAndsNode<Tx>(){
          Ands = Enumerable.Empty<IFtrNode<Tx>>(), 
          AtomicFilter = (x) => false
        } 
        :
        new FtrAndsNode<Tx>(){
          Ands = ands, 
          AtomicFilter = x => ands.All(and => and.Filter(x))
        };
  }

  public class FtrOrsNode<Tx> : IFtrNode<Tx>
  {
    public Func<Tx, bool>             AtomicFilter;
    public IEnumerable<IFtrNode<Tx>>  Ors;

    public IEnumerable<IFtrNode<Tx>> Descadents() => Ors;
    public bool Filter(Tx x) => AtomicFilter(x);
    public static FtrOrsNode<Tx> New(IEnumerable<IFtrNode<Tx>> ors)
      => ors == null ? 
        new FtrOrsNode<Tx>(){
          Ors = Enumerable.Empty<IFtrNode<Tx>>(), 
          AtomicFilter = (x) => false
        } 
        :
        new FtrOrsNode<Tx>(){
          Ors = ors, 
          AtomicFilter = x => ors.Any(and => and.Filter(x))
        };
  }

  // traverse
  // dynamic dispatch with options identify
  // filter function reference
  // descadents data reference
  public abstract class FtrNode<Tx>
  {
    private FtrNode(){}
    public abstract T Match<T>(
      Func<Func<Tx, bool>, T> mapFilter, 
      Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapAndNodes, 
      Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapOrNodes
    );
    public Func<Tx, bool> Filter;

    public sealed class Raw : FtrNode<Tx>
    {
      public override T Match<T>(
        Func<Func<Tx, bool>, T> mapFilter, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapAndNodes, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapOrNodes
      ) => mapFilter(Filter);
      public static Raw New(Func<Tx, bool> filter) =>
        new Raw() {Filter = filter};
    }

    public sealed class And : FtrNode<Tx>
    {
      public IEnumerable<FtrNode<Tx>> AndNodes;

      public override T Match<T>(
        Func<Func<Tx, bool>, T> mapFilter, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapAndNodes, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapOrNodes
      ) => mapAndNodes(Filter, AndNodes);
      public static And New(IEnumerable<FtrNode<Tx>> andNodes) =>
        new And() {
          AndNodes = andNodes,
          Filter = FtrBld.And(andNodes.Select(an => an.Filter))
        };
    }

    public sealed class Or : FtrNode<Tx>
    {
      public IEnumerable<FtrNode<Tx>> OrNodes;

      public override T Match<T>(
        Func<Func<Tx, bool>, T> mapFilter, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapAndNodes, 
        Func<Func<Tx, bool>, IEnumerable<FtrNode<Tx>>, T> mapOrNodes
      ) => mapOrNodes(Filter, OrNodes);
      public static Or New(IEnumerable<FtrNode<Tx>> orNodes) =>
        new Or() {
          OrNodes = orNodes, 
          Filter = FtrBld.Or(orNodes.Select(orn => orn.Filter))
        };
    }

  }

}