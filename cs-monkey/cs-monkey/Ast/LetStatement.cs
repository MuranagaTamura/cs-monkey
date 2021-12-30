using System.Text;

namespace CsMonkey.Ast
{
  public class LetStatement : IStatement
  {
    public Token token;
    public Identifier name;
    public IExpression value;

    public string TokenLiteral => token.Literal;

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      builder.Append($"{TokenLiteral} ");
      builder.Append($"{name} = ");

      if (value != null)
        builder.Append(value);

      builder.Append(";");

      return builder.ToString();
    }
  }
}
