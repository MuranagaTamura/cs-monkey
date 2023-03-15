using System.Collections.Generic;

namespace CsMonkey.Object
{
  public class Closure : IObject
  {
    public CompiledFunction function;
    public IList<IObject> free;

    public IObject.Type ObjectType => IObject.Type.CLOSURE_OBJ;

    public string Inspect() => $"Closure[{function.GetHashCode()}]";
  }
}
