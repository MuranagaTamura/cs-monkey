using CsMonkey.Ast;
using System.Collections.Generic;
using System.Windows.Markup;

namespace CsMonkey.Optimise
{
  public class ConstantOptimiser : Optimiserable
  {
    IDictionary<string, IExpression> variables;

    public Program Optimise(Program program)
    {
      variables = new Dictionary<string, IExpression>();

      for(int i =0; i < program.statements.Count; ++i)
      {
        INode optimised = Optimise(program.statements[i]);
        if(optimised is IStatement statement)
        {
          program.statements[i] = statement;
        }
      }
      return program;
    }

    private INode Optimise(INode node)
    {
      INode optimised;
      switch(node)
      {
        // 文
        case ExpressionStatement expressionStatement:
          {
            optimised = Optimise(expressionStatement.expression);
            if (!(optimised is IExpression expression))
            {
              return new ErrorNode() { message = $"Is not expression." };
            }
            expressionStatement.expression = expression;
            return expressionStatement;
          }
        case LetStatement letStatement:
          {
            optimised = Optimise(letStatement.value);
            if(!(optimised is IExpression expression))
            {
              return new ErrorNode() { message = $"Is not expression." };
            }
            letStatement.value = expression;
            variables[letStatement.name.value] = expression;
            return letStatement;
          }
        // 式
        case InfixExpression infixExpression:
          {
            // 左ノードを最適化します
            optimised = Optimise(infixExpression.left);
            if(!(optimised is IExpression left))
            {
              return new ErrorNode(){ message = $"Is not expression."};
            }
            // 右ノードを最適化します
            optimised = Optimise(infixExpression.right);
            if(!(optimised is IExpression right))
            {
              return new ErrorNode() { message = $"Is not expression." };
            }

            // 左右ノードの型を比較し、最適化できそうなら最適化を行う
            if(left is IntegerLiteral leftInteger && right is IntegerLiteral rigjtInteger)
            {
              return OptimiseInteger(infixExpression.op, leftInteger, rigjtInteger);
            }
            else if(left is StringLiteral leftString && right is StringLiteral rightString)
            {
              return OptimiseString(infixExpression.op, leftString, rightString);
            }
            else if(left is BooleanLiteral leftBoolean && right is BooleanLiteral rightBoolean)
            {
              return OptimiseBoolean(infixExpression.op, leftBoolean, rightBoolean);
            }
            return infixExpression;
          }
        case Identifier identifier:
          {
            // すでに定義されていた場合は変数の展開を試みます
            if(!variables.TryGetValue(identifier.value, out IExpression expression))
            {
              return identifier;
            }

            // 変数の展開で整数、文字列、ブール値の場合はその値を展開します
            switch(expression)
            {
              case IntegerLiteral integerLiteral: return integerLiteral;
              case StringLiteral stringLiteral: return stringLiteral;
              case BooleanLiteral booleanLiteral: return booleanLiteral;
              default: return identifier;
            }
          }
        case IntegerLiteral:
        case StringLiteral:
        case BooleanLiteral:
          {
            // 整数、文字列、ブール値リテラルはそのまま返します
            return node;
          }
        default:
          {
            return new ErrorNode() { message = $"Not implementation ${node.TokenLiteral}" };
          }
      } // switch
    }

    private INode OptimiseInteger(string op, IntegerLiteral left, IntegerLiteral right)
    {
      switch(op)
      {
        case "+":
          {
            return new IntegerLiteral() { value = left.value + right.value };
          }
        case "-":
          {
            return new IntegerLiteral() { value = left.value - right.value };
          }
        case "*":
          {
            return new IntegerLiteral() { value = left.value * right.value };
          }
        case "/":
          {
            return new IntegerLiteral() { value = left.value / right.value };
          }
        case ">":
          {
            return new BooleanLiteral() { value = left.value > right.value };
          }
        case "<":
          {
            return new BooleanLiteral() { value = left.value < right.value };
          }
        case "==":
          {
            return new BooleanLiteral() { value = left.value == right.value };
          }
        case "!=":
          {
            return new BooleanLiteral() { value = left.value != right.value };
          }
        default:
          {
            return new ErrorNode() { message = $"cannot supported operate int{op}int" };
          }
      } // switch
    }

    private INode OptimiseString(string op, StringLiteral left, StringLiteral right)
    {
      switch(op)
      {
        case "+":
          {
            return new StringLiteral() { value = left.value + right.value };
          }
        default:
          {
            return new ErrorNode() { message = $"cannot supported operate int{op}int" };
          }
      } // switch
    }

    private INode OptimiseBoolean(string op, BooleanLiteral left, BooleanLiteral right)
    {
      switch (op)
      {
        case "==":
          {
            return new BooleanLiteral() { value = left.value == right.value };
          }
        case "!=":
          {
            return new BooleanLiteral() { value = left.value != right.value };
          }
        default:
          {
            return new ErrorNode() { message = $"cannot supported operate int{op}int" };
          }
      } // switch
    }
  }
}
