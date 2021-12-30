namespace CsMonkey.Ast
{
  public class Boolean : IExpression
  {
    public Token token;
    public bool value;

    public string TokenLiteral => token.Literal;

    public override string ToString() => TokenLiteral;
  }
}
