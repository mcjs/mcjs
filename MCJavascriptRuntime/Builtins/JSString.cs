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
using System.Text.RegularExpressions;
using System.Collections.Generic;

using m.Util.Diagnose;

namespace mjr.Builtins
{
  class JSString : JSBuiltinConstructor
  {
    public JSString()
      : base(mdr.Runtime.Instance.DStringPrototype, "String")
    {
      JittedCode = ctor;

      // ECMA-262 section 15.5.5.1
      // "length" is already added to the DStringPrototype in Runtime.cs

      // ECMA-262 section 15.5.3.2
      this.DefineOwnProperty("fromCharCode", new mdr.DFunction(fromCharCode), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

      // ECMA-262 section 15.5.4.2
      TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction(toString), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.3
      TargetPrototype.DefineOwnProperty("valueOf", new mdr.DFunction(valueOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262, section 15.5.4.4
      TargetPrototype.DefineOwnProperty("charAt", new mdr.DFunction(charAt), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.5
      TargetPrototype.DefineOwnProperty("charCodeAt", new mdr.DFunction(charCodeAt), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262, section 15.5.4.6
      TargetPrototype.DefineOwnProperty("concat", new mdr.DFunction(concat), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.7
      TargetPrototype.DefineOwnProperty("indexOf", new mdr.DFunction(indexOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.8
      TargetPrototype.DefineOwnProperty("lastIndexOf", new mdr.DFunction(lastIndexOf), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.9
      TargetPrototype.DefineOwnProperty("localeCompare", new mdr.DFunction(localeCompare), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.10
      TargetPrototype.DefineOwnProperty("match", new mdr.DFunction(match), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262, section 15.5.4.11
      TargetPrototype.DefineOwnProperty("replace", new mdr.DFunction(replace), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.12
      TargetPrototype.DefineOwnProperty("search", new mdr.DFunction(search), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.13
      TargetPrototype.DefineOwnProperty("slice", new mdr.DFunction(slice), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.14
      TargetPrototype.DefineOwnProperty("split", new mdr.DFunction(split), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.15
      TargetPrototype.DefineOwnProperty("substring", new mdr.DFunction(substring), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      TargetPrototype.DefineOwnProperty("substr", new mdr.DFunction(substr), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.16
      TargetPrototype.DefineOwnProperty("toLowerCase", new mdr.DFunction(toLowerCase), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.17
      TargetPrototype.DefineOwnProperty("toLocaleLowerCase", new mdr.DFunction(toLocaleLowerCase), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.18
      TargetPrototype.DefineOwnProperty("toUpperCase", new mdr.DFunction(toUpperCase), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.19
      TargetPrototype.DefineOwnProperty("toLocaleUpperCase", new mdr.DFunction(toLocaleUpperCase), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
      // ECMA-262 section 15.5.4.20
      TargetPrototype.DefineOwnProperty("trim", new mdr.DFunction(trim), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
    }


    // ECMA-262 section 15.5.4.1
    void ctor(ref mdr.CallFrame callFrame)
    {
      mdr.DString str;
      if (callFrame.PassedArgsCount > 0)
      {
        str = new mdr.DString(Operations.Convert.ToString.Run(ref callFrame.Arg0));
      }
      else
        str = new mdr.DString("");

      if (IsConstrutor)
        callFrame.This = (str);
      else
        callFrame.Return.Set(str);
    }

    // ECMA-262 section 15.5.3.2
    void fromCharCode(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.fromCharCode()");
      var l = callFrame.PassedArgsCount;
      var str = new System.Text.StringBuilder();
      for (var i = 0; i < l; i++)
      {
        var arg = callFrame.Arg(i);
        str.Append(Operations.Convert.ToChar.Run(Operations.Convert.ToUInt16.Run(ref arg)));
      }

      callFrame.Return.Set(str.ToString());
    }

    // ECMA-262 section 15.5.4.2
    void toString(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.toString()");
      if (!(callFrame.This is mdr.DString))
        throw new Exception("String.prototype.toString is not generic");
      callFrame.Return.Set(callFrame.This.ToString());
    }

    // ECMA-262 section 15.5.4.3
    void valueOf(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.valueOf()");
      if (!(callFrame.This is mdr.DString))
        throw new Exception("String.prototype.valueOf is not generic");
      callFrame.Return.Set(callFrame.This.ToString());
    }

    // ECMA-262, section 15.5.4.4
    void charAt(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.charAt()");
      string thisString = callFrame.This.ToString();
      int index = callFrame.Arg0.AsInt32();
      if (index >= 0 && index < thisString.Length)
        callFrame.Return.Set(thisString.Substring(index, 1));
      else
        callFrame.Return.Set("");
    }

    // ECMA-262 section 15.5.4.5
    void charCodeAt(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.charCodeAt()");
      string thisString = callFrame.This.ToString();
      int index = Operations.Convert.ToInt32.Run(ref callFrame.Arg0);
      if (index >= 0 && index < thisString.Length)
        callFrame.Return.Set((int)thisString[index]);
      else
        callFrame.Return.Set(double.NaN);
    }

    // ECMA-262, section 15.5.4.6
    void concat(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.concat()");
      var argLen = callFrame.PassedArgsCount;
      string thisString = callFrame.This.ToString();
      if (argLen == 1)
      {
        callFrame.Return.Set(thisString + Operations.Convert.ToString.Run(ref callFrame.Arg0));
      }

      string result = thisString;
      for (var i = 0; i < argLen; i++)
      {
        var tmp = callFrame.Arg(i);
        result += Operations.Convert.ToString.Run(ref tmp);
      }

      callFrame.Return.Set(result);
    }

    // ECMA-262 section 15.5.4.7
    void indexOf(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.indexOf()");
      string thisString = callFrame.This.ToString();
      string pattern = callFrame.Arg0.AsString();
      int index = 0;
      if (callFrame.PassedArgsCount > 1)
        index = callFrame.Arg1.AsInt32();
      callFrame.Return.Set(thisString.IndexOf(pattern, index));
    }

    // ECMA-262 section 15.5.4.8
    void lastIndexOf(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.lastIndexOf()");
      string thisString = callFrame.This.ToString();
      string pattern = callFrame.Arg0.AsString();
      int index = thisString.Length - pattern.Length;
      if (callFrame.PassedArgsCount > 1)
        index = callFrame.Arg1.AsInt32();
      callFrame.Return.Set(thisString.LastIndexOf(pattern, index));
    }

    // ECMA-262 section 15.5.4.9
    void localeCompare(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.localeCompare()");
      throw new NotSupportedException("localeCompare");
    }

    // ECMA-262 section 15.5.4.10
    void match(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.match()");
      string S = callFrame.This.ToString();
      mdr.DRegExp rx = callFrame.Arg0.AsDObject() as mdr.DRegExp;
      if (rx == null)
        rx = new mdr.DRegExp(callFrame.Arg0.AsString());
      if (!rx.Global)
      {
        callFrame.Return.Set(rx.ExecImplementation(S));
        return;
      }

      mdr.DArray result = new mdr.DArray();
      int i = 0;
      foreach (Match match in (rx.Value).Matches(S))
      {
        foreach (Group group in match.Groups)
        {
          //result.SetField(i++, new mdr.DString(group.Value.ToString()));
          result.SetField(i++, group.Value);
        }
      }
      callFrame.Return.Set(result);
    }

    // ECMA-262, section 15.5.4.11
    void replace(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.replace()");
      string source = Operations.Convert.ToString.Run(callFrame.This);
      if (callFrame.PassedArgsCount == 0
        || source.Length == 0 //This means if we enter the code, we have some chars in the string and don't have to keep checking the length
        )
      {
        callFrame.Return.Set(source);
        return;
      }

      //Trace.Assert(!string.IsNullOrEmpty(source), "Invalid situation, we should have returned by now!");

      var regexp = callFrame.Arg0.As<mdr.DRegExp>();
      if (regexp != null)
      {
        int count;
        int lastIndex;

        if (regexp.Global)
        {
          count = int.MaxValue;
          lastIndex = 0;
          regexp.LastIndex = 0;
        }
        else
        {
          count = 1;
          lastIndex = Math.Max(0, regexp.LastIndex - 1);
        }

        var result = source;
        if (lastIndex < source.Length)
        {
          if (callFrame.Arg1.ValueType == mdr.ValueTypes.Function)
          {
            var function = callFrame.Arg1.AsDFunction();
            result = regexp.Value.Replace(source, (Match m) =>
            {
              mdr.CallFrame replaceCallFrame = new mdr.CallFrame();
              replaceCallFrame.Function = function;
              replaceCallFrame.This = (JSRuntime.Instance.GlobalContext);
              if (!regexp.Global)
              {
                regexp.LastIndex = m.Index + 1;
              }

              replaceCallFrame.PassedArgsCount = m.Groups.Count + 2;
              var extraArgsCount = replaceCallFrame.PassedArgsCount - mdr.CallFrame.InlineArgsCount;
              if (extraArgsCount > 0)
                replaceCallFrame.Arguments = new mdr.DValue[extraArgsCount];

              replaceCallFrame.SetArg(0, m.Value);
              int i;
              for (i = 1; i < m.Groups.Count; i++)
              {
                if (m.Groups[i].Success)
                {
                  replaceCallFrame.SetArg(i, m.Groups[i].Value);
                }
                else
                {
                  replaceCallFrame.SetArg(i, mdr.Runtime.Instance.DefaultDUndefined);
                }
              }
              replaceCallFrame.SetArg(i++, m.Index);
              replaceCallFrame.SetArg(i++, source);
              replaceCallFrame.Signature = new mdr.DFunctionSignature(ref replaceCallFrame, i);
              function.Call(ref replaceCallFrame);
              return replaceCallFrame.Return.AsString();
            }, count, lastIndex);
          }
          else
          {
            var newString = Operations.Convert.ToString.Run(ref callFrame.Arg1);
            result = regexp.Value.Replace(source, (Match m) =>
            {
              if (!regexp.Global)
              {
                regexp.LastIndex = m.Index + 1;
              }

              string after = source.Substring(Math.Min(source.Length - 1, m.Index + m.Length));
              return EvaluateReplacePattern(m.Value, source.Substring(0, m.Index), after, newString, m.Groups);
            }, count, lastIndex);
          }
        }
        callFrame.Return.Set(result);
        return;
      }
      else
      {
        string search = callFrame.Arg0.AsString();
        int index = source.IndexOf(search);
        if (index != -1)
        {
          if (callFrame.Arg1.ValueType == mdr.ValueTypes.Function)
          {
            var function = callFrame.Arg1.AsDFunction();
            mdr.CallFrame replaceCallFrame = new mdr.CallFrame();
            replaceCallFrame.Function = function;
            replaceCallFrame.This = (JSRuntime.Instance.GlobalContext);
            replaceCallFrame.SetArg(0, search);
            replaceCallFrame.SetArg(1, index);
            replaceCallFrame.SetArg(2, source);

            replaceCallFrame.PassedArgsCount = 3;
            replaceCallFrame.Signature = new mdr.DFunctionSignature(ref replaceCallFrame, 3);
            function.Call(ref replaceCallFrame);
            string replaceString = Operations.Convert.ToString.Run(ref replaceCallFrame.Return);

            callFrame.Return.Set(source.Substring(0, index) + replaceString + source.Substring(index + search.Length));
            return;
          }
          else
          {
            string replaceValue = Operations.Convert.ToString.Run(ref callFrame.Arg1);
            string before = source.Substring(0, index);
            string after = source.Substring(index + search.Length);
            string newString = EvaluateReplacePattern(search, before, after, replaceValue, null);
            callFrame.Return.Set(before + newString + after);
            return;
          }
        }
        else
        {
          callFrame.Return.Set(source);
          return;
        }
      }
    }

    // ECMA-262 section 15.5.4.12
    void search(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.search()");
      string S = callFrame.This.ToString();
      mdr.DRegExp rx = callFrame.Arg0.AsDObject() as mdr.DRegExp;
      if (rx == null)
        rx = new mdr.DRegExp(callFrame.Arg0.AsString());
      Match m = rx.Value.Match(S);

      if (m != null)
        callFrame.Return.Set(m.Index);
      else
        callFrame.Return.Set(-1);
    }

    // ECMA-262 section 15.5.4.13
    void slice(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.slice()");
      string thisString = callFrame.This.ToString();
      int length = thisString.Length;
      int start = callFrame.Arg0.AsInt32();
      int end = callFrame.PassedArgsCount < 2 ? length : callFrame.Arg1.AsInt32();
      int from = start < 0 ? Math.Max(start + length, 0) : Math.Min(start, length);
      int to = end < 0 ? Math.Max(length + end, 0) : Math.Min(end, length);
      int span = Math.Max(to - from, 0);
      callFrame.Return.Set(thisString.Substring(from, span));
    }

    // ECMA-262 section 15.5.4.14
    void split(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.split()");

      var S = Operations.Convert.ToString.Run(callFrame.This);

      mdr.DArray A = new mdr.DArray();

      var limit = (callFrame.PassedArgsCount > 1 && callFrame.Arg1.ValueType != mdr.ValueTypes.Undefined)
        ? Operations.Convert.ToUInt32.Run(ref callFrame.Arg1)
        : UInt32.MaxValue;

      if (limit == 0)
      {
        callFrame.Return.Set(A);
        return;
      }

      if (callFrame.PassedArgsCount == 0 || callFrame.Arg0.ValueType == mdr.ValueTypes.Undefined)
      {
        A.SetField(0, S);
        callFrame.Return.Set(A);
        return;
      }

      if (string.IsNullOrEmpty(S))
      {
        if (callFrame.PassedArgsCount != 0
          //&& callFrame.Arg0.ValueType != mdr.ValueTypes.Undefined
          && callFrame.Arg0.ValueType == mdr.ValueTypes.String
          && !String.IsNullOrEmpty(callFrame.Arg0.AsString()))
        {
          //A.SetField(0, new mdr.DString(""));
          A.SetField(0, "");
        }
        callFrame.Return.Set(A);
        return;
      }

      int s = S.Length;

      string[] result;
      if (callFrame.Arg0.ValueType == mdr.ValueTypes.String)
      {
        string separator = callFrame.Arg0.AsString();
        if (String.IsNullOrEmpty(separator))
        {
          int i = 0;
          result = new string[s];
          foreach (char c in S)
            result[i++] = new string(c, 1);
        }
        else
        {
          result = S.Split(new string[] { separator }, StringSplitOptions.None);
        }
      }
      else
      {
        Debug.Assert(callFrame.Arg0.ValueType == mdr.ValueTypes.Object, "Does not know what to do with argument type {0} in split", callFrame.Arg0.ValueType);
        mdr.DRegExp regexpSeparator = callFrame.Arg0.AsDObject() as mdr.DRegExp;
        Debug.Assert(regexpSeparator != null, "Does not know what to do with argument type {0} in split", callFrame.Arg0.ValueType);
        //string pattern = regexpSeparator.Value.ToString();
        //result = Regex.Split(S, pattern, RegexOptions.ECMAScript);
        result = regexpSeparator.Value.Split(S);
      }

      for (int i = 0; i < result.Length && i < limit; i++)
      {
        //A.SetField(i, new mdr.DString(result[i]));
        A.SetField(i, result[i]);
      }

      callFrame.Return.Set(A);
    }

    // ECMA-262 section 15.5.4.15
    void substring(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.substring()");
      string thisString = callFrame.This.ToString();
      int argsCount = callFrame.PassedArgsCount;
      int start = Math.Min(Math.Max(callFrame.Arg0.AsInt32(), 0), thisString.Length);
      int end = Math.Min(Math.Max((argsCount > 1) ? callFrame.Arg1.AsInt32() : thisString.Length, 0), thisString.Length);
      int from = Math.Min(start, end);
      int to = Math.Max(start, end);
      callFrame.Return.Set(thisString.Substring(from, to - from));
    }

    void substr(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.substr()");
      string thisString = callFrame.This.ToString();
      int argsCount = callFrame.PassedArgsCount;
      int start = callFrame.Arg0.AsInt32();
      if (start < 0)
      {
        int index = start + thisString.Length;
        callFrame.Return.Set(thisString.Substring(index, thisString.Length - index));
        return;
      }
      int length = (argsCount > 1) ? callFrame.Arg1.AsInt32() : thisString.Length - start;
      if (length < 0)
      {
        callFrame.Return.Set("");
        return;
      }
      length = (length - start > thisString.Length) ? thisString.Length - start : length;
      callFrame.Return.Set(thisString.Substring(start, length));
    }

    // ECMA-262 section 15.5.4.16
    void toLowerCase(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.toLowerCase()");
      string thisString = callFrame.This.ToString();
      callFrame.Return.Set(thisString.ToLowerInvariant());
    }

    // ECMA-262 section 15.5.4.17
    void toLocaleLowerCase(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.toLocaleLowerCase()");
      string thisString = callFrame.This.ToString();
      callFrame.Return.Set(thisString.ToLower());
    }

    // ECMA-262 section 15.5.4.18
    void toUpperCase(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.toUpperCase()");
      string thisString = callFrame.This.ToString();
      callFrame.Return.Set(thisString.ToUpperInvariant());
    }

    // ECMA-262 section 15.5.4.19
    void toLocaleUpperCase(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.toLocaleUpperCase()");
      string thisString = callFrame.This.ToString();
      callFrame.Return.Set(thisString.ToUpper());
    }

    // ECMA-262 section 15.5.4.20
    void trim(ref mdr.CallFrame callFrame)
    {
      Debug.WriteLine("Calling JSString.trim()");
      string thisString = callFrame.This.ToString();
      callFrame.Return.Set(thisString.Trim());
    }

    private static string EvaluateReplacePattern(string matched, string before, string after, string newString, GroupCollection groups)
    {
      if (newString.Contains("$"))
      {
        Regex rr = new Regex(@"\$\$|\$&|\$`|\$'|\$\d{1,2}", RegexOptions.Compiled);
        var res = rr.Replace(newString, delegate(Match m)
        {
          switch (m.Value)
          {
            case "$$": return "$";
            case "$&": return matched;
            case "$`": return before;
            case "$'": return after;
            default: int n = int.Parse(m.Value.Substring(1)); return n == 0 ? m.Value : groups[n].Value;
          }
        });

        return res.ToString();
      }
      return newString;
    }


  }
}
