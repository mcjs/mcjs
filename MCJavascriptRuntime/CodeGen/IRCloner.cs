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
using System.Text;

using m.Util.Diagnose;
using mjr.IR;
using System.Collections.Generic;

namespace mjr.CodeGen
{
  class IRCloner : IR.IClassHierarchyVisitor
  {
    /// <summary>
    /// holds the result of cloning a node after visiting it
    /// </summary>
    protected Node result;

    /// <summary>
    /// When the leafe class clones a node, it will need to pass it to the visitor of the base
    /// classes. We use the unfinisedClone node in these visitors to fill in the remaining fields
    /// </summary>
    protected Node unfinishedClone;

    protected Dictionary<Node, WriteTemporaryExpression> TempClones;

    protected IRCloner()
    {
      TempClones = new Dictionary<Node, WriteTemporaryExpression>();
    }

    protected T GetCloneOf<T>(T node) where T : Node
    {
      VisitNode(node);
      return (T)result;
    }

    protected List<T> GetCloneOf<T>(List<T> nodes) where T : Node
    {
      var cloned = new List<T>(nodes.Count);
      for (var i = 0; i < nodes.Count; ++i)
        cloned.Add(GetCloneOf(nodes[i]));
      return cloned;
    }

    //We use the following mostly for cloning any class member in base classes. 
    #region abstract classes
    protected override void Visit(Node node)
    {
      ///When we are here, it means we have reached top of class hierarcy
      ///so cloning is finished and result is ready

      result = unfinishedClone;
      unfinishedClone = null;
    }

    //protected override void Visit(Statement node)
    //{
    //  var cloned = unfinishedClone;

    //  unfinishedClone = cloned;
    //  base.Visit(node);
    //}

    //protected override void Visit(Literal node)
    //{
    //  var cloned = unfinishedClone;
    //  //
    //  unfinishedClone = cloned;
    //  base.Visit(node);
    //}

    //protected override void Visit(Reference node)
    //{
    //  var cloned = unfinishedClone;
    //  //
    //  unfinishedClone = cloned;
    //  base.Visit(node);
    //}

    //protected override void Visit(ConversionExpression node)
    //{
    //  var cloned = unfinishedClone;
    //  //
    //  unfinishedClone = cloned;
    //  base.Visit(node);
    //}

    //protected override void Visit(InternalExpression node)
    //{
    //  var cloned = unfinishedClone;
    //  //
    //  unfinishedClone = cloned;
    //  base.Visit(node);
    //}

    protected override void Visit(LoopStatement node)
    {
      base.Visit(node);
    }

    protected override void Visit(Expression node)
    {
      base.Visit(node);
    }

    protected override void Visit(Identifier node)
    {
      base.Visit(node);
    }

    protected override void Visit(Indexer node)
    {
      base.Visit(node);
    }

    protected override void Visit(UnaryExpression node)
    {
      base.Visit(node);
    }

    protected override void Visit(BinaryExpression node)
    {
      base.Visit(node);
    }

    protected override void Visit(Invocation node)
    {
      var cloned = (Invocation)unfinishedClone;

      if (cloned != null)
        cloned.TargetFunctionMetadata = node.TargetFunctionMetadata;
      else
        throw new InvalidProgramException("cloned should not be null");

      unfinishedClone = cloned;
      base.Visit(node);
    }

    protected override void Visit(InternalInvocation node)
    {
      base.Visit(node);
    }

    #endregion


    #region Statements; ECMA 12. -------------------------------------------------------------------------------------
    public override void Visit(BlockStatement node)
    {
      // TODO: Check for infinite BlockDepth?
      result = new BlockStatement(GetCloneOf(node.Statements), node.SourceOffset);
    }

    public override void Visit(VariableDeclarationStatement node)
    {
      result = new VariableDeclarationStatement(GetCloneOf(node.Declarations));
    }

    public override void Visit(VariableDeclaration node)
    {
      Trace.Fail("Cannot direclty clone VariableDeclaration");
    }

    public override void Visit(EmptyStatement node)
    {
      result = new EmptyStatement(node.SourceOffset);
    }

    public override void Visit(ExpressionStatement node)
    {
      result = new ExpressionStatement(GetCloneOf(node.Expression));
    }

    public override void Visit(IfStatement node)
    {
      result = new IfStatement((ToBoolean)GetCloneOf(node.Condition), GetCloneOf(node.Then), GetCloneOf(node.Else));
    }

    public override void Visit(DoWhileStatement node)
    {
      unfinishedClone = new DoWhileStatement((ToBoolean)GetCloneOf(node.Condition), GetCloneOf(node.Body));
      base.Visit(node);
    }

    public override void Visit(WhileStatement node)
    {
      unfinishedClone = new WhileStatement((ToBoolean)GetCloneOf(node.Condition), GetCloneOf(node.Body));
      base.Visit(node);
    }

    public override void Visit(ForStatement node)
    {
      unfinishedClone = new ForStatement(
        GetCloneOf(node.Initialization)
        , (ToBoolean)GetCloneOf(node.Condition)
        , GetCloneOf(node.Increment)
        , GetCloneOf(node.Body));
      base.Visit(node);
    }

    public override void Visit(ForEachInStatement node)
    {
      unfinishedClone = new ForEachInStatement(
        GetCloneOf(node.Initialization)
        , (ToObject)GetCloneOf(node.Expression)
        , GetCloneOf(node.OriginalBody)
        , GetCloneOf(node.IteratorInitialization)
        , (ToBoolean)GetCloneOf(node.Condition)
        , GetCloneOf(node.ExtendedBody));
      base.Visit(node);
    }

    public override void Visit(LabelStatement node)
    {
      result = new LabelStatement(node.Name, GetCloneOf(node.Target));
    }

    public override void Visit(GotoStatement node)
    {
      result = new GotoStatement(node.Target);
    }

    public override void Visit(ContinueStatement node)
    {
      unfinishedClone = new ContinueStatement(node.Target);
      base.Visit(node);
    }

    public override void Visit(BreakStatement node)
    {
      unfinishedClone = new BreakStatement(node.Target);
      base.Visit(node);
    }

    public override void Visit(ReturnStatement node)
    {
      result = new ReturnStatement(GetCloneOf(node.Expression));
    }

    public override void Visit(WithStatement node)
    {
      result = new WithStatement(GetCloneOf(node.Expression), GetCloneOf(node.Statement));
    }

    public override void Visit(SwitchStatement node)
    {
      result = new SwitchStatement(GetCloneOf(node.Expression), GetCloneOf(node.CaseClauses));
    }

    public override void Visit(CaseClause node)
    {
      result = new CaseClause(GetCloneOf(node.Expression), GetCloneOf(node.Body));
    }

    public override void Visit(ThrowStatement node)
    {
      result = new ThrowStatement(GetCloneOf(node.Expression));
    }

    public override void Visit(TryStatement node)
    {
      result = new TryStatement(GetCloneOf(node.Statement), GetCloneOf(node.Catch), GetCloneOf(node.Finally));
    }

    public override void Visit(CatchClause node)
    {
      result = new CatchClause(null, GetCloneOf(node.Identifier), GetCloneOf(node.Statement));
    }

    public override void Visit(FinallyClause node)
    {
      result = new FinallyClause(GetCloneOf(node.Statement));
    }

    #endregion

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
    public override void Visit(ThisLiteral node)
    {
      unfinishedClone = new ThisLiteral(node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(NullLiteral node)
    {
      unfinishedClone = new NullLiteral(node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(BooleanLiteral node)
    {
      unfinishedClone = new BooleanLiteral(node.Value, node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(IntLiteral node)
    {
      unfinishedClone = new IntLiteral(node.Value, node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(DoubleLiteral node)
    {
      unfinishedClone = new DoubleLiteral(node.Value, node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(StringLiteral node)
    {
      unfinishedClone = new StringLiteral(node.Value, node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(RegexpLiteral node)
    {
      unfinishedClone = new RegexpLiteral(node.Regexp, node.Options, node.SourceOffset);
      base.Visit(node);
    }

    public override void Visit(ArrayLiteral node)
    {
      result = new ArrayLiteral(GetCloneOf(node.Items), node.SourceOffset);
    }

    public override void Visit(ObjectLiteral node)
    {
      result = new ObjectLiteral(GetCloneOf(node.Properties), node.SourceOffset);
    }

    public override void Visit(PropertyAssignment node)
    {
      result =
        (node.Expression != null)
        ? new PropertyAssignment(node.Name, GetCloneOf(node.Expression))
        : new PropertyAssignment(node.Name, GetCloneOf(node.GetFunction), GetCloneOf(node.SetFunction))
      ;
    }


    public override void Visit(ParenExpression node)
    {
      unfinishedClone = new ParenExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(GuardedCast node)
    {
      var clone = new GuardedCast(GetCloneOf(node.Expression));
      clone.ProfileIndex = node.ProfileIndex;
      unfinishedClone = clone;
      base.Visit(node);
    }


    public override void Visit(ReadIdentifierExpression node)
    {
      Trace.Fail("Cannot directly clone ReadIdentifier");
    }

    public override void Visit(ReadIndexerExpression node)
    {
      var clone = new ReadIndexerExpression(GetCloneOf(node.Container), GetCloneOf(node.Index));
      clone.ProfileIndex = node.ProfileIndex; //TODO: should this be moved to Indexer?
      unfinishedClone = clone;
      base.Visit(node);
    }

    public override void Visit(ReadPropertyExpression node)
    {
      unfinishedClone = new ReadPropertyExpression(GetCloneOf(node.Container), (StringLiteral)GetCloneOf(node.Index));
      Visit((Indexer)node); //We need to skip ReadIndexerExpression
      // TODO: Why are we visiting Indexer?
    }

    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    public override void Visit(ToPrimitive node)
    {
      unfinishedClone = new ToPrimitive(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToBoolean node)
    {
      unfinishedClone = new ToBoolean(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToNumber node)
    {
      unfinishedClone = new ToNumber(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToDouble node)
    {
      unfinishedClone = new ToDouble(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToInteger node)
    {
      unfinishedClone = new ToInteger(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToInt32 node)
    {
      unfinishedClone = new ToInt32(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToUInt32 node)
    {
      unfinishedClone = new ToUInt32(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToUInt16 node)
    {
      unfinishedClone = new ToUInt16(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToString node)
    {
      unfinishedClone = new ToString(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToObject node)
    {
      unfinishedClone = new ToObject(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(ToFunction node)
    {
      unfinishedClone = new ToFunction(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    public override void Visit(DeleteExpression node)
    {
      unfinishedClone = new DeleteExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(VoidExpression node)
    {
      unfinishedClone = new VoidExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(TypeofExpression node)
    {
      unfinishedClone = new TypeofExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(PositiveExpression node)
    {
      unfinishedClone = new PositiveExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(NegativeExpression node)
    {
      unfinishedClone = new NegativeExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(BitwiseNotExpression node)
    {
      unfinishedClone = new BitwiseNotExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    public override void Visit(LogicalNotExpression node)
    {
      unfinishedClone = new LogicalNotExpression(GetCloneOf(node.Expression));
      base.Visit(node);
    }

    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    public override void Visit(MultiplyExpression node)
    {
      unfinishedClone = new MultiplyExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(DivideExpression node)
    {
      unfinishedClone = new DivideExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(RemainderExpression node)
    {
      unfinishedClone = new RemainderExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    public override void Visit(AdditionExpression node)
    {
      unfinishedClone = new AdditionExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(SubtractionExpression node)
    {
      unfinishedClone = new SubtractionExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    public override void Visit(LeftShiftExpression node)
    {
      unfinishedClone = new LeftShiftExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(RightShiftExpression node)
    {
      unfinishedClone = new RightShiftExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(UnsignedRightShiftExpression node)
    {
      unfinishedClone = new UnsignedRightShiftExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    public override void Visit(LesserExpression node)
    {
      unfinishedClone = new LesserExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(GreaterExpression node)
    {
      unfinishedClone = new GreaterExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(LesserOrEqualExpression node)
    {
      unfinishedClone = new LesserOrEqualExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(GreaterOrEqualExpression node)
    {
      unfinishedClone = new GreaterOrEqualExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(InstanceOfExpression node)
    {
      unfinishedClone = new InstanceOfExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(InExpression node)
    {
      unfinishedClone = new InExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    public override void Visit(EqualExpression node)
    {
      unfinishedClone = new EqualExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(NotEqualExpression node)
    {
      unfinishedClone = new NotEqualExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(SameExpression node)
    {
      unfinishedClone = new SameExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(NotSameExpression node)
    {
      unfinishedClone = new NotSameExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    public override void Visit(BitwiseAndExpression node)
    {
      unfinishedClone = new BitwiseAndExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(BitwiseOrExpression node)
    {
      unfinishedClone = new BitwiseOrExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    public override void Visit(BitwiseXorExpression node)
    {
      unfinishedClone = new BitwiseXorExpression(GetCloneOf(node.Left), GetCloneOf(node.Right));
      base.Visit(node);
    }

    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    public override void Visit(LogicalAndExpression node)
    {
      var implementation = GetCloneOf(node.Implementation);
      unfinishedClone = new LogicalAndExpression(implementation.Left, implementation.Middle, implementation);
      base.Visit(node);
    }

    public override void Visit(LogicalOrExpression node)
    {
      var implementation = GetCloneOf(node.Implementation);
      unfinishedClone = new LogicalOrExpression(implementation.Left, implementation.Right, implementation);
      base.Visit(node);
    }

    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
    public override void Visit(TernaryExpression node)
    {
      result = new TernaryExpression(GetCloneOf(node.Left), GetCloneOf(node.Middle), GetCloneOf(node.Right));
    }

    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    public override void Visit(WriteTemporaryExpression node)
    {
      if (TempClones.ContainsKey(node))
      {
        result = TempClones[node];
      }
      else
      {
        var cloned = new WriteTemporaryExpression((Expression)GetCloneOf(node.Value));
        TempClones.Add(node, cloned);
        result = cloned;
      }
    }

    public override void Visit(WriteIdentifierExpression node)
    {
      Trace.Fail("Cannot directly clone WriteIdentifierExpression");
    }

    public override void Visit(WriteIndexerExpression node)
    {
      unfinishedClone = new WriteIndexerExpression(GetCloneOf(node.Container), GetCloneOf(node.Index), GetCloneOf(node.Value));
      base.Visit(node);
    }

    public override void Visit(WritePropertyExpression node)
    {
      unfinishedClone = new WritePropertyExpression(GetCloneOf(node.Container), GetCloneOf(node.Index), GetCloneOf(node.Value));
      Visit((Indexer)node); //we need to skip over WriteIndexerExpression
    }

    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    public override void Visit(CommaOperatorExpression node)
    {
      result = new CommaOperatorExpression(GetCloneOf(node.Expressions));
    }

    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    public override void Visit(NewExpression node)
    {
      unfinishedClone = new NewExpression((ToFunction)GetCloneOf(node.Function), GetCloneOf(node.Arguments));
      base.Visit(node);
    }

    public override void Visit(CallExpression node)
    {
      var clone = new CallExpression(
        (ToFunction)GetCloneOf(node.Function)
        , GetCloneOf(node.ThisArg)
        , GetCloneOf(node.Arguments)
        , node.IsDirectEvalCall
        );
      clone.ProfileIndex = node.ProfileIndex;
      unfinishedClone = clone;
      base.Visit(node);
    }

    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    public override void Visit(FunctionExpression node)
    {
      result = new FunctionExpression(null, GetCloneOf(node.Name), GetCloneOf(node.Parameters), GetCloneOf(node.Statement));
    }

    public override void Visit(FunctionDeclarationStatement node)
    {
      result = new FunctionDeclarationStatement(GetCloneOf(node.Expression), GetCloneOf(node.Implementation));
    }

    #endregion

    #region Program; ECMA 14 -------------------------------------------------------------------------------------
    public override void Visit(Program node)
    {
      result = new Program(GetCloneOf(node.Expression));
    }

    #endregion

    #region Internals
    public override void Visit(InternalCall node)
    {
      unfinishedClone = new InternalCall(node.Method, GetCloneOf(node.Arguments));
      base.Visit(node);
    }

    public override void Visit(InternalNew node)
    {
      unfinishedClone = new InternalNew(node.Constructor, GetCloneOf(node.Arguments));
      base.Visit(node);
    }
    #endregion
  }

}
