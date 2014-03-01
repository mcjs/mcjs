// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
using System;
using System.Text.RegularExpressions;

using m.Util.Diagnose;

namespace mjr.Builtins
{
  using mdr;
  class JSRegExp : JSBuiltinConstructor
  {
    int MaxMatchedGroupIndex = 0;
    DRegExp _lastDRegExp; //To support properties on global RegExp, like RegExp.$1
    public DRegExp LastDRegExp
    {
      get { return _lastDRegExp; }
      set
      {
        //TODO
        _lastDRegExp = value;
      }
    }
    /// <summary>
    /// JSRegExp constructor
    /// </summary>
    public JSRegExp()
      : base(mdr.Runtime.Instance.DRegExpPrototype, "RegExp")
    {
      JittedCode = ctor;

      TargetPrototype.DefineOwnProperty("exec", new mdr.DFunction(exec)
        , mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

      TargetPrototype.DefineOwnProperty("test", new mdr.DFunction(test)
        , mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

      TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction(toString)
        , mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

      TargetPrototype.DefineOwnProperty("global", new mdr.DProperty()
      {
        TargetValueType = mdr.ValueTypes.Boolean,
        //read only
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
        {
          var attval = This.FirstInPrototypeChainAs<DRegExp>().Global;
          v.Set(attval);
        },
      }, mdr.PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotConfigurable);

      TargetPrototype.DefineOwnProperty("ignoreCase", new mdr.DProperty()
      {
        TargetValueType = mdr.ValueTypes.Boolean,
        //read only
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
        {
          var attval = This.FirstInPrototypeChainAs<DRegExp>().IgnoreCase;
          v.Set(attval);
        },
      }, mdr.PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotConfigurable);

      TargetPrototype.DefineOwnProperty("multiline", new mdr.DProperty()
      {
        TargetValueType = mdr.ValueTypes.Boolean,
        //read only
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
        {
          var attval = This.FirstInPrototypeChainAs<DRegExp>().Multiline;
          v.Set(attval);
        },
      }, mdr.PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotConfigurable);

      // ECMA 262 15.10.7.1
      TargetPrototype.DefineOwnProperty("source", new mdr.DProperty()
      {
        TargetValueType = mdr.ValueTypes.String,
        //read only
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
        {
          var attval = This.FirstInPrototypeChainAs<DRegExp>().Source;
          v.Set(attval);
        },
      }, mdr.PropertyDescriptor.Attributes.NotWritable | PropertyDescriptor.Attributes.NotEnumerable | PropertyDescriptor.Attributes.NotConfigurable);

    }

    // ECMA 262 - 15.10.4
    private void ctor(ref mdr.CallFrame callFrame)
    {
      DRegExp regexp = null;
      string pattern;
      string flags;
      switch (callFrame.PassedArgsCount)
      {
        case 0:
          regexp = new DRegExp("");
          break;
        case 1:
          if (callFrame.Arg0.ValueType == mdr.ValueTypes.Object && IsRegExp(callFrame.Arg0.AsDObject()))
          {
            regexp = callFrame.Arg0.AsDObject() as DRegExp;
            if (IsConstrutor)
            {
              //We have to create a new copy
              regexp = new DRegExp(regexp.Value.ToString());
            }
          }
          else
          {
            pattern = Operations.Convert.ToString.Run(ref callFrame.Arg0);
            regexp = new DRegExp(pattern);
          }
          break;
        case 2:
          if (callFrame.Arg0.ValueType == mdr.ValueTypes.Object)
          {
            if (IsRegExp(callFrame.Arg0.AsDObject()))
              RegExpError("TypeError");
          }
          else
          {
            pattern = Operations.Convert.ToString.Run(ref callFrame.Arg0);
            flags = Operations.Convert.ToString.Run(ref callFrame.Arg1);
            regexp = new DRegExp(pattern, flags);
          }
          break;
        default:
          RegExpError("Invalid arguments in RegExp constructor");
          break;
      }

      if (IsConstrutor)
      {
        callFrame.This = (regexp);
      }
      else
        callFrame.Return.Set(regexp);
    }
    // ECMA 262 - 15.10.6.2
    private static void exec(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("calling JSRegExp.exec S {0} \n R {1}", callFrame.Arg0, callFrame.This);
      string S = Operations.Convert.ToString.Run(ref callFrame.Arg0);
      DRegExp R = callFrame.This as DRegExp;
      callFrame.Return.Set(R.ExecImplementation(S));
    }

    // ECMA 262 - 15.10.6.3
    private void test(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("calling JSRegExp.test");
      string S = Operations.Convert.ToString.Run(ref callFrame.Arg0);
      DRegExp R = callFrame.This as DRegExp;
      LastDRegExp = R;
      if (R != null && R.MatchImplementation(S) != null)
      {
        if (R.MatchedGroups.Count > MaxMatchedGroupIndex)
        {
          for (var i = 0; i < (R.MatchedGroups.Count - MaxMatchedGroupIndex); i++)
            AddNewMatchedGroup();
        }
        callFrame.Return.Set(true);
      }
      else
        callFrame.Return.Set(false);
    }

    // ECMA 262 - 15.10.6.4
    private static void toString(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("calling JSRegExp.toString");
      callFrame.Return.Set((callFrame.This as DRegExp).ToString());
    }

    public bool IsRegExp(DObject obj)
    {
      if (obj.Prototype == this.TargetPrototype) //first arg is RegExp and flags is not undefined
        return true;
      return false;
    }

    static void RegExpError(string message)
    {
      if (String.IsNullOrEmpty(message))
        throw new Exception("Unknown error in Regular expression processing!");
      else
        throw new Exception(message);
    }

    void AddNewMatchedGroup()
    {
      MaxMatchedGroupIndex++;
      //var proto = TargetPrototype;

      this.DefineOwnProperty(
          "$" + MaxMatchedGroupIndex.ToString()
          , new DMatchedGroup(MaxMatchedGroupIndex)
          , PropertyDescriptor.Attributes.Accessor
          | PropertyDescriptor.Attributes.NotEnumerable
          | PropertyDescriptor.Attributes.NotConfigurable
          | PropertyDescriptor.Attributes.NotWritable
      );
    }

    class DMatchedGroup : mdr.DProperty
    {
      public DMatchedGroup(int n)
      {
        TargetValueType = ValueTypes.String;
        OnGetString = (This) =>
        {
          return GetGroup((This as JSRegExp), n);
        };
        OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
        {
          v.Set(GetGroup(This as JSRegExp, n));
        };
      }

      string GetGroup(JSRegExp regexp, int n)
      {
        var lastregex = regexp.LastDRegExp;
        if (lastregex != null)
        {
          var groups = lastregex.MatchedGroups;
          if (groups != null && groups.Count >= n)
            return groups[n].Value;
        }
        return mdr.Runtime.Instance.DefaultDUndefined.GetTypeOf();
      }
    }
  }
}
