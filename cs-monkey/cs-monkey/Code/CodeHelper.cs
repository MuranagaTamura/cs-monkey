using CsMonkey.Ast;
using CsMonkey.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsMonkey.Code
{
  public enum Opcode : byte
  {
    OpConstant = 0,
    OpPop,
    OpAdd,
    OpSub,
    OpMul,
    OpDiv,
    OpTrue,
    OpFalse,
    OpNull,
    OpEqual,
    OpNotEqual,
    OpGreaterThan,
    OpMinus,
    OpBang,
    OpJumpNotTruthy,
    OpJump,
    OpGetGlobal,
    OpSetGlobal,
    OpArray,
    OpHash,
    OpIndex,
    OpCall,
    OpReturnValue,
    OpReturn,
    OpGetLocal,
    OpSetLocal,
    OpGetBuiltin,
    OpClosure,
    OpGetFree,
    OpCurrentClosure,
  }

  public class CodeHelper
  {

    private static Dictionary<Opcode, Definition> definitions = new Dictionary<Opcode, Definition>()
    {
      { Opcode.OpConstant, new Definition(){ name = nameof(Opcode.OpConstant), operandWidths = new List<int>(){ 2 } } },
      { Opcode.OpPop, new Definition(){ name = nameof(Opcode.OpPop), operandWidths = new List<int>{  } } },
      { Opcode.OpAdd, new Definition(){ name = nameof(Opcode.OpAdd), operandWidths = new List<int>{  } } },
      { Opcode.OpSub, new Definition(){ name = nameof(Opcode.OpSub), operandWidths = new List<int>{  } } },
      { Opcode.OpMul, new Definition(){ name = nameof(Opcode.OpMul), operandWidths = new List<int>{  } } },
      { Opcode.OpDiv, new Definition(){ name = nameof(Opcode.OpDiv), operandWidths = new List<int>{  } } },
      { Opcode.OpTrue, new Definition(){ name = nameof(Opcode.OpTrue), operandWidths = new List<int>{  } } },
      { Opcode.OpFalse, new Definition(){ name = nameof(Opcode.OpFalse), operandWidths = new List<int>{  } } },
      { Opcode.OpNull, new Definition(){ name = nameof(Opcode.OpNull), operandWidths = new List<int>{  } } },
      { Opcode.OpEqual, new Definition(){ name = nameof(Opcode.OpEqual), operandWidths = new List<int>{  } } },
      { Opcode.OpNotEqual, new Definition(){ name = nameof(Opcode.OpNotEqual), operandWidths = new List<int>{  } } },
      { Opcode.OpGreaterThan, new Definition(){ name = nameof(Opcode.OpGreaterThan), operandWidths = new List<int>{  } } },
      { Opcode.OpMinus, new Definition(){ name = nameof(Opcode.OpMinus), operandWidths = new List<int>{  } } },
      { Opcode.OpBang, new Definition(){ name = nameof(Opcode.OpBang), operandWidths = new List<int>{  } } },
      { Opcode.OpJumpNotTruthy, new Definition(){ name = nameof(Opcode.OpJumpNotTruthy), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpJump, new Definition(){ name = nameof(Opcode.OpJump), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpGetGlobal, new Definition(){ name = nameof(Opcode.OpGetGlobal), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpSetGlobal, new Definition(){ name = nameof(Opcode.OpSetGlobal), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpArray, new Definition(){ name = nameof(Opcode.OpArray), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpHash, new Definition(){ name = nameof(Opcode.OpHash), operandWidths = new List<int>{ 2 } } },
      { Opcode.OpIndex, new Definition(){ name = nameof(Opcode.OpIndex), operandWidths = new List<int>{  } } },
      { Opcode.OpCall, new Definition(){ name = nameof(Opcode.OpCall), operandWidths = new List<int>{ 1 } } },
      { Opcode.OpReturnValue, new Definition(){ name = nameof(Opcode.OpReturnValue), operandWidths = new List<int>{  } } },
      { Opcode.OpReturn, new Definition(){ name = nameof(Opcode.OpReturn), operandWidths = new List<int>{  } } },
      { Opcode.OpGetLocal, new Definition(){ name = nameof(Opcode.OpGetLocal), operandWidths = new List<int>{ 1 } } },
      { Opcode.OpSetLocal, new Definition(){ name = nameof(Opcode.OpSetLocal), operandWidths = new List<int>{ 1 } } },
      { Opcode.OpGetBuiltin, new Definition(){ name = nameof(Opcode.OpGetBuiltin), operandWidths = new List<int>{ 1 } } },
      { Opcode.OpClosure, new Definition(){ name = nameof(Opcode.OpClosure), operandWidths = new List<int>{ 2, 1 } } },
      { Opcode.OpGetFree, new Definition(){ name = nameof(Opcode.OpGetFree), operandWidths = new List<int>{ 1 } } },
      { Opcode.OpCurrentClosure, new Definition(){ name = nameof(Opcode.OpCurrentClosure), operandWidths = new List<int>{ } } },
    };

    private static (Definition, bool) Lookup(byte op)
    {
      if (!definitions.TryGetValue((Opcode)op, out Definition definition))
      {
        return (null, false);
      }
      return (definition, true);
    }

    public static IList<byte> Make(Opcode op, params int[] oprands)
    {
      if (!definitions.TryGetValue(op, out Definition definition))
      {
        return new byte[0];
      }

      List<byte> instruction = new List<byte>() { (byte)op };

      int offset = 1;
      for (int i = 0; i < oprands.Length; ++i)
      {
        int width = definition.operandWidths[i];
        switch (width)
        {
          case 2:
            {
              instruction.AddRange(BitConverter.GetBytes((ushort)oprands[i]));
              break;
            }
          case 1:
            {
              instruction.Add((byte)oprands[i]);
              break;
            }
        }
        offset += width;
      }

      return instruction;
    }

    public static string String(IList<byte> instruction, IList<IObject> constants, IObject[] globals, bool has_tab = false)
    {
      StringBuilder builder = new StringBuilder();
      string prefix = has_tab ? "  " : "";

      for (int i = 0; i < instruction.Count;)
      {
        (Definition definition, bool ok) = Lookup(instruction[i]);
        if (!ok)
        {
          builder.AppendLine($"{prefix}ERROR: opcode {instruction[i]} undefined");
          continue;
        }
        (IList<int> oprands, int read) = ReadOprands(definition, instruction, i + 1);
        builder.AppendLine($"{prefix}{i:0000} {FormatInstruction(definition, constants, globals, oprands)}");
        i = read;
      }

      return builder.ToString();
    }

    private static string FormatInstruction(Definition definition, IList<IObject> constants, IObject[] globals, IList<int> oprands)
    {
      if (oprands.Count != definition.operandWidths.Count)
      {
        return $"ERROR: oprand length {oprands.Count} does not match define {definition.operandWidths.Count}";
      }

      switch (oprands.Count)
      {
        case 0:
          {
            return definition.name;
          }
        case 1:
          {
            return $"{definition.name} {oprands[0]}{GetObjectInspect(definition, constants, globals, oprands[0])}";
          }
        case 2:
          {
            return $"{definition.name} {oprands[0]}{GetObjectInspect(definition, constants, globals, oprands[0])} {oprands[1]}";
          }
      }
      return $"ERROR: unhandled oprand Cound for {definition.name}";
    }

    private static string GetObjectInspect(Definition definition, IList<IObject> constants, IObject[] globals, int oprand)
    {
      switch (definition.name)
      {
        case nameof(Opcode.OpConstant):
          {
            if (constants == null)
            {
              return "";
            }
            if (Math.Clamp(oprand, 0, constants.Count - 1) != oprand)
            {
              return "";
            }
            return $"(= {constants[oprand].Inspect()})";
          }
        case nameof(Opcode.OpGetGlobal):
          {
            if (globals == null)
            {
              return "";
            }
            if (Math.Clamp(oprand, 0, globals.Length - 1) != oprand)
            {
              return "";
            }
            if (globals[oprand] == null)
            {
              return "";
            }
            return $"(= {globals[oprand].Inspect()})";
          }
        default:
          {
            return "";
          }
      }
    }

    public static (IList<int>, int) ReadOprands(Definition definition, IList<byte> instruction, int read)
    {
      IList<int> oprands = new int[definition.operandWidths.Count];
      int offset = read;

      for (int i = 0; i < definition.operandWidths.Count; ++i)
      {
        int width = definition.operandWidths[i];
        switch (width)
        {
          case 2:
            {
              oprands[i] = ReadUint16(instruction, offset);
              break;
            }
          case 1:
            {
              oprands[i] = ReadUint8(instruction, offset);
              break;
            }
        }
        offset += width;
      }

      return (oprands, offset);
    }

    public static byte ReadUint8(IList<byte> instruction, int offset)
    {
      return instruction[offset];
    }

    public static ushort ReadUint16(IList<byte> instruction, int offset)
    {
      return BitConverter.ToUInt16(instruction.ToArray(), offset);
    }
  }
}
