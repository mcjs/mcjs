// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Collections.Generic;
using mjr.IR;

namespace mjr
{
  public class JSSymbol
  {
    public enum SymbolTypes
    {
      Unknown,

      /// <summary>
      /// Declared locally and used only in this function
      /// </summary>
      Local,

      /// <summary>
      /// Declared locally and closed on by sufunctions
      /// </summary>
      ClosedOnLocal,

      /// <summary>
      /// Declared in parent function and used in this function
      /// </summary>
      ParentLocal,

      /// <summary>
      /// Declared globally and used in this function
      /// </summary>
      Global,

      /// <summary>
      /// This is the "arguments" symbol referenced in the function
      /// </summary>
      Arguments,

      /// <summary>
      /// This is added by the compiler and may not have a name (potentially should always go on stack)
      /// </summary>
      HiddenLocal,

      /// <summary>
      /// This is the name of an intrinsic function
      /// </summary>
      Intrinsic,

      /// <summary>
      /// this symbol is equivalent to some symbol in the outer scope
      /// we might be able to eliminate this kind of symbol in the parser by better managing the scopes
      /// </summary>
      OuterDuplicate,
    }
    public SymbolTypes SymbolType { get; set; }

    /// <summary>
    /// Name of the symbole
    /// </summary>
    public readonly string Name;
    
    public override string ToString() { return string.Format("{0} : {1},{2}", Name, SymbolType, ValueType); }

    /// <summary>
    /// The scope that contains this symbol
    /// </summary>
    public readonly Scope ContainerScope;

    /// <summary>
    /// Index in the function, used by different algorithms to speed up loopups, etc.
    /// </summary>
    public readonly int Index;

    /// <summary>
    /// This is the index of the symbol in the Values vector where the runtime value of this symbol is stored
    /// </summary>
    public int ValueIndex { get; set; }

    public int FieldId { get; private set; }
    public int ParameterIndex { get; set; }
    public int SubFunctionIndex { get; set; }
    //public int AncestorDistance { get; set; }
    public int NonLocalWritersCount { get; set; }

    public bool IsParameter { get { return ParameterIndex != mdr.Runtime.InvalidIndex; } }
    public bool IsLocal { get { return SymbolType == SymbolTypes.Local || SymbolType == SymbolTypes.ClosedOnLocal; } }
    public bool IsClosedOn { get { return SymbolType == SymbolTypes.ClosedOnLocal; } }

    public List<ReadIdentifierExpression> Readers = new List<ReadIdentifierExpression>();
    public List<WriteIdentifierExpression> Writers = new List<WriteIdentifierExpression>();


    /// <summary>
    /// For non local symbols, analyzer may resolve an actual symbol in an outer scope that this symbol resolves to
    /// The symbol may have a SymbolType, but still have a null ResolvedSymbol
    /// </summary>
    public JSSymbol ResolvedSymbol { get; set; }

    public JSSymbol(string name, Scope containingScope, int index)
    {
      Name = name;
      ContainerScope = containingScope;
      Index = index;

      ValueIndex = mdr.Runtime.InvalidIndex;
      ParameterIndex = mdr.Runtime.InvalidIndex;
      FieldId = mdr.Runtime.InvalidFieldId;
      SubFunctionIndex = mdr.Runtime.InvalidIndex;
      //AncestorDistance = mdr.Runtime.InvalidIndex;

      ValueType = mdr.ValueTypes.Unknown;
    }

    /// <summary>
    /// To avoid unnecessary overhead, call this function as late as possible, and only when FieldId is going to be used 
    /// </summary>
    public void AssignFieldId()
    {
      if (FieldId == mdr.Runtime.InvalidFieldId)
        FieldId = mdr.Runtime.Instance.GetFieldId(Name);
    }

    public mdr.ValueTypes ValueType { get; set; }
  }
}
