using System.Text;

namespace CsMonkey.Ast
{
  public class IfExpression : IExpression
  {
    public Token token;
    public IExpression condition;
    public BlockStatement consequence;
    public BlockStatement alternative;

    public string TokenLiteral => token.Literal;

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      builder.Append($"if{condition} {consequence}");

      if (alternative != null)
        builder.Append($"else {alternative}");

      return builder.ToString();
    }
  }
}
