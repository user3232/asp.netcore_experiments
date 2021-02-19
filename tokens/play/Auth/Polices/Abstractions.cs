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

  public interface IPrimaryFilter<Tx> {
    Func<Tx, bool> Filter {get;set;}
  }


  public interface IListFilter<Tx> {
    Func<Tx, bool> Filter {get;set;}
  }

  public class AndLFilter<Tx> : IListFilter<Tx>
  {
    public ITreeFilter<Tx> Origin = null;
    public HashSet<IListFilter<Tx>> AndComponents = new HashSet<IListFilter<Tx>>();

    public Func<Tx, bool> Filter { get;set; }
  }

  public class OrLFilter<Tx> : IListFilter<Tx>
  {
    public ITreeFilter<Tx> Origin = null;
    public Func<Tx, bool> Filter { get;set; }
    public IListFilter<Tx> OrComponent = null;
  }


  public interface ITreeFilter<Tx> {
    Func<Tx, bool> Filter {get;set;}
  }

  public class AndTFilter<Tx> : ITreeFilter<Tx>
  {
    public Func<Tx, bool> Filter { get;set; }
    public HashSet<ITreeFilter<Tx>> AndComponents = new HashSet<ITreeFilter<Tx>>();
  }

  public class OrTFilter<Tx> : ITreeFilter<Tx>
  {
    public Func<Tx, bool> Filter { get;set; }
    public HashSet<ITreeFilter<Tx>> OrComponents = new HashSet<ITreeFilter<Tx>>();
  }

  public class XorTFilter<Tx> : ITreeFilter<Tx>
  {
    public Func<Tx, bool> Filter { get;set; }
    public HashSet<ITreeFilter<Tx>> XorComponents = new HashSet<ITreeFilter<Tx>>();
  }

  public class PrimaryFilter<Tx> : ITreeFilter<Tx>, IListFilter<Tx>, IPrimaryFilter<Tx>
  {
    public Func<Tx, bool> Filter { get;set; }
  }

}