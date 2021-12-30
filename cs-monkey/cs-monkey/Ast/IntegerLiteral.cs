namespace CsMonkey.Ast
{
  public class IntegerLiteral : IExpression
  {
    public Token token;
    public long value;

    public string TokenLiteral => token.Literal;

    public override string ToString() => TokenLiteral;
  }
}
