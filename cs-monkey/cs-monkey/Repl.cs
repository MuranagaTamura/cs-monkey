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

        // レキサーの結果を表示
        for (Token token = lexer.NextToken(); token.TokenType != Token.Type.EOF; token = lexer.NextToken())
          Console.WriteLine($"{token}");
      }
    }
  } // class
} // namespace
