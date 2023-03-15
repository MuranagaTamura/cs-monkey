using CsMonkey.Object;
using System.Collections.Generic;

namespace CsMonkey.Compiler
{
  public class Bytecode
  {
    public IList<byte> instruction;
    public IList<IObject> constants;
  }
}
