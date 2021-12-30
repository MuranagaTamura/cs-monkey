namespace CsMonkey.Ast
{
  public class Identifier : IExpression
  {
    public Token token;
    public string value;

    public string TokenLiteral => token.Literal;

    public override string ToString() => value;
  }
}
