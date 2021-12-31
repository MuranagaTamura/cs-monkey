using CsMonkey.Ast;
using CsMonkey.Object;
using System.Collections.Generic;
using System.Linq;

namespace CsMonkey
{
  public class Evaluator
  {
    public static readonly Null NULL = new Null();
    public static readonly Object.Boolean TRUE = new Object.Boolean() { value = true };
    public static readonly Object.Boolean FALSE = new Object.Boolean() { value = false };

    public IObject Eval(INode node, Environment environment)
    {
      switch (node)
      {
        // ルート
        case Program program:
          return EvalProgram(program, environment);
        // 文
        case BlockStatement blockStatement:
          return EvalBlockStatements(blockStatement, environment);
        case ExpressionStatement expressionStatement:
          return Eval(expressionStatement.expression, environment);
        case LetStatement letStatement:
          {
            IObject value = Eval(letStatement.value, environment);
            if (value.ObjectType == IObject.Type.ERROR_OBJ)
              // エラーが返ってきたのでエラーを返す
              return value;
            return environment.Set(letStatement.name.value, value);
          }
        case ReturnStatement returnStatement:
          {
            IObject value = Eval(returnStatement.returnValue, environment);
            return new ReturnValue() { value = value };
          }
        // 式
        case Ast.Boolean boolean:
          return NativeBooleanObject(boolean.value);
        case CallExpression callExpression:
          {
            IObject function = Eval(callExpression.function, environment);
            if (function.ObjectType == IObject.Type.ERROR_OBJ)
              return function;
            IList<IObject> args = EvalExpressions(callExpression.arguments, environment);
            if(args.Count == 1 && args[0].ObjectType == IObject.Type.ERROR_OBJ )
            {
              return args[0];
            }

            return ApplyFunction(function, args);
          }
        case FunctionLiteral functionLiteral:
          {
            var parameters = functionLiteral.parameters;
            BlockStatement body = functionLiteral.body;
            return new Function()
            {
              parameters = parameters,
              body = body,
              environment = environment,
            };
          }
        case Identifier identifier:
          return EvalIdentifier(identifier, environment);
        case IfExpression ifExpression:
          return EvalIfExpression(ifExpression, environment);
        case InfixExpression infixExpression:
          {
            IObject left = Eval(infixExpression.left, environment);
            IObject right = Eval(infixExpression.right, environment);
            return EvalInfixExpression(infixExpression.op, left, right);
          }
        case IntegerLiteral integerLiteral:
          return new Integer() { value = integerLiteral.value };
        case PrefixExpression prefixExpression:
          {
            IObject right = Eval(prefixExpression.right, environment);
            return EvalPrefixExpression(prefixExpression.op, right);
          }
      }

      return NULL;
    }

    private IObject EvalBangOperatorExpression(IObject right)
    {
      switch (right)
      {
        case Object.Boolean boolean: return boolean.value ? FALSE : TRUE;
        case Null _: return TRUE;
        default: return FALSE;
      }
    }

    private IObject EvalBlockStatements(BlockStatement blockStatement, Environment environment)
    {
      IObject result = NULL;

      foreach (IStatement statement in blockStatement.statements)
      {
        // 各文をそれぞれ評価して結果を設定する
        result = Eval(statement, environment);

        if (result != null)
        {
          if (result.ObjectType == IObject.Type.RETURN_VALUE_OBJ
            || result.ObjectType == IObject.Type.ERROR_OBJ)
            // return文かerrorなので即時IObjectを返す
            return result;
        }
      }

      return result;
    }

    private IObject ApplyFunction(IObject fn, IList<IObject> args)
    {
      if(!(fn is Function function))
      {
        return new Error() { message = $"not a function: {fn.ObjectType}"};
      }

      Environment extendEnvironment = ExtendFunctionEnvironment(function, args);
      IObject evaluated = Eval(function.body, extendEnvironment);
      return UnwrapReturnValue(evaluated);
    }

    private IList<IObject> EvalExpressions(IList<IExpression> expressions, Environment environment)
    {
      IList<IObject> results = new List<IObject>();

      foreach(IExpression expression in expressions)
      {
        IObject evaluated = Eval(expression, environment);
        if (evaluated.ObjectType == IObject.Type.ERROR_OBJ)
          return new List<IObject>() { evaluated };
        results.Add(evaluated);
      }

      return results;
    }

    private IObject EvalIdentifier(Identifier identifier, Environment environment)
    {
      (IObject value, bool ok) = environment.Get(identifier.value);
      if (!ok)
        // 識別子が見つからない
        return new Error() { message = $"identifier not found: {identifier.value}" };
      return value;
    }

    private IObject EvalIfExpression(IfExpression ifExpression, Environment environment)
    {
      IObject condition = Eval(ifExpression.condition, environment);

      if (IsTruthy(condition))
      {
        return Eval(ifExpression.consequence, environment);
      }
      else if (ifExpression.alternative != null)
      {
        return Eval(ifExpression.alternative, environment);
      }
      else
      {
        return NULL;
      }
    }

    private IObject EvalInfixExpression(string op, IObject left, IObject right)
    {
      if (left.ObjectType == IObject.Type.INTEGER_OBJ
        && right.ObjectType == IObject.Type.INTEGER_OBJ)
      {
        // 左オブジェクト、右オブジェクトが整数だった
        return EvalIntegerInfixExpression(op, (Integer)left, (Integer)right);
      }
      else if (op == "==")
      {
        return NativeBooleanObject(left == right);
      }
      else if (op == "!=")
      {
        return NativeBooleanObject(left == right);
      }
      else if (left.ObjectType != right.ObjectType)
      {
        return new Error()
        {
          message = $"type mismatch: {left.ObjectType} {op} {right.ObjectType}"
        };
      }

      return new Error()
      {
        message = $"unknown operator: {left.ObjectType} {op} {right.ObjectType}"
      };
    }

    private IObject EvalIntegerInfixExpression(string op, Integer left, Integer right)
    {
      long leftValue = left.value;
      long rightValue = right.value;

      switch (op)
      {
        case "+": return new Integer() { value = leftValue + rightValue };
        case "-": return new Integer() { value = leftValue - rightValue };
        case "*": return new Integer() { value = leftValue * rightValue };
        case "/": return new Integer() { value = leftValue / rightValue };
        case "<": return NativeBooleanObject(leftValue < rightValue);
        case ">": return NativeBooleanObject(leftValue > rightValue);
        case "==": return NativeBooleanObject(leftValue == rightValue);
        case "!=": return NativeBooleanObject(leftValue != rightValue);
        default:
          return new Error()
          {
            message = $"unknown operator: {left.ObjectType} {op} {right.ObjectType}"
          };
      }
    }

    private IObject EvalMinusOperatorExpression(IObject right)
    {
      if (right.ObjectType != IObject.Type.INTEGER_OBJ)
      {
        // 整数型ではない
        return new Error()
        {
          message = $"unknown operator: -{right.ObjectType}"
        };
      }

      return new Integer() { value = -((Integer)right).value };
    }

    private IObject EvalPrefixExpression(string op, IObject right)
    {
      switch (op)
      {
        case "!": return EvalBangOperatorExpression(right);
        case "-": return EvalMinusOperatorExpression(right);
        default:
          return new Error()
          {
            message = $"unknown operator: {op}{right.ObjectType}"
          };
      }
    }

    private IObject EvalProgram(Program program, Environment environment)
    {
      IObject result = NULL;

      foreach (IStatement statement in program.Statements)
      {
        // 各文をそれぞれ評価して結果を設定する
        result = Eval(statement, environment);

        if (result is ReturnValue returnValue)
          // return文なので即時返す
          return returnValue;
        else if (result is Error error)
          // errorを返す
          return error;
      }

      return result;
    }

    private Environment ExtendFunctionEnvironment(Function fn, IList<IObject> args)
    {
      Environment environment = new Environment(fn.environment);

      foreach ((int paramIdx, Identifier param) in fn.parameters.Select((p, i) => (i, p)))
      {
        environment.Set(param.value, args[paramIdx]);
      }

      return environment;
    }

    private bool IsTruthy(IObject obj)
    {
      switch (obj)
      {
        case Null _: return false;
        case Object.Boolean boolean: return boolean.value;
        case Integer integer: return integer.value != 0;
        default: return true;
      }
    }

    private Object.Boolean NativeBooleanObject(bool input) => input ? TRUE : FALSE;

    private IObject UnwrapReturnValue(IObject obj)
    {
      if(obj is ReturnValue returnValue)
      {
        return returnValue.value;
      }

      return obj;
    }
  } // class
} // namespace
