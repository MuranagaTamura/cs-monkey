namespace CsMonkey.Ast
{
  public class StringLiteral : IExpression
  {
    public Token token;
    public string value;

    public string TokenLiteral => token.Literal;

    public override string ToString() => TokenLiteral;
  }
}
