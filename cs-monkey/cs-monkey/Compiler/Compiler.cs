using CsMonkey.Ast;
using CsMonkey.Code;
using CsMonkey.Object;
using System.Collections.Generic;

namespace CsMonkey.Compiler
{
  struct EmittedInstruction
  {
    public Opcode opcode;
    public int position;
  }

  class CompilationScope
  {
    public IList<byte> instructions;
    public EmittedInstruction lastInstruction;
    public EmittedInstruction prevInstruction;
  }

  public class Compiler
  {

    IList<IObject> constants = new List<IObject>();

    SymbolTable symbolTable;

    IList<CompilationScope> scopes;
    int scopeIndex;

    public Compiler()
    {
      symbolTable = new SymbolTable();

      for (int i = 0; i < BuiltinHelper.builtins.Count; ++i)
      {
        symbolTable.DefineBuiltin(i, BuiltinHelper.builtins[i].name);
      }

      scopeIndex = 0;
      scopes = new List<CompilationScope>
      {
        new CompilationScope
        {
          instructions = new List<byte>(),
          lastInstruction = new EmittedInstruction(),
          prevInstruction = new EmittedInstruction(),
        }
      };
    }


    public static Compiler WithState(SymbolTable symbolTable, IList<IObject> constants)
    {
      Compiler compiler = new Compiler();
      compiler.symbolTable = symbolTable;
      compiler.constants = constants;
      return compiler;
    }

    public (bool, string) Compile(INode node)
    {
      bool success;
      string message;
      switch (node)
      {
        case Program program:
          {
            foreach (IStatement statement in program.statements)
            {
              (success, message) = Compile(statement);
              if (!success)
              {
                return (success, message);
              }
            }
            return (true, "");
          }
        // 文
        case ExpressionStatement expressionStatement:
          {
            (success, message) = Compile(expressionStatement.expression);
            if (!success)
            {
              return (success, message);
            }
            Emit(Opcode.OpPop);
            return (true, "");
          }
        case BlockStatement blockStatement:
          {
            foreach (IStatement statement in blockStatement.statements)
            {
              (success, message) = Compile(statement);
              if (!success)
              {
                return (success, message);
              }
            }
            return (true, "");
          }
        case LetStatement letStatement:
          {
            Symbol symbol = symbolTable.Define(letStatement.name.value);
            (success, message) = Compile(letStatement.value);
            if (!success)
            {
              return (success, message);
            }
            if (symbol.scope == Symbol.GLOBAL_SCOPE)
            {
              Emit(Opcode.OpSetGlobal, symbol.index);
            }
            else
            {
              Emit(Opcode.OpSetLocal, symbol.index);
            }
            return (true, "");
          }
        case ReturnStatement returnStatement:
          {
            (success, message) = Compile(returnStatement.returnValue);
            if (!success)
            {
              return (success, message);
            }

            Emit(Opcode.OpReturnValue);
            return (true, "");
          }
        // 式
        case InfixExpression infixExpression:
          {
            if (infixExpression.op == "<")
            {
              (success, message) = Compile(infixExpression.right);
              if (!success)
              {
                return (success, message);
              }

              (success, message) = Compile(infixExpression.left);
              if (!success)
              {
                return (success, message);
              }
              Emit(Opcode.OpGreaterThan);
              return (true, "");
            }

            (success, message) = Compile(infixExpression.left);
            if (!success)
            {
              return (success, message);
            }

            (success, message) = Compile(infixExpression.right);
            if (!success)
            {
              return (success, message);
            }

            switch (infixExpression.op)
            {
              case "+":
                {
                  Emit(Opcode.OpAdd);
                  break;
                }
              case "-":
                {
                  Emit(Opcode.OpSub);
                  break;
                }
              case "*":
                {
                  Emit(Opcode.OpMul);
                  break;
                }
              case "/":
                {
                  Emit(Opcode.OpDiv);
                  break;
                }
              case ">":
                {
                  Emit(Opcode.OpGreaterThan);
                  break;
                }
              case "==":
                {
                  Emit(Opcode.OpEqual);
                  break;
                }
              case "!=":
                {
                  Emit(Opcode.OpNotEqual);
                  break;
                }
              default:
                {
                  return (false, $"unknown operator {infixExpression.op}");
                }
            }
            return (true, "");
          }
        case PrefixExpression prefixExpression:
          {
            (success, message) = Compile(prefixExpression.right);
            if (!success)
            {
              return (success, message);
            }
            switch (prefixExpression.op)
            {
              case "-":
                {
                  Emit(Opcode.OpMinus);
                  return (true, "");
                }
              case "!":
                {
                  Emit(Opcode.OpBang);
                  return (true, "");
                }
              default:
                {
                  return (false, $"unknown operator {prefixExpression.op}");
                }
            }
          }
        case IfExpression ifExpression:
          {
            // 条件部分をコンパイルします
            (success, message) = Compile(ifExpression.condition);
            if (!success)
            {
              return (false, message);
            }

            // trueのときの処理をコンパイルします
            int jumpNotTruthyPos = Emit(Opcode.OpJumpNotTruthy, -1);
            (success, message) = Compile(ifExpression.consequence);
            if (!success)
            {
              return (success, message);
            }
            if (LastInstructionIs(Opcode.OpPop))
            {
              RemoveLastPop();
            }

            int jumpPos = Emit(Opcode.OpJump, -1);
            int afterConsequencePos = CurrentInstructions().Count;
            ChangeOprand(jumpNotTruthyPos, afterConsequencePos);

            // falseがあったときの処理をコンパイルします
            if (ifExpression.alternative == null)
            {
              Emit(Opcode.OpNull);
            }
            else
            {
              (success, message) = Compile(ifExpression.alternative);
              if (!success)
              {
                return (success, message);
              }

              if (LastInstructionIs(Opcode.OpPop))
              {
                RemoveLastPop();
              }

              int afterAlternativePos = CurrentInstructions().Count;
              ChangeOprand(jumpPos, afterAlternativePos);
            }

            return (true, "");
          }
        case IndexExpression indexExpression:
          {
            (success, message) = Compile(indexExpression.left);
            if (!success)
            {
              return (success, message);
            }

            (success, message) = Compile(indexExpression.index);
            if (!success)
            {
              return (success, message);
            }

            Emit(Opcode.OpIndex);
            return (true, "");
          }
        case CallExpression callExpression:
          {
            (success, message) = Compile(callExpression.function);
            if (!success)
            {
              return (success, message);
            }

            foreach (IExpression argument in callExpression.arguments)
            {
              (success, message) = Compile(argument);
              if (!success)
              {
                return (success, message);
              }
            }

            Emit(Opcode.OpCall, callExpression.arguments.Count);
            return (true, "");
          }
        case Identifier identifier:
          {
            (success, Symbol symbol) = symbolTable.Resolve(identifier.value);
            if (!success)
            {
              return (false, $"Undefined variable {identifier.value}");
            }
            LoadSymbol(symbol);
            return (true, "");
          }
        case IntegerLiteral integerLiteral:
          {
            Integer integer = new Integer() { value = integerLiteral.value };
            Emit(Opcode.OpConstant, AddConstant(integer));
            return (true, "");
          }
        case BooleanLiteral boolean:
          {
            if (boolean.value)
            {
              Emit(Opcode.OpTrue);
            }
            else
            {
              Emit(Opcode.OpFalse);
            }
            return (true, "");
          }
        case StringLiteral stringLiteral:
          {
            String @string = new String() { value = stringLiteral.value };
            Emit(Opcode.OpConstant, AddConstant(@string));
            return (true, "");
          }
        case ArrayLiteral arrayLiteral:
          {
            foreach (IExpression element in arrayLiteral.elements)
            {
              (success, message) = Compile(element);
              if (!success)
              {
                return (success, message);
              }
            }
            Emit(Opcode.OpArray, arrayLiteral.elements.Count);
            return (true, "");
          }
        case HashLiteral hashLiteral:
          {
            foreach ((IExpression key, IExpression value) in hashLiteral.pairs)
            {
              (success, message) = Compile(key);
              if (!success)
              {
                return (success, message);
              }
              (success, message) = Compile(value);
              if (!success)
              {
                return (success, message);
              }
            }
            Emit(Opcode.OpHash, hashLiteral.pairs.Count * 2);
            return (true, "");
          }
        case FunctionLiteral functionLiteral:
          {
            EnterScope();

            if (functionLiteral.name != "")
            {
              symbolTable.DefineFunctionName(functionLiteral.name);
            }

            foreach (Identifier parameter in functionLiteral.parameters)
            {
              symbolTable.Define(parameter.value);
            }

            (success, message) = Compile(functionLiteral.body);
            if (!success)
            {
              return (success, message);
            }

            if (LastInstructionIs(Opcode.OpPop))
            {
              ReplaceLastPopWithReturn();
            }
            if (!LastInstructionIs(Opcode.OpReturnValue))
            {
              Emit(Opcode.OpReturn);
            }

            IList<Symbol> freeSymbols = symbolTable.freeSymbols;
            int numLocals = symbolTable.numDefinitions;
            IList<byte> instructions = LeaveScope();

            foreach (Symbol symbol in freeSymbols)
            {
              LoadSymbol(symbol);
            }

            CompiledFunction compiledFunction = new CompiledFunction
            {
              instlactions = instructions,
              numLocals = numLocals,
              numParameters = functionLiteral.parameters.Count,
            };
            int functionIndex = AddConstant(compiledFunction);
            Emit(Opcode.OpClosure, functionIndex, freeSymbols.Count);
            return (true, "");
          }
      } // switch

      return (false, $"Unsupported node: token->{node.TokenLiteral}");
    }

    private int AddConstant(IObject @object)
    {
      int index = constants.IndexOf(@object);
      if (index != -1)
      {
        return index;
      }
      else
      {
        constants.Add(@object);
        return constants.Count - 1;
      }
    }

    private int Emit(Opcode op, params int[] oprands)
    {
      IList<byte> instruction = CodeHelper.Make(op, oprands);
      int pos = AddInstruction(instruction);
      SetLastInstruction(op, pos);
      return pos;
    }

    private IList<byte> CurrentInstructions()
    {
      return scopes[scopeIndex].instructions;
    }

    private int AddInstruction(IList<byte> instruction)
    {
      int posNewInstruction = CurrentInstructions().Count;
      IList<byte> updatedInstructions = CurrentInstructions();
      foreach (byte itr in instruction)
      {
        updatedInstructions.Add(itr);
      }
      scopes[scopeIndex].instructions = updatedInstructions;
      return posNewInstruction;
    }

    private void SetLastInstruction(Opcode op, int pos)
    {
      EmittedInstruction prev = scopes[scopeIndex].lastInstruction;
      EmittedInstruction last = new EmittedInstruction() { opcode = op, position = pos };

      scopes[scopeIndex].prevInstruction = prev;
      scopes[scopeIndex].lastInstruction = last;
    }

    private bool LastInstructionIs(Opcode op)
    {
      if (CurrentInstructions().Count == 0)
      {
        return false;
      }
      return scopes[scopeIndex].lastInstruction.opcode == op;
    }

    private void RemoveLastPop()
    {
      EmittedInstruction last = scopes[scopeIndex].lastInstruction;
      EmittedInstruction prev = scopes[scopeIndex].prevInstruction;

      IList<byte> old = CurrentInstructions();
      IList<byte> @new = new List<byte>(old);
      int start_index = last.position;
      for (int index = old.Count - 1; index >= start_index; --index)
      {
        @new.RemoveAt(index);
      }

      scopes[scopeIndex].instructions = @new;
      scopes[scopeIndex].lastInstruction = prev;
    }

    private void ChangeOprand(int opPos, int oprand)
    {
      Opcode op = (Opcode)CurrentInstructions()[opPos];
      IList<byte> newInstruction = CodeHelper.Make(op, oprand);
      ReplaceInstruction(opPos, newInstruction);
    }

    private void ReplaceInstruction(int pos, IList<byte> newInstruction)
    {
      IList<byte> instruction = CurrentInstructions();
      for (int i = 0; i < newInstruction.Count; ++i)
      {
        instruction[pos + i] = newInstruction[i];
      }
    }
    private void ReplaceLastPopWithReturn()
    {
      int lastPos = scopes[scopeIndex].lastInstruction.position;
      ReplaceInstruction(lastPos, CodeHelper.Make(Opcode.OpReturnValue));
      scopes[scopeIndex].lastInstruction.opcode = Opcode.OpReturnValue;
    }

    private void EnterScope()
    {
      CompilationScope scope = new CompilationScope
      {
        instructions = new List<byte>(),
        lastInstruction = new EmittedInstruction(),
        prevInstruction = new EmittedInstruction(),
      };
      scopes.Add(scope);
      ++scopeIndex;
      symbolTable = new SymbolTable(symbolTable);
    }

    private IList<byte> LeaveScope()
    {
      IList<byte> instructions = CurrentInstructions();

      scopes.RemoveAt(scopes.Count - 1);
      --scopeIndex;

      symbolTable = symbolTable.outer;

      return instructions;
    }

    private void LoadSymbol(Symbol symbol)
    {
      switch (symbol.scope)
      {
        case Symbol.GLOBAL_SCOPE:
          {
            Emit(Opcode.OpGetGlobal, symbol.index);
            break;
          }
        case Symbol.LOCAL_SCOPE:
          {
            Emit(Opcode.OpGetLocal, symbol.index);
            break;
          }
        case Symbol.BUILTIN_SCOPE:
          {
            Emit(Opcode.OpGetBuiltin, symbol.index);
            break;
          }
        case Symbol.FREE_SCOPE:
          {
            Emit(Opcode.OpGetFree, symbol.index);
            break;
          }
        case Symbol.FUNCTION_SCOPE:
          {
            Emit(Opcode.OpCurrentClosure);
            break;
          }
      }
    }

    public Bytecode Bytecode()
    {
      return new Bytecode()
      {
        instruction = CurrentInstructions(),
        constants = constants,
      };
    }
  }
}
