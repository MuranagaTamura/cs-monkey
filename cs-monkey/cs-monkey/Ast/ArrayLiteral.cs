using System.Collections.Generic;

namespace CsMonkey.Ast
{
  public class ArrayLiteral : IExpression
  {
    public Token token;
    public IList<IExpression> elements;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"[{string.Join(", ", elements)}]";
  }
}
