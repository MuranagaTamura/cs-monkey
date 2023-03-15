using System.Collections.Generic;

namespace CsMonkey.Compiler
{
  public class Symbol
  {
    public const string GLOBAL_SCOPE = "GLOBAL";
    public const string LOCAL_SCOPE = "LOCAL";
    public const string BUILTIN_SCOPE = "BUILTIN";
    public const string FREE_SCOPE = "FREE";
    public const string FUNCTION_SCOPE = "FUNCTION";

    public string name;
    public string scope;
    public int index;
  }

  public class SymbolTable
  {
    private IDictionary<string, Symbol> store = new Dictionary<string, Symbol>();

    public SymbolTable outer { get; private set; } = null;
    public int numDefinitions { get; private set; } = 0;
    public IList<Symbol> freeSymbols { get; private set; } = new List<Symbol>();


    public SymbolTable()
    {
      // nop
    }

    public SymbolTable(SymbolTable outer)
    {
      this.outer = outer;
    }

    public Symbol Define(string name)
    {
      Symbol symbol = new Symbol() { name = name, index = numDefinitions };
      symbol.scope = (outer == null) ? Symbol.GLOBAL_SCOPE : Symbol.LOCAL_SCOPE;
      store[name] = symbol;
      ++numDefinitions;
      return symbol;
    }

    public Symbol DefineBuiltin(int index, string name)
    {
      Symbol symbol = new Symbol() { name = name, index = index, scope = Symbol.BUILTIN_SCOPE };
      store[name] = symbol;
      return symbol;
    }

    public Symbol DefineFree(Symbol original)
    {
      freeSymbols.Add(original);

      Symbol symbol = new Symbol() { name = original.name, index = freeSymbols.Count - 1};
      symbol.scope = Symbol.FREE_SCOPE;

      store[original.name] = symbol;
      return symbol;
    }

    public Symbol DefineFunctionName(string name)
    {
      Symbol symbol = new Symbol() { name = name, index = 0, scope = Symbol.FUNCTION_SCOPE };
      store[name] = symbol;
      return symbol;
    }

    public (bool, Symbol) Resolve(string name)
    {
      if(!(store.TryGetValue(name, out Symbol symbol)) && outer != null)
      {
        (bool success, Symbol resolveSymbol) = outer.Resolve(name);
        if(!success)
        {
          return (success, resolveSymbol);
        }

        if(resolveSymbol.scope == Symbol.GLOBAL_SCOPE || resolveSymbol.scope == Symbol.LOCAL_SCOPE)
        {
          return (success, resolveSymbol);
        }

        Symbol free = DefineFree(resolveSymbol);
        return (true, free);
      }
      return (store.ContainsKey(name), symbol);
    }
  }
}
