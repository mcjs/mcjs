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
using System.Reflection.Emit;

using m.Util.Diagnose;

namespace mjr.CodeGen
{
  /// <summary>
  /// This is an struct because it is only used in the Code Gen classes and is not passed around
  /// </summary>
  internal struct LocalVarManager
  {
    ILGen.BaseILGenerator _ilGen;

    class AvailableLocals
    {
      LocalBuilder[] _items;

      internal int Count = 0;

      internal void Add(LocalBuilder l)
      {
        if (_items == null || Count >= _items.Length)
          Array.Resize<LocalBuilder>(ref _items, Count + 1);
        _items[Count++] = l;
      }

      internal LocalBuilder Get()
      {
        if (_items == null || Count == 0)
          return null;
        return _items[--Count];
      }

      int IndexOf(LocalBuilder l)
      {
        if (_items != null)
          for (var i = Count - 1; i >= 0; --i)
            if (_items[i] == l)
              return i;
        return -1;
      }

      internal bool Contains(LocalBuilder l) { return IndexOf(l) != -1; }

      internal void Remove(LocalBuilder l)
      {
        var i = IndexOf(l);
        Debug.Assert(i != -1, "Invalid situation! local variable is not in the list");
        --Count;
        for (; i < Count; ++i)
          _items[i] = _items[i + 1];
      }
    }

    /// <summary>
    /// locals that can be reused
    /// </summary>
    Dictionary<Type, AvailableLocals> _availableLocals;

    Dictionary<JSSymbol, LocalBuilder> _symbolLocals;
    Dictionary<string, LocalBuilder> _namedLocals;
    Dictionary<IR.WriteTemporaryExpression, LocalBuilder> _unnamedLocals;
    List<LocalBuilder> _temporaryStackLocals;


    internal void Init(ILGen.BaseILGenerator ilGen)
    {
      _ilGen = ilGen;

      if (_availableLocals == null)
      {
        //First time initialization
        _availableLocals = new Dictionary<Type, AvailableLocals>();
        _symbolLocals = new Dictionary<JSSymbol, LocalBuilder>();
        _namedLocals = new Dictionary<string, LocalBuilder>();
        _unnamedLocals = new Dictionary<IR.WriteTemporaryExpression, LocalBuilder>();
        _temporaryStackLocals = new List<LocalBuilder>();
      }
      else
        Debug.Assert(
          _availableLocals.Count == 0
          && _symbolLocals.Count == 0
          && _namedLocals.Count == 0
          && _unnamedLocals.Count == 0
          && _temporaryStackLocals.Count == 0
          , "LocalVarManager was not cleared properly in the last use!");
    }

    internal void Clear()
    {
      _ilGen = null;
      _availableLocals.Clear();
      _symbolLocals.Clear();
      _namedLocals.Clear();
      _unnamedLocals.Clear();

      Debug.Assert(_temporaryStackLocals.Count == 0, "Invalid situation, some temporaries are left on the stack! You need to cleanup!");
      _temporaryStackLocals.Clear();
    }

    private AvailableLocals GetAvailableLocalListOfType(Type type, bool addMissing = false)
    {
      AvailableLocals locals;
      if (!_availableLocals.TryGetValue(type, out locals) && addMissing)
      {
        locals = new AvailableLocals();
        _availableLocals.Add(type, locals);
      }
      return locals;
    }
    private LocalBuilder GetAvailableLocalOfType(Type type)
    {
      var locals = GetAvailableLocalListOfType(type, false);
      var local = (locals != null && locals.Count > 0) ? locals.Get() : null;
      return local;
    }

    //###############################################################################################################################
    // General local handling
    internal LocalBuilder Declare(mdr.ValueTypes type) { return Declare(Types.TypeOf(type)); }
    internal LocalBuilder Declare(Type type)
    {
      var local = GetAvailableLocalOfType(type);
      if (local == null)
        local = _ilGen.DeclareLocal(type);
      return local;
    }

    private void Release(LocalBuilder l)
    {
      var locals = GetAvailableLocalListOfType(l.LocalType, true);
      Debug.Assert(!locals.Contains(l), "Invalid situation! Local variable was already released before!");
      locals.Add(l);
    }


    //###############################################################################################################################
    // Named local handling
    internal LocalBuilder Declare(mdr.ValueTypes type, string name) { return Declare(Types.TypeOf(type), name); }
    internal LocalBuilder Declare(Type type, string name)
    {
      //Get(name) is not cheap, we want to write the following ASSERT in a way that it goes away in release
      Debug.Assert(Get(name) == null, "Variable {0} already has a local variable of type {0}", name, ((Get(name) != null) ? Get(name).LocalType.ToString() : ""));

      var local = GetAvailableLocalOfType(type);
      if (local == null)
        local = _ilGen.DeclareLocal(type, name);
      _namedLocals.Add(name, local);
      return local;
    }

    private LocalBuilder Get(string name)
    {
      Debug.Assert(!string.IsNullOrEmpty(name), "invalid empty name for named local variable");
      LocalBuilder local;
      _namedLocals.TryGetValue(name, out local);
      return local;
    }

    internal LocalBuilder GetOrDeclare(Type type, string name)
    {
      var local = Get(name);
      if (local == null)
        local = Declare(type, name);
      else
        Debug.Assert(local.LocalType == type, "Named local {0}:{1} is already defined and cannot be redefined with type {2}", name, local.LocalType, type);
      return local;
    }

    private void Release(string name)
    {
      var local = Get(name);
      Debug.Assert(local != null, "Named local variable {0} does not exist", name);
      _namedLocals.Remove(name);
      Release(local);
    }

    //###############################################################################################################################
    // Symbols' local handling
    internal LocalBuilder Declare(mdr.ValueTypes type, JSSymbol symbol) { return Declare(Types.TypeOf(type), symbol); }
    internal LocalBuilder Declare(Type type, JSSymbol symbol) { return Declare(Declare(type/*, symbol.Name*/), symbol); }
    internal LocalBuilder Declare(LocalBuilder local, JSSymbol symbol)
    {
      //Get(symbol) is not cheap, we want to write the following ASSERT in a way that it goes away in release
      Debug.Assert(Get(symbol) == null, "Symbol {0} already has a local variable of type {1}", symbol.Name, ((Get(symbol) != null) ? Get(symbol).LocalType.ToString() : ""));
      _symbolLocals.Add(symbol, local);
      return local;
    }

    internal LocalBuilder Get(JSSymbol symbol)
    {
      LocalBuilder local;
      _symbolLocals.TryGetValue(symbol, out local);
      return local;
    }

    private void Release(JSSymbol symbol)
    {
      //Technically we should never call this function, it will be unsafe!
      var local = Get(symbol);
      Debug.Assert(local != null, "Symbol {0} does not have a local variable assigned to", symbol.Name);
      _symbolLocals.Remove(symbol);
      Release(symbol.Name);
    }


    //###############################################################################################################################
    // Unnamed local handling
    internal LocalBuilder Declare(mdr.ValueTypes type, IR.WriteTemporaryExpression temporary) { return Declare(Types.TypeOf(type), temporary); }
    internal LocalBuilder Declare(Type type, IR.WriteTemporaryExpression temporary)
    {
      Debug.Assert(Get(temporary) == null, "temporary {0} alread has a local variable assigned to it", temporary);
      var local = Declare(type);
      _unnamedLocals.Add(temporary, local);
      return local;
    }

    internal LocalBuilder Get(IR.WriteTemporaryExpression temporary)
    {
      LocalBuilder local;
      _unnamedLocals.TryGetValue(temporary, out local);
      return local;
    }

    private void Release(IR.WriteTemporaryExpression temporary)
    {
      var local = Get(temporary);
      Debug.Assert(local != null, "temporary {0} does not have a local variable assigned to it", temporary);
      _unnamedLocals.Remove(temporary);
      Release(local);
    }


    //###############################################################################################################################
    // temporary local handling
    internal LocalBuilder PushTemporary(mdr.ValueTypes type) { return PushTemporary(Types.TypeOf(type)); }
    internal LocalBuilder PushTemporary(Type type)
    {
      var local = Declare(type);
      _temporaryStackLocals.Add(local);
      return local;
    }
    internal LocalBuilder PushTemporary(LocalBuilder l)
    {
      var locals = GetAvailableLocalListOfType(l.LocalType, false);
      Debug.Assert(locals != null && locals.Contains(l), "Invalid situation! Local variable is already reserved and cannot be used as temporary!");
      locals.Remove(l);
      _temporaryStackLocals.Add(l);
      return l;
    }

    internal int GetTemporaryStackState() { return _temporaryStackLocals.Count; }

    internal void PopTemporary(LocalBuilder l)
    {
      var index = _temporaryStackLocals.Count - 1;
      Debug.Assert(_temporaryStackLocals[index] == l, "Invalid situation");
      Release(l);
      _temporaryStackLocals.RemoveAt(index);
    }

    internal void PopTemporariesAfter(int stackState)
    {
      //We rather have the last variable user first to help code gen reuse values immediately
      for (var i = stackState; i < _temporaryStackLocals.Count; ++i)
        Release(_temporaryStackLocals[i]);

      _temporaryStackLocals.RemoveRange(stackState, _temporaryStackLocals.Count - stackState);
    }
  }

}
