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

using m.Util.Diagnose;

namespace mdr
{
  public class DFunctionCodeCache<T> where T : DFunctionCode
  {
    struct CachedCode
    {
      public int KnownArgTypesCount;
      public T Code;
    }
    List<CachedCode> _items = new List<CachedCode>();
    public List<T> Items
    {
      get {
        var retList = new List<T>();
        foreach (var i in _items)
          retList.Add(i.Code);
        return retList;
      }
    }

    public int Count { get { return _items.Count; } }

    public T Get(ref DFunctionSignature signature)
    {
      for (int i = _items.Count - 1; i >= 0; --i)
      {
        var funcCode = _items[i].Code;
        if (funcCode.MatchSignature(ref signature))
        {
          //TODO: optimizations such as: update count, bring to front, etc. 
          return funcCode;
        }
      }
      return null;
    }

    public void Add(T code)
    {
      var knownArgTypesCount = code.Signature.GetKnownArgTypesCount();
      int i = 0;
      while (i < _items.Count && knownArgTypesCount >= _items[i].KnownArgTypesCount)
      {
        Debug.Assert(code.Signature.Value != _items[i].Code.Signature.Value, "Code signature {0} already exists in the cache", code.Signature.ToString());
        ++i;
      }
      _items.Insert(i, new CachedCode() { KnownArgTypesCount = knownArgTypesCount, Code = code });
    }

    public void Remove(T code)
    {
        var knownArgTypesCount = code.Signature.GetKnownArgTypesCount();
        int i = 0;
        while (i < _items.Count)
        {
            if (_items[i].Code == code)
                _items.Remove(_items[i]);
            ++i;
        }
    }
  }
}
