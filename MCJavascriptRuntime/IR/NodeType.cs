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

namespace mjr.IR
{
  /// <summary>
  /// There might be cases that we need an enum for node types; we can use the following. 
  /// If you add a new type, make sure it matches the name of class
  /// </summary>
  public enum NodeType
  {
    Unknown,

    #region Type Conversions; ECMA 9 -------------------------------------------------------------------------------------
    ToPrimitive,
    ToBoolean,
    ToNumber,
    ToDouble,
    ToInteger,
    ToInt32,
    ToUInt32,
    ToUInt16,
    ToString,
    ToObject,
    ToFunction,
    #endregion

    #region Unary Operators; ECMA 11.4 -------------------------------------------------------------------------------------
    DeleteExpression,
    VoidExpression,
    TypeofExpression,
    PositiveExpression,
    NegativeExpression,
    BitwiseNotExpression,
    LogicalNotExpression,
    #endregion

    #region Binary Multiplicative Operators; ECMA 11.5 -------------------------------------------------------------------------------------
    MultiplyExpression,
    DivideExpression,
    RemainderExpression,
    #endregion
    #region Binary Additive Operators; ECMA 11.6 -------------------------------------------------------------------------------------
    AdditionExpression,
    SubtractionExpression,
    #endregion
    #region Binary Bitwise Shift Operators; ECMA 11.7 -------------------------------------------------------------------------------------
    LeftShiftExpression,
    RightShiftExpression,
    UnsignedRightShiftExpression,
    #endregion
    #region Binary Relational Operators; ECMA 11.8 -------------------------------------------------------------------------------------
    LesserExpression,
    GreaterExpression,
    LesserOrEqualExpression,
    GreaterOrEqualExpression,
    InstanceOfExpression,
    InExpression,
    #endregion
    #region Binary Equality Operators; ECMA 11.9 -------------------------------------------------------------------------------------
    EqualExpression,
    NotEqualExpression,
    SameExpression,
    NotSameExpression,
    #endregion
    #region Binary Bitwise Operators; ECMA 11.10 -------------------------------------------------------------------------------------
    BitwiseAndExpression,
    BitwiseOrExpression,
    BitwiseXorExpression,
    #endregion
    #region Binary Logical Operators; ECMA 11.11 -------------------------------------------------------------------------------------
    LogicalAndExpression,
    LogicalOrExpression,
    #endregion

    ///The followings are un-used
    //AssignExpression,

    //ReadIdentifierExpression,
    //ReadIndexerExpression,

    //WriteIdentifierExpression,
    //WriteIndexerExpression,

    //TernaryExpression,
    //CommaOperatorExpression
  }
}
