namespace CsMonkey
{
  public class Lexer
  {
    public string input;
    public int position;
    public int readPosition;
    public char ch;

    public Lexer(string input)
    {
      this.input = input;

      // chの初期化
      ReadChar();
    }

    public Token NextToken()
    {
      // WhiteSpace対象をスキップする
      SkipWhiteSpace();

      // 初期値はLiteralがch、TokenTypeはILLIGAL
      Token token = new Token()
      {
        TokenType = Token.Type.ILLIGAL,
        Literal = $"{ch}"
      };

      switch (ch)
      {
        // chによって返すトークンタイプTyokenTypeを変更する
        case '=':
          if (PeekChar() == '=')
          {
            char ch = this.ch;
            ReadChar();
            string literal = $"{ch}" + this.ch;
            token.Literal = literal;
            token.TokenType = Token.Type.EQ;
          }
          else
          {
            token.TokenType = Token.Type.ASSIGN;
          }
          break;
        case '+':
          token.TokenType = Token.Type.PLUS;
          break;
        case '-':
          token.TokenType = Token.Type.MINUS;
          break;
        case '*':
          token.TokenType = Token.Type.ASTERISK;
          break;
        case '/':
          token.TokenType = Token.Type.SLASH;
          break;
        case '!':
          if (PeekChar() == '=')
          {
            char ch = this.ch;
            ReadChar();
            string literal = $"{ch}" + this.ch;
            token.Literal = literal;
            token.TokenType = Token.Type.NOT_EQ;
          }
          else
          {
            token.TokenType = Token.Type.BANG;
          }
          break;
        case '<':
          token.TokenType = Token.Type.LT;
          break;
        case '>':
          token.TokenType = Token.Type.GT;
          break;
        case ',':
          token.TokenType = Token.Type.COMMA;
          break;
        case ';':
          token.TokenType = Token.Type.SEMICOLON;
          break;
        case ':':
          token.TokenType = Token.Type.COLON;
          break;
        case '(':
          token.TokenType = Token.Type.LPAREN;
          break;
        case ')':
          token.TokenType = Token.Type.RPAREN;
          break;
        case '{':
          token.TokenType = Token.Type.LBRACE;
          break;
        case '}':
          token.TokenType = Token.Type.RBRACE;
          break;
        case '[':
          token.TokenType = Token.Type.LBRACKET;
          break;
        case ']':
          token.TokenType = Token.Type.RBRACKET;
          break;
        case '"':
          token.TokenType = Token.Type.STRING;
          token.Literal = ReadString();
          break;
        case '\0':
          token.TokenType = Token.Type.EOF;
          break;
        default:
          if (IsLetter(ch))
          {
            // chが[a-zA-Z_]だった
            // 対象となる文字列を取得する
            token.Literal = ReadIdentifier();
            // キーワードなら、それに適したToken.Typeが設定される
            token.TokenType = Token.LookupIdent(token.Literal);
            return token;
          }
          else if (IsDigit(ch))
          {
            // chが[0-9]だった
            // TokenTypeをINTに設定する
            token.TokenType = Token.Type.INT;
            // 数値部分のみ取得、設定する
            token.Literal = ReadNumber();
            return token;
          }
          break;
      }

      // chを更新
      ReadChar();
      return token;
    }

    public void ReadChar()
    {
      if (readPosition >= input.Length)
      {
        // 読み込み対象超えの文字を読み込もうとした
        ch = '\0';
      }
      else
      {
        // 読み込み対象内
        ch = input[readPosition];
      }

      // 読み込み位置を変更する
      position = readPosition;
      ++readPosition;
    }

    private bool IsDigit(char ch) => '0' <= ch && ch <= '9';

    private bool IsLetter(char ch)
      => 'a' <= ch && ch <= 'z' || 'A' <= ch && ch <= 'Z' || ch == '_';

    private char PeekChar()
    {
      if(readPosition >= input.Length)
      {
        // 読み込み対象超えの文字だった
        return '\0';
      }
      else
      {
        // 読み込み対象内
        return input[readPosition];
      }
    }

    private string SafeSubString(string text, int start, int length)
    {
      if (start >= text.Length) /* startが範囲外 */ return "";
      if (start + length >= text.Length) /* start + lengthが範囲外 */ return text.Substring(start);
      return text.Substring(start, length);
    }

    private string ReadIdentifier()
    {
      int position = this.position;
      while (IsLetter(ch))
      {
        // chが[a-zA-Z_]の場合ループする
        // chがEOFまで行ったら、ループは止まる
        ReadChar();
      }

      // 開始位置から[a-zA-Z_]かEOFまでの文字列を返す
      return SafeSubString(input, position, this.position - position);
    }

    private string ReadNumber()
    {
      int position = this.position;
      while (IsDigit(ch))
      {
        // chが[0-9]の場合ループする
        // chがEOFまで行ったら、ループは止まる
        ReadChar();
      }

      // 開始位置から[0-9]かEOFまでの文字列を返す
      return SafeSubString(input, position, this.position - position);
    }

    public string ReadString()
    {
      int position = this.position + 1;
      while(true)
      {
        ReadChar();
        if (ch == '"' || ch == '\0')
          break;
      }
      return SafeSubString(input, position, this.position - position);
    }

    private void SkipWhiteSpace()
    {
      while (ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r')
        // chが[ \t\n\r]の場合は読み飛ばす
        ReadChar();
    }
  } // class
} // namespace
