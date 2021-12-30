namespace CsMonkey.Ast
{
  public interface INode
  {
    string TokenLiteral { get; }
  }
  public interface IStatement : INode { }
  public interface IExpression : INode { }
}
