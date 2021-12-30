using System.Collections.Generic;

namespace CsMonkey.Ast
{
  public class CallExpression : IExpression
  {
    public Token token;
    public IExpression function;
    public IList<IExpression> arguments = new List<IExpression>();

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"{function}({string.Join(", ", arguments)})";
  }
}
