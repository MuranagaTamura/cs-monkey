using System.Collections.Generic;

namespace CsMonkey.Object
{
  public class Environment
  {
    public Dictionary<string, IObject> store = new Dictionary<string, IObject>();
    public Environment outer;

    public Environment() { }
    public Environment(Environment environment)
    {
      outer = environment;
    }

    public (IObject, bool) Get(string name)
    {
      (IObject obj, bool ok) = store.TryGetValue(name, out IObject inner)
                               ? (inner, true) : (Evaluator.NULL, false);
      if (!ok && outer != null)
        (obj, ok) = outer.Get(name);

      return (obj, ok);
    }

    public IObject Set(string name, IObject value)
    {
      store[name] = value;
      return value;
    }
  }
}
