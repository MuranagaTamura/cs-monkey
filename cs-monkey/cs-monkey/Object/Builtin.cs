using System;
using System.Collections.Generic;

namespace CsMonkey.Object
{
  public class Builtin : IObject
  {
    public Func<IList<IObject>, IObject> fn;

    public IObject.Type ObjectType => IObject.Type.BULTIN_OBJ;

    public string Inspect() => "builtin function";
  }
}
