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

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  class TypeCalculator : DepthFirstVisitor
  {
    ///The following type lattice is used
    ///
    /// DValueRef
    ///    |---------\----------------------\
    ///  String    Object                 Double
    ///              |-----------\          |--------\--------\
    ///              Function    Array    Float    Int64    UInt64
    ///                                              |        |
    ///                                            Int32    UInt32
    ///                                              |        |
    ///                                            Int16    UInt16
    ///                                              |        |------\
    ///                                            Int8     UInt8   Char
    /// file://./Types.cs
    /// 
    /// 
    internal static mdr.ValueTypes ResolveType(mdr.ValueTypes currentType, mdr.ValueTypes assignedType)
    {
      if (assignedType == mdr.ValueTypes.Unknown || currentType == assignedType)
        return assignedType;

      //We will no map to currentType, assignedType, or specific types.
      switch (currentType)
      {
        case mdr.ValueTypes.Undefined:
        case mdr.ValueTypes.Unknown:
          return assignedType;
        case mdr.ValueTypes.String:
          switch (assignedType)
          {
            case mdr.ValueTypes.String:
              return currentType;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Char:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Boolean:
          switch (assignedType)
          {
            case mdr.ValueTypes.Boolean:
              return currentType;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Float:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
              return currentType;
            case mdr.ValueTypes.Double:
              return assignedType;
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt64:
              return mdr.ValueTypes.Double;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Double:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return currentType;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Int8:
          switch (assignedType)
          {
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
              return currentType;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.UInt8:
              return mdr.ValueTypes.Int16;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Int16:
          switch (assignedType)
          {
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.UInt8:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.UInt16:
              return mdr.ValueTypes.Int32;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Int32:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.UInt32:
              return mdr.ValueTypes.Int64;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Int64:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
              return assignedType;
            case mdr.ValueTypes.UInt64:
              return mdr.ValueTypes.Double;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.UInt8:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.UInt8:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Int8:
              return mdr.ValueTypes.Int16;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.UInt16:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Int16:
              return mdr.ValueTypes.Int32;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.UInt32:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt64:
              return assignedType;
            case mdr.ValueTypes.Int32:
              return mdr.ValueTypes.Int64;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.UInt64:
          switch (assignedType)
          {
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
              return currentType;
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
              return assignedType;
            case mdr.ValueTypes.Int64:
              return mdr.ValueTypes.Double;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Object:
          switch (assignedType)
          {
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
              return currentType;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Function:
          switch (assignedType)
          {
            case mdr.ValueTypes.Function:
              return currentType;
            case mdr.ValueTypes.Object:
              return assignedType;
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
              return mdr.ValueTypes.Object;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Array:
          switch (assignedType)
          {
            case mdr.ValueTypes.Array:
              return currentType;
            case mdr.ValueTypes.Object:
              return assignedType;
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Property:
              return mdr.ValueTypes.Object;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Null:
          switch (assignedType)
          {
            case mdr.ValueTypes.Null:
              return currentType;
            case mdr.ValueTypes.Object:
              return assignedType;
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Property:
              return mdr.ValueTypes.Object;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.Property:
          switch (assignedType)
          {
            case mdr.ValueTypes.Property:
              return currentType;
            case mdr.ValueTypes.Object:
              return assignedType;
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Null:
              return mdr.ValueTypes.Object;
            case mdr.ValueTypes.Char:
            case mdr.ValueTypes.Boolean:
            case mdr.ValueTypes.Float:
            case mdr.ValueTypes.Double:
            case mdr.ValueTypes.Int8:
            case mdr.ValueTypes.Int16:
            case mdr.ValueTypes.Int32:
            case mdr.ValueTypes.Int64:
            case mdr.ValueTypes.UInt8:
            case mdr.ValueTypes.UInt16:
            case mdr.ValueTypes.UInt32:
            case mdr.ValueTypes.UInt64:
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.String:
            default:
              return mdr.ValueTypes.DValueRef;
          }
        case mdr.ValueTypes.DValue:
        case mdr.ValueTypes.DValueRef:
          return currentType;
      }
      Trace.Fail(new InvalidOperationException(string.Format("Cannot assign type {0} to {1}", assignedType, currentType)));
      return mdr.ValueTypes.DValueRef; //The most generic type!
    }

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(ThisLiteral expression) { return mdr.ValueTypes.Object; }
    internal static mdr.ValueTypes GetType(NullLiteral expression) { return mdr.ValueTypes.Null; }
    internal static mdr.ValueTypes GetType(BooleanLiteral expression) { return mdr.ValueTypes.Boolean; }
    internal static mdr.ValueTypes GetType(IntLiteral expression) { return mdr.ValueTypes.Int32; }
    internal static mdr.ValueTypes GetType(DoubleLiteral expression) { return mdr.ValueTypes.Double; }
    internal static mdr.ValueTypes GetType(StringLiteral expression) { return mdr.ValueTypes.String; }
    internal static mdr.ValueTypes GetType(RegexpLiteral expression) { return mdr.ValueTypes.Object; }

    internal static mdr.ValueTypes GetType(ArrayLiteral expression) { return mdr.ValueTypes.Array; }
    internal static mdr.ValueTypes GetType(ObjectLiteral expression) { return mdr.ValueTypes.Object; }

    internal static mdr.ValueTypes GetType(ParenExpression node) { return node.Expression.ValueType; }
    internal static mdr.ValueTypes GetType(GuardedCast node, Profiler profiler)
    {
      var expType = node.Expression.ValueType;
      if (expType != mdr.ValueTypes.DValueRef && JSRuntime.Instance.Configuration.EnableGuardElimination)
          node.IsRequired = false;
      if (expType == mdr.ValueTypes.DValueRef && JSRuntime.Instance.Configuration.EnableSpeculativeJIT && profiler != null)
      {
        var nodeProfile = profiler.GetNodeProfile(node) as GuardNodeProfile;
        var speculativeType = nodeProfile != null ? nodeProfile.GetHotPrimitiveType() : mdr.ValueTypes.DValueRef;
        if (speculativeType != mdr.ValueTypes.DValueRef)
        {
          expType = speculativeType;
        }
        if (JSRuntime.Instance.Configuration.EnableGuardElimination) node.IsRequired = true;
      }
      return expType;
    }
    internal static mdr.ValueTypes GetType(InlinedInvocation node) { return GetType(node.ReturnedValue); }

    internal static mdr.ValueTypes GetType(ReadIdentifierExpression expression) { return (expression.Writer != null) ? GetType(expression.Writer) : expression.Symbol.ValueType; }
    internal static mdr.ValueTypes GetType(ReadIndexerExpression expression) { return mdr.ValueTypes.DValueRef; }
    internal static mdr.ValueTypes GetType(ReadPropertyExpression expression) { return mdr.ValueTypes.DValueRef; }
    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(ToPrimitive expression) { return Types.Operations.Convert.ToPrimitive.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToBoolean expression) { return Types.Operations.Convert.ToBoolean.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToNumber expression) { return Types.Operations.Convert.ToNumber.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToDouble expression) { return Types.Operations.Convert.ToDouble.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToInteger expression) { return Types.Operations.Convert.ToInt32.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToInt32 expression) { return Types.Operations.Convert.ToInt32.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToUInt32 expression) { return Types.Operations.Convert.ToUInt32.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToUInt16 expression) { return mdr.ValueTypes.UInt16; }
    internal static mdr.ValueTypes GetType(ToString expression) { return Types.Operations.Convert.ToString.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToObject expression) { return Types.Operations.Convert.ToObject.ReturnType(expression.ValueType); }
    internal static mdr.ValueTypes GetType(ToFunction expression) { return mdr.ValueTypes.Function; }
    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(DeleteExpression expression) { return Types.Operations.Unary.Delete.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(VoidExpression expression) { return Types.Operations.Unary.Void.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(TypeofExpression expression) { return Types.Operations.Unary.Typeof.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(PositiveExpression expression) { return Types.Operations.Unary.Positive.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(NegativeExpression expression) { return Types.Operations.Unary.Negative.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(BitwiseNotExpression expression) { return Types.Operations.Unary.BitwiseNot.ReturnType(expression.Expression.ValueType); }
    internal static mdr.ValueTypes GetType(LogicalNotExpression expression) { return Types.Operations.Unary.LogicalNot.ReturnType(expression.Expression.ValueType); }
    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(MultiplyExpression expression) { return Types.Operations.Binary.Multiply.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(DivideExpression expression) { return Types.Operations.Binary.Divide.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(RemainderExpression expression) { return Types.Operations.Binary.Remainder.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(AdditionExpression expression) { return Types.Operations.Binary.Addition.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(SubtractionExpression expression) { return Types.Operations.Binary.Subtraction.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(LeftShiftExpression expression) { return Types.Operations.Binary.LeftShift.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(RightShiftExpression expression) { return Types.Operations.Binary.RightShift.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(UnsignedRightShiftExpression expression) { return Types.Operations.Binary.UnsignedRightShift.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(LesserExpression expression) { return Types.Operations.Binary.LessThan.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(GreaterExpression expression) { return Types.Operations.Binary.GreaterThan.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(LesserOrEqualExpression expression) { return Types.Operations.Binary.LessThanOrEqual.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(GreaterOrEqualExpression expression) { return Types.Operations.Binary.GreaterThanOrEqual.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(InstanceOfExpression expression) { return Types.Operations.Binary.InstanceOf.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(InExpression expression) { return Types.Operations.Binary.In.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(EqualExpression expression) { return Types.Operations.Binary.Equal.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(NotEqualExpression expression) { return Types.Operations.Binary.Equal.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(SameExpression expression) { return Types.Operations.Binary.Same.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(NotSameExpression expression) { return Types.Operations.Binary.Same.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(BitwiseAndExpression expression) { return Types.Operations.Binary.BitwiseAnd.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(BitwiseOrExpression expression) { return Types.Operations.Binary.BitwiseOr.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    internal static mdr.ValueTypes GetType(BitwiseXorExpression expression) { return Types.Operations.Binary.BitwiseXor.ReturnType(expression.Left.ValueType, expression.Right.ValueType); }
    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(LogicalAndExpression expression) { return GetType(expression.Implementation); }
    internal static mdr.ValueTypes GetType(LogicalOrExpression expression) { return GetType(expression.Implementation); }
    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(TernaryExpression expression) { return ResolveType(expression.Middle.ValueType, expression.Right.ValueType); }
    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(WriteTemporaryExpression expression) { return expression.Value.ValueType; }
    internal static mdr.ValueTypes GetType(WriteIdentifierExpression expression) { return expression.Value.ValueType; }
    internal static mdr.ValueTypes GetType(WriteIndexerExpression expression) { return expression.Value.ValueType; }
    internal static mdr.ValueTypes GetType(WritePropertyExpression expression) { return expression.Value.ValueType; }
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(CommaOperatorExpression expression) { return expression.Expressions.Last().ValueType; }
    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(NewExpression expression) { return mdr.ValueTypes.DValueRef; /*It may return subtypes of .Object, so we cannot be particular about iot!;*/ }
    internal static mdr.ValueTypes GetType(CallExpression expression)
    {
      if (expression.InlinedIR != null)
        return GetType(expression.InlinedIR);
      else
        if (expression.TargetFunctionMetadata != null)
        {
          //launch a new TI for the target function
          return mdr.ValueTypes.DValueRef;
        }
        else
          return mdr.ValueTypes.DValueRef;
    }
    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    internal static mdr.ValueTypes GetType(FunctionExpression expression) { return mdr.ValueTypes.Function; }
    internal static mdr.ValueTypes GetType(FunctionDeclarationStatement expression) { return mdr.ValueTypes.Function; }
    #endregion

    #region Interanls
    internal static mdr.ValueTypes GetType(InternalCall node) { return mdr.ValueTypes.Unknown; }
    internal static mdr.ValueTypes GetType(InternalNew node) { return mdr.ValueTypes.Unknown; }
    #endregion

    #region Expression visitors

    CodeGenerationInfo _cgInfo;
    Profiler _currProfile;

    /// <summary>
    /// We need to collect the profile used for each guard to be used later in the TypeInferer
    /// </summary>
    public Dictionary<GuardedCast, Profiler> GuardedCastProfile;
    HashSet<WriteTemporaryExpression> VisitedWriteTemporaries;

    public void Execute(CodeGenerationInfo cgInfo)
    {
      _cgInfo = cgInfo;
      _currProfile = _cgInfo.FuncCode.Profiler;
      GuardedCastProfile = new Dictionary<GuardedCast, Profiler>();
      VisitedWriteTemporaries = new HashSet<WriteTemporaryExpression>();

      VisitNode(cgInfo.FuncMetadata.FunctionIR.Statement);
    }

    #region Primary Expressions; ECMA 11.1 -------------------------------------------------------------------------------------
    public override void Visit(ThisLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(NullLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(BooleanLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(IntLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(DoubleLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(StringLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(RegexpLiteral node) { base.Visit(node); node.ValueType = GetType(node); }

    public override void Visit(ArrayLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ObjectLiteral node) { base.Visit(node); node.ValueType = GetType(node); }
    //public override void Visit(PropertyAssignment node) { base.Visit(node); node.ValueType = GetType(node); }

    public override void Visit(ParenExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(GuardedCast node)
    {
      base.Visit(node);
      node.ValueType = GetType(node, _currProfile);
      GuardedCastProfile[node] = _currProfile; 
    }
    public override void Visit(InlinedInvocation node)
    {
      var currProfile = _currProfile;
      _currProfile = node.TargetProfile;
      base.Visit(node);
      node.ValueType = GetType(node);
      _currProfile = currProfile;
    }

    public override void Visit(ReadIdentifierExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ReadIndexerExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ReadPropertyExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    public override void Visit(ToPrimitive node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToBoolean node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToNumber node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToDouble node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToInteger node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToInt32 node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToUInt32 node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToUInt16 node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToString node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToObject node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(ToFunction node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    public override void Visit(DeleteExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(VoidExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(TypeofExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(PositiveExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(NegativeExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(BitwiseNotExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(LogicalNotExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    public override void Visit(MultiplyExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(DivideExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(RemainderExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    public override void Visit(AdditionExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(SubtractionExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    public override void Visit(LeftShiftExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(RightShiftExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(UnsignedRightShiftExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    public override void Visit(LesserExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(GreaterExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(LesserOrEqualExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(GreaterOrEqualExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(InstanceOfExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(InExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    public override void Visit(EqualExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(NotEqualExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(SameExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(NotSameExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    public override void Visit(BitwiseAndExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(BitwiseOrExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(BitwiseXorExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    public override void Visit(LogicalAndExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(LogicalOrExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Conditional Operators; ECMA 11.12 -------------------------------------------------------------------------------------
    public override void Visit(TernaryExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Assignment; ECMA 11.13 -------------------------------------------------------------------------------------
    public override void Visit(WriteTemporaryExpression node)
    {
      if (!VisitedWriteTemporaries.Contains(node))
      {
        base.Visit(node);
        node.ValueType = GetType(node);
        VisitedWriteTemporaries.Add(node);
      }
    }
    public override void Visit(WriteIdentifierExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(WriteIndexerExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(WritePropertyExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Comma Operator; ECMA 11.14 -------------------------------------------------------------------------------------
    public override void Visit(CommaOperatorExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Function Calls; ECMA 11.2.2, 11.2.3 -------------------------------------------------------------------------------------
    public override void Visit(NewExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(CallExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Function Definition; ECMA 13 -------------------------------------------------------------------------------------
    public override void Visit(FunctionExpression node) { base.Visit(node); node.ValueType = GetType(node); }
    //public override void Visit(FunctionDeclarationStatement node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Program; ECMA 14 -------------------------------------------------------------------------------------
    //public override void Visit(Program node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #region Interanls
    public override void Visit(InternalCall node) { base.Visit(node); node.ValueType = GetType(node); }
    public override void Visit(InternalNew node) { base.Visit(node); node.ValueType = GetType(node); }
    #endregion

    #endregion
  }
}
