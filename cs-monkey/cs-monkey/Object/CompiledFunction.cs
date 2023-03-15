using CsMonkey.Code;
using System.Collections.Generic;

namespace CsMonkey.Object
{
  public class CompiledFunction : IObject
  {
    public IList<byte> instlactions;
    public int numLocals;
    public int numParameters;

    public IObject.Type ObjectType => IObject.Type.COMPILED_FUNCTION_OBJ;

    public string Inspect() => $"__bytecode_fn()\n{{\n{CodeHelper.String(instlactions, null, null, true)}}}";
  }
}
