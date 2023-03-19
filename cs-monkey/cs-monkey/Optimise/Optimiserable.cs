using CsMonkey.Ast;

namespace CsMonkey.Optimise
{
  public interface Optimiserable
  {
    Program Optimise(Program program);
  }
}
