// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using mjr.IR.Syntax;
using m.Util.Diagnose;

namespace mjr.IR
{
  public class NodeFactory
  {
    public NodeFactory()
    { }

    /// <summary>
    /// For now we have only ParenExpression that wraps stuff. 
    /// we may add more in the future
    /// </summary>
    Expression GetEquivalent(Expression exp)
    {
      var parenExp = exp as ParenExpression;
      if (parenExp != null)
        return parenExp.Expression;
      else
        return exp;
    }

    /// <summary>
    /// In javascript all "var" declarations must be hoisted to the top of the function. 
    /// in other words, they are local symbols of the function scope, even if they are 
    /// defined in any inner scope. 
    /// this function will take care of that. 
    /// </summary>
    void DeclareHoistedLocal(JSSymbol symbol)
    {
      var scope = symbol.ContainerScope;
      if (scope.IsFunction)
      {
        symbol.SymbolType = JSSymbol.SymbolTypes.Local;
      }
      else
      {
        while (!scope.IsFunction)
          scope = scope.OuterScope;
        var resolvedSymbol = scope.GetOrAddSymbol(symbol.Name);
        resolvedSymbol.SymbolType = JSSymbol.SymbolTypes.Local;
        symbol.SymbolType = JSSymbol.SymbolTypes.OuterDuplicate;
        symbol.ResolvedSymbol = resolvedSymbol;
      }
    }
    #region List types. -------------------------------------------------------------------------------------

    public IStatementList MakeStatementList()
    {
      return new IStatementList();
    }

    public IExpressionList MakeExpressionList()
    {
      return new IExpressionList();
    }

    public IIdentifierList MakeIdentifierList()
    {
      return new IIdentifierList();
    }

    public IVariableDeclarationList MakeVariableDeclarationList()
    {
      return new IVariableDeclarationList();
    }

    public IPropertyAssignmentList MakePropertyAssignmentList()
    {
      return new IPropertyAssignmentList();
    }

    public ICaseClauseList MakeCaseClauseList()
    {
      return new ICaseClauseList();
    }

    #endregion

    #region Statements; ECMA 12. -------------------------------------------------------------------------------------

    public BlockStatement MakeBlockStatement(List<Statement> statements, int offset)
    {
      return new BlockStatement(statements, offset);
    }

    public VariableDeclaration MakeVariableDeclaration(IIdentifier identifier, IExpression initialization)
    {
      var symbol = ((Identifier)identifier).Symbol;
      DeclareHoistedLocal(symbol);

      if (initialization == null)
        return new VariableDeclaration((Identifier)identifier, null);
      else
      {
        var writeId = MakeWriteIdentifierExpression(symbol, (Expression)initialization);
        return new VariableDeclaration((Identifier)identifier, writeId);
      }
    }

    public VariableDeclarationStatement MakeVariableDeclarationStatement(IVariableDeclarationList declarations)
    {
      return new VariableDeclarationStatement(declarations);
    }

    public EmptyStatement MakeEmptyStatement(int offset)
    {
      return new EmptyStatement(offset);
    }

    public ExpressionStatement MakeExpressionStatement(IExpression expression)
    {
      return new ExpressionStatement((Expression)expression);
    }

    public IfStatement MakeIfStatement(IExpression condition, IStatement thenStatement, IStatement elseStatement = null)
    {
      var booleanCondition = MakeToBoolean((Expression)condition);
      var n = new IfStatement(booleanCondition, (Statement)thenStatement, (Statement)elseStatement);
      return n;
    }

    public DoWhileStatement MakeDoWhileStatement(Scope scope, IExpression condition, IStatement body)
    {
      scope.HasLoop = true;
      var booleanCondition = MakeToBoolean((Expression)condition);
      var n = new DoWhileStatement(booleanCondition, (Statement)body);
      return n;
    }

    public WhileStatement MakeWhileStatement(Scope scope, IExpression condition, IStatement body)
    {
      scope.HasLoop = true;
      var booleanCondition = MakeToBoolean((Expression)condition);
      var n = new WhileStatement(booleanCondition, (Statement)body);
      return n;
    }

    public ForStatement MakeForStatement(Scope scope, IExpression initialization, IExpression condition, IExpression increment, IStatement body)
    {
      return MakeForStatement(scope, initialization != null ? MakeExpressionStatement(initialization) : null, condition, increment, body);
    }

    public ForStatement MakeForStatement(Scope scope, IStatement initialization, IExpression condition, IExpression increment, IStatement body)
    {
      scope.HasLoop = true;
      ToBoolean booleanCondition = null;
      if (condition != null)
        booleanCondition = MakeToBoolean((Expression)condition);
      var n = new ForStatement((Statement)initialization, booleanCondition, (Expression)increment, (Statement)body);
      return n;
    }

    public ForEachInStatement MakeForEachInStatement(Scope scope, IStatement initialization, IExpression expression, IStatement body)
    {
      scope.HasLoop = true;
      var container = MakeToObject((Expression)expression);
      
      var iterator = MakeInternalNew(CodeGen.Types.JSPropertyNameEnumerator.CONSTRUCTOR_DObject, new List<Expression> { container });
      var iteratorInitialization = MakeExpressionStatement(iterator);

      var moveNext = MakeInternalCall(CodeGen.Types.JSPropertyNameEnumerator.MoveNext, new List<Expression> { iterator });
      var iteratorCondition = MakeToBoolean(moveNext);
      
      var readCurrent = MakeToString(MakeInternalCall(CodeGen.Types.JSPropertyNameEnumerator.GetCurrent, new List<Expression> { iterator }));
      Reference assignToLeft;

      var vds = initialization as VariableDeclarationStatement;
      if (vds != null)
      {
        var vd = vds.Declarations[0];
        //Standard 12.6.4 is wague on the following situation, we just throw for now!
        Debug.Assert(vds.Declarations.Count == 1 && vd.Initialization == null, "var initizliation in the for-in statement is not supported! ");
        assignToLeft = MakeWriteIdentifierExpression(vd.Symbol, readCurrent);
      }
      else
      {
        var expStatement = initialization as ExpressionStatement;
        Debug.Assert(expStatement != null, "Invalid situation! Need an Expression statement instead of {0}", initialization);
        var readId = expStatement.Expression as ReadIdentifierExpression;
        if (readId != null)
        {
          assignToLeft = MakeWriteIdentifierExpression(readId.Symbol, readCurrent);
        }
        else
        {
          var readIndex = expStatement.Expression as ReadIndexerExpression;
          Debug.Assert(readIndex != null, "Invalid situation, expression {0} in for-in must be a reference", expStatement.Expression);
          assignToLeft = MakeWriteIndexerExpression(readIndex.Container, readIndex.Index, readCurrent);
        }

      }

      var extendedBody = new List<Statement>();
      extendedBody.Add(MakeExpressionStatement(assignToLeft));
      extendedBody.Add((Statement)body);

      return new ForEachInStatement((Statement)initialization, container, (Statement)body,
                                    (Statement)iteratorInitialization, iteratorCondition,
                                    MakeBlockStatement(extendedBody, ((Statement)body).SourceOffset));
    }

    public LabelStatement MakeLabelStatement(IIdentifier name, IStatement target)
    {
      //TODO: we have to remove name.Symbol, or define a new SymbolType.Label
      return new LabelStatement((Identifier)name, (Statement)target);
    }

    public GotoStatement MakeGotoStatement(IIdentifier target)
    {
      var t = (Identifier)target;
      return new GotoStatement(t.Symbol.Name, t.SourceOffset);
    }

    public ContinueStatement MakeContinueStatement(IIdentifier target = null, int offset = 0)
    {
      var label = target != null ? ((Identifier)target).Symbol.Name : null;

      return new ContinueStatement(label, offset);
    }

    public BreakStatement MakeBreakStatement(IIdentifier target = null, int offset = 0)
    {
      var label = target != null ? ((Identifier)target).Symbol.Name : null;

      return new BreakStatement(label, offset);
    }

    public ReturnStatement MakeReturnStatement(Scope scope, IExpression expression, int offset)
    {
      var n = new ReturnStatement((Expression)expression, offset);
      scope.Returns.Add(n);
      return n;
    }

    public WithStatement MakeWithStatement(IExpression expression, IStatement statement)
    {
      var n = new WithStatement((Expression)expression, (Statement)statement);
      return n;
    }

    public SwitchStatement MakeSwitchStatement(Scope scope, IExpression expression, ICaseClauseList caseClauses)
    {
      var expr = (Expression)expression;
      foreach (var c in caseClauses)
      {
        if (!c.IsDefault)
        {
          c.Comparison = MakeSameExpressionInternal(expr, c.Expression);
          c.Comparison.AddUser(c);
        }
      }
      var n = new SwitchStatement(expr, caseClauses);
      return n;
    }

    public CaseClause MakeCaseClause(IExpression expression, IStatement body)
    {
      return new CaseClause((Expression)expression, (BlockStatement)body);
    }

    public CaseClause MakeDefaultClause(IStatement body)
    {
      return new CaseClause(null, (BlockStatement)body);
    }

    public ThrowStatement MakeThrowStatement(IExpression expression)
    {
      return new ThrowStatement((Expression)expression);
    }

    public CatchClause MakeCatchClause(IIdentifier identifier, IStatement statement, Scope innerScope)
    {
      var readId = identifier as ReadIdentifierExpression;
      Debug.Assert(readId != null, "Catch clause identifier {0} is not a ReadIdentifierExpression", ((Identifier)identifier));
      Debug.Assert(readId.Symbol != null, "Cannot find symbol for identifier {0} in catch clause", ((Identifier)identifier));
      Debug.Assert(innerScope.GetSymbol(readId.Symbol.Name) == readId.Symbol, "Catch clause identifier {0} must belong to its inner scope", readId);
      readId.Symbol.SymbolType = JSSymbol.SymbolTypes.Local; //this symbol is trully local to the innerScope
      return new CatchClause(innerScope, readId, (Statement)statement);
    }

    public FinallyClause MakeFinallyClause(IStatement statement)
    {
      return new FinallyClause((Statement)statement);
    }

    public TryStatement MakeTryStatement(IStatement statement, ICatch catchClause, IFinally finallyClause)
    {
      return new TryStatement((Statement)statement, (CatchClause)catchClause, (FinallyClause)finallyClause);
    }

    public IStatement MakeDebuggerStatement(int offset)
    {
      return MakeEmptyStatement(offset);
    }

    #endregion

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

    public ThisLiteral MakeThisLiteral(Scope scope, int offset = 0)
    {
      scope.HasThisSymbol = true;
      return new ThisLiteral(offset);
    }

    public NullLiteral MakeNullLiteral(Scope scope = null, int offset = 0)
    {
      return new NullLiteral(offset);
    }

    public BooleanLiteral MakeBooleanLiteral(bool value, int offset = 0)
    {
      return new BooleanLiteral(value, offset);
    }

    public IntLiteral MakeIntLiteral(uint value, int offset = 0)
    {
      return new IntLiteral(value, offset);
    }

    public DoubleLiteral MakeDoubleLiteral(double value, int offset = 0)
    {
      return new DoubleLiteral(value, offset);
    }

    public StringLiteral MakeStringLiteral(string value, int offset = 0)
    {
      return new StringLiteral(value, offset);
    }

    public RegexpLiteral MakeRegexLiteral(string regex, string options, int offset = 0)
    {
      return new RegexpLiteral(regex, options, offset);
    }

    public ArrayLiteral MakeArrayLiteral(List<Expression> items, int offset = 0)
    {
      return new ArrayLiteral(items, offset);
    }

    public ObjectLiteral MakeObjectLiteral(List<PropertyAssignment> properties, int offset = 0)
    {
      return new ObjectLiteral(properties, offset);
    }

    public ILeftHandSideExpression MakeParenExpression(IExpression expression)
    {
      return new ParenExpression((Expression)expression);
    }

    private Expression MakeGuardedCast(Expression expression)
    {
      ///We want to peel ParenExpression, but donot need to repeat GuardedCast node
      ///Also, there are only few types of nodes that generate values whose type may only be known at runtime

      var parenExpt = expression as ParenExpression;
      if (parenExpt != null 
        && !(expression is GuardedCast))
        expression = parenExpt.Expression;

      if ( expression is ReadIdentifierExpression
        || expression is CallExpression
        || expression is ReadIndexerExpression)
        return new GuardedCast(expression);
      else
        return expression;
    }

    public IPropertyAssignment MakePropertyAssignment(ILiteral name, IExpression expression)
    {
      // TODO: Is calling ToString() adequate here?
      return new PropertyAssignment(((Literal)name).ToString(), (Expression)expression);
    }

    public IPropertyAssignment MakePropertyGetAssignment(Scope scope, ILiteral name, IStatement body, Scope innerScope)
    {
      // TODO: Is calling ToString() adequate here?
      // TODO: Implement properly. This is just a quick hack to get things semi-working.
      var getterName = MakeIdentifier(innerScope, name.ToString(), ((Node)name).SourceOffset);
      var getter = MakeFunctionExpression(scope, getterName, MakeIdentifierList(), body, innerScope);
      return new PropertyAssignment(name.ToString(), null, getter);
    }

    public IPropertyAssignment MakePropertySetAssignment(Scope scope, ILiteral name, IIdentifier paramName, IStatement body, Scope innerScope)
    {
      // TODO: Is calling ToString() adequate here?
      // TODO: Implement properly. This is just a quick hack to get things semi-working.
      var paramList = MakeIdentifierList();
      paramList.Add(paramName);
      var setterName = MakeIdentifier(innerScope, name.ToString(), ((Node)name).SourceOffset);
      var setter = MakeFunctionExpression(scope, setterName, paramList, body, innerScope);
      return new PropertyAssignment(name.ToString(), setter, null);
    }

    public IIdentifier MakeIdentifier(Scope scope, string identifier, int offset)
    {
      return MakeReadIdentifierExpression(scope, identifier, offset);
    }

    private IIdentifier MakeIdentifier(JSSymbol symbol, int offset) { return MakeReadIdentifierExpression(symbol, offset); }

    private ReadIdentifierExpression MakeReadIdentifierExpression(Scope scope, string identifier, int offset)
    {
      return MakeReadIdentifierExpression(scope.GetOrAddSymbol(identifier), offset);
    }

    private ReadIdentifierExpression MakeReadIdentifierExpression(JSSymbol symbol, int offset)
    {
      return new ReadIdentifierExpression(symbol, offset);
    }

    public ILeftHandSideExpression MakeIndexerExpression(IExpression container, IExpression index)
    {
      return MakeReadIndexerExpression((Expression)container, (Expression)index);
    }

    private ReadIndexerExpression MakeReadIndexerExpression(Expression container, Expression index)
    {
      StringLiteral constIndex = index as StringLiteral;
      int integerIndex;
      ReadIndexerExpression n;

      //We need to make sure that the string is not an integer, because in the case of Arrays integer strings are equivalent to integers!!
      if (constIndex != null && !int.TryParse(constIndex.Value, out integerIndex))
        n = new ReadPropertyExpression(MakeToObject((Expression)container), constIndex);
      else
        n = new ReadIndexerExpression(MakeToObject((Expression)container), (Expression)index);
      return n;
    }

    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    public ToPrimitive MakeToPrimitive(Expression expression)
    {
      var n = expression as ToPrimitive;
      if (n == null)
      {
//        n = new ToPrimitive(MakeGuardedCast(expression));
        n = new ToPrimitive(expression);
      }
      return n;
    }

    public ToBoolean MakeToBoolean(Expression expression)
    {
      var n = expression as ToBoolean;
      if (n == null)
      {
        n = new ToBoolean(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToNumber MakeToNumber(Expression expression)
    {
      var n = expression as ToNumber;
      if (n == null)
      {
        n = new ToNumber(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToDouble MakeToDouble(Expression expression)
    {
      var n = expression as ToDouble;
      if (n == null)
      {
        n = new ToDouble(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToInteger MakeToInteger(Expression expression)
    {
      var n = expression as ToInteger;
      if (n == null)
      {
        n = new ToInteger(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToInt32 MakeToInt32(Expression expression)
    {
      var n = expression as ToInt32;
      if (n == null)
      {
//        n = new ToInt32(MakeGuardedCast(expression));
        n = new ToInt32(expression);
      }
      return n;
    }

    public ToUInt32 MakeToUInt32(Expression expression)
    {
      var n = expression as ToUInt32;
      if (n == null)
      {
        n = new ToUInt32(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToUInt16 MakeToUInt16(Expression expression)
    {
      var n = expression as ToUInt16;
      if (n == null)
      {
        n = new ToUInt16(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToString MakeToString(Expression expression)
    {
      var n = expression as ToString;
      if (n == null)
      {
        n = new ToString(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToObject MakeToObject(Expression expression)
    {
      var n = expression as ToObject;
      if (n == null)
      {
        n = new ToObject(MakeGuardedCast(expression));
      }
      return n;
    }

    public ToFunction MakeToFunction(Expression expression)
    {
      var n = expression as ToFunction;
      if (n == null)
      {
        n = new ToFunction(MakeGuardedCast(expression));
      }
      return n;
    }

    #endregion

    #region Unary Operators ECMA 11.4 -------------------------------------------------------------------------------------

    public DeleteExpression MakeDeleteExpression(IExpression expression)
    {
      var n = new DeleteExpression((Expression)expression);
      return n;
    }

    public VoidExpression MakeVoidExpression(IExpression expression)
    {
      var n = new VoidExpression((Expression)expression);
      return n;
    }

    public TypeofExpression MakeTypeofExpression(IExpression expression)
    {
      var n = new TypeofExpression((Expression)expression);
      return n;
    }

    public PositiveExpression MakePositiveExpression(IExpression expression)
    {
      var n = new PositiveExpression((Expression)expression);
      return n;
    }

    public NegativeExpression MakeNegativeExpression(IExpression expression)
    {
      var n = new NegativeExpression((Expression)expression);
      return n;
    }

    public BitwiseNotExpression MakeBitwiseNotExpression(IExpression expression)
    {
      var n = new BitwiseNotExpression(MakeToInt32((Expression)expression));
      return n;
    }

    public LogicalNotExpression MakeLogicalNotExpression(IExpression expression)
    {
      var n = new LogicalNotExpression(MakeToBoolean((Expression)expression));
      return n;
    }

    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------

    public IExpression MakeMultiplyExpression(IExpression left, IExpression right)
    {
      return MakeMultiplyExpressionInternal((Expression)left, (Expression)right);
    }

    private MultiplyExpression MakeMultiplyExpressionInternal(Expression left, Expression right)
    {
      var n = new MultiplyExpression(MakeGuardedCast(left), MakeGuardedCast(right));
      return n;
    }

    public IExpression MakeDivideExpression(IExpression left, IExpression right)
    {
      return MakeDivideExpressionInternal((Expression)left, (Expression)right);
    }

    private DivideExpression MakeDivideExpressionInternal(Expression left, Expression right)
    {
      var n = new DivideExpression(MakeToDouble(left), MakeToDouble(right));
      return n;
    }

    public IExpression MakeRemainderExpression(IExpression left, IExpression right)
    {
      return MakeRemainderExpressionInternal((Expression)left, (Expression)right);
    }

    private RemainderExpression MakeRemainderExpressionInternal(Expression left, Expression right)
    {
      var n = new RemainderExpression(MakeToDouble(left), MakeToDouble(right));
      return n;
    }

    #endregion

    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------

    public IExpression MakeAdditionExpression(IExpression left, IExpression right)
    {
      return MakeAdditionExpressionInternal((Expression)left, (Expression)right);
    }

    private AdditionExpression MakeAdditionExpressionInternal(Expression left, Expression right)
    {
      return new AdditionExpression(MakeGuardedCast(left), MakeGuardedCast(right));
    }

    public IExpression MakeSubtractionExpression(IExpression left, IExpression right)
    {
      return MakeSubtractionExpressionInternal((Expression)left, (Expression)right);
    }

    private SubtractionExpression MakeSubtractionExpressionInternal(Expression left, Expression right)
    {
      return new SubtractionExpression(MakeGuardedCast(left), MakeGuardedCast(right));
    }

    #endregion

    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------

    public IExpression MakeLeftShiftExpression(IExpression left, IExpression right)
    {
      return MakeLeftShiftExpressionInternal((Expression)left, (Expression)right);
    }

    private LeftShiftExpression MakeLeftShiftExpressionInternal(Expression left, Expression right)
    {
      var n = new LeftShiftExpression(MakeToInt32(left), MakeToUInt32(right));
      return n;
    }

    public IExpression MakeRightShiftExpression(IExpression left, IExpression right)
    {
      return MakeRightShiftExpressionInternal((Expression)left, (Expression)right);
    }

    private RightShiftExpression MakeRightShiftExpressionInternal(Expression left, Expression right)
    {
      var n = new RightShiftExpression(MakeToInt32(left), MakeToUInt32(right));
      return n;
    }

    public IExpression MakeUnsignedRightShiftExpression(IExpression left, IExpression right)
    {
      return MakeUnsignedRightShiftExpressionInternal((Expression)left, (Expression)right);
    }

    private UnsignedRightShiftExpression MakeUnsignedRightShiftExpressionInternal(Expression left, Expression right)
    {
      var n = new UnsignedRightShiftExpression(MakeToUInt32(left), MakeToUInt32(right));
      return n;
    }

    #endregion

    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------

    public IExpression MakeLesserExpression(IExpression left, IExpression right)
    {
      var n = new LesserExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeGreaterExpression(IExpression left, IExpression right)
    {
      var n = new GreaterExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeLesserOrEqualExpression(IExpression left, IExpression right)
    {
      var n = new LesserOrEqualExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeGreaterOrEqualExpression(IExpression left, IExpression right)
    {
      var n = new GreaterOrEqualExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeInstanceOfExpression(IExpression left, IExpression right)
    {
      var n = new InstanceOfExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeInExpression(IExpression left, IExpression right)
    {
      var n = new InExpression((Expression)left, (Expression)right);
      return n;
    }

    #endregion

    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------

    public IExpression MakeEqualExpression(IExpression left, IExpression right)
    {
      var n = new EqualExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeNotEqualExpression(IExpression left, IExpression right)
    {
      var n = new NotEqualExpression((Expression)left, (Expression)right);
      return n;
    }

    public IExpression MakeSameExpression(IExpression left, IExpression right)
    {
      return MakeSameExpressionInternal((Expression)left, (Expression)right);
    }

    private SameExpression MakeSameExpressionInternal(Expression left, Expression right)
    {
      var n = new SameExpression(MakeGuardedCast(left), MakeGuardedCast(right));
      return n;
    }

    public IExpression MakeNotSameExpression(IExpression left, IExpression right)
    {
      var n = new NotSameExpression(MakeGuardedCast((Expression)left), MakeGuardedCast((Expression)right));
      return n;
    }

    #endregion

    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------

    public IExpression MakeBitwiseAndExpression(IExpression left, IExpression right)
    {
      return MakeBitwiseAndExpressionInternal((Expression)left, (Expression)right);
    }

    private BitwiseAndExpression MakeBitwiseAndExpressionInternal(Expression left, Expression right)
    {
      var n = new BitwiseAndExpression(MakeToInt32(left), MakeToInt32(right));
      return n;
    }

    public IExpression MakeBitwiseOrExpression(IExpression left, IExpression right)
    {
      return MakeBitwiseOrExpressionInternal((Expression)left, (Expression)right);
    }

    private BitwiseOrExpression MakeBitwiseOrExpressionInternal(Expression left, Expression right)
    {
      var n = new BitwiseOrExpression(MakeToInt32(left), MakeToInt32(right));
      return n;
    }

    public IExpression MakeBitwiseXorExpression(IExpression left, IExpression right)
    {
      return MakeBitwiseXorExpressionInternal((Expression)left, (Expression)right);
    }

    private BitwiseXorExpression MakeBitwiseXorExpressionInternal(Expression left, Expression right)
    {
      var n = new BitwiseXorExpression(MakeToInt32(left), MakeToInt32(right));
      return n;
    }

    #endregion

    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    //TODO: eventually, we just need to return ternary and avoid the extra objects

    public IExpression MakeLogicalAndExpression(IExpression leftExpr, IExpression rightExpr)
    {
      var left = (Expression)leftExpr;
      var right = (Expression)rightExpr;
      var implementation = MakeTernaryExpressionInternal(left, right, left);
      var n = new LogicalAndExpression(left, right, implementation);
      return n;
    }

    public IExpression MakeLogicalOrExpression(IExpression leftExpr, IExpression rightExpr)
    {
      var left = (Expression)leftExpr;
      var right = (Expression)rightExpr;
      var implementation = MakeTernaryExpressionInternal(left, left, right);
      var n = new LogicalOrExpression(left, right, implementation);
      return n;
    }

    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------

    public IExpression MakeTernaryExpression(IExpression left, IExpression middle, IExpression right)
    {
      return MakeTernaryExpressionInternal((Expression)left, (Expression)middle, (Expression)right);
    }

    private TernaryExpression MakeTernaryExpressionInternal(Expression left, Expression middle, Expression right)
    {
      return new TernaryExpression(MakeToBoolean(left), middle, right);
    }

    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    //Externally, the MakeAssignment should be used

    private WriteIdentifierExpression MakeWriteIdentifierExpression(Scope scope, string identifier, Expression value)
    {
      return MakeWriteIdentifierExpression(scope.GetOrAddSymbol(identifier), MakeGuardedCast(value));
    }

    private WriteIdentifierExpression MakeWriteIdentifierExpression(JSSymbol symbol, Expression value)
    {
      return new WriteIdentifierExpression(symbol, MakeGuardedCast(value));
    }

    private WriteIndexerExpression MakeWriteIndexerExpression(Expression container, Expression index, Expression value)
    {
      // TODO: Is the comment below still valid? Doesn't seem to make sense anymore.
      //we need to make sure the correct GetyType(n) is callsed, that's why the code is repeated
      if (index is StringLiteral)
        return new WritePropertyExpression(MakeToObject(container), index, value);
      else
        return new WriteIndexerExpression(MakeToObject(container), index, value);
    }
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------

    public CommaOperatorExpression MakeCommaOperatorExpression(List<Expression> expressions)
    {
      return new CommaOperatorExpression(expressions);
    }

    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------

    public NewExpression MakeNewExpression(Scope scope, IExpression function, IExpressionList arguments = null)
    {
      scope.HasCall = true;
      var n = new NewExpression(MakeToFunction((Expression)function), arguments ?? MakeExpressionList());
      scope.Invocations.Add(n);
      return n;
    }

    public CallExpression MakeCallExpression(Scope scope, IExpression function, IExpressionList arguments)
    {
      scope.HasCall = true;

      ///We have to detect the followings immediately now, before future phases modify the IR

      var func = MakeToFunction((Expression)function); //This will ensure we have uniform type going forward

      Expression thisArg = null;
      bool isDirectEvalCall = false;

      var funcExp = GetEquivalent(func.Expression);

      var indexer = funcExp as ReadIndexerExpression;
      if (indexer != null)
      {
        thisArg = indexer.Container;
      }
      else
      {
        var id = funcExp as ReadIdentifierExpression;
        Debug.WriteLineIf(id == null, "Runtime error or coding error?");
        isDirectEvalCall = (id != null && id.Symbol != null && id.Symbol.Name == "eval");
        if (isDirectEvalCall)
          scope.HasEval = isDirectEvalCall; //We have to be carefull not to over write the old value!
      }

      var n = new CallExpression(func, thisArg, arguments, isDirectEvalCall);
      scope.Invocations.Add(n);
      return n;
    }

    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------

    public FunctionExpression MakeFunctionExpression(Scope scope, IIdentifier name, IIdentifierList parameters,
                                                     IStatement statement, Scope newScope)
    {
      var namedParams = parameters as List<ReadIdentifierExpression>;
      Debug.Assert(namedParams.Count == 0 || newScope.IsFunction, "parameters must be declared in a function scope");
      for (var i = 0; i < namedParams.Count; ++i)
      {
        var symbol = namedParams[i].Symbol;
        Debug.Assert(newScope.GetSymbol(symbol.Name) == symbol && symbol.ContainerScope == newScope, "Invalid situation, parameter symbol is not in the newScope");
        symbol.SymbolType = JSSymbol.SymbolTypes.Local; //already know symbol.ContainerScope.IsFunction, so no need for hoisting
        symbol.ParameterIndex = i;
      }

      var funcName = name as ReadIdentifierExpression;
      if (newScope.IsFunctionDeclaration)
      {
        Debug.Assert(funcName != null, "the function declaration must have a name");
        Debug.Assert(scope.GetSymbol(funcName.Symbol.Name) == funcName.Symbol && funcName.Symbol.ContainerScope == scope, "Name of function declaration must exist in the outer scope");
        DeclareHoistedLocal(funcName.Symbol); //This is defined now in its scope!
      }
      else if (newScope.IsProgram)
      {
        Debug.Assert(funcName == null, "program scope cannot have a name!");
      }
      else
      {
        Debug.Assert(newScope.IsFunction == true, "The FunctionExpression scope is not properly marked");
        if (funcName != null)
        {
          Debug.Assert(newScope.GetSymbol(funcName.Symbol.Name) == funcName.Symbol && funcName.Symbol.ContainerScope == newScope, "Name of function expression must exist in its own scope");
          funcName.Symbol.SymbolType = JSSymbol.SymbolTypes.Local; //This is defined now in its scope, & we already know funcName.Symbol.ContainerScope.IsFunction, so no need for hoisting
        }
      }

      var func = new FunctionExpression(newScope, funcName, namedParams, (BlockStatement)statement);

      var metadata = new JSFunctionMetadata(func);
      func.Metadata = metadata;
      if (scope != null) scope.AddSubFunction(metadata);

      return func;
    }

    public FunctionDeclarationStatement MakeFunctionDeclarationStatement(Scope scope, IIdentifier name, IIdentifierList parameters,
                                                                         IStatement statement, Scope newScope)
    {
      Debug.Assert(newScope.IsFunctionDeclaration == true, "The FunctionDeclaration scope is not properly marked");

      var func = MakeFunctionExpression(scope, name, parameters, statement, newScope);
      //After making the func expression, we are sure name.Symbol belongs to scope
      return new FunctionDeclarationStatement(func, MakeWriteIdentifierExpression(((Identifier)name).Symbol, func));
    }


    #endregion

    #region Program; ECMA 14 -------------------------------------------------------------------------------------

    public IProgram MakeProgram(Scope scope, IStatement block)
    {
      Debug.Assert(scope.IsProgram == true, "The Program scope is not properly marked");
      
      var func = MakeFunctionExpression(null, null, MakeIdentifierList(), (BlockStatement)block, scope);
      return new Program(func);
      //return MakeProgram(func);
    }

    private IProgram MakeProgram(FunctionExpression func)
    {
      func.Metadata.Scope.IsProgram = true;
      var n = new Program(func);
      return n;
    }


    #endregion

    #region Interanls
    private InternalCall MakeInternalCall(System.Reflection.MethodInfo method, List<Expression> arguments)
    {
      if (arguments == null)
        return new InternalCall(method, new List<Expression>());
      else
        return new InternalCall(method, arguments);
    }
    private InternalNew MakeInternalNew(System.Reflection.ConstructorInfo ctor, List<Expression> arguments)
    {
      if (arguments == null)
        return new InternalNew(ctor, new List<Expression>());
      else
        return new InternalNew(ctor, arguments);
    }
    #endregion

    public IExpression MakeAssignmentExpression(IExpression left, IExpression right, AssignmentOperator operation)
    {
      return MakeAssignmentExpressionInternal((Expression)left, (Expression)right, operation);
    }

    private Expression MakeAssignmentExpressionInternal(Expression left, Expression right, AssignmentOperator operation)
    {
      Expression value = null;
      switch (operation)
      {
        case AssignmentOperator.Equal: value = right; break;
        case AssignmentOperator.Multiply: value = MakeMultiplyExpressionInternal(left, right); break;
        case AssignmentOperator.Divide: value = MakeDivideExpressionInternal(left, right); break;
        case AssignmentOperator.Remainder: value = MakeRemainderExpressionInternal(left, right); break;
        case AssignmentOperator.Addition: value = MakeAdditionExpressionInternal(left, right); break;
        case AssignmentOperator.Subtraction: value = MakeSubtractionExpressionInternal(left, right); break;
        case AssignmentOperator.LeftShift: value = MakeLeftShiftExpressionInternal(left, right); break;
        case AssignmentOperator.RightShift: value = MakeRightShiftExpressionInternal(left, right); break;
        case AssignmentOperator.UnsignedRightShift: value = MakeUnsignedRightShiftExpressionInternal(left, right); break;
        case AssignmentOperator.BitwiseAnd: value = MakeBitwiseAndExpressionInternal(left, right); break;
        case AssignmentOperator.BitwiseXor: value = MakeBitwiseXorExpressionInternal(left, right); break;
        case AssignmentOperator.BitwiseOr: value = MakeBitwiseOrExpressionInternal(left, right); break;
      }

      Expression targetExp = GetEquivalent(left);

      var id = targetExp as ReadIdentifierExpression;

      Expression result;
      if (id != null)
      {
        result = MakeWriteIdentifierExpression(id.Symbol, value);
      }
      else
      {
        var indexer = targetExp as ReadIndexerExpression;
        if (indexer != null)
        {
          var container = indexer.Container;
          var index = indexer.Index;
          if (indexer.User == null)
          {
            //we are going to throw this away soon! 
            container.RemoveUser(indexer);
            index.RemoveUser(indexer);
          }
          result = MakeWriteIndexerExpression(container, index, value);
        }
        else
        {
          //this is a runtime error! 
          result = MakeCommaOperatorExpression(new List<Expression> { left, right, MakeInternalCall(CodeGen.Types.Operations.Error.ReferenceError, null)});
        }
      }

      return result;
    }

    public IExpression MakePrefixIncrement(IExpression expression)
    {
      return MakePrefixIncrementDecrement((Expression)expression, true);
    }

    public IExpression MakePrefixDecrement(IExpression expression)
    {
      return MakePrefixIncrementDecrement((Expression)expression, false);
    }

    private CommaOperatorExpression MakePrefixIncrementDecrement(Expression expression, bool isIncrement)
    {
      var oldValue = MakeToNumber(expression);

      BinaryExpression newValue;
      if (isIncrement)
        newValue = MakeAdditionExpressionInternal(oldValue, MakeIntLiteral(1));
      else
        newValue = MakeSubtractionExpressionInternal(oldValue, MakeIntLiteral(1));

      var updateExpression = MakeAssignmentExpressionInternal(expression, newValue, AssignmentOperator.Equal);
      //TODO: fixe types to make this work
      //return assignment;

      var result = MakeCommaOperatorExpression(new List<Expression>()
      {
        updateExpression,
      });

      return result;
    }

    public IExpression MakePostfixIncrement(ILeftHandSideExpression expression)
    {
      return MakePostfixIncrementDecrement((Expression)expression, true);
    }

    public IExpression MakePostfixDecrement(ILeftHandSideExpression expression)
    {
      return MakePostfixIncrementDecrement((Expression)expression, false);
    }

    private CommaOperatorExpression MakePostfixIncrementDecrement(Expression expression, bool isIncrement)
    {
      var oldValue = MakeToNumber(expression);

      BinaryExpression newValue;
      if (isIncrement)
        newValue = MakeAdditionExpressionInternal(oldValue, MakeIntLiteral(1));
      else
        newValue = MakeSubtractionExpressionInternal(oldValue, MakeIntLiteral(1));

      var updateExpression = MakeAssignmentExpressionInternal(expression, newValue, AssignmentOperator.Equal);

      var result = MakeCommaOperatorExpression(new List<Expression>()
      {
        updateExpression, //update expression, will also write oldValue
        oldValue, //return oldValue
      });

      return result;
    }
  }
}
