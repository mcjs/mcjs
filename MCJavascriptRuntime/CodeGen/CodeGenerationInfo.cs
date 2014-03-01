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
using System.Reflection.Emit;

using mjr.IR;

namespace mjr.CodeGen
{
  public class CodeGenerationInfo
  {
    public readonly JSFunctionMetadata FuncMetadata;
    public readonly JSFunctionCode FuncCode;
    internal readonly ILGen.BaseILGenerator IlGen;

    internal CodeGenerationInfo(JSFunctionMetadata funcMetadata, JSFunctionCode funcCode, ILGen.BaseILGenerator ilGen)
    {
      FuncMetadata = funcMetadata;
      FuncCode = funcCode;
      IlGen = ilGen;
    }

#if USE_FAST_SERIAL_VERSION
    readonly Dictionary<JSSymbol, mdr.ValueTypes> SymbolTypes = new Dictionary<JSSymbol, mdr.ValueTypes>();
    readonly Dictionary<Expression, mdr.ValueTypes> ExpressionTypes = new Dictionary<Expression, mdr.ValueTypes>();

    public mdr.ValueTypes GetType(JSSymbol symbol)
    {
      return SymbolTypes[symbol];
    }
    public void SetType(JSSymbol symbol, mdr.ValueTypes type)
    {
      SymbolTypes[symbol] = type;
    }

    public mdr.ValueTypes GetType(Expression expression)
    {
      mdr.ValueTypes type;
      if (!ExpressionTypes.TryGetValue(expression, out type))
        type = mdr.ValueTypes.Unknown;
      return type;
    }
    public void SetType(Expression expression, mdr.ValueTypes type)
    {
      ExpressionTypes[expression] = type;
    }
#else
    public mdr.ValueTypes GetType(JSSymbol symbol)
    {
      return symbol.ValueType;
    }
    public void SetType(JSSymbol symbol, mdr.ValueTypes type)
    {
      symbol.ValueType = type;
    }

    public mdr.ValueTypes GetType(Expression expression)
    {
      return expression.ValueType;
    }
    public void SetType(Expression expression, mdr.ValueTypes type)
    {
      expression.ValueType = type;
    }
#endif
  }
}
