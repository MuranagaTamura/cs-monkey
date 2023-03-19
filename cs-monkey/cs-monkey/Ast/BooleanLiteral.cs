namespace CsMonkey.Ast
{
  public class BooleanLiteral : IExpression
  {
    public Token token;
    public bool value;

    public string TokenLiteral => token.Literal;

    public override string ToString() => TokenLiteral;
  }
}
