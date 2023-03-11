using System;
using CsMonkey.Ast;
using System.Collections.Generic;

namespace CsMonkey
{
  using PrefixParseFunc = Func<IExpression>;
  using InfixParseFunc = Func<IExpression, IExpression>;

  public class Parser
  {
    // %left "==" "!="
    // %left "<" ">"
    // %left "+" "-"
    // %left "*" "/"
    // %left "("
    // %left "["
    public enum Precedence
    {
      LOWEST,
      EQUALS, // ==, !=
      LESSGREATER, // <, >, <=, >=
      SUM, // +, -
      PRODUCT, // *, /, %
      PREFIX, // -X, !X
      CALL, // X(Y)
      INDEX, // X[Y]
    }

    public Lexer lexer;
    public Token currentToken;
    public Token peekToken;
    public List<string> errors = new List<string>();
    public Dictionary<Token.Type, PrefixParseFunc> prefixParseFuncs = new Dictionary<Token.Type, PrefixParseFunc>();
    public Dictionary<Token.Type, InfixParseFunc> infixParseFuncs = new Dictionary<Token.Type, InfixParseFunc>();
    public Dictionary<Token.Type, Precedence> precedences = new Dictionary<Token.Type, Precedence>()
    {
      { Token.Type.EQ, Precedence.EQUALS },
      { Token.Type.NOT_EQ, Precedence.EQUALS },
      { Token.Type.LT, Precedence.LESSGREATER },
      { Token.Type.GT, Precedence.LESSGREATER },
      { Token.Type.PLUS, Precedence.SUM },
      { Token.Type.MINUS, Precedence.SUM },
      { Token.Type.ASTERISK, Precedence.PRODUCT },
      { Token.Type.SLASH, Precedence.PRODUCT },
      { Token.Type.LPAREN, Precedence.CALL },
      { Token.Type.LBRACKET, Precedence.INDEX },
    };

    public Precedence CurrentPrecedence
      => precedences.TryGetValue(currentToken.TokenType, out Precedence p) ? p : Precedence.LOWEST;

    public Precedence PeekPrecedence
      => precedences.TryGetValue(peekToken.TokenType, out Precedence p) ? p : Precedence.LOWEST;

    public Parser(Lexer lexer)
    {
      this.lexer = lexer;

      // 2つトークンを読み込み、currentとpeekのトークンを設定する
      NextToken(); NextToken();

      // prefixParseFuncsに関数を登録する
      prefixParseFuncs[Token.Type.IDENT] = ParseIdentifier;
      prefixParseFuncs[Token.Type.INT] = ParseIntegerLiteral;
      prefixParseFuncs[Token.Type.TRUE] = ParseBoolean;
      prefixParseFuncs[Token.Type.FALSE] = ParseBoolean;
      prefixParseFuncs[Token.Type.BANG] = ParsePrefixExpression;
      prefixParseFuncs[Token.Type.MINUS] = ParsePrefixExpression;
      prefixParseFuncs[Token.Type.LPAREN] = ParseGroupedExpression;
      prefixParseFuncs[Token.Type.IF] = ParseIfExpression;
      prefixParseFuncs[Token.Type.FUNCTION] = ParseFunctionLiteral;
      prefixParseFuncs[Token.Type.STRING] = ParseStringLiteral;
      prefixParseFuncs[Token.Type.LBRACKET] = ParseArrayLiteral;
      prefixParseFuncs[Token.Type.LBRACE] = ParseHashLiteral;
      prefixParseFuncs[Token.Type.MACRO] = ParseMacroLiteral;

      // infixParseFuncsに関数を登録する
      infixParseFuncs[Token.Type.EQ] = ParseInfixExpression;
      infixParseFuncs[Token.Type.NOT_EQ] = ParseInfixExpression;
      infixParseFuncs[Token.Type.LT] = ParseInfixExpression;
      infixParseFuncs[Token.Type.GT] = ParseInfixExpression;
      infixParseFuncs[Token.Type.PLUS] = ParseInfixExpression;
      infixParseFuncs[Token.Type.MINUS] = ParseInfixExpression;
      infixParseFuncs[Token.Type.ASTERISK] = ParseInfixExpression;
      infixParseFuncs[Token.Type.SLASH] = ParseInfixExpression;
      infixParseFuncs[Token.Type.LPAREN] = ParseCallExpression;
      infixParseFuncs[Token.Type.LBRACKET] = ParseIndexExpression;
    }

    private bool ExpectPeek(Token.Type type)
    {
      if (peekToken.TokenType == type)
      {
        // 望んでいたトークンタイプであった
        // 現在とピークであるトークンを更新する
        NextToken();
        return true;
      }

      // 臨んだトークンタイプではなかった
      // 現在とピークであるトークンを更新しない
      PeekError(type);
      return false;
    }

    private void NextToken()
    {
      currentToken = peekToken;
      peekToken = lexer.NextToken();
    }

    private void NoPrefixParseFuncError(Token.Type type)
    {
      errors.Add($"no prefix parse function for {type} found.");
    }

    private void PeekError(Token.Type type)
    {
      errors.Add($"expected next token to be {type}, got {peekToken.TokenType} instead.");
    }

    // Program ::= ( Statement )*
    public Program ParseProgram()
    {
      Program program = new Program();

      while (currentToken.TokenType != Token.Type.EOF)
      {
        // 現在のトークンがEOFではない
        IStatement statement = ParseStatement();
        if (statement != null)
        {
          // Statementがパースできたら
          program.statements.Add(statement);
        }

        // 現在とピークであるトークンを更新する
        NextToken();
      }

      return program;
    }

    // Statement ::= LetStatement
    //             | ReturnStatement
    //             | ExpressionStatement
    private IStatement ParseStatement()
    {
      switch (currentToken.TokenType)
      {
        // 現在のトークンに応じてStatementをパースする
        case Token.Type.LET: return ParseLetStatement();
        case Token.Type.RETURN: return ParseReturnStatement();
        default: return ParseExpressionStatement();
      }
    }

    // ExpressionStatement ::= Expression ( ";" )?
    private ExpressionStatement ParseExpressionStatement()
    {
      ExpressionStatement statement = new ExpressionStatement();
      statement.token = currentToken;

      // 解析優先順位を考慮して式を解析する
      statement.expression = ParseExpression(Precedence.LOWEST);

      if (peekToken.TokenType == Token.Type.SEMICOLON)
        NextToken();

      return statement;
    }

    // LetStatement ::= "let" <Identifier> "=" Expression ( ";" )?
    private LetStatement ParseLetStatement()
    {
      LetStatement statement = new LetStatement();
      statement.token = currentToken;

      if (!ExpectPeek(Token.Type.IDENT))
        // ピークのトークンがIDENTではない
        return null;

      // 束縛する変数名を決定する
      statement.name = new Identifier()
      {
        token = currentToken,
        value = currentToken.Literal
      };

      if (!ExpectPeek(Token.Type.ASSIGN))
        // ピークトークンがASSIGNではない
        return null;

      NextToken();

      // 束縛対象の式をパースする
      statement.value = ParseExpression(Precedence.LOWEST);

      if (peekToken.TokenType == Token.Type.SEMICOLON)
        // ピークトークンが";"だった
        NextToken();

      return statement;
    }

    // ReturnStatement ::= "return" Expression ( ";" )?
    private ReturnStatement ParseReturnStatement()
    {
      ReturnStatement statement = new ReturnStatement();
      statement.token = currentToken;

      // 現在とピークであるトークンを更新する
      NextToken();

      // 返す式をパースする
      statement.returnValue = ParseExpression(Precedence.LOWEST);

      if (peekToken.TokenType == Token.Type.SEMICOLON)
        // ピークトークンが";"だった
        NextToken();

      return statement;
    }

    // Expression ::= InfixExpression
    //              | PrefixExpression
    private IExpression ParseExpression(Precedence precedence)
    {
      PrefixParseFunc prefix;
      if (!prefixParseFuncs.TryGetValue(currentToken.TokenType, out prefix))
      {
        // prefixParseFuncsに登録されていないのでパースできない
        NoPrefixParseFuncError(currentToken.TokenType);
        return null;
      }
      // 前置演算子に応じてパースする
      IExpression left = prefix();

      // 中置演算子に応じてパースする
      while (peekToken.TokenType != Token.Type.SEMICOLON && precedence < PeekPrecedence)
      {
        // 現在のトークンタイプがセミコロンでもなく、
        // 引数で指定された解析優先順位より次の優先順位の方が高かった
        InfixParseFunc infix;
        if (!infixParseFuncs.TryGetValue(peekToken.TokenType, out infix))
        {
          // 解析優先順位に応じるパース関数が存在しないため、左ノードを返す
          return left;
        }

        // トークンを更新する
        NextToken();

        // 左ノードを子ノードとした新たな左ノードを作成
        left = infix(left);
      }

      // 左ノードを返す
      return left;
    }

    // PrefixExpression ::= <Identifier>
    private Identifier ParseIdentifier()
    {
      return new Identifier() { token = currentToken, value = currentToken.Literal };
    }

    // PrefixExpression ::= <IntegerLiteral>
    private IntegerLiteral ParseIntegerLiteral()
    {
      IntegerLiteral literal = new IntegerLiteral();
      literal.token = currentToken;

      long value;
      if (!long.TryParse(currentToken.Literal, out value))
      {
        // 整数のみ文字列以外が含まれていてパースに失敗した
        errors.Add($"could not parse {currentToken.Literal} as integer.");
        return null;
      }

      literal.value = value;
      return literal;
    }

    // PrefixExpression ::= "true"
    //                    | "false"
    private Ast.Boolean ParseBoolean()
    {
      return new Ast.Boolean()
      {
        token = currentToken,
        value = currentToken.TokenType == Token.Type.TRUE
      };
    }

    // PrefixExpression ::= "(" Expression ")"
    private IExpression ParseGroupedExpression()
    {
      // 現在のトークンである前置演算子"("を更新
      NextToken();

      // 式をパースする
      IExpression expression = ParseExpression(Precedence.LOWEST);

      if (!ExpectPeek(Token.Type.RPAREN))
        // ピークトークンが")"ではなかった
        return null;

      return expression;
    }

    // PrefixExpression ::= "if" "(" Expression ")"
    //                      "{" BlockStatement ( "else" "{" BlockStatement )?
    private IfExpression ParseIfExpression()
    {
      IfExpression expression = new IfExpression();
      expression.token = currentToken;

      if (!ExpectPeek(Token.Type.LPAREN))
        // ピークトークンが"("ではなかった
        return null;

      NextToken();
      // ifの条件である式をパースする
      expression.condition = ParseExpression(Precedence.LOWEST);

      if (!ExpectPeek(Token.Type.RPAREN))
        // ピークトークンが")"ではなかった
        return null;

      if (!ExpectPeek(Token.Type.LBRACE))
        // ピークトークンが"{"ではなかった
        return null;

      // 条件に一致した場合のブロック文をパースする
      expression.consequence = ParseBlockStatement();

      if (peekToken.TokenType == Token.Type.ELSE)
      {
        // else キーワードがある
        NextToken();

        if (!ExpectPeek(Token.Type.LBRACE))
          // ピークトークンが"{"ではなかった
          return null;

        // 条件に一致しなかった場合のブロック文をパースする
        expression.alternative = ParseBlockStatement();
      }

      return expression;
    }

    // PrefixExpression ::= "fn" "(" FunctionParameters "{" BlockStatement
    private FunctionLiteral ParseFunctionLiteral()
    {
      FunctionLiteral literal = new FunctionLiteral();
      literal.token = currentToken;

      if (!ExpectPeek(Token.Type.LPAREN))
        // ピークトークンが"("ではなかった
        return null;

      literal.parameters = ParseFunctionParameters();

      if (!ExpectPeek(Token.Type.LBRACE))
        // ピークトークンが"{"ではなかった
        return null;

      literal.body = ParseBlockStatement();

      return literal;
    }

    // BlockStatement ::= ( Statement )* "}"
    private BlockStatement ParseBlockStatement()
    {
      BlockStatement block = new BlockStatement();
      block.token = currentToken;

      NextToken();

      while (currentToken.TokenType != Token.Type.RBRACE
        && currentToken.TokenType != Token.Type.EOF)
      {
        // 現在のトークンが"}"とEOFではない
        IStatement statement = ParseStatement();
        if (statement != null)
          // 文のパース成功している
          block.statements.Add(statement);
        // トークンを更新
        NextToken();
      }

      return block;
    }

    // FunctionParameters ::=  ( <Identifier> ( "," <Identifier> )*)? ")"
    private IList<Identifier> ParseFunctionParameters()
    {
      IList<Identifier> identifiers = new List<Identifier>();

      if (peekToken.TokenType == Token.Type.RPAREN)
      {
        // ピークトークンが")"だった
        // 関数の引数がない場合
        NextToken();
        return identifiers;
      }

      NextToken();

      // 第一引数
      identifiers.Add(new Identifier()
      {
        token = currentToken,
        value = currentToken.Literal
      });

      // 第二引数から第n引数までをパース
      while (peekToken.TokenType == Token.Type.COMMA)
      {
        // ピークトークンに”,”が存在している
        NextToken();
        NextToken();
        identifiers.Add(new Identifier()
        {
          token = currentToken,
          value = currentToken.Literal
        });
      }

      if (!ExpectPeek(Token.Type.RPAREN))
        // ピークトークンが")"ではなかった
        return null;

      return identifiers;
    }

    // PrefixExpression ::= <StringLiteral>
    private StringLiteral ParseStringLiteral()
      => new StringLiteral() { token = currentToken, value = currentToken.Literal };

    // PrefixExpression ::= "[" ExpressionList "]"
    private ArrayLiteral ParseArrayLiteral()
    {
      ArrayLiteral arrayLiteral = new ArrayLiteral();
      arrayLiteral.token = currentToken;

      arrayLiteral.elements = ParseExpressionList(Token.Type.RBRACKET);

      return arrayLiteral;
    }

    // PrefixExpression ::= "{" ( Expression ":" Expression ( "," Expression ":" Expression ) )? "}"
    private HashLiteral ParseHashLiteral()
    {
      HashLiteral hashLiteral = new HashLiteral();
      hashLiteral.token = currentToken;
      
      while(peekToken.TokenType != Token.Type.RBRACE)
      {
        // ピークトークンが"}"だった
        NextToken();
        // ハッシュのキーである式をパースする
        IExpression key = ParseExpression(Precedence.LOWEST);

        if (!ExpectPeek(Token.Type.COLON))
          // ピークトークンが":"ではなかった
          return null;

        NextToken();
        // ハッシュの値である式をパースする
        IExpression value = ParseExpression(Precedence.LOWEST);

        hashLiteral.pairs[key] = value;

        if(peekToken.TokenType != Token.Type.RBRACE && !ExpectPeek(Token.Type.COMMA))
        {
          // ピークトークンが"}"だったらその時点で終了
          // しかし、上記内容でないなら、ピークトークンが","であるはず
          // つまり、ペア―の区切りがあるはずである
          // それがない場合はどこが区切りか判断できないので、nullを返す
          return null;
        }
      }

      if (!ExpectPeek(Token.Type.RBRACE))
        // ピークトークンが"}"ではない
        return null;

      return hashLiteral;
    }

    // InfixExpression ::= Expression "==" Expression
    //                   | Expression "!=" Expression
    //                   | Expression "<" Expression
    //                   | Expression ">" Expression
    //                   | Expression "+" Expression
    //                   | Expression "-" Expression
    //                   | Expression "*" Expression
    //                   | Expression "/" Expression
    //                   | Expression "(" CallExpression
    //                   | Expression "[" IndexExpression
    private InfixExpression ParseInfixExpression(IExpression left)
    {
      InfixExpression expression = new InfixExpression();
      expression.token = currentToken;
      expression.op = currentToken.Literal;
      expression.left = left;

      // 現在のトークンタイプに応じたPrecedenceを取得して、式をパースする
      Precedence precedence = CurrentPrecedence;
      NextToken();
      expression.right = ParseExpression(precedence);

      return expression;
    }

    // MEMO: "%prec"の後に配置されているトークンと同じ優先度にする
    //       つまり、"-"と"!"は"*"と同じ優先度を持つ
    // PrefixExpression ::= "-" Expression %prec "*"
    //                    | "!" Expression %prec "*"
    private PrefixExpression ParsePrefixExpression()
    {
      PrefixExpression expression = new PrefixExpression();
      expression.token = currentToken;
      expression.op = currentToken.Literal;

      // 現在のトークンである前置演算子を更新する
      NextToken();

      // 前置演算子以降の式をパースする
      expression.right = ParseExpression(Precedence.PREFIX);

      return expression;
    }

    // CallExpression ::= "(" ExpressionList ")"
    private CallExpression ParseCallExpression(IExpression function)
    {
      CallExpression expression = new CallExpression();
      expression.token = currentToken;
      expression.function = function;
      expression.arguments = ParseExpressionList(Token.Type.RPAREN);
      return expression;
    }

    // IndexExpresssion ::= "[" Expression "]"
    private IndexExpression ParseIndexExpression(IExpression left)
    {
      IndexExpression indexExpression = new IndexExpression();
      indexExpression.token = currentToken;
      indexExpression.left = left;

      NextToken();
      indexExpression.index = ParseExpression(Precedence.LOWEST);

      if (!ExpectPeek(Token.Type.RBRACKET))
        return null;

      return indexExpression;
    }

    // ExpressionList ::= ( Expression ( "," Expression )* )*
    private IList<IExpression> ParseExpressionList(Token.Type end)
    {
      IList<IExpression> list = new List<IExpression>();

      if (peekToken.TokenType == end)
      {
        // ピークトークンがendだった
        NextToken();
        return list;
      }

      // 第0要素
      NextToken();
      list.Add(ParseExpression(Precedence.LOWEST));

      // 第2要素から第n要素まで
      while (peekToken.TokenType == Token.Type.COMMA)
      {
        // ピークトークンが","だった
        NextToken();
        NextToken();
        list.Add(ParseExpression(Precedence.LOWEST));
      }

      if (!ExpectPeek(end))
      {
        // ピークトークンがendではなかった
        return null;
      }

      return list;
    }

    // PrefixExpression ::= "macro" "(" FunctionParameters "{" BlockStatement
    private IExpression ParseMacroLiteral()
    {
      MacroLiteral literal = new MacroLiteral() { token = currentToken };

      if(!ExpectPeek(Token.Type.LPAREN))
      {
        return null;
      }

      literal.parameters = ParseFunctionParameters();

      if(!ExpectPeek(Token.Type.LBRACE))
      {
        return null;
      }

      literal.body = ParseBlockStatement();

      return literal;
    }
  } // class
} // namespace
