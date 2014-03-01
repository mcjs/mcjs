// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using mjr.IR;
using mjr.IR.Syntax;
using m.Util.Diagnose;

// Disable "Possible mistaken empty statement" warning.
#pragma warning disable 642

// Tips for working with the parser code:
//
// - External code should never interact with the parser directly, and the parser's knowledge
//   of the outside world should be minimized as much as possible. Keep the details of interfacing
//   with the rest of the code in JSParser.cs rather than here.
//
// - Most parsing functions are written by chaining together functions with boolean return values.
//   Whatever the actual return type of the function you're calling, you'll generally wrap it in
//   a Try, Require, or Allow call (or variants on those). Try(X) means to continue (that is,
//   it returns true) only if matching X at the current position succeeds. Require(X) means that
//   X _must_ matching at the current position; it will throw an exception otherwise which will
//   terminate parsing. Allow(X) means that X _may_ match at the current position, but it's OK
//   if it doesn't; it returns true no matter what. You'll usually use Try(X) when you have several
//   legal alternatives or where X can fail without the overall parse failing. You'll use Require(X)
//   when there is no alternative and failing at this point means that the overall parse must fail.
//   Allow(X) is for optional things. (If this arrangement reminds you of a monad, it's no coincidence.)
//   Generally, be sure to read over the utility methods and what they do before modifying anything
//   in the parser.
//
// - There are three layers of parsing functions. V*() functions correspond to syntactic variables
//   (nonterminals, in other words). T*() functions correspond to terminals. R*() functions correspond
//   to 'restricted' functions that do not preserve parser invariants; these are called 'restricted' because
//   they should only be called by functions that have been designed with knowledge of their internal behavior
//   and which will take responsibility for preserving parser invariants themselves. Always read the
//   definition of an R*() function before writing new code that makes use of it, so you know how it will behave.
//   Generally R*() functions are very low-level lexing functions, and should only be called by the higher-level
//   T*() lexing functions or by each other. T*() functions should in turn only be called by each other or
//   by the higher-level V*() functions, and V*() functions should only be called by other V*() functions or
//   by the public API.
//
// - The IR types are not directly used in the parser; this is very important, so that NodeFactory can
//   freely return different types of objects without breaking the parser. Instead, the parser should only
//   use the interfaces in Syntax.cs, or primitive C# types where appropriate. If the existing interfaces
//   don't suffice, a new interface should be added to Syntax.cs, and that should be used in the parser.
//
// - An important invariant that parsing functions must satisfy is that they must either match
//   successfully, terminate parsing,  or return the position in the input to where it was when they
//   started. This invariant is often satisfied by the appropriate use of Try(X) and Require(X).
//   Consider the following parsing function body:
//
//     if (Try(X))
//     {
//       Require(Y);
//       return ...;
//     }
//     else
//       return Failed<...>();
//
//   This is enough to satisfy the invariant, because if Try(X) fails, we haven't moved (since X
//   satisfies the invariant) and if Try(X) succeeds, the other checks are all Require checks that
//   will terminate parsing if they don't succeed. However, if there are multiple Try calls chained,
//   one may succeed and later ones will fail, which might result in the parser's position changing
//   when we return. In that case we should save and resture our position using the following pattern:
//
//   var atPosition = SavePosition();
//   if (Try(X) && Try(Y))
//     ...
//   else
//     return Failed<...>(atPosition);
//
//   SavePosition() returns whatever information is necessary to save the current position, and
//   passing that information to Failed<...>() will restore the position to what it was when
//   SavePosition() was called. Failed<...>() will also take care of returning a value that Try
//   and Require will recognize as failure. (This is why it takes the return type of the function
//   as a type parameter; it must construct the appropriate "failure value" for that type.)
//
// - Another important invariant in the parser is that if a rule matches, it should consume all
//   following whitespace as well. This means that we never have to check for whitespace at the
//   beginning of a rule. Since in the course of parsing many rules are tried and failed compared
//   to the few that succeed, it makes sense to read whitespace on success instead of reading it
//   over and over for rules that mostly fail. The entire parser makes this assumption, so forgetting
//   about it will introduce bugs.
//
// - When debugging, it is good to be aware of the Context() method, which will print the input
//   text around the parser's current position. This can be very useful in the Visual Studio
//   debugger as a way of quickly getting an idea of where the parser is running into trouble.
//   Another debugging tip is to put conditional breakpoints at the beginning of frequently called functions
//   like VStatement() and have them break when the position in the input (Position) nears the
//   problem area, then step forward from there.
//
// - One final reminder: order matters! You cannot always change the order in which rules are tested
//   and get the same results! Consider carefully before changing ordering.

namespace mjr.JavaScriptParser
{
  public class SequentialParser
  {
    string       Script                    { get; set; }
    int          Start                     { get; set; }
    int          Length                    { get; set; }
    NodeFactory  Factory                   { get; set; }
    int          Position                  { get; set; }
    Stack<Scope> Scopes                    { get; set; }
    Scope        Scope                     { get { return Scopes.Peek(); } }
    bool         StrictMode                { get; set; }  // Whether strict mode is enabled for the current function.
    bool         InOperatorAllowed         { get; set; }  // Whether the 'in' operator is allowed in the current context.
    bool         CommentsAllowed           { get; set; }  // Whether comments are currently allowed.
    bool         LineBreakInLastWhiteSpace { get; set; }  // Automatic semicolon insertion needs info about whitespace linebreaks.
    bool         UnaryNewInLastExpression  { get; set; }  // Used to determine when 'new' calls that take arguments are allowed.

    public SequentialParser(string script) : this(script, 0, script.Length) { }
    public SequentialParser(string script, int start, int length)
    {
      Script = script;
      Start = start;
      Length = length;
      StrictMode = false;
      Position = start;
      Factory = new NodeFactory();
      Scopes = new Stack<Scope>();
      InOperatorAllowed = true;
      CommentsAllowed = true;
      LineBreakInLastWhiteSpace = false;
      UnaryNewInLastExpression = false;
    }

    public IProgram ParseScript()
    {
      return VProgram();
    }

    public ILiteral ParseNumber()
    {
      return VStringNumericLiteral();
    }

    public string ParseRegularExpression()
    {
      return VStringRegularExpressionLiteral();
    }

    #region Syntactic Grammar - Functions and Programs

    // [ECMA] Program : SourceElements?
    // [MCJS] Program : SourceElements
    IProgram VProgram()
    {
      Scopes.Push(new Scope(null));
      Scope.IsProgram = true;

      Allow(TWhiteSpace());
      var sourceElements = RequiredValue(VSourceElements());
      Require(TEOF());

      return Factory.MakeProgram(Scopes.Pop(), sourceElements);
    }

    // [ECMA] SourceElements : SourceElement+
    // [ECMA] SourceElement  : Statement | FunctionDeclaration
    // [MCJS] SourceElements : DirectivePrologueStatement* (FunctionDeclaration | Statement)*
    // We always try FunctionDeclaration first to ensure that we don't treat function declarations as expressions.
    IStatement VSourceElements()
    {
      var atPosition = CurrentPosition();
      var statements = Factory.MakeStatementList();
      
      // Match the Directive Prologue discussed in ECMA 262 14.1.
      while (TryAndAddTo(statements, VDirectivePrologueStatement()));

      while (TryAndAddTo(statements, VFunctionDeclaration()) ||
             TryAndAddTo(statements, VStatement()));

      return Factory.MakeBlockStatement(statements, atPosition);
    }

    // [MCJS] DirectivePrologueStatement : (StringLiteral ';')*
    IStatement VDirectivePrologueStatement()
    {
      var atPosition = SavePosition();

      ILiteral directive;

      if (Try(directive = TUseStrictLiteral()) &&
          Try(TSemicolon()))
      {
        Scope.IsStrict = true;
        return Factory.MakeExpressionStatement(directive);
      }
      else
        return Failed<IStatement>(atPosition);
    }

    // [MCJS] FunctionDeclaration : FunctionDeclaration
    IStatement VFunctionStatement()
    {
      if (!StrictMode)
        return VFunctionDeclaration();
      else
        return Failed<IStatement>();
    }
    // [ECMA] FunctionDeclaration : 'function' Identifier '(' FormalParameterList? ')' '{' FunctionBody '}'
    // [ECMA] FunctionBody : SourceElements?
    // [MCJS] FunctionDeclaration : 'function' Identifier '(' FormalParameterList ')' '{' SourceElements '}'
    IStatement VFunctionDeclaration()
    {
      var atPosition = SavePosition();

      IIdentifier name;
      
      if (Try(TKeyword("function")) &&
          Try(name = TIdentifier()))
      {
        // Note that the name of a function declaration goes in its parent's scope.
        Scopes.Push(new Scope(Scope));
        Scope.IsFunctionDeclaration = true;

        Require(TChar('('));
        var args = RequiredValue(VFormalParameterList());
        Require(TChar(')'));
        Require(TChar('{'));
        var body = RequiredValue(VSourceElements());
        Require(TChar('}'));

        var innerScope = Scopes.Pop();

        return Factory.MakeFunctionDeclarationStatement(Scope, name, args, body, innerScope);
      }
      else
        return Failed<IStatement>(atPosition);
    }

    // [ECMA] FunctionExpression : 'function' Identifier? '(' FormalParameterList? ')' '{' FunctionBody '}'
    // [ECMA] FunctionBody : SourceElements?
    // [MCJS] FunctionExpression : 'function' Identifier? '(' FormalParameterList ')' '{' SourceElements '}'
    ILeftHandSideExpression VFunctionExpression()
    {
      if (Try(TKeyword("function")))
      {
        Scopes.Push(new Scope(Scope));
        Scope.IsFunction = true;

        // Note that the name of a function expression goes in its own scope, not the parent's.
        var name = AllowedValue(TIdentifier());

        Require(TChar('('));
        var args = RequiredValue(VFormalParameterList());
        Require(TChar(')'));
        Require(TChar('{'));
        var body = RequiredValue(VSourceElements());
        Require(TChar('}'));

        var innerScope = Scopes.Pop();

        return Factory.MakeFunctionExpression(Scope, name, args, body, innerScope);
      }
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [ECMA] FormalParameterList : Identifier (',' Identifier)*
    // [MCJS] FormalParameterList : (Identifier (',' Identifier)*)?
    IIdentifierList VFormalParameterList()
    {
      var identifiers = Factory.MakeIdentifierList();

      while (TryAndAddTo(identifiers, TIdentifier()) &&
             Try(TChar(',')));

      return identifiers;
    }

    #endregion

    #region Syntactic Grammar - Statements

    // [ECMA] Statement : Block | VariableStatement | EmptyStatement | ExpressionStatement | IfStatement
    //                          | IterationStatement | ContinueStatement | BreakStatement | ReturnStatement
    //                          | WithStatement | LabelledStatement | SwitchStatement | ThrowStatement
    //                          | TryStatement | DebuggerStatement
    IStatement VStatement()
    {
      IStatement statement;

      if (Try(statement = VFunctionStatement())    || 
          Try(statement = VBlock())                || 
          Try(statement = VEmptyStatement())       || 
          Try(statement = VVariableStatement())    || 
          Try(statement = VIfStatement())          || 
          Try(statement = VIterationStatement())   || 
          Try(statement = VContinueStatement())    || 
          Try(statement = VBreakStatement())       || 
          Try(statement = VReturnStatement())      || 
          Try(statement = VWithStatement())        || 
          Try(statement = VSwitchStatement())      || 
          Try(statement = VThrowStatement())       || 
          Try(statement = VTryStatement())         || 
          Try(statement = VDebuggerStatement())    ||
          Try(statement = VExpressionStatement()))
        return statement;
      else
        return Failed<IStatement>();
    }

    // [ECMA] Block : '{' StatementList? '}'
    // [MCJS] Block : '{' StatementList '}'
    IStatement VBlock()
    {
      IStatement block;

      if (Try(TChar('{')))
      {
        Require(block = VStatementList());
        Require(TChar('}'));

        return block;
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] StatementList : Statement+
    // [MCJS] StatementList : Statement*
    IStatement VStatementList()
    {
      var atPosition = CurrentPosition();
      var statements = Factory.MakeStatementList();

      while (TryAndAddTo(statements, VStatement()));

      return Factory.MakeBlockStatement(statements, atPosition);
    }

    // [ECMA] VariableStatement : 'var' VariableDeclarationList ';'
    IStatement VVariableStatement()
    {
      if (Try(TKeyword("var")))
      {
        var declarations = RequiredValue(VVariableDeclarationList());
        Require(TSemicolon());

        return Factory.MakeVariableDeclarationStatement(declarations);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] VariableDeclarationList : ( VariableDeclaration ',' )+
    IVariableDeclarationList VVariableDeclarationList()
    {
      var declarations = Factory.MakeVariableDeclarationList();

      while (TryAndAddTo(declarations, VVariableDeclaration()) &&
             TChar(','));

      if (declarations.Count == 0) return Failed<IVariableDeclarationList>();
      else                         return declarations;
    }

    // [ECMA] VariableDeclarationListNoIn : VariableDeclarationNoIn+
    IVariableDeclarationList VVariableDeclarationListNoIn()
    {
      InOperatorAllowed = false;
      var declarations = VVariableDeclarationList();
      InOperatorAllowed = true;

      return declarations;
    }

    // [ECMA] VariableDeclaration : Identifier Initialiser?
    IVariableDeclaration VVariableDeclaration()
    {
      IIdentifier identifier;

      if (Try(identifier = TIdentifier())) return Factory.MakeVariableDeclaration(identifier, VInitialiser());
      else                                 return Failed<IVariableDeclaration>();
    }

    // [ECMA] Initialiser : '=' AssignmentExpression
    IExpression VInitialiser()
    {
      if (Try(TChar('='))) return RequiredValue(VAssignmentExpression());
      else                 return Failed<IExpression>();
    }

    // [ECMA] EmptyStatement : ';'
    IStatement VEmptyStatement()
    {
      return Try(TRealSemicolon()) ? Factory.MakeEmptyStatement(Position) : Failed<IStatement>();
    }

    // [ECMA] ExpressionStatement : [lookahead not '{' or 'function'] Expression ';'
    // [ECMA] LabelledStatement : Identifier ':' Statement
    // [MCJS] ExpressionStatement : Expression ';' | Identifier ':' Statement
    //        For ExpressionStatement we don't worry about lookahead. Instead, we
    //        rely on the fact that we always try FunctionDeclarations before Statements
    //        (which takes care of the 'function' restriction) and Blocks before
    //        ExpressionStatements (which takes care of the '{' restriction). We combine
    //        LabelledStatement and ExpressionStatement to avoid parsing the same identifier
    //        multiple times.
    IStatement VExpressionStatement()
    {
      IExpression expression;

      if (Try(expression = VExpression()))
      {
        IIdentifier id;

        if (Try(TChar(':')) && Require(id = expression as IIdentifier))
          return Factory.MakeLabelStatement(id, RequiredValue(VStatement()));

        Require(TSemicolon(), "Missing semicolon after ExpressionStatement (could indicate nonstandard use of FunctionDeclarations inside blocks)");
        return Factory.MakeExpressionStatement(expression);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] IfStatement : 'if' '(' Expression ')' Statement ('else' Statement)?
    IStatement VIfStatement()
    {
      if (Try(TKeyword("if")))
      {
        Require(TChar('('));
        var expression = RequiredValue(VExpression());
        Require(TChar(')'));
        var thenStatement = RequiredValue(VStatement());
        
        if (Try(TKeyword("else"))) return Factory.MakeIfStatement(expression, thenStatement, RequiredValue(VStatement()));
        else                       return Factory.MakeIfStatement(expression, thenStatement);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] IterationStatement : <every type of iteration statement jumbled together>
    // [MCJS] IterationStatement : DoWhileStatement | WhileStatement | ForStatement | ForEachInStatement
    IStatement VIterationStatement()
    {
      IStatement statement;

      if (Try(statement = VForStatement())       || 
          Try(statement = VWhileStatement())     || 
          Try(statement = VDoWhileStatement()))
        return statement;
      else
        return Failed<IStatement>();
    }

    // [ECMA] DoWhileStatement : 'do' Statement 'while' '(' Expression ')' ';'
    IStatement VDoWhileStatement()
    {
      if (Try(TKeyword("do")))
      {
        var body = RequiredValue(VStatement());
        Require(TKeyword("while"));
        Require(TChar('('));
        var condition = RequiredValue(VExpression());
        Require(TChar(')'));
        Require(TSemicolon());

        return Factory.MakeDoWhileStatement(Scope, condition, body);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] WhileStatement : 'while' '(' Expression ')' Statement
    IStatement VWhileStatement()
    {
      if (Try(TKeyword("while")))
      {
        Require(TChar('('));
        var condition = RequiredValue(VExpression());
        Require(TChar(')'));

        return Factory.MakeWhileStatement(Scope, condition, RequiredValue(VStatement()));
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] ForStatement : 'for' '(' ExpressionNoIn? ';' Expression? ';' Expression? ')' Statement
    //                     | 'for' '(' 'var' VariableDeclarationListNoIn ';' Expression? ';' Expression? ')' Statement
    //                     | 'for' '(' LeftHandSideExpression 'in' Expression ')' Statement
    //                     | 'for' '(' 'var' VariableDeclarationListNoIn 'in' Expression ')' Statement
    // [MCJS] ForStatement : 'for' '(' ( 'var' ( ';' ForWithDeclarationRest | ForEachWithDeclarationRest )
    //                                 | ';' ExpressionNoIn? ForRest
    //                                 | LeftHandSideExpression ForEachRest )
    IStatement VForStatement()
    {
      if (Try(TKeyword("for")))
      {
        Require(TChar('('));

        if (Try(TKeyword("var")))
        {
          // Variants that start with a variable declaration.
          var declarations = RequiredValue(VVariableDeclarationListNoIn());

          if (Try(TRealSemicolon())) return VForWithDeclarationRest(declarations);
          else                       return VForEachWithDeclarationRest(declarations);
        }
        else
        {
          // Variants that start with an expression.
          var initialization = VExpressionNoIn();

          if (Try(TRealSemicolon())) return VForRest(initialization);
          else                       return VForEachRest(initialization as ILeftHandSideExpression);
        }
      }
      else
        return Failed<IStatement>();
    }

    // [MCJS] ForWithDeclarationRest : Expression? ';' Expression? ')' Statement
    IStatement VForWithDeclarationRest(IVariableDeclarationList declarations)
    {
      IExpression condition;
      IExpression increment;

      if (Allow(condition = VExpression()) &&
          Require(TRealSemicolon())        &&
          Allow(increment = VExpression()) &&
          Require(TChar(')')))
        return Factory.MakeForStatement(Scope,
                                        Factory.MakeVariableDeclarationStatement(declarations),
                                        condition,
                                        increment,
                                        RequiredValue(VStatement()));
      else
        return Failed<IStatement>();
    }

    // [MCJS] ForEachWithDeclarationRest : 'in' Expression ')' Statement
    IStatement VForEachWithDeclarationRest(IVariableDeclarationList declarations)
    {
      IExpression expression;

      if (Require(VOperatorIn())              &&
          Require(expression = VExpression()) &&
          Require(TChar(')')))
        return Factory.MakeForEachInStatement(Scope,
                                              Factory.MakeVariableDeclarationStatement(declarations),
                                              expression,
                                              RequiredValue(VStatement()));
      else
        return Failed<IStatement>();
    }

    // [MCJS] ForRest : Expression? ';' Expression? ')' Statement
    IStatement VForRest(IExpression initialization)
    {
      IExpression condition;
      IExpression increment;

      if (Allow(condition = VExpression()) &&
          Require(TRealSemicolon())        &&
          Allow(increment = VExpression()) &&
          Require(TChar(')')))
        return Factory.MakeForStatement(Scope,
                                        initialization,
                                        condition,
                                        increment,
                                        RequiredValue(VStatement()));
      else
        return Failed<IStatement>();
    }

    // [MCJS] ForEachRest : LeftHandSideExpression 'in' Expression ')' Statement
    IStatement VForEachRest(ILeftHandSideExpression initialization)
    {
      IExpression expression;

      if (Require(initialization)             &&
          Try(VOperatorIn())                  &&
          Require(expression = VExpression()) &&
          Require(TChar(')')))
        return Factory.MakeForEachInStatement(Scope,
                                              Factory.MakeExpressionStatement(initialization),
                                              expression,
                                              RequiredValue(VStatement()));
      else
        return Failed<IStatement>();
    }

    // [ECMA] ContinueStatement : 'continue' ([no LineTerminator here] Identifier)? ';'
    IStatement VContinueStatement()
    {
      var atPosition = CurrentPosition();

      if (Try(TKeyword("continue")))
      {
        IIdentifier identifier;
        if (!LineBreakInLastWhiteSpace) identifier = AllowedValue(TIdentifier());
        else                            identifier = null;

        Require(TSemicolon());

        return Factory.MakeContinueStatement(identifier, atPosition);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] BreakStatement : 'break' ([no LineTerminator here] Identifier)? ';'
    IStatement VBreakStatement()
    {
      var atPosition = CurrentPosition();

      if (Try(TKeyword("break")))
      {
        IIdentifier identifier;
        if (!LineBreakInLastWhiteSpace) identifier = AllowedValue(TIdentifier());
        else                            identifier = null;

        Require(TSemicolon());

        return Factory.MakeBreakStatement(identifier, atPosition);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] ReturnStatement : 'return' ([no LineTerminator here] Expression)? ';'
    IStatement VReturnStatement()
    {
      var atPosition = CurrentPosition();

      if (Try(TKeyword("return")))
      {
        IExpression expression;
        if (!LineBreakInLastWhiteSpace) expression = AllowedValue(VExpression());
        else                            expression = null;

        Require(TSemicolon());

        return Factory.MakeReturnStatement(Scope, expression, atPosition);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] WithStatement : 'with' '(' Expression ')' Statement
    IStatement VWithStatement()
    {
      if (Try(TKeyword("with")))
      {
        Require(TChar('('));
        var expression = RequiredValue(VExpression());
        Require(TChar(')'));

        return Factory.MakeWithStatement(expression, RequiredValue(VStatement()));
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] SwitchStatement : 'switch' '(' Expression ')' CaseBlock
    //        CaseBlock : '{' CaseClauses? '}'
    //                  | '{' CaseClauses? DefaultClause CaseClauses? '}'
    // [MCJS] SwitchStatement : 'switch' '(' Expression ')' '{' CaseClauses '}'
    IStatement VSwitchStatement()
    {
      if (Try(TKeyword("switch")))
      {
        Require(TChar('('));
        var expression = RequiredValue(VExpression());
        Require(TChar(')'));
        Require(TChar('{'));
        var caseClauses = RequiredValue(VCaseClauses());
        Require(TChar('}'));

        return Factory.MakeSwitchStatement(Scope, expression, caseClauses);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] CaseClauses : CaseClause+
    // [MCJS] CaseClauses : CaseClause*
    ICaseClauseList VCaseClauses()
    {
      var caseClauses = Factory.MakeCaseClauseList();

      while (TryAndAddTo(caseClauses, VCaseClause()) ||
             TryAndAddTo(caseClauses, VDefaultClause()));

      return caseClauses;
    }

    // [ECMA] CaseClause : 'case' Expression ':' StatementList?
    ICaseClause VCaseClause()
    {
      if (Try(TKeyword("case")))
      {
        var expression = RequiredValue(VExpression());
        Require(TChar(':'));
        var body = AllowedValue(VStatementList());

        return Factory.MakeCaseClause(expression, body);
      }
      else
        return Failed<ICaseClause>();
    }

    // [ECMA] DefaultClause : 'default' ':' StatementList?
    ICaseClause VDefaultClause()
    {
      if (Try(TKeyword("default")))
      {
        Require(TChar(':'));
        var body = AllowedValue(VStatementList());

        return Factory.MakeDefaultClause(body);
      }
      else
        return Failed<ICaseClause>();
    }

    // [ECMA] ThrowStatement : 'throw' [no LineTerminator here] Expression ';'
    IStatement VThrowStatement()
    {
      if (Try(TKeyword("throw")))
      {
        Require(!LineBreakInLastWhiteSpace);
        var expression = RequiredValue(VExpression());
        Require(TSemicolon());

        return Factory.MakeThrowStatement(expression);
      }
      else
        return Failed<IStatement>();
    }

    // [ECMA] TryStatement : 'try' Block (Catch Finally? | Finally)
    IStatement VTryStatement()
    {
      if (Try(TKeyword("try")))
      {
        var block = RequiredValue(VBlock());

        ICatch cc;
        if (Try(cc = VCatch())) return Factory.MakeTryStatement(block, cc, AllowedValue(VFinally()));
        else                    return Factory.MakeTryStatement(block, null, RequiredValue(VFinally()));
      }
      else
        return Failed<TryStatement>();
    }

    // [ECMA] Catch : 'catch' '(' Identifier ')' Block
    ICatch VCatch()
    {
      if (Try(TKeyword("catch")))
      {
        //we need to make a separate scope for the catch variable. 
        Scopes.Push(new Scope(Scope));

        Require(TChar('('));
        var identifier = RequiredValue(TIdentifier());
        Require(TChar(')'));

        var body = RequiredValue(VBlock());

        var innerScope = Scopes.Pop();

        return Factory.MakeCatchClause(identifier, body, innerScope);
      }
      else
        return Failed<ICatch>();
    }

    // [ECMA] Finally : 'finally' Block
    IFinally VFinally()
    {
      if (Try(TKeyword("finally"))) return Factory.MakeFinallyClause(RequiredValue(VBlock()));
      else                          return Failed<IFinally>();
    }

    // [ECMA] DebuggerStatement : 'debugger' ';'
    IStatement VDebuggerStatement()
    {
      var atPosition = CurrentPosition();
      if (Try(TKeyword("debugger")) && Require(TSemicolon())) return Factory.MakeDebuggerStatement(atPosition);
      else                                                    return Failed<IStatement>();
    }

    #endregion

    #region Syntactic Grammar - Expressions

    // [ECMA] PrimaryExpression : this | Identifier | Literal | ArrayLiteral | ObjectLiteral
    //                          | '(' Expression ')'
    // [MCJS] PrimaryExpression : NullLiteral | BooleanLiteral | NumericLiteral | StringLiteral
    //                          | RegularExpressionLiteral | Identifier | ArrayLiteral | ObjectLiteral
    //                          | ParenExpression
    ILeftHandSideExpression VPrimaryExpression()
    {
      ILeftHandSideExpression expression;

      if (Try(expression = TThisLiteral())              ||
          Try(expression = TNullLiteral())              ||
          Try(expression = TBooleanLiteral())           ||
          Try(expression = TNumericLiteral())           ||
          Try(expression = TStringLiteral())            ||
          Try(expression = TRegularExpressionLiteral()) ||
          Try(expression = TIdentifier())               ||
          Try(expression = VArrayLiteral())             ||
          Try(expression = VObjectLiteral())            ||
          Try(expression = VParenExpression()))
        return expression;
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [ECMA] ArrayLiteral : '[' Elision? ']' | '[' ElementList ']' | '[' ElementList ',' Elision? ']'
    // [MCJS] ArrayLiteral : '[' ElementList Elision ']'
    ILiteral VArrayLiteral()
    {
      var atPosition = CurrentPosition();

      if (Try(TChar('[')))
      {
        var elementList = RequiredValue(VElementList());
        Try(VElision());
        Require(TChar(']'));

        return Factory.MakeArrayLiteral(elementList, atPosition);
      }
      else
        return Failed<ILiteral>();
    }

    // [ECMA] ElementList : Elision? AssignmentExpression | ElementList ',' Elision? AssignmentExpression
    // [MCJS] ElementList : (Elision AssignmentExpression (',' Elision AssignmentExpression)*)?
    IExpressionList VElementList()
    {
      var expressions = Factory.MakeExpressionList();

      while (Try(VElision())                                   &&
             TryAndAddTo(expressions, VAssignmentExpression()) &&
             Try(TChar(',')));

      return expressions;
    }

    // [ECMA] Elision : ','+
    // [MCJS] Elision : ','*
    bool VElision()
    {
      while (Try(TChar(',')));

      return true;
    }

    // [ECMA] ObjectLiteral : '{' (PropertyNameAndValueList ',')? '}'
    // [MCJS] ObjectLiteral : '{' PropertyNameAndValueList '}'
    ILiteral VObjectLiteral()
    {
      var atPosition = CurrentPosition();

      if (Try(TChar('{')))
      {
        var properties = RequiredValue(VPropertyNameAndValueList());
        Require(TChar('}'));

        return Factory.MakeObjectLiteral(properties, atPosition);
      }
      else
        return Failed<ILiteral>();
    }

    // [ECMA] PropertyNameAndValueList : PropertyAssignment (',' PropertyAssignment)*
    // [MCJS] PropertyNameAndValueList : PropertyAssignment?
    //                                 | PropertyAssignment ',' PropertyNameAndValueList?
    IPropertyAssignmentList VPropertyNameAndValueList()
    {
      var properties = Factory.MakePropertyAssignmentList();

      while (TryAndAddTo(properties, VPropertyAssignment()) &&
             Try(TChar(','))); 

      return properties;
    }

    // [ECMA] PropertyAssignment : PropertyName ':' AssignmentExpression
    //                           | 'get' PropertyName '(' ')' '{' FunctionBody '}'
    //                           | 'set' PropertyName '(' PropertySetParameterList ')'
    //                             '{' FunctionBody '}'
    // [MCJS] PropertyAssignment : GetPropertyAssignment | SetPropertyAssignment | SimplePropertyAssignment
    IPropertyAssignment VPropertyAssignment()
    {
      IPropertyAssignment property;

      if (Try(property = VGetPropertyAssignment())     ||
          Try(property = VSetPropertyAssignment())     ||
          Try(property = VSimplePropertyAssignment()))
        return property;
      else
        return Failed<IPropertyAssignment>();
    }

    // [MCJS] GetPropertyAssignment : 'get' PropertyName '(' ')' '{' SourceElements '}'
    IPropertyAssignment VGetPropertyAssignment()
    {
      var atPosition = SavePosition();

      ILiteral name;

      if (Try(TKeyword("get"))         &&
          Try(name = VPropertyName()))
      {
        Scopes.Push(new Scope(Scope));
        Scope.IsFunction = true;

        Require(TChar('('));
        Require(TChar(')'));
        Require(TChar('{'));
        var body = RequiredValue(VSourceElements());
        Require(TChar('}'));

        var innerScope = Scopes.Pop();

        return Factory.MakePropertyGetAssignment(Scope, name, body, innerScope);
      }
      else
        return Failed<IPropertyAssignment>(atPosition);
    }

    // [MCJS] SetPropertyAssignment : 'set' PropertyName '(' Identifier ')' '{' SourceElements '}'
    IPropertyAssignment VSetPropertyAssignment()
    {
      var atPosition = SavePosition();

      ILiteral name;

      if (Try(TKeyword("set"))         &&
          Try(name = VPropertyName()))
      {
        Scopes.Push(new Scope(Scope));
        Scope.IsFunction = true;

        Require(TChar('('));
        var paramName = RequiredValue(TIdentifier());
        Require(TChar(')'));
        Require(TChar('{'));
        var body = RequiredValue(VSourceElements());
        Require(TChar('}'));

        var innerScope = Scopes.Pop();

        return Factory.MakePropertySetAssignment(Scope, name, paramName, body, innerScope);
      }
      else
        return Failed<IPropertyAssignment>(atPosition);
    }

    // [MCJS] SimplePropertyAssignment : PropertyName ':' AssignmentExpression
    IPropertyAssignment VSimplePropertyAssignment()
    {
      ILiteral name;

      if (Try(name = VPropertyName()))
      {
        Require(TChar(':'));
        var expression = RequiredValue(VAssignmentExpression());

        return Factory.MakePropertyAssignment(name, expression);
      }
      else
        return Failed<IPropertyAssignment>();
    }

    // [ECMA] PropertyName : IdentifierName | StringLiteral | NumericLiteral
    ILiteral VPropertyName()
    {
      ILiteral name;

      if (Try(name = TIdentifierNameLiteral())  ||
          Try(name = TStringLiteral())          ||
          Try(name = TNumericLiteral()))
        return name;
      else
        return Failed<ILiteral>();
    }

    // [MCJS] ParenExpression : '(' Expression ')'
    ILeftHandSideExpression VParenExpression()
    {
      if (Try(TChar('(')))
      {
        var expression = RequiredValue(VExpression());
        Require(TChar(')'));

        return Factory.MakeParenExpression(expression);
      }
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [ECMA] MemberExpression : PrimaryExpression | FunctionExpression | MemberExpression '[' Expression ']'
    //                         | MemberExpression '.' IdentifierName | 'new' MemberExpression Arguments
    // [MCJS] MemberExpressionInternal : PrimaryExpression | FunctionExpression | NewMemberExpression
    ILeftHandSideExpression VMemberExpressionInternal()
    {
      ILeftHandSideExpression expression;

      if (Try(expression = VPrimaryExpression())    ||
          Try(expression = VFunctionExpression())   ||
          Try(expression = VNewMemberExpression()))
        return expression;
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [MCJS] NewMemberExpression : 'new' MemberExpression Arguments
    ILeftHandSideExpression VNewMemberExpression()
    {
      if (Try(TKeyword("new")))
      {
        var expression = RequiredValue(VMemberExpression());

        IExpressionList arguments;

        if (UnaryNewInLastExpression)
        {
          // Once we've matched a unary 'new' we can only match unary 'new's, because
          // in the original grammar unary 'new' is at a lower precedence.
          return Factory.MakeNewExpression(Scope, expression);
        }
        else if (Try(arguments = VArguments()))
          return Factory.MakeNewExpression(Scope, expression, arguments);
        else
        {
          // We've matched our first unary 'new'. Any 'new' operators still on the stack
          // will be unable to match Arguments - that is, they must all be unary now.
          UnaryNewInLastExpression = true;
          return Factory.MakeNewExpression(Scope, expression);
        }
      }
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [MCJS] MemberExpression : MemberExpressionInternal (ArrayIndexer| PropertyIndexer)*
    ILeftHandSideExpression VMemberExpression()
    {
      ILeftHandSideExpression expression;

      if (Try(expression = VMemberExpressionInternal()))
      {
        ILeftHandSideExpression indexerExpression;

        while (Try(indexerExpression = VArrayIndexer(expression))     ||
               Try(indexerExpression = VPropertyIndexer(expression)))
          expression = indexerExpression;

        return expression;
      }
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [MCJS] ArrayIndexer: '[' Expression ']'
    ILeftHandSideExpression VArrayIndexer(IExpression parent)
    {
      if (Try(TChar('[')))
      {
        var expression = RequiredValue(VExpression());
        Require(TChar(']'));

        return Factory.MakeIndexerExpression(parent, expression);
      }
      else
        return Failed<ILeftHandSideExpression>();
    }

    // [MCJS] PropertyIndexer : '.' IdentifierName
    ILeftHandSideExpression VPropertyIndexer(IExpression parent)
    {
      if (Try(TChar('.'))) return Factory.MakeIndexerExpression(parent, RequiredValue(TIdentifierNameLiteral()));
      else                 return Failed<ILeftHandSideExpression>();
    }

    // [ECMA] LeftHandSideExpression : NewExpression | CallExpression
    // [ECMA] CallExpression : MemberExpression Arguments | CallExpression Arguments
    //                       | CallExpression '[' Expression ']' | CallExpression '.' IdentifierName
    // [MCJS] CallExpression : NewExpression
    //                       | (NewExpression but no unary 'new') Arguments (Invocation | ArrayIndexer| PropertyIndexer)*
    // We merge NewExpression and CallExpression to prevent unnecessary and expensive backtracking.
    ILeftHandSideExpression VCallExpression()
    {
      Debug.Assert(!UnaryNewInLastExpression, "Shouldn't ever encounter nesting of VCallExpression() calls.");

      ILeftHandSideExpression expression;

      if (Try(expression = VMemberExpression()))
      {
        IExpressionList arguments;

        if (UnaryNewInLastExpression)
        {
          // This can't be a CallExpression with arguments since you can't use a unary 'new' inside one.
          UnaryNewInLastExpression = false;
        }
        else if (Try(arguments = VArguments()))
        {
          // This is a non-trivial CallExpression (one which isn't simply a NewExpression).
          expression = Factory.MakeCallExpression(Scope, expression, arguments);

          ILeftHandSideExpression callExpression;
          while (Try(callExpression = VArrayIndexer(expression))     ||
                 Try(callExpression = VPropertyIndexer(expression))  ||
                 Try(callExpression = VInvocation(expression)))
            expression = callExpression;
        }

        return expression;
      }
      else
      {
        UnaryNewInLastExpression = false;
        return Failed<ILeftHandSideExpression>();
      }
    }

    // [MCJS] Invocation : Arguments
    ILeftHandSideExpression VInvocation(IExpression parent)
    {
      IExpressionList args;

      if (Try(args = VArguments())) return Factory.MakeCallExpression(Scope, parent, args);
      else                          return Failed<ILeftHandSideExpression>();
    }

    // [ECMA] Arguments : '(' ArgumentList? ')'
    // [MCJS] Arguments : '(' ArgumentList ')'
    IExpressionList VArguments()
    {
      if (Try(TChar('(')))
      {
        var args = RequiredValue(VArgumentList());
        Require(TChar(')'));

        return args;
      }
      else
        return Failed<IExpressionList>();
    }

    // [ECMA] ArgumentList : AssignmentExpression (',' Identifier)*
    // [MCJS] ArgumentList : (AssignmentExpression (',' Identifier)*)?
    IExpressionList VArgumentList()
    {
      var args = Factory.MakeExpressionList();

      while (TryAndAddTo(args, VAssignmentExpression()) &&
             Try(TChar(',')));

      return args;
    }

    // [ECMA] PostfixExpression : LeftHandSideExpression ([no LineTerminator here] ('++' | '--'))?
    IExpression VPostfixExpression()
    {
      ILeftHandSideExpression expression;

      if (Try(expression = VCallExpression()))
      {
        if (!LineBreakInLastWhiteSpace)
        {
          if (Try(TChars("++")))      return Factory.MakePostfixIncrement(expression);
          else if (Try(TChars("--"))) return Factory.MakePostfixDecrement(expression);
        }

        return expression;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] UnaryExpression : PostfixExpression | 'delete' UnaryExpression | 'void' UnaryExpression
    //                        | 'typeof' UnaryExpression | '++' UnaryExpression | '--' UnaryExpression
    //                        | '+' UnaryExpression | '-' UnaryExpression | '~' UnaryExpression
    //                        | '!' UnaryExpression
    IExpression VUnaryExpression()
    {
      if (Try(TKeyword("delete")))      return Factory.MakeDeleteExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TKeyword("void")))   return Factory.MakeVoidExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TKeyword("typeof"))) return Factory.MakeTypeofExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TChars("++")))       return Factory.MakePrefixIncrement(RequiredValue(VUnaryExpression()));
      else if (Try(TChars("--")))       return Factory.MakePrefixDecrement(RequiredValue(VUnaryExpression()));
      else if (Try(TChar('+')))         return Factory.MakePositiveExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TChar('-')))         return Factory.MakeNegativeExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TChar('~')))         return Factory.MakeBitwiseNotExpression(RequiredValue(VUnaryExpression()));
      else if (Try(TChar('!')))         return Factory.MakeLogicalNotExpression(RequiredValue(VUnaryExpression()));
      else                              return VPostfixExpression();
    }

    // [ECMA] MultiplicativeExpression : UnaryExpression | MultiplicativeExpression '*' UnaryExpression
    //                                 | MultiplicativeExpression '/' UnaryExpression
    //                                 | MultiplicativeExpression '%' UnaryExpression
    // [MCJS] MultiplicativeExpression : UnaryExpression ('*' UnaryExpression | '/' UnaryExpression | '%' UnaryExpression)*
    IExpression VMultiplicativeExpression()
    {
      IExpression expr;

      if (Try(expr = VUnaryExpression()))
      {
        while ((Try(TCharsWithout("*", "=")) && Do(expr = Factory.MakeMultiplyExpression(expr, RequiredValue(VUnaryExpression()))))    ||
               (Try(TCharsWithout("/", "=")) && Do(expr = Factory.MakeDivideExpression(expr, RequiredValue(VUnaryExpression()))))      ||
               (Try(TCharsWithout("%", "=")) && Do(expr = Factory.MakeRemainderExpression(expr, RequiredValue(VUnaryExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] AdditiveExpression : MultiplicativeExpression | AdditiveExpression '+' MultiplicativeExpression
    //                           | AdditiveExpression '-' MultiplicativeExpression
    // [MCJS] AdditiveExpression : MultiplicativeExpression ('+' MultiplicativeExpression | '-' MultiplicativeExpression)*
    IExpression VAdditiveExpression()
    {
      IExpression expr;

      if (Try(expr = VMultiplicativeExpression()))
      {
        while ((Try(TCharsWithout("+", "=+")) && Do(expr = Factory.MakeAdditionExpression(expr, RequiredValue(VMultiplicativeExpression()))))      ||
               (Try(TCharsWithout("-", "=-")) && Do(expr = Factory.MakeSubtractionExpression(expr, RequiredValue(VMultiplicativeExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] ShiftExpression : AdditiveExpression | ShiftExpression '<<' AdditiveExpression
    //                        | ShiftExpression '>>' AdditiveExpression | ShiftExpression '>>>' AdditiveExpression
    // [MCJS] ShiftExpression : AdditiveExpression ( '<<' AdditiveExpression | '>>' AdditiveExpression
    //                                             | '>>>' AdditiveExpression )*
    IExpression VShiftExpression()
    {
      IExpression expr;

      if (Try(expr = VAdditiveExpression()))
      {
        while ((Try(TCharsWithout(">>>", "=")) && Do(expr = Factory.MakeUnsignedRightShiftExpression(expr, RequiredValue(VAdditiveExpression())))) ||
               (Try(TCharsWithout(">>", "=>")) && Do(expr = Factory.MakeRightShiftExpression(expr, RequiredValue(VAdditiveExpression()))))         ||
               (Try(TCharsWithout("<<", "=<")) && Do(expr = Factory.MakeLeftShiftExpression(expr, RequiredValue(VAdditiveExpression()))))); 

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] RelationalExpression : ShiftExpression | RelationalExpression '<' ShiftExpression
    //                             | RelationalExpression '>' ShiftExpression | RelationalExpression '<=' ShiftExpression
    //                             | RelationalExpression '>=' ShiftExpression | RelationalExpression 'in' ShiftExpression
    //                             | RelationalExpression 'instanceof' ShiftExpression
    // [MCJS] RelationalExpression : ShiftExpression ( '<=' ShiftExpression | '>=' ShiftExpression
    //                                               | '<' ShiftExpression | '>' ShiftExpression
    //                                               | 'instanceof' ShiftExpression | 'in' ShiftExpression )*
    IExpression VRelationalExpression()
    {
      IExpression expr;

      if (Try(expr = VShiftExpression()))
      {
        while ((Try(TChars("<=")) && Do(expr = Factory.MakeLesserOrEqualExpression(expr, RequiredValue(VShiftExpression()))))        ||
               (Try(TChars(">=")) && Do(expr = Factory.MakeGreaterOrEqualExpression(expr, RequiredValue(VShiftExpression()))))       ||
               (Try(TCharsWithout("<", "<")) && Do(expr = Factory.MakeLesserExpression(expr, RequiredValue(VShiftExpression()))))    ||
               (Try(TCharsWithout(">", ">")) && Do(expr = Factory.MakeGreaterExpression(expr, RequiredValue(VShiftExpression()))))   ||
               (Try(TKeyword("instanceof")) && Do(expr = Factory.MakeInstanceOfExpression(expr, RequiredValue(VShiftExpression())))) ||
               (Try(VOperatorIn()) && Do(expr = Factory.MakeInExpression(expr, RequiredValue(VShiftExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] EqualityExpression : RelationalExpression | EqualityExpression == RelationalExpression
    //                           | EqualityExpression != RelationalExpression
    //                           | EqualityExpression === RelationalExpression
    //                           | EqualityExpression !== RelationalExpression
    // [MCJS] EqualityExpression : RelationalExpression ( '===' RelationalExpression | '!==' RelationalExpression
    //                                                  | '==' RelationalExpression | '!=' RelationalExpression )*
    IExpression VEqualityExpression()
    {
      IExpression expr;

      if (Try(expr = VRelationalExpression()))
      {
        while ((Try(TChars("===")) && Do(expr = Factory.MakeSameExpression(expr, RequiredValue(VRelationalExpression()))))      ||
               (Try(TChars("!==")) && Do(expr = Factory.MakeNotSameExpression(expr, RequiredValue(VRelationalExpression()))))   ||
               (Try(TChars("==")) && Do(expr = Factory.MakeEqualExpression(expr, RequiredValue(VRelationalExpression()))))      ||
               (Try(TChars("!=")) && Do(expr = Factory.MakeNotEqualExpression(expr, RequiredValue(VRelationalExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] BitwiseANDExpression : EqualityExpression | BitwiseANDExpression '&' EqualityExpression
    // [MCJS] BitwiseANDExpression : EqualityExpression ('&' EqualityExpression)*
    IExpression VBitwiseANDExpression()
    {
      IExpression expr;

      if (Try(expr = VEqualityExpression()))
      {
        while ((Try(TCharsWithout("&", "&=")) && Do(expr = Factory.MakeBitwiseAndExpression(expr, RequiredValue(VEqualityExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }
 
    // [ECMA] BitwiseXORExpression : BitwiseANDExpression | BitwiseXORExpression '^' BitwiseANDExpression
    // [MCJS] BitwiseXORExpression : BitwiseANDExpression ('^' BitwiseANDExpression)*
    IExpression VBitwiseXORExpression()
    {
      IExpression expr;

      if (Try(expr = VBitwiseANDExpression()))
      {
        while ((Try(TCharsWithout("^", "=")) && Do(expr = Factory.MakeBitwiseXorExpression(expr, RequiredValue(VBitwiseANDExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] BitwiseORExpression : BitwiseXORExpression | BitwiseORExpression '|' BitwiseXORExpression
    // [MCJS] BitwiseORExpression : BitwiseXORExpression ('|' BitwiseXORExpression)*
    IExpression VBitwiseORExpression()
    {
      IExpression expr;

      if (Try(expr = VBitwiseXORExpression()))
      {
        while ((Try(TCharsWithout("|", "|=")) && Do(expr = Factory.MakeBitwiseOrExpression(expr, RequiredValue(VBitwiseXORExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] LogicalANDExpression : BitwiseORExpression | LogicalANDExpression '&&' BitwiseORExpression
    // [MCJS] LogicalANDExpression : BitwiseORExpression ('&&' BitwiseORExpression)*
    IExpression VLogicalANDExpression()
    {
      IExpression expr;

      if (Try(expr = VBitwiseORExpression()))
      {
        while ((Try(TChars("&&")) && Do(expr = Factory.MakeLogicalAndExpression(expr, RequiredValue(VBitwiseORExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] LogicalORExpression : LogicalANDExpression | LogicalORExpression '||' LogicalANDExpression
    // [MCJS] LogicalORExpression : LogicalANDExpression ('||' LogicalANDExpression)*
    IExpression VLogicalORExpression()
    {
      IExpression expr;

      if (Try(expr = VLogicalANDExpression()))
      {
        while ((Try(TChars("||")) && Do(expr = Factory.MakeLogicalOrExpression(expr, RequiredValue(VLogicalANDExpression())))));

        return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] ConditionalExpression : LogicalORExpression | LogicalORExpression '?' AssignmentExpression
    //                                                                          ':' AssignmentExpression
    // [MCJS] ConditionalExpression : LogicalORExpression ConditionalExpressionRest?
    IExpression VConditionalExpression()
    {
      IExpression expr;

      if (Try(expr = VLogicalORExpression()))
      {
        IExpression conditional;

        if (Try(conditional = VConditionalExpressionRest(expr))) return conditional;
        else                                                     return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [MCJS] ConditionalExpressionRest : '?' AssignmentExpression ':' AssignmentExpression
    IExpression VConditionalExpressionRest(IExpression condition)
    {
      if (Try(TChar('?')))
      {
        var thenExpression = RequiredValue(VAssignmentExpression());
        Require(TChar(':'));
        var elseExpression = RequiredValue(VAssignmentExpression());

        return Factory.MakeTernaryExpression(condition, thenExpression, elseExpression);
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] AssignmentExpression : ConditionalExpression
    //                             | LeftHandSideExpression = AssignmentExpression
    //                             | LeftHandSideExpression AssignmentOperator AssignmentExpression
    // [MCJS] AssignmentExpression : ConditionalExpression | LeftHandSideExpression AssignmentOperator AssignmentExpression
    IExpression VAssignmentExpression()
    {
      IExpression left;

      if (Try(left = VConditionalExpression()))
      {
        AssignmentOperator? op;
        IExpression right;

        if (left is ILeftHandSideExpression           &&
            Try(op = VAssignmentOperator())           &&
            Require(right = VAssignmentExpression()))
          return Factory.MakeAssignmentExpression(left as ILeftHandSideExpression, right, op.Value);
        else
          return left;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] AssignmentOperator : '*=' | '/=' | '%=' | '+=' | '-=' | '<<=' | '>>=' | '>>>=' | '&=' | '^=' | '|='
    // [MCJS] AssignmentOperator : '=' | '*=' | '/=' | '%=' | '+=' | '-=' | '<<=' | '>>=' | '>>>=' | '&=' | '^=' | '|='
    AssignmentOperator? VAssignmentOperator()
    {
      if (Try(TChars("=")))         return AssignmentOperator.Equal;
      else if (Try(TChars("*=")))   return AssignmentOperator.Multiply;
      else if (Try(TChars("/=")))   return AssignmentOperator.Divide;
      else if (Try(TChars("%=")))   return AssignmentOperator.Remainder;
      else if (Try(TChars("+=")))   return AssignmentOperator.Addition;
      else if (Try(TChars("-=")))   return AssignmentOperator.Subtraction;
      else if (Try(TChars("<<=")))  return AssignmentOperator.LeftShift;
      else if (Try(TChars(">>=")))  return AssignmentOperator.RightShift;
      else if (Try(TChars(">>>="))) return AssignmentOperator.UnsignedRightShift;
      else if (Try(TChars("&=")))   return AssignmentOperator.BitwiseAnd;
      else if (Try(TChars("^=")))   return AssignmentOperator.BitwiseXor;
      else if (Try(TChars("|=")))   return AssignmentOperator.BitwiseOr;
      else                          return Failed<AssignmentOperator?>();
    }

    // [ECMA] Expression : AssignmentExpression | Expression ',' AssignmentExpression
    // [MCJS] Expression : AssignmentExpression (',' AssignmentExpression)*
    IExpression VExpression()
    {
      IExpression expr;

      if (Try(expr = VAssignmentExpression()))
      {
        if (Try(TChar(',')))
        {
          var exprs = Factory.MakeExpressionList();
          exprs.Add(expr);

          while (RequireAndAddTo(exprs, VAssignmentExpression()) &&
                 Try(TChar(',')));

          return Factory.MakeCommaOperatorExpression(exprs);
        }
        else
          return expr;
      }
      else
        return Failed<IExpression>();
    }

    // [ECMA] ExpressionNoIn : AssignmentExpressionNoIn | ExpressionNoIn ',' AssignmentExpressionNoIn
    // This is the same as Expression, but using 'NoIn' variants of all nonterminals. We don't use
    // NoIn variants, so we can simply use Expression directly after setting the appropriate state
    // for InOperatorAllowed.
    IExpression VExpressionNoIn()
    {
      Debug.Assert(InOperatorAllowed, "Shouldn't ever encounter nesting of VExpressionNoIn() calls.");

      InOperatorAllowed = false;
      var expression = VExpression();
      InOperatorAllowed = true;

      return expression;
    }

    // [MCJS] OperatorIn : 'in'
    // Matches the 'in' operator 'in'. Always fails in certain circumstances
    // controlled by the InOperatorAllowed property.
    bool VOperatorIn()
    {
      if (!InOperatorAllowed) return Failed<bool>();
      else                    return Try(TKeyword("in"));
    }

    // [ECMA] StringNumericLiteral : StrWhiteSpaceChar* StrNumericLiteral StrWhiteSpaceChar*
    //        StrWhiteSpaceChar : WhiteSpace | LineTerminator
    ILiteral VStringNumericLiteral()
    {
      ILiteral number;

      // String numeric literals are always parsed within a strict scope.
      var outerScope = (Scopes.Count > 0) ? Scope : null;
      Scopes.Push(new Scope(outerScope));
      Scope.IsStrict = true;
      CommentsAllowed = false;

      Allow(TWhiteSpace());

      // Try to match a string numeric literal; if we fail, evaluate to +0.
      if (!Try(number = TStrNumericLiteral())) number = Factory.MakeIntLiteral(0, Position);
       
      // If we haven't yet consumed all the input, evaluate to NaN.
      if (!Try(TEOF()))                        number = Factory.MakeDoubleLiteral(Double.NaN, Position);

      Scopes.Pop();

      return number;
    }

    // [MCJS] StringRegularExpressionLiteral : TRegexBody
    string VStringRegularExpressionLiteral()
    {
      string regex;

      var outerScope = (Scopes.Count > 0) ? Scope : null;
      Scopes.Push(new Scope(outerScope));
      regex = RequiredValue(TRegexBody());
      Scopes.Pop();

      return regex;
    }

    #endregion

    #region Lexical Grammar

    // [MCJS] ThisLiteral : 'this'
    ILiteral TThisLiteral()
    {
      var atPosition = CurrentPosition();
      if (Try(TKeyword("this"))) return Factory.MakeThisLiteral(Scope, atPosition);
      else                       return Failed<ILiteral>();
    }

    // [ECMA] NullLiteral : 'null'
    ILiteral TNullLiteral()
    {
      var atPosition = CurrentPosition();
      if (Try(TKeyword("null"))) return Factory.MakeNullLiteral(Scope, atPosition);
      else                       return Failed<ILiteral>();
    }

    // [ECMA] BooleanLiteral : 'true' | 'false'
    ILiteral TBooleanLiteral()
    {
      var atPosition = CurrentPosition();
      if (Try(TKeyword("true")))       return Factory.MakeBooleanLiteral(true, atPosition);
      else if (Try(TKeyword("false"))) return Factory.MakeBooleanLiteral(false, atPosition);
      else                             return Failed<ILiteral>();
    }

    // [ECMA] NumericLiteral : DecimalLiteral | HexIntegerLiteral
    ILiteral TNumericLiteral()
    {
      ILiteral number;

      if (Try(TEOF()))
        return Failed<ILiteral>();
      else if (Script[Position] == '0') 
        number = (Try(RRawChars("0x")) || Try(RRawChars("0X"))) ? RequiredValue(RHexIntegerLiteral(Position))
                                                                : RequiredValue(RDecimalOrOctalLiteral(Position));
      else if (Script[Position] == '1' ||
               Script[Position] == '2' ||
               Script[Position] == '3' ||
               Script[Position] == '4' ||
               Script[Position] == '5' ||
               Script[Position] == '6' ||
               Script[Position] == '7' ||
               Script[Position] == '8' ||
               Script[Position] == '9' ||
               Script[Position] == '.')
        number =  RequiredValue(RDecimalOrOctalLiteral(Position));
      else
        return Failed<ILiteral>();

      Allow(TWhiteSpace());

      return number;
    }

    // [ECMA] StrNumericLiteral : StrDecimalLiteral | HexIntegerLiteral
    //        StrDecimalLiteral : ('+' | '-')? StrUnsignedDecimalLiteral
    //        StrUnsignedDecimalLiteral : 'Infinity' | DecimalLiteral
    //        HexIntegerLiteral : '0' ('x' | 'X') HexDigit*
    // [MCJS] StrNumericLiteral : (all of the above)
    ILiteral TStrNumericLiteral()
    {
      var atPosition = CurrentPosition();

      ILiteral number;

      if (Try(TEOF()))
        return Failed<ILiteral>(atPosition);
      else if (Script[Position] == '+')
      {
        if (Try(RRawChars("+Infinity"))) number = Factory.MakeDoubleLiteral(Double.PositiveInfinity, atPosition);
        else                             number = RDecimalOrOctalLiteral(atPosition);
      }
      else if (Script[Position] == '-')
      {
        if (Try(RRawChars("-Infinity"))) number = Factory.MakeDoubleLiteral(Double.NegativeInfinity, atPosition);
        else                             number = RDecimalOrOctalLiteral(atPosition);
      }
      else if (Script[Position] == '0') 
      {
        if (Try(RRawChars("0x")) || Try(RRawChars("0X")))
          number = RHexIntegerLiteral(atPosition);
        else
        {
          while (RRawChar('0'));        // Eat all leading zeros.

          if (Try(TEOF()) || Script[Position] == '.')
            --Position;                 // Rewind by one to include a single zero before the decimal point or eof

          number = RDecimalOrOctalLiteral(atPosition);
        }
      }
      else if (Script[Position] == '1' ||
               Script[Position] == '2' ||
               Script[Position] == '3' ||
               Script[Position] == '4' ||
               Script[Position] == '5' ||
               Script[Position] == '6' ||
               Script[Position] == '7' ||
               Script[Position] == '8' ||
               Script[Position] == '9' ||
               Script[Position] == '.')
        number = RDecimalOrOctalLiteral(atPosition);
      else if (Try(RRawChars("Infinity")))
        number = Factory.MakeDoubleLiteral(Double.PositiveInfinity, atPosition);
      else
        return Failed<ILiteral>();

      // Ensure that we matched a numeric literal successfully.
      if (!Try(number)) return Failed<ILiteral>();

      Allow(TWhiteSpace());

      return number;
    }

    // [ECMA] DecimalLiteral : DecimalIntegerLiteral '.' DecimalDigits? ExponentPart?
    //                       | '.' DecimalDigits ExponentPart?
    //                       | DecimalIntegerLiteral ExponentPart?
    //        DecimalIntegerLiteral : 0 | NonZeroDigit DecimalDigits?
    //        DecimalDigits : DecimalDigit+
    //        NonZeroDigit : one of '1' '2' '3' '4' '5' '6' '7' '8' '9'
    //        ExponentPart : ExponentIndicator SignedInteger
    //        ExponentIndicator : one of 'e' 'E'
    //        SignedInteger : ('+' | '-')? DecimalDigits
    //        OctalIntegerLiteral : 0 OctalDigit | OctalIntegerLiteral OctalDigit
    // [MCJS] DecimalOrOctalLiteral : (all of the above)
    ILiteral RDecimalOrOctalLiteral(int atPosition)
    {
      // Find the substring that the number covers and the type of number it is.
      var start = Position;
      bool hasSign = false;
      bool hasFractionalPart = false;
      bool hasExponent = false;
      
      // Check for a sign. Note that we will only ever observe a sign if parsing a StringNumericLiteral.
      if (Try(RRawChar('+')) || Try(RRawChar('-'))) hasSign = true;

      // Check for a leading decimal place. We must enforce the presence of at least one digit in this case.
      if (Try(RRawChar('.')))
      {
        hasFractionalPart = true;
        if (!Try(RDecimalDigit())) return Failed<ILiteral>();
      }

      while (true)
      {
        if (Try(RRawChar('.')))
        {
          if (hasFractionalPart || hasExponent) return Failed<ILiteral>();
          else                                  hasFractionalPart = true;
        }
        else if (Try(RRawChar('e') || RRawChar('E'))    &&
                 Allow(RRawChar('+') || RRawChar('-')))
        {
          if (hasExponent) return Failed<ILiteral>();
          else             hasExponent = true;
        }
        else if (!Try(RDecimalDigit()))
          break;
      }

      var length = Position - start;
 
      // Prevent illegal empty numbers.
      if (length < 1) return Failed<ILiteral>();

      // Parse and return the number based on its apparent type.
      var number = Script.Substring(start, length);

      if (length > 1 && Script[start] == '0' && Script[start + 1] != '.')
      {
        // Only permit octal numbers in non-strict mode.
        if (Scope.IsStrict) return Failed<ILiteral>();
        else                return RParseOctal(number, atPosition);
      }
      else if (hasSign || hasFractionalPart || hasExponent) return RParseDouble(number, atPosition);
      else                                                  return RParseInt(number, atPosition);
    }

    ILiteral RParseDouble(string number, int atPosition)
    {
      var doubleStyle = NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent | NumberStyles.AllowLeadingSign;
      try               { return Factory.MakeDoubleLiteral(double.Parse(number, doubleStyle, NumberFormatInfo.InvariantInfo), atPosition); }
      catch (Exception) { return Failed<ILiteral>(); }
    }

    ILiteral RParseInt(string number, int atPosition)
    {
        try                       { return Factory.MakeIntLiteral(uint.Parse(number, NumberStyles.None), atPosition); }
        catch (OverflowException) { return RParseDouble(number, atPosition); }
        catch (Exception)         { return Failed<ILiteral>(); }
    }

    ILiteral RParseOctal(string number, int atPosition)
    {
      try               { return Factory.MakeIntLiteral(Convert.ToUInt32(number, 8), atPosition); }
      catch (Exception) { return Failed<ILiteral>(); }
    }

    // [ECMA] DecimalDigit : one of 0 1 2 3 4 5 6 7 8 9
    bool RDecimalDigit()
    {
      return Try(RRawChar('0'))  ||
             Try(RRawChar('1'))  ||
             Try(RRawChar('2'))  ||
             Try(RRawChar('3'))  ||
             Try(RRawChar('4'))  ||
             Try(RRawChar('5'))  ||
             Try(RRawChar('6'))  ||
             Try(RRawChar('7'))  ||
             Try(RRawChar('8'))  ||
             Try(RRawChar('9'));
    }
 
    // [ECMA] OctalDigit : one of 0 1 2 3 4 5 6 7
    bool ROctalDigit()
    {
      return Try(RRawChar('0'))  ||
             Try(RRawChar('1'))  ||
             Try(RRawChar('2'))  ||
             Try(RRawChar('3'))  ||
             Try(RRawChar('4'))  ||
             Try(RRawChar('5'))  ||
             Try(RRawChar('6'))  ||
             Try(RRawChar('7'));
    }

    // [ECMA] HexIntegerLiteral : '0' ('x' | 'X') HexDigit+
    ILiteral RHexIntegerLiteral(int atPosition)
    {
      // Find the substring that the number covers.
      var start = Position;
      
      while (Try(RHexDigit()));

      var length = Position - start;
 
      // Parse and return non-empty numbers.
      if (length < 1) return Failed<ILiteral>();
      else            return RParseHex(Script.Substring(start, length), atPosition);
    }

    ILiteral RParseHex(string number, int atPosition)
    {
      try                       { return Factory.MakeIntLiteral(uint.Parse(number, NumberStyles.AllowHexSpecifier), atPosition); }
      catch (OverflowException) { return RParseLongHex(number, atPosition); }
      catch (Exception)         { return Failed<ILiteral>(); }
    }

    ILiteral RParseLongHex(string number, int atPosition)
    {
      try               { return Factory.MakeDoubleLiteral(Convert.ToDouble(ulong.Parse(number, NumberStyles.AllowHexSpecifier)), atPosition); }
      catch (Exception) { return Failed<ILiteral>(); }
    }

    // [ECMA] HexDigit : one of 0 1 2 3 4 5 6 7 8 9 a b c d e f A B C D E F
    bool RHexDigit()
    {
      return Try(RDecimalDigit()) ||
             Try(RRawChar('a'))   ||
             Try(RRawChar('b'))   ||
             Try(RRawChar('c'))   ||
             Try(RRawChar('d'))   ||
             Try(RRawChar('e'))   ||
             Try(RRawChar('f'))   ||
             Try(RRawChar('A'))   ||
             Try(RRawChar('B'))   ||
             Try(RRawChar('C'))   ||
             Try(RRawChar('D'))   ||
             Try(RRawChar('E'))   ||
             Try(RRawChar('F'));
    }

    // [ECMA] StringLiteral : '"' DoubleStringCharacters? '"' | '\'' SingleStringCharacters? '\''
    // [MCJS] StringLiteral : '"' StringLiteralRest('"') | '\'' StringLiteralRest('\'')
    ILiteral TStringLiteral()
    {
      if (Try(RRawChar('"')))       return RequiredValue(RStringLiteralRest('"'));
      else if (Try(RRawChar('\''))) return RequiredValue(RStringLiteralRest('\''));
      else                          return Failed<ILiteral>();
    }

    // [MCJS] UseStrictLiteral : '"use strict"' | '\'use strict\''
    ILiteral TUseStrictLiteral()
    {
      if (Try(TChars("\"use strict\"")))    return Factory.MakeStringLiteral("\"use strict\"", Position);
      else if (Try(TChars("'use strict'"))) return Factory.MakeStringLiteral("'use strict'", Position);
      else                                  return Failed<ILiteral>();
    }

    // [ECMA] DoubleStringCharacters : ( SourceCharacter but not one of '"' or '\' or LineTerminator
    //                                 | '\' EscapeSequence
    //                                 | '\' LineTerminatorSequence )*
    //        SingleStringCharacters : (same as above, but substitute '\'' for '"')
    // [MCJS] StringLiteralRest(quote) : ( SourceCharacter but not one of quote or '\' or LineTerminator
    //                                   | '\' EscapeSequence )*
    // Alas the Try/Require mechanism doesn't handle the case where a function may return a value,
    // return nothing, _or_ signal an error. Since REscapeSequence needs this (some escape sequences
    // are merely elided and do not contribute anything to the final value) we don't use that
    // mechanism here, and instead pass the StringBuilder around.
    ILiteral RStringLiteralRest(char quote)
    {
      var atPosition = CurrentPosition();
      var builder = new StringBuilder();

      while (true)
      {
        Require(!TLineTerminatorSequence());

        if (Try(RRawChar(quote)))     break;
        else if (Try(RRawChar('\\'))) REscapeSequence(builder);
        else                          builder.Append(RequiredValue(RNextRawChar()));
      }

      Allow(TWhiteSpace());

      return Factory.MakeStringLiteral(builder.ToString(), atPosition);
    }

    // [ECMA] EscapeSequence : '\' ( CharacterEscapeSequence | '0' [DecimalDigit not in lookahead]
    //                             | OctalEscapeSequence | HexEscapeSequence | UnicodeEscapeSequence )
    //        CharacterEscapeSequence : SourceCharacter but not LineTerminator
    //        LineContinuation : '\' ( <LF> | <CR> <LF>? | <LS> | <PS> )
    // [MCJS] EscapeSequence : SourceCharacter | <LF> | <CR> <LF>? | <LS> | <PS>
    //                         | OctalEscapeSequence | HexEscapeSequence | UnicodeEscapeSequence
    void REscapeSequence(StringBuilder builder)
    {
      string ch;

      if (Try(RRawChar('b')))                    builder.Append('\b');
      else if (Try(RRawChar('t')))               builder.Append('\t');
      else if (Try(RRawChar('n')))               builder.Append('\n');
      else if (Try(RRawChar('v')))               builder.Append('\v');
      else if (Try(RRawChar('f')))               builder.Append('\f');
      else if (Try(RRawChar('r')))               builder.Append('\r');
      else if (Try(RRawChar('x')))               builder.Append(RequiredValue(RHexEscapeSequence()));
      else if (Try(RRawChar('u')))               builder.Append(RequiredValue(RUnicodeEscapeSequence()));
      else if (Try(ch = ROctalEscapeSequence())) builder.Append(ch);
      else if (Try(TLineTerminatorSequence()))   ;  // Simply elide line terminators.
      else                                       builder.Append(RequiredValue(RNextRawChar()));
    }

    // [ECMA] OctalEscapeSequence : OctalDigit [DecimalDigit not in lookahead]
    //                            | ZeroToThree OctalDigit [DecimalDigit not in lookahead]
    //                            | FourToSeven OctalDigit
    //                            | ZeroToThree OctalDigit OctalDigit
    //        ZeroToThree : '0' | '1' | '2' | '3'
    //        FourToSeven : '4' | '5' | '6' | '7'
    string ROctalEscapeSequence()
    {
      // Forbid anything but '0' in strict mode.
      if (Scope.IsStrict)
      {
        if (Try(RRawChar('0')) && Require(!RDecimalDigit())) return "\0";
        else                                                 return Failed<string>();
      }

      // Read up to three octal digits.
      var start = Position;
      var length = 0;
      for ( ; length < 3 && Try(ROctalDigit()) ; ++length);

      // Fail if we don't match at least one octal digit.
      if (length == 0) return Failed<string>(start);

      // Apply the fairly baroque lookahead constraints.
      if (length == 1)
        Require(!RDecimalDigit());
      else if (length == 2)
      {
        if (Script[start] == '0' || Script[start] == '1'  ||
            Script[start] == '2' || Script[start] == '3')
          Require(!RDecimalDigit());
      }

      return RParseOctalEscape(Script.Substring(start, length));
    }

    string RParseOctalEscape(string number)
    {
      try               { return Char.ConvertFromUtf32(Convert.ToInt32(number, 8)); }
      catch (Exception) { return Failed<string>(); }
    }

    // [ECMA] HexEscapeSequence : 'x' HexDigit HexDigit
    // [MCJS] HexEscapeSequence : HexDigit HexDigit
    string RHexEscapeSequence()
    {
      var start = Position;
      
      Require(RHexDigit());
      Require(RHexDigit());
      
      return RParseUnicodeEscape(Script.Substring(start, 2));
    }

    // [ECMA] UnicodeEscapeSequence : 'u' HexDigit HexDigit HexDigit HexDigit
    // [MCJS] UnicodeEscapeSequence : HexDigit HexDigit HexDigit HexDigit
    string RUnicodeEscapeSequence()
    {
      var start = Position;

      Require(RHexDigit());
      Require(RHexDigit());
      Require(RHexDigit());
      Require(RHexDigit());
      
      return RParseUnicodeEscape(Script.Substring(start, 4));
    }

    string RParseUnicodeEscape(string number)
    {
        try                                 { return Char.ConvertFromUtf32(int.Parse(number, NumberStyles.AllowHexSpecifier)); }
        catch (ArgumentOutOfRangeException) { return "\u00A0"; }  // TODO: We need to properly handle surrogate pairs. Return NBSP for now.
        catch (Exception)                   { return Failed<string>(); }
    }

    // [ECMA] RegularExpressionLiteral : '/' RegularExpressionBody '/' RegularExpressionFlags
    // [MCJS] RegularExpressionLiteral : '/' RegexBody '/' RegexFlags
    ILiteral TRegularExpressionLiteral()
    {
      if (Try(RRawChar('/')))
      {
        var atPosition = CurrentPosition();
        var body = RequiredValue(TRegexBody());
        var flags = RequiredValue(RRegexFlags());

        Allow(TWhiteSpace());

        return Factory.MakeRegexLiteral(body, flags, atPosition);
      }
      else
        return Failed<ILiteral>();
    }

    // [ECMA] RegularExpressionBody : !'*' RegularExpressionChar+
    //        RegularExpressionChar : SourceCharacter but not LineTerminator or '\' or '/' or '['
    //                              | RegularExpressionBackslashSequence
    //                              | RegularExpressionClass
    // [MCJS] RegexBody : !'*' ( SourceCharacter but not LineTerminator or '\' or '/' or '['
    //                         | '\' RegexEscape | '[' RegexClass )+ '/'
    // Conveniently, .NET allows you to treat a regular expression as an ECMAScript regex by
    // setting RegexOptions.ECMAScript. As a result, we don't have to worry about translating
    // between the two or handling most escape sequences.
    string TRegexBody()
    {
      StringBuilder builder = new StringBuilder();

      while (true)
      {
        Require(!TEOF());
        Require(!TLineTerminatorSequence());

        if (Try(RRawChar('/')))       break;
        else if (Try(RRawChar('\\'))) RRegexEscape(builder);
        else if (Try(RRawChar('[')))  RRegexClass(builder);
        else                          builder.Append(RequiredValue(RNextRawChar()));
      }

      var body = builder.ToString();

      // Forbid illegal regexes that are empty or start with the Kleene star.
      Require(body.Length > 0);
      Require(!body.StartsWith("*"));

      return body;
    }

    // [ECMA] RegularExpressionBackslashSequence : '\' SourceCharacter but not LineTerminator
    // [MCJS] RegexEscape : SourceCharacter but not LineTerminator
    void RRegexEscape(StringBuilder builder)
    {
      builder.Append('\\');

      Require(!TLineTerminatorSequence());

      var ch = RNextRawChar();
      if (ch.HasValue) builder.Append((char) ch);
    }

    // [ECMA] RegularExpressionClass : '[' RegularExpressionClassChar* ']'
    //        RegularExpressionClassChar : SourceCharacter but not one of ']' or '\'
    //                                   | RegularExpressionBackslashSequence
    // [MCJS] RegexClass : (SourceCharacter but not one of ']' or '\' | RegexEscape)* ']'
    void RRegexClass(StringBuilder builder)
    {
      builder.Append('[');

      while (true)
      {
        Require(!TEOF());
        Require(!TLineTerminatorSequence());

        if (Try(RRawChar(']')))       break;
        else if (Try(RRawChar('\\'))) RRegexEscape(builder);
        else                          builder.Append(RequiredValue(RNextRawChar()));
      }

      builder.Append(']');
    }

    // [ECMA] RegularExpressionFlags : IdentifierPart*
    // [MCJS] RegexFlags : IdentifierPart*
    string RRegexFlags()
    {
      StringBuilder builder = new StringBuilder();

      while (TryAndAddTo(builder, RIdentifierPart()));

      return builder.ToString();
    }

    // [ECMA] IdentifierName but not ReservedWord
    IIdentifier TIdentifier()
    {
      var atPosition = SavePosition();

      string id;

      if (Try(id = RIdentifierNameInternal()))
      {
        var reserved = StrictMode ? StrictModeReservedWords : ReservedWords;

        if (!reserved.Contains(id)) return Factory.MakeIdentifier(Scope, id, atPosition);
        else                        return Failed<IIdentifier>(atPosition);
      }
      else
        return Failed<IIdentifier>(atPosition);
    }

    static HashSet<string> ReservedWords = new HashSet<string>()
    {
      // Keywords.
      "break", "case", "catch", "continue", "debugger", "default", "delete", "do", "else",
      "finally", "for", "function", "if", "in", "instanceof", "new", "return", "switch",
      "this", "throw", "try", "typeof", "var", "void", "while", "with",

      // Future reserved words.
      "class", "const", "enum", "export", "extends", "import", "super",
    };

    static HashSet<string> StrictModeReservedWords = new HashSet<string>()
    {
      // Keywords.
      "break", "case", "catch", "continue", "debugger", "default", "delete", "do", "else",
      "finally", "for", "function", "if", "in", "instanceof", "new", "return", "switch",
      "this", "throw", "try", "typeof", "var", "void", "while", "with",

      // Future reserved words.
      "class", "const", "enum", "export", "extends", "import", "super",

      // Strict mode reserved words.
      "implements", "interface", "yield", "let", "package", "private", "protected",
      "public", "static",
    };

    // [ECMA] IdentifierName : IdentifierStart IdentifierPart*
    IIdentifier TIdentifierName()
    {
      var atPosition = CurrentPosition();
      string id;

      if (Try(id = RIdentifierNameInternal())) return Factory.MakeIdentifier(Scope, id, atPosition);
      else                                     return Failed<IIdentifier>();
    }

    // [ECMA] IdentifierName : IdentifierStart IdentifierPart*
    // [MCJS] IdentifierNameLiteral : IdentifierStart IdentifierPart*
    ILiteral TIdentifierNameLiteral()
    {
      var atPosition = CurrentPosition();
      string id;

      if (Try(id = RIdentifierNameInternal())) return Factory.MakeStringLiteral(id, atPosition);
      else                                     return Failed<ILiteral>();
    }

    // [ECMA] IdentifierName : IdentifierStart IdentifierPart*
    // [MCJS] IdentifierNameInternal : IdentifierStart IdentifierPart*
    string RIdentifierNameInternal()
    {
      StringBuilder builder = new StringBuilder();

      if (TryAndAddTo(builder, TIdentifierStart()))
      {
        while (TryAndAddTo(builder, RIdentifierPart()));

        Allow(TWhiteSpace());

        return builder.ToString();
      }
      else
        return Failed<string>();
    }

    // [ECMA] IdentifierStart : '$' | '_' | '\' UnicodeEscapeSequence | UnicodeLetter
    string TIdentifierStart()
    {
      char? ch;

      if (Try(RRawChar('$')))              return "$";
      else if (Try(RRawChar('_')))         return "_";
      else if (Try(RRawChar('\\'))     &&
               Require(RRawChar('u')))     return RequiredValue(RUnicodeEscapeSequence());
      else if (Try(ch = RUnicodeLetter())) return ch.Value.ToString();
      else                                 return Failed<string>();
    }

    // [ECMA] IdentifierPart : IdentifierStart | UnicodeCombiningMark | UnicodeDigit
    //                       | UnicodeConnectorPunctuation | <ZWNJ> | <ZWJ>
    // [MCJS] IdentifierPart : '$' | '_' | '\' UnicodeEscapeSequence | <ZWNJ> | <ZWJ>
    //                       | UnicodeIdentifierPart
    string RIdentifierPart()
    {
      if (Try(RRawChar('$')))             return "$";
      else if (Try(RRawChar('_')))        return "_";
      else if (Try(RRawChar('\u200C')))   return "\u200C";  // <ZWNJ> (zero-width non-joiner)
      else if (Try(RRawChar('\u200D')))   return "\u200D";  // <ZWJ> (zero-width joiner)
      else if (Try(RRawChar('\\'))     &&
               Require(RRawChar('u')))    return RequiredValue(RUnicodeEscapeSequence());
      else                                return RUnicodeIdentifierPart();
    }

    // [ECMA] UnicodeLetter : any character in the Unicode categories "Uppercase letter (Lu)",
    //                        "Lowercase letter (Ll)", "Titlecase letter (Lt)", "Modifier letter (Lm)",
    //                        "Other letter (Lo)", or "Letter number (Nl)".
    char? RUnicodeLetter()
    {
      var atPosition = SavePosition();

      char? ch;
      if (!Try(ch = RNextRawChar())) return Failed<char?>(atPosition);
      var category = CharUnicodeInfo.GetUnicodeCategory((char) ch);

      if (category == UnicodeCategory.UppercaseLetter ||
          category == UnicodeCategory.LowercaseLetter ||
          category == UnicodeCategory.TitlecaseLetter ||
          category == UnicodeCategory.ModifierLetter  ||
          category == UnicodeCategory.OtherLetter     ||
          category == UnicodeCategory.LetterNumber)
        return ch;
      else
        return Failed<char?>(atPosition);
    }

    // [ECMA] UnicodeCombiningMark : any character in the Unicode categories "Non-spacing mark (Mn)"
    //                               or "Combining spacing mark (Mc)"
    //        UnicodeDigit : any character in the Unicode category "Decimal number (Nd)"
    //        UnicodeConnectorPunctuation : any character in the Unicode category "Connector punctuation (Pc)"
    // [MCJS] UnicodeIdentifierPart : any character in the Unicode categories Lu, Ll, Lt, Lm, Lo, Nl,
    //                                Mn, Mc, Nd, or Pc
    string RUnicodeIdentifierPart()
    {
      var atPosition = SavePosition();

      char? ch;
      if (!Try(ch = RNextRawChar())) return Failed<string>(atPosition);
      var category = CharUnicodeInfo.GetUnicodeCategory(ch.Value);

      if (category == UnicodeCategory.UppercaseLetter       || 
          category == UnicodeCategory.LowercaseLetter       || 
          category == UnicodeCategory.TitlecaseLetter       || 
          category == UnicodeCategory.ModifierLetter        || 
          category == UnicodeCategory.OtherLetter           || 
          category == UnicodeCategory.LetterNumber          || 
          category == UnicodeCategory.NonSpacingMark        || 
          category == UnicodeCategory.SpacingCombiningMark  || 
          category == UnicodeCategory.DecimalDigitNumber    || 
          category == UnicodeCategory.ConnectorPunctuation)
        return ch.Value.ToString();
      else
        return Failed<string>(atPosition);
    }

    // Matches a sequence of contiguous characters in the input exactly; no extra whitespace
    // is included.
    bool RRawChars(string sequence)
    {
      var atPosition = SavePosition();
      
      foreach (var ch in sequence)
        if (!Try(RRawChar(ch)))
          return Failed<bool>(atPosition);

      return true;
    }
 
    // Matches a sequence of contiguous characters in the input, including any
    // following whitespace.
    bool TChars(string sequence)
    {
      if (Try(RRawChars(sequence)) &&
          Allow(TWhiteSpace()))
        return true;
      else
        return Failed<bool>();
    }

    // Attempts to match the given string in the input. Same as TChars(), but
    // for TKeyword() to succeed, the character after the string in the input
    // must be a non-identifier character, to avoid matching identifiers which
    // have a keyword as a prefix.
    bool TKeyword(string keyword)
    {
      var atPosition = SavePosition();

      if (Try(RRawChars(keyword))   &&
          !(Try(RIdentifierPart())) &&
          Allow(TWhiteSpace()))
        return true;
      else
        return Failed<bool>(atPosition);
    }

    // Attempts to match the given string, but only if it is not followed by
    // one of a list of forbidden characters (itself specified as a string).
    bool TCharsWithout(string sequence, string without)
    {
      var atPosition = SavePosition();

      if (Try(RRawChars(sequence)))
      {
        for (int i = 0 ; i < without.Length ; ++i)
          if (Try(RRawChar(without[i])))
            return Failed<bool>(atPosition);

        Allow(TWhiteSpace());

        return true;
      }

      return Failed<bool>(atPosition);
    }
 
    // Attempts to match the given character in the input, including any
    // following whitespace.
    bool TChar(char ch)
    {
      var atPosition = SavePosition();

      if (Try(RRawChar(ch)) &&
          Allow(TWhiteSpace()))
        return true;
      else
        return Failed<bool>(atPosition);
    }

    // Attempts to match the given character in the input; no extra whitespace
    // is included.
    bool RRawChar(char ch)
    {
      if (Try(TEOF()))
        return Failed<bool>();
      else if (Script[Position] == ch)
      {
        ++Position;
        return true;
      }
      else
        return Failed<bool>();
    }

    // Returns the next character in the input, if one exists. Does not
    // automatically eat whitespace after the character.
    char? RNextRawChar()
    {
      if (Try(TEOF())) return Failed<char?>();
      else             return Script[Position++];
    }

    // Like TChar(), but specialized for ';'. Will fail if it encounters a
    // non-whitespace character unless a semicolon can be inserted, which can
    // happen when any of these are true:
    //  (a) A LineTerminator was encountered before the non-whitespace character.
    //  (b) The non-whitespace character is '}'.
    //  (c) EOF is encountered.
    // In any of these circumstances, TSemicolon() will succeed.
    bool TSemicolon()
    {
      var atPosition = SavePosition();

      if (LineBreakInLastWhiteSpace ||
          Try(TEOF())               ||
          Script[Position] == '}'   ||
          Try(TRealSemicolon()))
        return true;
      else
        return Failed<bool>(atPosition);
    }

    // Only matches a real ';' that is actually present in the source code.
    bool TRealSemicolon()
    {
      return TChar(';');
    }
    // [ECMA] LineTerminatorSequence : <LF> | <CR> !<LF> | <LS> | <PS> | <CR> <LF>
    // [MCJS] LineTerminatorSequence : <LF> | <CR> <LF>? | <LS> | <PS>
    bool TLineTerminatorSequence()
    {
      if (Try(RRawChar('\n')))           return true; // <LF> (Line Feed).
      else if (Try(RRawChar('\r'))    &&
               Allow(RRawChar('\n')))    return true; // <CR> (Carriage Return). Allow Windows-style following <LF>.
      else if (Try(RRawChar('\u2028')))  return true; // <LS> (Line Separator).
      else if (Try(RRawChar('\u2029')))  return true; // <PS> (Paragraph Separator).
      else                               return Failed<bool>();
    }

    // [ECMA] WhiteSpace : <TAB> | <VT> | <FF> | <SP> | <NBSP> | <BOM> | <USP>
    //        LineTerminator : <LF> | <CR> | <LS> | <PS>
    // [MCJS] WhiteSpace : <TAB> | <VT> | <FF> | <SP> | <NBSP> | <BOM> | <USP>
    //                   | LineTerminator | Comment
    bool TWhiteSpace()
    {
      LineBreakInLastWhiteSpace = false;

      while (Position < Length)
      {
        char ch = Script[Position];

        if (ch == ' ')                                      ;                                 // <SP> (Space).
        else if (ch == '\t')                                ;                                 // <TAB> (Horizontal Tab).
        else if (ch == '\n')                                LineBreakInLastWhiteSpace = true; // <LF> (Line Feed).
        else if (ch == '\r')                                LineBreakInLastWhiteSpace = true; // <CR> (Carriage Return).
        else if (ch == '\u2028')                            LineBreakInLastWhiteSpace = true; // <LS> (Line Separator).
        else if (ch == '\u2029')                            LineBreakInLastWhiteSpace = true; // <PS> (Paragraph Separator).
        else if (ch == '\v')                                ;                                 // <VT> (Vertical Tab).
        else if (ch == '\f')                                ;                                 // <FF> (Form Feed).
        else if (ch == '\u00A0')                            ;                                 // <NBSP> (Non-breaking Space).
        else if (ch == '\uFEFF')                            ;                                 // <BOM> (Byte Order Mark).
        else if (CharUnicodeInfo.GetUnicodeCategory(ch) ==
                 UnicodeCategory.SpaceSeparator)            ;                                 // <USP> (Unicode Space).
        else if (ch == '/' && Try(RComment()))              continue;
        else                                                return true;

        // We've found some sort of whitespace, so advance over it.
        ++Position;
      }

      // We made it to the end of the input.
      return true;
    }

    // [ECMA] MultiLineComment : '/*' MultiLineCommentChars? '*/'
    //        MultiLineCommentChars : MultiLineNotAsteriskChar MultiLineCommentChars?
    //                              | '*' PostAsteriskCommentChars?
    //        PostAsteriskCommentChars : MultiLineNotForwardSlashOrAsteriskChar MultiLineCommentChars?
    //                                 | '*' PostAsteriskCommentChars?
    //        MultiLineNotAsteriskChar : !'*' SourceCharacter
    //        MultiLineNotForwardSlashOrAsteriskChar : !'/' !'*' SourceCharacter
    //        SingleLineComment : '//' (!LineTerminator SourceCharacter)*
    bool RComment()
    {
      if (!CommentsAllowed) return Failed<bool>();

      var atPosition = SavePosition();

      ++Position; // Advance past initial '/'.

      if (Try(RRawChar('/')))
      {
        while (Position < Length)
        {
          if (Try(RRawChar('\n')))          { LineBreakInLastWhiteSpace = true; return true; } // <LF> (Line Feed).
          else if (Try(RRawChar('\r')))     { LineBreakInLastWhiteSpace = true; return true; } // <CR> (Carriage Return).
          else if (Try(RRawChar('\u2028'))) { LineBreakInLastWhiteSpace = true; return true; } // <LS> (Line Separator).
          else if (Try(RRawChar('\u2029'))) { LineBreakInLastWhiteSpace = true; return true; } // <PS> (Paragraph Separator).
          else                              ++Position;
        }
        
        // We made it to the end of the input.
        return true;
      }
      else if (Try(RRawChar('*')))
      {
        while (RequiredValue(Position < Length))
        {
          if (Try(RRawChars("*/")))         return true;
          else if (Try(RRawChar('\n')))     LineBreakInLastWhiteSpace = true; // <LF> (Line Feed).
          else if (Try(RRawChar('\r')))     LineBreakInLastWhiteSpace = true; // <CR> (Carriage Return).
          else if (Try(RRawChar('\u2028'))) LineBreakInLastWhiteSpace = true; // <LS> (Line Separator).
          else if (Try(RRawChar('\u2029'))) LineBreakInLastWhiteSpace = true; // <PS> (Paragraph Separator).
          else                              ++Position;
        }

        return Failed<bool>(atPosition);  // Will never be reached; just to satisfy the compiler.
      }
      else
        return Failed<bool>(atPosition);
    }

    // Matches EOF.
    bool TEOF()
    {
      if (Position >= Length) return true;
      else                    return false;
    }

    #endregion

    #region Utility Methods

    // Require and friends: throw exception on failure.
    // Use when a match failure indicates that parsing this input will fail.
    // Think "A : B C" in a grammar.
    bool Require<T>(T value, string message = "Parse failure") where T : class
    {
      if (value == null) throw  new SyntaxException(String.Format("{0} at char {1}", message, Position));
      else               return true;
    }

    bool Require(bool value, string message = "Parse failure")
    {
      if (!value) throw  new SyntaxException(String.Format("{0} at char {1}", message, Position));
      else        return true;
    }

    bool RequiredValue(bool value, string message = "Parse failure")
    {
      if (!value) throw  new SyntaxException(String.Format("{0} at char {1}", message, Position));
      else        return true;
    }

    T RequiredValue<T>(T value, string message = "Parse failure") where T : class
    {
      if (value == null) throw  new SyntaxException(String.Format("{0} at char {1}", message, Position));
      else               return value;
    }

    T RequiredValue<T>(Nullable<T> value, string message = "Parse failure") where T : struct
    {
      if (value.HasValue) return value.Value;
      else                throw  new SyntaxException(String.Format("{0} at char {1}", message, Position));
    }

    bool RequireAndAddTo<T>(ICollection<T> group, ISyntaxNode value, string message = "Parse failure") where T : ISyntaxNode
    {
      if (value != null) { group.Add(value); return true; }
      else               throw new SyntaxException(String.Format("{0} at char {1}", message, Position));
    }

    // Try and friends: return false on failure.
    // Use when a match failure indicates that this rule will fail, but parsing
    // overall may still succeed. Think "A : B | C" in a grammar.
    static bool Try<T>(T value) where T : class
    {
      return value != null;
    }

    static bool Try<T>(Nullable<T> value) where T : struct
    {
      return value.HasValue;
    }

    static bool Try(bool value)
    {
      return value;
    }

    static bool TryAndAddTo<T>(ICollection<T> group, ISyntaxNode value) where T : ISyntaxNode
    {
      if (value != null) { group.Add(value); return true; }
      else               return false;
    }

    static bool TryAndAddTo<T>(StringBuilder builder, Nullable<T> value) where T : struct
    {
      if (value.HasValue) { builder.Append(value.Value); return true; }
      else                return false;
    }

    static bool TryAndAddTo<T>(StringBuilder builder, T value) where T : class
    {
      if (value != null) { builder.Append(value); return true; }
      else               return false;
    }

    // Allow and friends: ignore failure; always return true.
    // Use when a match failure does not mean that this rule will fail, i.e.
    // for optional content. Think "A : B? C" in a grammar.
    static bool Allow<T>(T value) where T : class
    {
      return true;
    }

    static T AllowedValue<T>(T value) where T : class
    {
      return value;
    }

    static bool Allow(bool value)
    {
      return true;
    }

    // Do: intended to be used in chains of boolean expressions to evaluate a side-effecting
    // expression that doesn't return a bool without leaving the chain or conveying the wrong
    // semantics with its name. Think "if (A() && Do(b = c) && D())", for example.
    static bool Do<T>(T value)
    {
      return true;
    }

    // SavePosition: Returns the position we would have to backtrack to if the current rule failed.
    // This method exists for two reasons:
    // - Conceptually, position information is part of the lexer. If the lexer were moved to a
    //   separate class, it would need to be accessed through a method like this. Thus even
    //   though its name doesn't start with a T (since it doesn't correspond to a terminal),
    //   SavePosition() should logically be grouped with the T*() methods.
    // - It provides a hook for inserting debugging code.
    int SavePosition()
    {
      return Position;
    }

    // CurrentPosition: Same as SavePosition(), and also conceptually grouped with the T*() methods.
    // The only difference is that this method should be called when the position is only required
    // for informational purposes (e.g. source offset information for IR nodes) but will not be
    // used to backtrack. This distinction makes SavePosition() more useful as a debugging hook.
    int CurrentPosition()
    {
      return Position;
    }

    // Failed: called when a rule fails to match. Can optionally rewind our position in the input.
    // Rewinding is required when the rule may match something and ultimately fail without parsing
    // as a whole failing. To make this concrete, generally if you have any Try() or Allow() calls
    // that can succeed without forcing the parser on a path where it will encounter Require() calls,
    // you must rewind the parser's position on failure.
    T Failed<T>()
    {
      return default(T);
    }

    T Failed<T>(int position)
    {
      Position = position;
      return default(T);
    }

    #endregion

    #region Debugging Methods

    string Context(int size = 30)
    {
      var leftStart = Math.Max(Position - size, 0);
      var rightLength = Math.Min(size, Length - Position);
      return Script.Substring(leftStart, Position - leftStart) + "|^|" + Script.Substring(Position, rightLength);
    }

    #endregion
  }
}
