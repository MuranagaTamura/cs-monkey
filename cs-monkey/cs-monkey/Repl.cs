using CsMonkey.Code;
using CsMonkey.Compiler;
using CsMonkey.Object;
using CsMonkey.Optimise;
using System;
using System.Collections.Generic;

namespace CsMonkey
{
  public class Repl
  {
    readonly string PROMPT = "$ ";

    public void Start()
    {
      IList<IObject> constants = new List<IObject>();
      IObject[] globals = new IObject[Vm.Vm.GLOBAL_SIZE];
      SymbolTable symbolTable = new SymbolTable();
      for(int i = 0; i < BuiltinHelper.builtins.Count; ++i)
      {
        symbolTable.DefineBuiltin(i, BuiltinHelper.builtins[i].name);
      }

      while (true)
      {
        Console.Write(PROMPT);
        string scanned = Console.ReadLine();
        if (string.IsNullOrEmpty(scanned))
          // 入力が特に何もないので終了する
          return;

        // レキサー開始
        Lexer lexer = new Lexer(scanned);
        // パーサー開始
        Parser parser = new Parser(lexer);

        // パースしてエラーがあったら表示する
        Ast.Program program = parser.ParseProgram();
        if(parser.errors.Count != 0)
        {
          PrintParseErrors(parser.errors);
          continue;
        }

        // 最適化を行います
        program = OptimiseHelper.Optimise(program);

        // コンパイルします
        Compiler.Compiler compiler = Compiler.Compiler.WithState(symbolTable, constants);
        (bool success, string message) = compiler.Compile(program);
        if(!success)
        {
          Console.WriteLine($"\tCompilation failed:\n\t\t{message}");
          continue;
        }

        // コンパイル結果を取得します
        Bytecode bytecode = compiler.Bytecode();
        constants = bytecode.constants;

        Console.WriteLine("Compilation Result:");
        Console.WriteLine($"(Instructions):\n{CodeHelper.String(bytecode.instruction, constants, globals)}");

        // VMを起動します
        Vm.Vm vm = Vm.Vm.WithGlobalStore(bytecode, globals);
        (success, message) = vm.Run();
        if(!success)
        {
          Console.WriteLine($"\tExecuting bytecode failed:\n\t\t{message}");
          continue;
        }

        // VMのスタックトップを出力します
        IObject lastPoped = vm.LastPopedStackElement();
        if(lastPoped != null)
        {
          Console.WriteLine($"(Vm) >> {lastPoped.Inspect()}");
        }
      }
    }

    private void PrintParseErrors(IList<string> errors)
    {
      foreach (string error in errors)
        Console.WriteLine($"\t{error}");
    }
  } // class
} // namespace
