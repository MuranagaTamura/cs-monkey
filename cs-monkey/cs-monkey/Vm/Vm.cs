using CsMonkey.Code;
using CsMonkey.Compiler;
using CsMonkey.Object;
using System.Collections.Generic;

namespace CsMonkey.Vm
{
  public class Vm
  {
    public const int STACK_SIZE = 2048;
    public const int GLOBAL_SIZE = 65536;
    public const int FRAME_SIZE = 1024;

    public static readonly Null NULL = new Null();
    public static readonly Boolean TRUE = new Boolean() { value = true };
    public static readonly Boolean FALSE = new Boolean() { value = false };

    public IList<IObject> constants;

    private IObject[] stack;
    private int sp;

    private IObject[] globals;

    Frame[] frames = new Frame[FRAME_SIZE];
    int frameIndex;

    public Vm(Bytecode bytecode)
    {
      constants = bytecode.constants;

      stack = new IObject[STACK_SIZE];
      sp = 0;

      globals = new IObject[GLOBAL_SIZE];

      CompiledFunction mainFunction = new CompiledFunction
      {
        instlactions = bytecode.instruction,
      };
      Closure mainClosure = new Closure() { function = mainFunction };
      frames[0] = new Frame(mainClosure, 0);
      frameIndex = 1;
    }

    public static Vm WithGlobalStore(Bytecode bytecode, IObject[] globals)
    {
      Vm vm = new Vm(bytecode);
      vm.globals = globals;
      return vm;
    }

    public IObject StackTop() => sp == 0 ? null : stack[sp - 1];

    public IObject LastPopedStackElement() => stack[sp];

    public (bool, string) Run()
    {
      while (true)
      {
        bool success;
        string message;
        Frame currentFrame;

        (success, currentFrame) = CurrentFrame();
        if (!success)
        {
          return (false, $"cannot access this frame");
        }
        if (currentFrame.ip >= currentFrame.Instructions().Count - 1)
        {
          break;
        }

        ++currentFrame.ip;

        int ip = currentFrame.ip;
        IList<byte> instructions = currentFrame.Instructions();
        Opcode op = (Opcode)instructions[ip];

        switch (op)
        {
          case Opcode.OpConstant:
            {
              int constIndex = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;
              if (System.Math.Clamp(constIndex, 0, constants.Count - 1) != constIndex)
              {
                return (false, $"Undefined const index -> {constIndex}");
              }
              (success, message) = Push(constants[constIndex]);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpPop:
            {
              (success, _) = Pop();
              if (!success)
              {
                return (false, "cannot pop this object");
              }
              break;
            }
          case Opcode.OpAdd:
          case Opcode.OpSub:
          case Opcode.OpMul:
          case Opcode.OpDiv:
            {
              (success, message) = ExecuteBinaryOperation(op);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpTrue:
            {
              (success, message) = Push(TRUE);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpFalse:
            {
              (success, message) = Push(FALSE);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpNull:
            {
              (success, message) = Push(NULL);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpEqual:
          case Opcode.OpNotEqual:
          case Opcode.OpGreaterThan:
            {
              (success, message) = ExecuteComparison(op);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpMinus:
            {
              (success, message) = ExecuteMinusOperator();
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpBang:
            {
              (success, message) = ExecuteBangOperator();
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpJumpNotTruthy:
            {
              int pos = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;

              (success, IObject condition) = Pop();
              if (!success)
              {
                return (false, $"cannot pop right expression({op}).");
              }
              if (!IsTruthy(condition))
              {
                currentFrame.ip = pos - 1;
              }
              break;
            }
          case Opcode.OpJump:
            {
              int pos = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip = pos - 1;
              break;
            }
          case Opcode.OpSetGlobal:
            {
              int globalIndex = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;
              (success, globals[globalIndex]) = Pop();
              if (!success)
              {
                return (false, $"cannot pop right expression({op}).");
              }
              break;
            }
          case Opcode.OpGetGlobal:
            {
              int globalIndex = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;

              (success, message) = Push(globals[globalIndex]);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpArray:
            {
              int numElemeents = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;

              (success, IObject array) = BuildArray(sp - numElemeents, sp);
              if (!success)
              {
                return (false, $"cannot build array: numElemets->{numElemeents}");
              }
              sp -= numElemeents;

              (success, message) = Push(array);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpHash:
            {
              int numElements = CodeHelper.ReadUint16(instructions, ip + 1);
              currentFrame.ip += 2;

              (success, IObject hash) = BuildHash(sp - numElements, sp);
              if (!success)
              {
                return (hash is Error error) ? (success, error.message) : (success, $"unusable as hash key: {hash.ObjectType}");
              }
              sp -= numElements;

              (success, message) = Push(hash);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpIndex:
            {
              (success, IObject index) = Pop();
              if (!success)
              {
                return (false, $"cannot pop index expression({op}).");
              }

              (success, IObject left) = Pop();
              if (!success)
              {
                return (false, $"cannot pop left expression({op})");
              }

              (success, message) = ExecuteIndexExpression(left, index);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpCall:
            {
              int numArguments = CodeHelper.ReadUint8(instructions, ip + 1);
              currentFrame.ip += 1;

              (success, message) = ExecuteCall(numArguments);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpReturnValue:
            {
              (success, IObject returnValue) = Pop();
              if (!success)
              {
                return (false, $"cannot pop returValue expression({op}).");
              }

              (success, Frame frame) = PopFrame();
              if (!success)
              {
                return (false, $"cannot pop this frame.");
              }
              sp = frame.basePointer - 1;

              (success, message) = Push(returnValue);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpReturn:
            {
              (success, Frame fraem) = PopFrame();
              if (!success)
              {
                return (false, "cannot pop this frame.");
              }
              sp = fraem.basePointer - 1;

              (success, message) = Push(NULL);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpSetLocal:
            {
              int localIndex = CodeHelper.ReadUint8(instructions, ip + 1);
              currentFrame.ip += 1;

              Frame frame = currentFrame;
              (success, stack[frame.basePointer + localIndex]) = Pop();
              if (!success)
              {
                return (false, $"cannot pop stack[bp({frame.basePointer}) + localIndex({localIndex})] expression({op}).");
              }
              break;
            }
          case Opcode.OpGetLocal:
            {
              int localIndex = CodeHelper.ReadUint8(instructions, ip + 1);
              currentFrame.ip += 1;

              Frame frame = currentFrame;

              (success, message) = Push(stack[frame.basePointer + localIndex]);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpGetBuiltin:
            {
              int builtinIndex = CodeHelper.ReadUint8(instructions, ip + 1);
              currentFrame.ip += 1;

              if (System.Math.Clamp(builtinIndex, 0, BuiltinHelper.builtins.Count - 1) != builtinIndex)
              {
                return (false, $"cannot access from buitin id({builtinIndex}).");
              }
              BuiltinHelper.Defininition definition = BuiltinHelper.builtins[builtinIndex];

              (success, message) = Push(definition.builtin);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpClosure:
            {
              int constIndex = CodeHelper.ReadUint16(instructions, ip + 1);
              int numFree = CodeHelper.ReadUint8(instructions, ip + 3);
              currentFrame.ip += 3;

              (success, message) = PushClosure(constIndex, numFree);
              if (!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpGetFree:
            {
              int freeIndex = CodeHelper.ReadUint8(instructions, ip + 1);
              currentFrame.ip += 1;

              Closure currentClosure = currentFrame.closure;

              if(System.Math.Clamp(freeIndex, 0, currentClosure.free.Count - 1) != freeIndex)
              {
                return (false, $"cannot access to free from closure.free[{freeIndex}].");
              }
              (success, message) = Push(currentClosure.free[freeIndex]);
              if(!success)
              {
                return (success, message);
              }
              break;
            }
          case Opcode.OpCurrentClosure:
            {
              Closure currentClosure = currentFrame.closure;
              (success, message) = Push(currentClosure);
              if(!success)
              {
                return (success, message);
              }
              break;
            }
          default:
            {
              return (false, $"unsupported opcode -> {op}.");
            }
        }
      }
      return (true, "");
    }

    private (bool, string) Push(IObject @object)
    {
      if (sp >= STACK_SIZE)
      {
        return (false, "Stack overflow.");
      }
      stack[sp] = @object;
      ++sp;
      return (true, "");
    }

    private (bool, IObject) Pop()
    {
      if (sp <= 0)
      {
        return (false, null);
      }
      return (true, stack[--sp]);
    }

    private (bool, Frame) CurrentFrame()
    {
      if (System.Math.Clamp(frameIndex - 1, 0, FRAME_SIZE - 1) != frameIndex - 1)
      {
        return (false, null);
      }
      return (true, frames[frameIndex - 1]);
    }

    private bool PushFrame(Frame frame)
    {
      if (System.Math.Clamp(frameIndex + 1, 0, FRAME_SIZE - 1) != frameIndex + 1)
      {
        return false;
      }
      frames[frameIndex++] = frame;
      return true;
    }

    private (bool, Frame) PopFrame()
    {
      if (System.Math.Clamp(frameIndex - 1, 0, FRAME_SIZE - 1) != frameIndex - 1)
      {
        return (false, null);
      }
      --frameIndex;
      return (true, frames[frameIndex]);
    }

    private (bool, string) PushClosure(int constIndex, int numFree)
    {
      if (System.Math.Clamp(constIndex, 0, constants.Count - 1) != constIndex)
      {
        return (false, $"cannot access to closure from const[{constIndex}].");
      }
      IObject constant = constants[constIndex];
      if (!(constant is CompiledFunction compiledFunction))
      {
        return (false, $"not a function: {constant.Inspect()}");
      }

      IList<IObject> free = new List<IObject>();
      for (int i = 0; i < numFree; ++i)
      {
        if (System.Math.Clamp(sp - numFree + i, 0, STACK_SIZE - 1) != sp - numFree + i)
        {
          return (false, $"cannot access to free from stack[{sp - numFree + i}].");
        }
        free.Add(stack[sp - numFree + i]);
      }
      sp = sp - numFree;
      if(sp < 0)
      {
        return (false, $"sp is minus in closure.");
      }

      Closure closure = new Closure() { function = compiledFunction, free = free };
      return Push(closure);
    }

    private bool IsTruthy(IObject @object)
    {
      switch (@object)
      {
        case Boolean boolean:
          {
            return boolean.value;
          }
        case Null:
          {
            return false;
          }
        default:
          {
            return true;
          }
      }
    }

    private (bool, IObject) BuildArray(int startIndex, int endIndex)
    {
      IList<IObject> elements = new IObject[endIndex - startIndex];
      for (int i = startIndex; i < endIndex; ++i)
      {
        if (System.Math.Clamp(i - startIndex, 0, elements.Count - 1) != i - startIndex)
        {
          return (false, null);
        }
        elements[i - startIndex] = stack[i];
      }
      return (true, new Array() { elements = elements });
    }

    private (bool, IObject) BuildHash(int startIndex, int endIndex)
    {
      Dictionary<HashKey, HashPair> hashedPairs = new Dictionary<HashKey, HashPair>();
      for (int i = startIndex; i < endIndex; i += 2)
      {
        if (System.Math.Clamp(i + 1, 0, STACK_SIZE - 1) != i + 1)
        {
          return (false, new Error() { message = $"cannot access to stack by index({i}, {i + 1})." });
        }
        IObject key = stack[i];
        IObject value = stack[i + 1];
        HashPair pair = new HashPair() { key = key, value = value };

        if (!(key is Hashable hashKey))
        {
          return (false, key);
        }
        hashedPairs[hashKey.HashKey()] = pair;
      }
      return (true, new Hash() { pairs = hashedPairs });
    }

    private (bool, string) ExecuteBinaryOperation(Opcode op)
    {
      (bool success, IObject right) = Pop();
      if (!success)
      {
        return (false, $"cannot pop right expression({op}).");
      }
      (success, IObject left) = Pop();
      if (!success)
      {
        return (false, $"cannot pop left expression({op}).");
      }

      IObject.Type leftType = left.ObjectType;
      IObject.Type rightType = right.ObjectType;

      if (leftType == IObject.Type.INTEGER_OBJ && rightType == IObject.Type.INTEGER_OBJ)
      {
        return ExecuteBinaryIntegerOperation(op, (Integer)left, (Integer)right);
      }
      else if (leftType == IObject.Type.STRING_OBJ && rightType == IObject.Type.STRING_OBJ)
      {
        return ExecuteBinaryStringOperation(op, (String)left, (String)right);
      }

      return (false, $"unsupported types for binary operation: ${leftType} %{rightType}");
    }

    private (bool, string) ExecuteBinaryIntegerOperation(Opcode op, Integer left, Integer right)
    {
      long leftValue = left.value;
      long rightValue = right.value;

      long result = leftValue;

      switch (op)
      {
        case Opcode.OpAdd:
          {
            result += rightValue;
            break;
          }
        case Opcode.OpSub:
          {
            result -= rightValue;
            break;
          }
        case Opcode.OpMul:
          {
            result *= rightValue;
            break;
          }
        case Opcode.OpDiv:
          {
            result /= rightValue;
            break;
          }
        default:
          {
            return (false, $"unknown integer operation: {op}");
          }
      } // switch
      return Push(new Integer() { value = result });
    }

    private (bool, string) ExecuteBinaryStringOperation(Opcode op, String left, String right)
    {
      if (op != Opcode.OpAdd)
      {
        return (false, $"unknown integer operation: {op}");
      }

      string leftValue = left.value;
      string rightValue = right.value;
      return Push(new String() { value = leftValue + rightValue });
    }

    private (bool, string) ExecuteComparison(Opcode op)
    {
      (bool success, IObject right) = Pop();
      if (!success)
      {
        return (false, $"cannot pop right expression({op}).");
      }
      (success, IObject left) = Pop();
      if (!success)
      {
        return (false, $"cannot pop left expression({op}).");
      }

      IObject.Type leftType = left.ObjectType;
      IObject.Type rightType = right.ObjectType;

      if (leftType == IObject.Type.INTEGER_OBJ && rightType == IObject.Type.INTEGER_OBJ)
      {
        return ExecuteIntegerComparison(op, (Integer)left, (Integer)right);
      }

      switch (op)
      {
        case Opcode.OpEqual:
          {
            return Push(NativeBoolToBooleanObject(right == left));
          }
        case Opcode.OpNotEqual:
          {
            return Push(NativeBoolToBooleanObject(right != left));
          }
        default:
          {
            return (false, $"unknown operator: {op} ({leftType}, {rightType})");
          }
      }
    }

    private (bool, string) ExecuteIntegerComparison(Opcode op, Integer left, Integer right)
    {
      long leftValue = left.value;
      long rightValue = right.value;

      switch (op)
      {
        case Opcode.OpEqual:
          {
            return Push(NativeBoolToBooleanObject(leftValue == rightValue));
          }
        case Opcode.OpNotEqual:
          {
            return Push(NativeBoolToBooleanObject(leftValue != rightValue));
          }
        case Opcode.OpGreaterThan:
          {
            return Push(NativeBoolToBooleanObject(leftValue > rightValue));
          }
        default:
          {
            return (false, $"unknown operator: {op}");
          }
      }
    }

    private Boolean NativeBoolToBooleanObject(bool input)
    {
      return input ? TRUE : FALSE;
    }

    private (bool, string) ExecuteMinusOperator()
    {
      (bool success, IObject oprand) = Pop();
      if (!success)
      {
        return (false, $"cannot pop right expression(OpMinus).");
      }

      if (oprand.ObjectType != IObject.Type.INTEGER_OBJ)
      {
        return (false, $"unsupported type for negation: {oprand.ObjectType}");
      }

      long value = ((Integer)oprand).value;
      return Push(new Integer() { value = -value });
    }

    private (bool, string) ExecuteBangOperator()
    {
      (bool success, IObject oprand) = Pop();
      if (!success)
      {
        return (false, $"cannot pop right expression(OpBang).");
      }
      switch (oprand)
      {
        case Boolean boolean:
          {
            return Push(boolean.value ? FALSE : TRUE);
          }
        case Null:
          {
            return Push(TRUE);
          }
        default:
          {
            return Push(FALSE);
          }
      }
    }

    private (bool, string) ExecuteIndexExpression(IObject left, IObject index)
    {
      if (left.ObjectType == IObject.Type.ARRAY_OBJ && index.ObjectType == IObject.Type.INTEGER_OBJ)
      {
        return ExecuteArrayIndex((Array)left, (Integer)index);
      }
      if (left.ObjectType == IObject.Type.HASH_OBJ)
      {
        return ExecuteHashIndex((Hash)left, index);
      }
      return (false, $"index operator not supported: {left.ObjectType}");
    }

    private (bool, string) ExecuteArrayIndex(Array array, Integer index)
    {
      int i = (int)index.value;
      long max = array.elements.Count - 1;

      if (System.Math.Clamp(i, 0, max) != i)
      {
        return Push(NULL);
      }
      return Push(array.elements[i]);
    }

    private (bool, string) ExecuteHashIndex(Hash hash, IObject index)
    {
      if (!(index is Hashable key))
      {
        return (false, $"unusable as hash key: {index.ObjectType}");
      }

      if (!hash.pairs.TryGetValue(key.HashKey(), out HashPair pair))
      {
        return Push(NULL);
      }
      return Push(pair.value);
    }

    private (bool, string) ExecuteCall(int numArguments)
    {
      if (System.Math.Clamp(sp - 1 - numArguments, 0, STACK_SIZE - 1) != sp - 1 - numArguments)
      {
        return (false, $"cannnot access to stack by index({sp - 1 - numArguments}).");
      }

      IObject caller = stack[sp - 1 - numArguments];
      switch (caller)
      {
        case Closure closure:
          {
            return CallClosure(closure, numArguments);
          }
        case Builtin builtin:
          {
            return CallBuiltin(builtin, numArguments);
          }
        default:
          {
            return (false, $"calling non-function and non-built-in");
          }
      }
    }

    private (bool, string) CallClosure(Closure closure, int numArguments)
    {
      if (numArguments != closure.function.numParameters)
      {
        return (false, $"wrong number of argumetns: want={closure.function.numParameters}, got={numArguments}");
      }

      Frame frame = new Frame(closure, sp - numArguments);
      PushFrame(frame);
      sp = frame.basePointer + closure.function.numLocals;
      return (true, "");
    }

    private (bool, string) CallBuiltin(Builtin builtin, int numArguments)
    {
      if (System.Math.Clamp(sp - numArguments, 0, STACK_SIZE - 1) != sp - numArguments)
      {
        return (false, $"cannot access to args: stack[{sp - numArguments}=..{sp}]");
      }

      IList<IObject> args = new List<IObject>();
      for (int i = sp - numArguments; i < sp; i++)
      {
        args.Add(stack[i]);
      }

      IObject result = builtin.fn(args);
      sp = sp - numArguments - 1;
      if (sp < 0)
      {
        return (false, "stack overflow");
      }

      return Push(result != null ? result : NULL);
    }
  }
}
