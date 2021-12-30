using System.Text;

namespace CsMonkey.Ast
{
  public class ReturnStatement : IStatement
  {
    public Token token;
    public IExpression returnValue;

    public string TokenLiteral => token.Literal;

    public override string ToString()
    {
      StringBuilder builder = new StringBuilder();

      builder.Append($"{TokenLiteral} ");
      
      if(returnValue != null)
        builder.Append(returnValue);

      builder.Append(";");

      return builder.ToString();
    }
  }
}
