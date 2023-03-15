using CsMonkey.Object;
using System.Collections.Generic;

namespace CsMonkey.Vm
{
  public class Frame
  {
    public Closure closure;
    public int ip;
    public int basePointer;

    public Frame(Closure closure, int basePointer)
    {
      this.closure = closure;
      ip = -1;
      this.basePointer = basePointer;
    }

    public IList<byte> Instructions()
    {
      return closure.function.instlactions;
    }
  }
}
