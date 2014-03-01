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

using m.Util.Diagnose;

namespace mjr
{
  public class Scope
  {
    #region Properties

    [Flags]
    enum Flags
    {
      None,
      HasEval = 1 << 0,
      HasLoop = 1 << 1,
      HasCall = 1 << 2,
      HasThisSymbol = 1 << 3,
      HasLocalSymbol = 1 << 4,
      HasClosedOnSymbol = 1 << 5,
      HasArgumentsSymbol = 1 << 6,
      HasParentLocalSymbol = 1 << 7,
      HasUnknownSubFunction = 1 << 8,
      IsProgram = 1 << 9,
      IsFunction = 1 << 10,
      IsEvalFunction = 1 << 11,
      IsFunctionDeclaration = 1 << 12,
      IsStrict = 1 << 13,
    }
    Flags _flags;
    bool HasFlags(Flags flags) { return (_flags & flags) != 0; }
    void SetFlags(Flags flags, bool set)
    {
      if (set)
        _flags |= flags;
      else
        _flags &= ~flags;
    }
    private void SetScopeType(Flags flag, bool value)
    {
      Debug.Assert(!HasFlags(Flags.IsProgram | Flags.IsEvalFunction | Flags.IsFunctionDeclaration), "Cannot change the scope type to {0}", flag);
      Debug.Assert(value == true, "Cannot change back the type of scope after setting it!");
      SetFlags(flag, value);
    }
    public bool HasEval { get { return HasFlags(Flags.HasEval); } set { SetFlags(Flags.HasEval, value); } }
    public bool HasLoop { get { return HasFlags(Flags.HasLoop); } set { SetFlags(Flags.HasLoop, value); } }
    public bool HasCall { get { return HasFlags(Flags.HasCall); } set { SetFlags(Flags.HasCall, value); } }
    public bool HasThisSymbol { get { return HasFlags(Flags.HasThisSymbol); } set { SetFlags(Flags.HasThisSymbol, value); } }
    public bool HasLocalSymbol { get { return HasFlags(Flags.HasLocalSymbol); } set { SetFlags(Flags.HasLocalSymbol, value); } }
    public bool HasClosedOnSymbol { get { return HasFlags(Flags.HasClosedOnSymbol); } set { SetFlags(Flags.HasClosedOnSymbol, value); } }
    public bool HasArgumentsSymbol { get { return HasFlags(Flags.HasArgumentsSymbol); } set { SetFlags(Flags.HasArgumentsSymbol, value); } }
    public bool HasParentLocalSymbol { get { return HasFlags(Flags.HasParentLocalSymbol); } set { SetFlags(Flags.HasParentLocalSymbol, value); } }
    public bool HasUnknownSubFunction { get { return HasFlags(Flags.HasUnknownSubFunction); } set { SetFlags(Flags.HasUnknownSubFunction, value); } }

    public bool IsProgram { get { return HasFlags(Flags.IsProgram); } set { SetScopeType(Flags.IsProgram | Flags.IsFunction, value); } }
    public bool IsFunction { get { return HasFlags(Flags.IsFunction); } set { SetScopeType(Flags.IsFunction, value); } }
    public bool IsEvalFunction { get { return HasFlags(Flags.IsEvalFunction); } set { SetScopeType(Flags.IsEvalFunction | Flags.IsFunction, value); } }
    public bool IsFunctionDeclaration { get { return HasFlags(Flags.IsFunctionDeclaration); } set { SetScopeType(Flags.IsFunctionDeclaration | Flags.IsFunction, value); } }
    
    public bool IsStrict { get { return HasFlags(Flags.IsStrict); } set { SetFlags(Flags.IsStrict, value); } }
    public bool IsConstContext { get { return !HasFlags(Flags.IsEvalFunction | Flags.HasEval | Flags.HasClosedOnSymbol); } }
    public int AstCost { get; set; }

    public readonly List<IR.ReturnStatement> Returns = new List<IR.ReturnStatement>();
    public readonly List<IR.Invocation> Invocations = new List<IR.Invocation>();

    #endregion

    #region Symbols

    Dictionary<string, JSSymbol> _name2symbol = new Dictionary<string, JSSymbol>(); //TODO:We can get rid of this, now that every Identifier points to its symbols. 
    List<JSSymbol> _symbols = new List<JSSymbol>(); //We use this to ensure symbols are always processed in the same order. 
    public List<JSSymbol> Symbols { get { return _symbols; } }

    public JSSymbol GetSymbol(string name)
    {
      JSSymbol symbol;
      _name2symbol.TryGetValue(name, out symbol);
      return symbol;
    }
    public JSSymbol AddSymbol(string name)
    {
      Debug.Assert(!_name2symbol.ContainsKey(name), string.Format("Symbol {0} already exists in this scope", name));
      var symbol = new JSSymbol(name, this, _symbols.Count);
      _symbols.Add(symbol);

      if (name != null)
        _name2symbol.Add(name, symbol);
      else
        symbol.SymbolType = JSSymbol.SymbolTypes.HiddenLocal;

      return symbol;
    }

    public JSSymbol GetOrAddSymbol(string name)
    {
      var symbol = GetSymbol(name);
      if (symbol == null)
        symbol = AddSymbol(name);
      return symbol;
    }

    #endregion

    /// <summary>
    /// The parent scope containing this scope
    /// </summary>
    public readonly Scope OuterScope;

    /// <summary>
    /// If this scope has an OuterScope, then OuterScope.InnerScopes[IndexInOuterScope] == this
    /// </summary>
    public readonly int IndexInOuterScope;

    /// <summary>
    /// The scopes contained within this scope
    /// </summary>
    public readonly List<Scope> InnerScopes = new List<Scope>();

    List<JSFunctionMetadata> _subFunctions;
    public JSFunctionMetadata ContainerFunction { get; private set; }
    public void SetContainer(JSFunctionMetadata container)
    {
      Debug.Assert(ContainerFunction == null, "Scope already has a container function");
      ContainerFunction = container;
      Debug.Assert(_subFunctions != null, "Invalid situation!");
      foreach (var f in _subFunctions)
      {
        f.ParentFunction = ContainerFunction; //This will set everything up!
      }
      _subFunctions = null; //From now on, we don't need to keep the list in this object!

      foreach (var s in InnerScopes)
        if (!s.IsFunction)
          s.SetContainer(container);
    }
    public void AddSubFunction(JSFunctionMetadata subFunc)
    {
      if (ContainerFunction != null)
      {
        Debug.Assert(_subFunctions == null, "Invalid situation!");
        subFunc.ParentFunction = ContainerFunction;
      }
      else
      {
        Debug.Assert(_subFunctions != null, "Invalid situation!");
        _subFunctions.Add(subFunc);
      }
    }

    /// <summary>
    /// This is only for haveing better debugger representation.
    /// </summary>
    public override string ToString() 
    {
      var sb = "";
      for(var scope = this; scope != null; scope = scope.OuterScope)
        sb = string.Format((scope.IndexInOuterScope != mdr.Runtime.InvalidIndex) ? "[{0}]::{1}" : "::{1}", scope.IndexInOuterScope, sb);
      return sb + _flags.ToString(); 
    }

    public Scope(Scope outerScope)
    {
      OuterScope = outerScope;
      if (OuterScope != null)
      {
        IndexInOuterScope = OuterScope.InnerScopes.Count;
        OuterScope.InnerScopes.Add(this);
      }
      else
        IndexInOuterScope = mdr.Runtime.InvalidIndex;

      _subFunctions = new List<JSFunctionMetadata>();
    }

    /// <summary>
    /// Merges the data of the other scope into this scope
    /// </summary>
    /// <param name="other"></param>
    public void Merge(Scope other)
    {
      Debug.Assert(ContainerFunction == null && other.ContainerFunction == null, "Cannot merge scopes that are already assigned to their container function!");
      Trace.Fail("This is not yet fully implemented, if you get here, you need to finish it");

      _flags |= other._flags;
      AstCost += other.AstCost;
      Returns.AddRange(other.Returns);
      Invocations.AddRange(other.Invocations);
      _subFunctions.AddRange(other._subFunctions);

      foreach (var otherSymbol in other.Symbols)
      {
        Debug.Assert(GetSymbol(otherSymbol.Name) == null, "Symbol {0} already exists in the scope; merge the symbols usages is not yet implemented!", otherSymbol.Name);
        
        var thisSymbol = GetOrAddSymbol(otherSymbol.Name);

        switch (thisSymbol.SymbolType)
        {
          case JSSymbol.SymbolTypes.Unknown:
            thisSymbol.SymbolType = otherSymbol.SymbolType;
            thisSymbol.ParameterIndex = otherSymbol.ParameterIndex;
            thisSymbol.SubFunctionIndex = otherSymbol.SubFunctionIndex;
            //thisSymbol.AncestorDistance = otherSymbol.AncestorDistance;
            thisSymbol.Readers.AddRange(otherSymbol.Readers);
            thisSymbol.Writers.AddRange(otherSymbol.Writers);
            break;
          default:
            throw new NotImplementedException();
        }
      }
    }
  }
}
