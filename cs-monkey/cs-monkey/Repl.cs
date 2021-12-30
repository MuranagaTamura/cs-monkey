using System;
using System.Collections.Generic;
using System.Text;

namespace CsMonkey
{
  public class Repl
  {
    readonly string PROMPT = "$ ";

    public void Start()
    {
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

        // パースした結果を表示する
        Console.WriteLine(program);
      }
    }

    private void PrintParseErrors(IList<string> errors)
    {
      foreach (string error in errors)
        Console.WriteLine($"\t{error}");
    }
  } // class
} // namespace
