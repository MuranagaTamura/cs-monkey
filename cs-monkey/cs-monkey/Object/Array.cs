using System.Collections.Generic;
using System.Linq;

namespace CsMonkey.Object
{
  public class Array : IObject
  {
    public IList<IObject> elements;

    public IObject.Type ObjectType => IObject.Type.ARRAY_OBJ;

    public string Inspect() => $"[{string.Join(", ", elements.Select((IObject obj) => obj.Inspect()))}]";
  }
}
