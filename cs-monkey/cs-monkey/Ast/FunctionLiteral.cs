using System.Collections.Generic;

namespace CsMonkey.Ast
{
  public class FunctionLiteral : IExpression
  {
    public Token token;
    public IList<Identifier> parameters = new List<Identifier>();
    public BlockStatement body;

    public string TokenLiteral => token.Literal;

    public override string ToString() => $"{TokenLiteral}({string.Join(", ", parameters)}){body}";
  }
}
