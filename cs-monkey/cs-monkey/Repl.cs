using CsMonkey.Object;
using System;
using System.Collections.Generic;

namespace CsMonkey
{
  public class Repl
  {
    readonly string PROMPT = "$ ";

    public void Start()
    {
      Object.Environment environment = new Object.Environment();
      Object.Environment macroEnvironment = new Object.Environment();

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

        // 評価器を起動
        Evaluator evaluator = new Evaluator();

        // マクロを定義します
        evaluator.DefineMacros(program, macroEnvironment);
        Ast.INode expanded = evaluator.ExpandMacros(program, macroEnvironment);

        // 評価器で評価開始
        IObject result =  evaluator.Eval(expanded, environment);
        if(result != null)
        {
          // 評価結果がNullではなかった
          Console.WriteLine($"{result.Inspect()}");
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
