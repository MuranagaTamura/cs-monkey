using System.Collections.Generic;

namespace CsMonkey
{
  public class Token
  {
    static readonly Dictionary<string, Type> KEYWORDS = new Dictionary<string, Type>()
    {
      { "fn", Type.FUNCTION },
      { "let", Type.LET },
      { "true", Type.TRUE },
      { "false", Type.FALSE },
      { "if", Type.IF },
      { "else", Type.ELSE },
      { "return", Type.RETURN },
      { "macro", Type.MACRO },
    };

    public static Type LookupIdent(string ident)
    {
      if (KEYWORDS.TryGetValue(ident, out Type type))
        // キーワードだったのでIDENTではないTypeを返す
        return type;

      // キーワードではないのでIDENTを返す
      return Type.IDENT;
    }

    public enum Type
    {
      ILLIGAL,
      EOF,

      // 識別子 + リテラル
      IDENT,
      INT,
      STRING,

      // 演算子
      ASSIGN, // =
      PLUS, // +
      MINUS, // -
      ASTERISK, // *
      SLASH, // /
      BANG, // !

      LT, // <
      GT, // >

      EQ, // ==
      NOT_EQ, // !=

      // デリミタ
      COMMA, // ,
      SEMICOLON, // ;
      COLON, // :

      LPAREN, // (
      RPAREN, // )
      LBRACE, // {
      RBRACE, // }
      LBRACKET, // [
      RBRACKET, // ]

      // キーワード
      FUNCTION,
      LET,
      TRUE,
      FALSE,
      IF,
      ELSE,
      RETURN,
      MACRO,
    }

    public Type TokenType;
    public string Literal;

    public override string ToString()
    {
      return $"{{TpkenType:{TokenType}, Literal:{Literal}}}";
    }
  } // class
} // namespace
