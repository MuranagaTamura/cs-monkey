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

    private IDictionary<string, Builtin> builtins_ = new Dictionary<string, Builtin>()
      {
        { "len", new Builtin(){ fn = (IList<IObject> args) => 
          { 
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }

            switch(args[0])
            {
              case String @string:
                {
                  return new Integer(){ value = @string.value.Length };
                }
                case Array array:
                {
                  return new Integer(){ value = array.elements.Count };
                }
              default:
                {
                  return new Error() { message = $"argument to `len` not supported, got {args[0].ObjectType}" };
                }
            }
          } } },
        { "first", new Builtin() { fn = (IList<IObject> args) => 
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `first` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              return array.elements[0];
            }

            return NULL;
          } } },
        { "last", new Builtin() { fn = (IList<IObject> args) => 
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `last` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              return array.elements.Last();
            }

            return NULL;
          } } },
        { "rest", new Builtin() { fn = (IList<IObject> args) => 
          {
            if(args.Count != 1)
            {
              return new Error() { message = $"wrong number of arguments. got={args.Count}, want=1"};
            }
            if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
            {
              return new Error() { message = $"argument to `rest` must be ARRAY, got {args[0].ObjectType}" };
            }

            if(args[0] is Array array && array.elements.Count > 0)
            {
              IList<IObject> newEelements = new List<IObject>(array.elements);
              newEelements.RemoveAt(0);
              return new Array(){ elements = newEelements };
            }

            return NULL;
          } } },
        { "push", new Builtin(){ fn = (IList<IObject> args) => 
          {
            if(args.Count != 2)
              {
                return new Error() { message = $"wrong number of arguments. got={args.Count}, want=2"};
              }
              if(args[0].ObjectType != IObject.Type.ARRAY_OBJ)
              {
                return new Error() { message = $"argument to `push` must be ARRAY, got {args[0].ObjectType}" };
              }

              Array array = args[0] as Array;
              long length = array.elements.Count;

              IList<IObject> newElements = new List<IObject>(array.elements) { args[1] };
              return new Array(){ elements = newElements };
          } } },
        { "puts", new Builtin(){ fn = (IList<IObject> args) => 
          {
            foreach(IObject arg in args)
            {
              System.Console.WriteLine(arg.Inspect());
            }
            return NULL;
          } } },
      };

    public IObject Eval(INode node, Environment environment)
    {
      switch (node)
      {
        // ルート
        case Program program:
          {
            return EvalProgram(program, environment);
          }
        // 文
        case BlockStatement blockStatement:
          {
            return EvalBlockStatements(blockStatement, environment);
          }
        case ExpressionStatement expressionStatement:
          {
            return Eval(expressionStatement.expression, environment);
          }
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
          {
            return NativeBooleanObject(boolean.value);
          }
        case CallExpression callExpression:
          {
            if (callExpression.function.TokenLiteral == "quote")
            {
              // 関数の呼び出しトークンがquoteでした
              return Quote(callExpression.arguments[0], environment);
            }

            IObject function = Eval(callExpression.function, environment);
            if (function.ObjectType == IObject.Type.ERROR_OBJ)
            {
              // 評価した結果失敗しました
              return function;
            }
            IList<IObject> args = EvalExpressions(callExpression.arguments, environment);
            if (args.Count == 1 && args[0].ObjectType == IObject.Type.ERROR_OBJ)
            {
              return args[0];
            }

            // 関数を呼び出した結果を返します
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
          {
            return EvalIdentifier(identifier, environment);
          }
        case IfExpression ifExpression:
          {
            return EvalIfExpression(ifExpression, environment);
          }
        case InfixExpression infixExpression:
          {
            IObject left = Eval(infixExpression.left, environment);
            IObject right = Eval(infixExpression.right, environment);
            return EvalInfixExpression(infixExpression.op, left, right);
          }
        case PrefixExpression prefixExpression:
          {
            IObject right = Eval(prefixExpression.right, environment);
            return EvalPrefixExpression(prefixExpression.op, right);
          }
        case IndexExpression indexExpression:
          {
            IObject left = Eval(indexExpression.left, environment);
            if(left is Error)
            {
              return left;
            }
            IObject index = Eval(indexExpression.index, environment);
            if(index is Error)
            { 
              return index;
            }
            return EvalIndexExpression(left, index);
          }
        case IntegerLiteral integerLiteral:
          {
            return new Integer() { value = integerLiteral.value };
          }
        case StringLiteral stringLiteral:
          {
            return new String() { value = stringLiteral.value };
          }
        case ArrayLiteral arrayLiteral:
          {
            IList<IObject> elemets = EvalExpressions(arrayLiteral.elements, environment);
            if(elemets.Count == 1 && elemets[0] is Error)
            {
              return elemets[0];
            }
            return new Array() { elements = elemets };
          }
        case HashLiteral hashLiteral:
          {
            return EvalHashLiteral(hashLiteral, environment);
          }
        // エラー
        case ErrorNode errorNode:
          {
            return new Error() { message = errorNode.message };
          }
        default:
          {
            return new Error() { message = $"unsupported node token: {node.TokenLiteral}" };
          }
      }
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
      switch(fn)
      {
        case Function function:
          {
            // 定義された関数
            Environment extendEnvironment = ExtendFunctionEnvironment(function, args);
            IObject evaluated = Eval(function.body, extendEnvironment);
            return UnwrapReturnValue(evaluated);
          }
        case Builtin builtin:
          {
            // 組み込み関数
            return builtin.fn(args);
          }
        default:
          {
            // 関数ではありません
            return new Error() { message = $"not a function: {fn.ObjectType}" };
          }
      }
    }

    private IList<IObject> EvalExpressions(IList<IExpression> expressions, Environment environment)
    {
      IList<IObject> results = new List<IObject>();

      foreach (IExpression expression in expressions)
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
      if (ok)
      {
        return value;
      }

      if(builtins_.TryGetValue(identifier.value, out Builtin builtin))
      {
        return builtin;
      }

      // 識別子が見つからない
      return new Error() { message = $"identifier not found: {identifier.value}" };
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
      if (left.ObjectType == IObject.Type.STRING_OBJ
        && right.ObjectType == IObject.Type.STRING_OBJ)
      {
        // 左オブジェクト、右オブジェクトが文字列だった
        return EvalStringInfixExpression(op, (String)left, (String)right);
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

    private IObject EvalStringInfixExpression(string op, String left, String right)
    {
      switch(op)
      {
        case "+": return new String() { value = left.value + right.value };
        default: return new Error()
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

      foreach (IStatement statement in program.statements)
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
      if (obj is ReturnValue returnValue)
      {
        return returnValue.value;
      }

      return obj;
    }

    private IObject EvalIndexExpression(IObject left, IObject index)
    {
      if(left.ObjectType == IObject.Type.ARRAY_OBJ && index.ObjectType == IObject.Type.INTEGER_OBJ)
      {
        // 配列[整数]でした
        return EvalArrayIndexExpression(left, index);
      }
      if(left.ObjectType == IObject.Type.HASH_OBJ)
      {
        // 連想配列[ハッシュ可能なオブジェクト]でした
        return EvalHashIndexExpression(left, index);
      }
      return new Error() { message = $"index operator not supported: {left.ObjectType}" };
    }

    private IObject EvalArrayIndexExpression(IObject array, IObject index)
    {
      Array arrayObject = (Array)array;
      long indexObject = ((Integer)index).value;
      long max = arrayObject.elements.Count;

      if(System.Math.Clamp(indexObject, 0, max) != indexObject)
      {
        return NULL;
      }
      return arrayObject.elements[(int)indexObject];
    }

    private IObject EvalHashIndexExpression(IObject hash, IObject index)
    {
      Hash hashObject = (Hash)hash;

      if(!(index is Hashable key))
      {
        return new Error() { message = $"unusuable as hash key: {index.ObjectType}" };
      }

      if(hashObject.pairs.TryGetValue(key.HashKey(), out HashPair pair))
      {
        return pair.value;
      }

      return NULL;
    }

    private IObject EvalHashLiteral(HashLiteral hashLiteral, Environment environment)
    {
      IDictionary<HashKey, HashPair> pairs = new Dictionary<HashKey, HashPair>();

      foreach((IExpression keyNode, IExpression valueNode) in hashLiteral.pairs)
      {
        IObject key = Eval(keyNode, environment);
        if(key is Error)
        {
          return key;
        }

        if(!(key is Hashable hashKey))
        {
          return new Error() { message = $"unusable as hash key: {key.ObjectType}" };
        }

        IObject value = Eval(valueNode, environment);
        if(value is Error)
        {
          return value;
        }

        HashKey hashed = hashKey.HashKey();
        pairs[hashed] = new HashPair() { key = key, value = value };
      }

      return new Hash() { pairs = pairs };
    }

    public INode Modify(INode node, System.Func<INode, INode> modifier)
    {
      switch (node)
      {
        // ルート
        case Program program:
          {
            for (int i = 0; i < program.statements.Count; ++i)
            {
              INode statement = Modify(program.statements[i], modifier);
              if (statement is IStatement ok)
              {
                program.statements[i] = ok;
              }
              else if (statement is ErrorNode error)
              {
                return error;
              }
              else
              {
                return new ErrorNode() { message = $"cannot modify: statemet[{i}] = {statement.TokenLiteral}" };
              }
            }
            break;
          }
        // 文
        case ExpressionStatement expressionStatement:
          {
            INode expression = Modify(expressionStatement.expression, modifier);
            if (expression is IExpression ok)
            {
              expressionStatement.expression = ok;
            }
            else if (expression is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: expression = {expression.TokenLiteral}" };
            }
            break;
          }
        case BlockStatement blockStatement:
          {
            for (int i = 0; i < blockStatement.statements.Count; ++i)
            {
              INode statement = Modify(blockStatement.statements[i], modifier);
              if (statement is IStatement okStatement)
              {
                blockStatement.statements[i] = okStatement;
              }
              else if (statement is ErrorNode error)
              {
                return error;
              }
              else
              {
                return new ErrorNode() { message = $"cannot modify: statements[{i}] = {statement.TokenLiteral}" };
              }
            }
            break;
          }
        case ReturnStatement returnStatement:
          {
            INode returnValue = Modify(returnStatement.returnValue, modifier);
            if (returnValue is IExpression okReturnValue)
            {
              returnStatement.returnValue = okReturnValue;
            }
            else if (returnValue is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: returnValue = {returnValue.TokenLiteral}" };
            }
            break;
          }
        case LetStatement letStatement:
          {
            INode value = Modify(letStatement.value, modifier);
            if (value is IExpression okValue)
            {
              letStatement.value = okValue;
            }
            else if (value is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: value = {value.TokenLiteral}" };
            }
            break;
          }
        // 式
        case InfixExpression infixExpression:
          {
            INode left = Modify(infixExpression.left, modifier);
            if (left is IExpression okLeft)
            {
              infixExpression.left = okLeft;
            }
            else if (left is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: left = {left.TokenLiteral}" };
            }

            INode right = Modify(infixExpression.right, modifier);
            if (right is IExpression okRight)
            {
              infixExpression.right = okRight;
            }
            else if (right is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: right = {right.TokenLiteral}" };
            }
            break;
          }
        case PrefixExpression prefixExpression:
          {
            INode right = Modify(prefixExpression.right, modifier);
            if (right is IExpression ok)
            {
              prefixExpression.right = ok;
            }
            else if (right is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: right = {right.TokenLiteral}" };
            }
            break;
          }
        case IndexExpression indexExpression:
          {
            INode left = Modify(indexExpression.left, modifier);
            if (left is IExpression okLeft)
            {
              indexExpression.left = okLeft;
            }
            else if (left is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: left = {left.TokenLiteral}" };
            }

            INode index = Modify(indexExpression.index, modifier);
            if (index is IExpression okIndex)
            {
              indexExpression.index = okIndex;
            }
            else if (index is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: index = {index.TokenLiteral}" };
            }
            break;
          }
        case IfExpression ifExpression:
          {
            INode condition = Modify(ifExpression.condition, modifier);
            if (condition is IExpression okCondition)
            {
              ifExpression.condition = okCondition;
            }
            else if (condition is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: condition = {condition.TokenLiteral}" };
            }

            INode consequence = Modify(ifExpression.consequence, modifier);
            if (consequence is BlockStatement okConsequence)
            {
              ifExpression.consequence = okConsequence;
            }
            else if (consequence is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: consequence = {consequence.TokenLiteral}" };
            }

            if (ifExpression.alternative != null)
            {
              INode alternative = Modify(ifExpression.alternative, modifier);
              if (alternative is BlockStatement okAlternative)
              {
                ifExpression.alternative = okAlternative;
              }
              else if (alternative is ErrorNode error)
              {
                return error;
              }
              else
              {
                return new ErrorNode() { message = $"cannot modify: alternative = {alternative.TokenLiteral}" };
              }
            }
            break;
          }
        case FunctionLiteral functionLiteral:
          {
            for (int i = 0; i < functionLiteral.parameters.Count; ++i)
            {
              INode parameter = Modify(functionLiteral.parameters[i], modifier);
              if (parameter is Identifier okParameter)
              {
                functionLiteral.parameters[i] = okParameter;
              }
              else if (parameter is ErrorNode error)
              {
                return error;
              }
              else
              {
                return new ErrorNode() { message = $"cannot modify: parameters[{i}] = {parameter.TokenLiteral}" };
              }
            }

            INode body = Modify(functionLiteral.body, modifier);
            if (body is BlockStatement okBody)
            {
              functionLiteral.body = okBody;
            }
            else if (body is ErrorNode error)
            {
              return error;
            }
            else
            {
              return new ErrorNode() { message = $"cannot modify: body = {body.TokenLiteral}" };
            }
            break;
          }
        case ArrayLiteral arrayLiteral:
          {
            for (int i = 0; i < arrayLiteral.elements.Count; ++i)
            {
              INode element = Modify(arrayLiteral.elements[i], modifier);
              if (element is Identifier okElement)
              {
                arrayLiteral.elements[i] = okElement;
              }
              else if (element is ErrorNode error)
              {
                return error;
              }
              else
              {
                return new ErrorNode() { message = $"cannot modify: elements[{i}] = {element.TokenLiteral}" };
              }
            }
            break;
          }
        case HashLiteral hashLiteral:
          {
            IDictionary<IExpression, IExpression> newPairs = new Dictionary<IExpression, IExpression>();
            foreach ((IExpression key, IExpression value) in hashLiteral.pairs)
            {
              INode newKey = Modify(key, modifier);
              if(newKey is ErrorNode errorNewKey)
              {
                return errorNewKey;
              }
              else if(!(newKey is IExpression))
              {
                return new ErrorNode() { message = $"cannot modify: newKey = {newKey.TokenLiteral}" };
              }

              INode newValue = Modify(value, modifier);
              if(newValue is ErrorNode errorNewValue)
              {
                return errorNewValue;
              }
              else if(!(newValue is IExpression))
              {
                return new ErrorNode() { message = $"cannot modify: newValue" };
              }

              newPairs[newKey as IExpression] = newValue as IExpression;
            }
            hashLiteral.pairs = newPairs;
            break;
          }
      } // switch
      return modifier(node);
    }

    private IObject Quote(INode node, Environment environment)
    {
      node = EvalUnquoteCalls(node, environment);
      if (node is ErrorNode errorNode)
      {
        return new Error() { message = errorNode.message };
      }
      return new Quote() { node = node };
    }

    private INode EvalUnquoteCalls(INode quoted, Environment environment)
    {
      return Modify(quoted, (INode node) =>
      {
        if (!IsUnquoteCall(node))
        {
          return node;
        }

        CallExpression callExpression = node as CallExpression;
        if (callExpression == null)
        {
          return node;
        }

        if (callExpression.arguments.Count != 1)
        {
          return node;
        }

        IObject unquoted = Eval(callExpression.arguments[0], environment);
        return ConvertObjectToNode(unquoted);
      });
    }

    private bool IsUnquoteCall(INode node)
    {
      if (node is CallExpression callExpression)
      {
        return callExpression.function.TokenLiteral == "unquote";
      }
      return false;
    }

    private INode ConvertObjectToNode(IObject @object)
    {
      switch (@object)
      {
        case Integer integer:
          {
            Token token = new Token() { TokenType = Token.Type.INT, Literal = $"{integer.value}" };
            return new IntegerLiteral() { token = token, value = integer.value };
          }
        case Object.Boolean boolean:
          {
            Token token = boolean.value
              ? new Token() { TokenType = Token.Type.TRUE, Literal = "true" }
              : new Token() { TokenType = Token.Type.FALSE, Literal = "false" };
            return new Ast.Boolean() { token = token, value = boolean.value };
          }
        case Quote quote:
          {
            return quote.node;
          }
        default:
          {
            return null;
          }
      }
    }

    public void DefineMacros(Program program, Environment environment)
    {
      IList<int> definitions = new List<int>();

      for(int i = 0; i < program.statements.Count; ++i)
      {
        if (IsMacroDefinition(program.statements[i]))
        {
          AddMacro(program.statements[i], environment);
          definitions.Add(i);
        }
      }

      for(int i = definitions.Count - 1; i >= 0; --i) 
      {
        int definitionIndex = definitions[i];
        program.statements.RemoveAt(definitionIndex);
      }
    }

    private bool IsMacroDefinition(IStatement node)
    {
      if(!(node is LetStatement letStatement))
      {
        return false;
      }
      return letStatement.value is MacroLiteral;
    }

    private void AddMacro(IStatement statement, Environment environment)
    {
      LetStatement letStatement = (LetStatement)statement;
      MacroLiteral macroLiteral = (MacroLiteral)letStatement.value;

      Macro macro = new Macro()
      {
        parameters = macroLiteral.parameters,
        environment = environment,
        body = macroLiteral.body,
      };

      environment.Set(letStatement.name.value, macro);
    }

    public INode ExpandMacros(Program program, Environment environment)
    {
      return Modify(program, (INode node) =>
      {
        if(!(node is CallExpression callExpression))
        {
          return node;
        }

        (Macro macro, bool ok) = IsMacroCall(callExpression, environment);
        if(!ok)
        {
          return node;
        }

        IList<Quote> args = QuoteArgs(callExpression);
        Environment evalEnvironment = ExtendMacroEnvironment(macro, args);

        IObject evaluated = Eval(macro.body, evalEnvironment);

        if(evaluated is Quote quote)
        {
          return quote.node;
        }
        if(evaluated is Error error)
        {
          return new ErrorNode() { message = error.message };
        }
        return new ErrorNode() { message = $"We only supporte returing AST-nodes from macros." };
      });
    }

    private (Macro, bool) IsMacroCall(CallExpression callExpression, Environment environment)
    {
      if(!(callExpression.function is Identifier identifier))
      {
        return (null, false);
      }

      (IObject @object, bool ok) = environment.Get(identifier.value);
      if(!ok)
      {
        return (null, false);
      }

      if(!(@object is Macro macro))
      {
        return (null, false);
      }

      return (macro, true);
    }

    private IList<Quote> QuoteArgs(CallExpression callExpression)
    {
      IList<Quote> args = new List<Quote>();

      foreach(IExpression arg in callExpression.arguments)
      {
        args.Add(new Quote() { node = arg });
      }

      return args;
    }


    private Environment ExtendMacroEnvironment(Macro macro, IList<Quote> args)
    {
      Environment extended = new Environment(macro.environment);

      for(int i = 0; i < macro.parameters.Count; ++i)
      {
        extended.Set(macro.parameters[i].value, args[i]);
      }

      return extended;
    }
  } // class
} // namespace
