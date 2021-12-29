using System;

namespace CsMonkey
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Hello! This is thw CsMonkey programming language!");
      Console.WriteLine("Feel to type commands");
      
      // REPLを起動
      Repl repl = new Repl();
      repl.Start();
    }
  }
}
