using CsMonkey.Ast;

namespace CsMonkey.Optimise
{
  public class OptimiseHelper
  {
    public static Program Optimise(Program program)
    {
      Optimiserable optimiser = new ConstantOptimiser();
      program = optimiser.Optimise(program);
      return program;
    }
  }
}
