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
using System.Linq;
using System.Collections.Generic;

using m.Util.Diagnose;
using mjr.IR;

namespace mjr.CodeGen
{
  class TypeInferer
  {
    static AlgorithmImplementation _pool = new AlgorithmPool<TypeInfererImp>("JS/TI/", () => new TypeInfererImp(), () => JSRuntime.Instance.Configuration.ProfileJitTime);
    public static void Execute(CodeGenerationInfo cgInfo) { _pool.Execute(cgInfo); }

    class TypeInfererImp : INodeVisitor, AlgorithmImplementation
    {
      static int _passNumber = 0;
      CodeGenerationInfo _cgInfo;
      TypeCalculator _typeCalculator;
      Queue<Expression> _worklist = new Queue<Expression>();

      public mdr.ValueTypes GetType(JSSymbol symbol) { return _cgInfo.GetType(symbol); }
      public void SetType(JSSymbol symbol, mdr.ValueTypes type) { _cgInfo.SetType(symbol, type); }

      public mdr.ValueTypes GetType(Expression expression) { return _cgInfo.GetType(expression); }
      public void SetType(Expression expression, mdr.ValueTypes type) { _cgInfo.SetType(expression, type); }

      public void Execute(JSFunctionMetadata funcMetadata) { throw new NotImplementedException(); }
      public void Execute(CodeGenerationInfo cgInfo)
      {
        Debug.WriteLine("Begun TI for function {0}", cgInfo.FuncMetadata.Declaration);
        _cgInfo = cgInfo;
        _worklist.Clear();
        ++_passNumber;

        InitializeSymbolTypes();

        _typeCalculator = new TypeCalculator();
        _typeCalculator.Execute(cgInfo);

        while (_worklist.Count > 0)
        {
          var exp = _worklist.Dequeue();
          VisitNode(exp);
        }

        ///It may happen that even after TI, still some symbol.ValueType==Unknown
        ///In these cases, we just assign a default type to them and see if any types change
        foreach (var symbol in _cgInfo.FuncMetadata.Scope.Symbols)
          if (GetType(symbol) == mdr.ValueTypes.Unknown)
            SetType(symbol, mdr.ValueTypes.DValueRef);
        while (_worklist.Count > 0)
        {
          var exp = _worklist.Dequeue();
          VisitNode(exp);
        }
        Debug.WriteLine("Ended TI for function {0}", cgInfo.FuncMetadata.Declaration);
      }

      void InitializeSymbolTypes()
      {
        foreach (var symbol in _cgInfo.FuncMetadata.Scope.Symbols)
        {
          SetType(symbol, mdr.ValueTypes.Unknown); //We just want to make sure all symbols are in the hash
          switch (symbol.SymbolType)
          {
            //case JSFunctionMetadata.Symbol.SymbolTypes.Unknown:
            //    //We should treat this as Global, but the difference will be in code generation where symbol's reference should be lookedup for every access
            //    goto case JSFunctionMetadata.Symbol.SymbolTypes.Global;
            case JSSymbol.SymbolTypes.HiddenLocal:
            case JSSymbol.SymbolTypes.Local:
              ///We need to reset the readers every time since the type of locals may change with the input arguments,
              ///However, we may not need to do so for other types of symbols. 
              ///TODO: in the current form that we call TypeCalculator.Execute() everytime, we may not need to reset readers each time. 
              foreach (var exp in symbol.Readers)
                UpdateType(exp, mdr.ValueTypes.Unknown);

              if (symbol.IsParameter)
              {
                //We might have less parameters passed than declared
                var argType = _cgInfo.FuncCode.Signature.GetArgType(symbol.ParameterIndex);
                switch (argType)
                {
                  //case mdr.ValueTypes.Undefined:
                  //    break;
                  case mdr.ValueTypes.Unknown:
                    UpdateType(symbol, mdr.ValueTypes.DValueRef);
                    break;
                  default:
                    UpdateType(symbol, argType);
                    break;
                }
              }
              foreach (var exp in symbol.Writers)
                _worklist.Enqueue(exp);
              break;
            case JSSymbol.SymbolTypes.Unknown:
            case JSSymbol.SymbolTypes.ClosedOnLocal:
            case JSSymbol.SymbolTypes.ParentLocal:
            case JSSymbol.SymbolTypes.Global:
              //We assign this type everytime, but practically, only in the firs run it will be passed to its readers. 
              //We should do at least one type inference in case there are statement that only depend on this symbol.
              UpdateType(symbol, mdr.ValueTypes.DValueRef);
              break;
            case JSSymbol.SymbolTypes.Arguments:
              UpdateType(symbol, mdr.ValueTypes.Array);
              foreach (var exp in symbol.Writers)
                _worklist.Enqueue(exp);
              break;
            default:
              m.Util.Diagnose.Trace.Fail(new InvalidOperationException(string.Format("cannot process symbol type {0} in {1}", symbol.SymbolType, _cgInfo.FuncMetadata.Declaration)));
              break;
          }
        }
      }

      void Enqueue(Expression expression)
      {
        if (!_worklist.Contains(expression))
          _worklist.Enqueue(expression);
      }
      void UpdateType(JSSymbol symbol, mdr.ValueTypes type)
      {
        if (type == mdr.ValueTypes.Unknown)
          return;

        var oldType = GetType(symbol);
        var newType = TypeCalculator.ResolveType(oldType, type);

        switch (newType)
        {
          case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Unknown:
            newType = mdr.ValueTypes.DValueRef; //All of these cases are handled by a DValue
            break;
          case mdr.ValueTypes.DValue:
            if (symbol.IsParameter)
            {
              //We already have a DValue as argument, so just need to point to that and use its storage
              newType = mdr.ValueTypes.DValueRef;
            }
            break;
        }

        if (oldType != newType)
        {
          m.Util.Diagnose.Debug.WriteLine("---->> Type of the symbol {0} changed to {1}", symbol.Name.ToString(), newType.ToString());
          SetType(symbol, newType);
          //symbol.Types.Add(type);
          foreach (var r in symbol.Readers)
            Enqueue(r);
        }
      }
      void UpdateType(Expression expression, mdr.ValueTypes type)
      {
        var oldType = GetType(expression);
        var newType = TypeCalculator.ResolveType(oldType, type);
        if (oldType != newType)
        {
          m.Util.Diagnose.Debug.WriteLine("---->> Type of the expression {0} changed to {1}", expression.ToString(), newType.ToString());
          SetType(expression, newType);
          if (type == mdr.ValueTypes.Unknown)
            Enqueue(expression);
          else
            VisitUser(expression);
        }
      }

      void VisitUser(Expression expression)
      {
        var expressionValueType = GetType(expression);
        if (expressionValueType == mdr.ValueTypes.Unknown)
          Enqueue(expression); //this is not done yet
        else if (expression.User != null)
        {
          VisitNode(expression.User);
        }
      }

      bool HasTypeChanged(Expression expression, mdr.ValueTypes oldType)
      {
        var expressionValueType = GetType(expression);
        return (oldType != expressionValueType);
      }

      #region INodeVisitor Members

      void FailTypeInference(Node node)
      {
        m.Util.Diagnose.Trace.Fail(new JSSourceLocation(_cgInfo.FuncMetadata, node), "Invalid attempt to type {0}", node.GetType());
      }

      #region Statements; ECMA 12. -------------------------------------------------------------------------------------
      public override void Visit(BlockStatement node) { FailTypeInference(node); }
      public override void Visit(VariableDeclarationStatement node) { FailTypeInference(node); }
      public override void Visit(VariableDeclaration node) { }
      public override void Visit(EmptyStatement node) { FailTypeInference(node); }
      public override void Visit(ExpressionStatement node) { }
      public override void Visit(IfStatement node)
      {
      }
      public override void Visit(DoWhileStatement node)
      {
      }
      public override void Visit(WhileStatement node)
      {
      }
      public override void Visit(ForStatement node)
      {
      }
      public override void Visit(ForEachInStatement node)
      {
        JSSymbol symbol = null;
        var vds = node.Initialization as VariableDeclarationStatement;
        if (vds != null && vds.Declarations.Count > 0)
          symbol = vds.Declarations[0].Symbol;// _cgInfo.FuncMetadata.GetSymbol(vds.Identifier);
        else
        {
          //TODO: Make sure that other cases are covered!
        }
        if (symbol != null)
          SetType(symbol, mdr.ValueTypes.String);

      }
      public override void Visit(LabelStatement node)
      {
      }

      public override void Visit(GotoStatement node)
      {
      }

      public override void Visit(ContinueStatement node) { FailTypeInference(node); }
      public override void Visit(BreakStatement node) { FailTypeInference(node); }
      public override void Visit(ReturnStatement node)
      {
      }
      public override void Visit(WithStatement node) { }
      public override void Visit(SwitchStatement node)
      {
      }
      public override void Visit(CaseClause node)
      {
      }
      public override void Visit(ThrowStatement node)
      {
      }
      public override void Visit(TryStatement node)
      {
      }
      public override void Visit(CatchClause node)
      {
      }
      public override void Visit(FinallyClause node)
      {
      }

      #endregion

      #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------

      public override void Visit(ThisLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(NullLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(BooleanLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(IntLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(DoubleLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(StringLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(RegexpLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ArrayLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ObjectLiteral node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(PropertyAssignment node) { }
      public override void Visit(ParenExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(GuardedCast node)
      {
        //UpdateType(node, TypeCalculator.GetType(node, _cgInfo.FuncCode.Profiler));
        Profiler profile;
        _typeCalculator.GuardedCastProfile.TryGetValue(node, out profile);
        UpdateType(node, TypeCalculator.GetType(node, profile));
      }
      public override void Visit(InlinedInvocation node) { Trace.Fail("Not yet supported"); }

      public override void Visit(ReadIdentifierExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ReadIndexerExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ReadPropertyExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
      public override void Visit(ToPrimitive node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToBoolean node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToNumber node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToDouble node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToInteger node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToInt32 node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToUInt32 node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToUInt16 node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToString node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToObject node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(ToFunction node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
      public override void Visit(DeleteExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(VoidExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(TypeofExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(PositiveExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(NegativeExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(BitwiseNotExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(LogicalNotExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
      public override void Visit(MultiplyExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(DivideExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(RemainderExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
      public override void Visit(AdditionExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(SubtractionExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
      public override void Visit(LeftShiftExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(RightShiftExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(UnsignedRightShiftExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
      public override void Visit(LesserExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(GreaterExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(LesserOrEqualExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(GreaterOrEqualExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(InstanceOfExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(InExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
      public override void Visit(EqualExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(NotEqualExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(SameExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(NotSameExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
      public override void Visit(BitwiseAndExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(BitwiseOrExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(BitwiseXorExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion
      #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
      public override void Visit(LogicalAndExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(LogicalOrExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
      public override void Visit(TernaryExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
      public override void Visit(WriteTemporaryExpression node)
      {
        var oldType = GetType(node);
        UpdateType(node, TypeCalculator.GetType(node));
        if (HasTypeChanged(node, oldType))
        {
          VisitNodes(node.Users);
        }
      }
      public override void Visit(WriteIdentifierExpression node)
      {
        //TODO: Double check the alg
        var oldType = GetType(node);
        UpdateType(node, TypeCalculator.GetType(node));
        var newType = GetType(node);
        if (HasTypeChanged(node, oldType) || (GetType(node.Symbol) != newType))
        {
          UpdateType(node.Symbol, GetType(node));
        }
      }
      public override void Visit(WriteIndexerExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(WritePropertyExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
      public override void Visit(CommaOperatorExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
      public override void Visit(NewExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(CallExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      #endregion

      #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
      public override void Visit(FunctionExpression node) { UpdateType(node, TypeCalculator.GetType(node)); }
      public override void Visit(FunctionDeclarationStatement node) { }
      #endregion

      #region Program; ECMA 14 -------------------------------------------------------------------------------------
      public override void Visit(Program node) { FailTypeInference(node); }
      #endregion

      //TODO: Is that the right behavior?
      #region Interanls
      public override void Visit(InternalCall node) { }
      public override void Visit(InternalNew node) { }
      #endregion
      #endregion

    }

  }
}
