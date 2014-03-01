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
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  class AutoJSOperations : Target
  {
    Target.Writer Write;

    public override string Filename { get { return "AutoJSOperations.cs"; } }

    enum JSTypes { Undefined, Null, Boolean, String, Number, Object };

    struct ReturnData
    {
      public string type;
      public bool Aggressive;
    };

    class TypeInfo
    {
      public string TypeEnum { get; set; }
      public string Accessor { get; set; }
      public string BasicType { get; set; }
      public JSTypes JSType { get; set; }
      public bool IsUndefined { get { return JSType == JSTypes.Undefined; } }
      public bool IsNull { get { return JSType == JSTypes.Null; } }
      public bool IsBoolean { get { return JSType == JSTypes.Boolean; } }
      public bool IsString { get { return JSType == JSTypes.String; } }
      public bool IsNumber { get { return JSType == JSTypes.Number; } }
      public bool IsDouble { get { return JSType == JSTypes.Number && BasicType == "double"; } }
      public bool IsInt { get { return JSType == JSTypes.Number && BasicType == "int"; } }
      public bool IsUInt { get { return JSType == JSTypes.Number && BasicType == "uint"; } }
      public bool IsUShort { get { return JSType == JSTypes.Number && BasicType == "ushort"; } }
      public bool IsULong { get { return JSType == JSTypes.Number && BasicType == "ulong"; } }
      public bool IsChar { get { return JSType == JSTypes.String && BasicType == "char"; } }
      public bool IsObject { get { return JSType == JSTypes.Object; } }
      public bool IsFunction { get { return JSType == JSTypes.Object && BasicType == "mdr.DFunction"; } }
      public bool IsArray { get { return JSType == JSTypes.Object && BasicType == "mdr.DArray"; } }

      public TypeInfo(string typeEnum, string accessor, string basicType, JSTypes jsType)
      {
        TypeEnum = typeEnum;
        Accessor = accessor;
        BasicType = basicType;
        JSType = jsType;
      }
    }

    class TypesInfo : List<TypeInfo>
    {
      public void Add(string typeEnum, string accessor, string basicType, JSTypes jsType) { Add(new TypeInfo(typeEnum, accessor, basicType, jsType)); }
    }
    static TypesInfo Types = new TypesInfo()
    {
      {"Undefined", "mjr.JSRuntime.Instance.DefaultDUndefined", "mdr.DUndefined", JSTypes.Undefined},
      {"Null", "mjr.JSRuntime.Instance.DefaultDNull", "mdr.DNull", JSTypes.Null},
      {"Boolean", ".AsBoolean()", "bool", JSTypes.Boolean},
      {"String", ".AsString()", "string", JSTypes.String},
      {"Char", ".AsChar()", "char", JSTypes.String},
      {"Float", ".AsFloat()", "float", JSTypes.Number},
      {"Double", ".AsDouble()", "double", JSTypes.Number},
      {"Int8", ".AsInt8()", null, JSTypes.Number},
      {"Int16", ".AsInt16()", null, JSTypes.Number},
      {"Int32", ".AsInt32()", "int", JSTypes.Number},
      {"Int64", ".AsInt64()", "long", JSTypes.Number},
      {"UInt8", ".AsUInt8()", null, JSTypes.Number},
      {"UInt16", ".AsUInt16()", "ushort", JSTypes.Number},
      {"UInt32", ".AsUInt32()", "uint", JSTypes.Number},
      {"UInt64", ".AsUInt64()", "ulong", JSTypes.Number},
      {"Object", ".AsDObject()", "mdr.DObject", JSTypes.Object},
      {"Function", ".AsDFunction()", "mdr.DFunction", JSTypes.Object},
      {"Array", ".AsDArray()", "mdr.DArray", JSTypes.Object},
    };

    class UnaryReturnType
    {
      Dictionary<TypeInfo, string> _returnTypes = new Dictionary<TypeInfo, string>();
      public string this[TypeInfo t]
      {
        get
        {
          string type = null;
          _returnTypes.TryGetValue(t, out type);
          return type;
        }
        set
        {
          _returnTypes[t] = value;
        }
      }
    }

    class BinaryReturnType
    {
      Dictionary<TypeInfo, Dictionary<TypeInfo, string>> _returnTypes = new Dictionary<TypeInfo, Dictionary<TypeInfo, string>>();
      public string this[TypeInfo t0, TypeInfo t1]
      {
        get
        {
          string type = null;
          Dictionary<TypeInfo, string> map = null;
          if (_returnTypes.TryGetValue(t0, out map))
            map.TryGetValue(t1, out type);
          return type;
        }
        set
        {
          Dictionary<TypeInfo, string> map = null;
          if (!_returnTypes.TryGetValue(t0, out map))
          {
            map = new Dictionary<TypeInfo, string>();
            _returnTypes[t0] = map;
          }
          map[t1] = value;
        }
      }
    }


    static string ResolveType(string t0, string t1)
    {
      if (t0 == t1) return t0;
      switch (t0)
      {
        //case "char": return t1;
        case "double": return t0;
        case "float": return t0;
        case "ushort":
        case "int": switch (t1)
          {
            case "int": return "int";
            case "ushort":
            case "long":
            case "uint": return "long";
            case "ulong":
            case "double": return "double";
            case "float": return "float";
            default: return t0;
          }
        case "long": switch (t1)
          {
            case "ushort":
            case "ulong":
            case "double": return "double";
            case "float": return "float";
            default: return t0;
          }
        case "uint": switch (t1)
          {
            case "ushort": return "double";
            case "int": return "long";
            case "long":
            case "ulong":
            case "double": return t1;
            case "float": return t1;
            default: return t0;
          }
        case "ulong": switch (t1)
          {
            case "ushort":
            case "long":
            case "double": return "double";
            case "float": return "float";
            default: return t0;
          }
      }
      throw new InvalidOperationException(string.Format("cannot resolve types {0} & {1}", t0, t1));
    }
    bool _generateCommentedStubs = false;

    protected override void Generate(Target.Writer Write)
    {
      this.Write = Write;

      Write(@"
using System.Runtime.CompilerServices;          
");

      #region Binay Operations

      OpenNamespace("mjr.Operations.Binary");
      WriteBinaryOperation("Multiply", null, null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;
        
        if (T0.IsNumber && T1.IsNumber)
        {
          var retType = ResolveType(T0.BasicType, T1.BasicType);
          
          if (retType == "char" || retType == "ushort")
            output.Write("return ({0})(({0})i0 * ({0})i1);", retType);
          else
            output.Write("return ({0})i0 * ({0})i1;", retType);

          data.type = retType;
        }

        return data;
      });

      WriteBinaryOperation("Divide", "double", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsDouble && T1.IsDouble)
        {
          output.Write("return i0 / i1;");
          data.type = "double";
        }

        return data;
      });

      WriteBinaryOperation("Remainder", "double", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsDouble && T1.IsDouble)
        {
          output.Write("return i0 % i1;");
          data.type = "double";
        }

        return data;
      });

      WriteBinaryOperation("Addition", null, null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue); Run(ref pValue, i1, ref result); ");
          data.type = "void";
          data.Aggressive = false;
          return data;
        }
        else if (T1.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i1, ref pValue); Run(i0, ref pValue, ref result); ");
          data.type = "void";
          data.Aggressive = false;
          return data;
        }
        else if (T0.IsString || T1.IsString)
        {
          output.Write("return Convert.ToString.Run(i0) + Convert.ToString.Run(i1);");
          data.type = "string";
          return data;
        }
        else if (T0.IsUndefined || T1.IsUndefined)
        {
          output.Write("return double.NaN;");
          data.type = "double";
          return data;
        }
        else if (T0.IsNull)
        {
          if (T1.IsBoolean)
          {
            output.Write("return Convert.ToNumber.Run(i1);");
            data.type = "sbyte";
            return data;
          }
          else
          {
            output.Write("return i1;");
            data.type = T1.BasicType;
            return data;
          }
        }
        else if (T1.IsNull)
        {
          if (T0.IsBoolean)
          {
            output.Write("return Convert.ToNumber.Run(i0);");
            data.type = "sbyte";
            return data;
          }
          else
          {
            output.Write("return i0;");
            data.type = T0.BasicType;
            return data;
          }
        }
        else if (T0.IsBoolean && T1.IsBoolean)
        {
          output.Write("return (sbyte)(Convert.ToNumber.Run(i0) + Convert.ToNumber.Run(i1));");
          data.type = "sbyte";
          return data;
        }
        else if (T0.IsBoolean && T1.IsNumber)
        {
          output.Write("return Run(({0})Convert.ToNumber.Run(i0), i1);", T1.BasicType);
          data.type = T1.BasicType;
          return data;
        }
        else if (T1.IsBoolean && T0.IsNumber)
        {
          output.Write("return Run(i0, ({0})Convert.ToNumber.Run(i1));", T0.BasicType);
          data.type = T0.BasicType;
          return data;
        }
        else if (T0.IsNumber && T1.IsNumber)
        {
          var retType = ResolveType(T0.BasicType, T1.BasicType);
          output.Write("return ({0})(({0})i0 + ({0})i1);", retType);
          data.type = retType;
          return data;
        }

        return data;
      });

      WriteBinaryOperation("Subtraction", null, null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue); Run(ref pValue, i1, ref result); ");
          data.Aggressive = false;
          data.type = "void";
          return data;
        }
        else if (T1.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i1, ref pValue); Run(i0, ref pValue, ref result); ");
          data.Aggressive = false;
          data.type = "void";
          return data;
        }
        else if (T0.IsUndefined || T1.IsUndefined)
        {
          output.Write("return double.NaN;");
          data.type = "double";
          return data;
        }
        else if (T0.IsNull)
        {
          output.Write("return i1;");
          data.type = T1.BasicType;
          return data;
        }
        else if (T1.IsNull)
        {
          output.Write("return i0;");
          data.type = T0.BasicType;
          return data;
        }
        else if (T0.IsString || T1.IsString)
        {
          output.Write("return Convert.ToDouble.Run(i0) - Convert.ToDouble.Run(i1);"); //TODO: we can use to ToNumber but then have to use "void" as return type
          data.type = "double";
          return data;
        }
        else if (T0.IsBoolean && T1.IsBoolean)
        {
          output.Write("return (sbyte)((i0 ? 1 : 0) - (i1 ? 1 : 0));");
          data.type = "sbyte";
          return data;
        }
        else if (T0.IsBoolean && T1.IsNumber)
        {
          output.Write("return ({0})(({0})(i0 ? 1 : 0) - i1);", T1.BasicType);
          data.type = T1.BasicType;
          return data;
        }
        else if (T1.IsBoolean && T0.IsNumber)
        {
          output.Write("return ({0})(i0 - ({0})(i1 ? 1 : 0));", T0.BasicType);
          data.type = T0.BasicType;
          return data;
        }
        else if (T0.IsNumber && T1.IsNumber)
        {
          var retType = ResolveType(T0.BasicType, T1.BasicType);
          output.Write("return ({0})(({0})i0 - ({0})i1);", retType);
          data.type = retType;
          return data;
        }

        return data;
      });

      WriteBinaryOperation("LeftShift", "int", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt && T1.IsUInt)
        {
          output.Write("return i0 << (int)(i1 & 0x1F);");
          data.type = "int";
        }

        return data;
      });

      WriteBinaryOperation("RightShift", "int", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt && T1.IsUInt)
        {
          output.Write("return i0 >> (int)(i1 & 0x1f);");
          data.type = "int";
        }

        return data;
      });

      WriteBinaryOperation("UnsignedRightShift", "uint", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsUInt && T1.IsUInt)
        {
          output.Write("return i0 >> (int)(i1 & 0x1F);");
          data.type = "uint";
        }

        return data;
      });

      WriteBinaryOperation("Compare", "Result", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (
          (T0.IsObject && T1.IsObject)
          || (T0.IsString && T1.IsString && !T0.IsChar && !T1.IsChar)
          || (T0.IsDouble && T1.IsDouble)
          )
          return data; //The custom cases

        if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue); return Run(ref pValue, i1); ");
        else if (T1.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i1, ref pValue); return Run(i0, ref pValue); ");
        //else if (T0.IsString && T1.IsString)
        //  output.Write("return (String.Compare(i0, i1) < 0) ? Result.True : Result.False;");
        else if (T0.IsBoolean)
          output.Write("return Run(Convert.ToNumber.Run(i0), i1);");
        else if (T1.IsBoolean)
          output.Write("return Run(i0, Convert.ToNumber.Run(i1));");
        else if (T0.IsChar && T1.IsChar)
          output.Write("return (i0 < i1 ? Result.True : Result.False);");
        else if (T0.IsNumber && T1.IsNumber)
          output.Write("return ((({0})i0) < (({0})i1) ? Result.True : Result.False);", ResolveType(T0.BasicType, T1.BasicType));
        else
          output.Write("return Run(Convert.ToDouble.Run(i0), Convert.ToDouble.Run(i1));");

        data.type = "Result";
        return data;
      });

      WriteBinaryOperation("LessThan", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsObject && T1.IsObject)
        {
          output.Write("return Compare.Run(i0, i1, true) == Compare.Result.True;");
        }
        else
        {
          output.Write("return Compare.Run(i0, i1) == Compare.Result.True;");
        }

        return data;
      });

      WriteBinaryOperation("GreaterThan", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsObject && T1.IsObject)
        {
          output.Write("return Compare.Run(i1, i0, false) == Compare.Result.True;");
        }
        else
        {
          output.Write("return Compare.Run(i1, i0) == Compare.Result.True;");
        }

        return data;
      });

      WriteBinaryOperation("LessThanOrEqual", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsObject && T1.IsObject)
        {
          output.Write("return Compare.Run(i1, i0, false) == Compare.Result.False;");
        }
        else
        {
          output.Write("return Compare.Run(i1, i0) == Compare.Result.False;");
        }

        return data;
      });

      WriteBinaryOperation("GreaterThanOrEqual", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsObject && T1.IsObject)
        {
          output.Write("return Compare.Run(i0, i1, true) == Compare.Result.False;");
        }
        else
        {
          output.Write("return Compare.Run(i0, i1) == Compare.Result.False;");
        }

        return data;
      });


      WriteBinaryOperation("InstanceOf", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsObject && T1.IsObject)
          return data; //The custom cases

        if (T1.IsObject)
        {
          output.Write("return false;");
        }
        else
        {
          output.Write("Error.TypeError(); return false;");
        }

        data.type = "bool";
        return data;
      });

      WriteBinaryOperation("In", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T1.IsObject)
        {
          output.Write("var pd = i1.GetPropertyDescriptor(i0); return (pd != null) && (!pd.IsUndefined);");
        }
        else
        {
          output.Write("Error.TypeError(); return false;");
        }

        return data;
      });

      WriteBinaryOperation("Equal", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsUndefined || T0.IsNull)
        {
          if (T1.IsUndefined || T1.IsNull)
            output.Write("return true;");
          else
            output.Write("return false;");
        }
        else if (T1.IsUndefined || T1.IsNull)
        {
          if (T0.IsUndefined || T0.IsNull)
            output.Write("return true;");
          else
            output.Write("return false;");
        }
        else if (T0.IsNumber && T1.IsNumber)
        {
          if (T0.IsDouble)
            if (T1.IsDouble)
              output.Write("return !double.IsNaN(i0) && !double.IsNaN(i1) && i0 == i1;");
            else
              output.Write("return !double.IsNaN(i0) && Run(i0, Convert.ToDouble.Run(i1));");
          else
            if (T1.IsDouble)
              output.Write("return !double.IsNaN(i1) && Run(Convert.ToDouble.Run(i0), i1);");
            else
              output.Write("return ({0})i0 == ({0})i1;", ResolveType(T0.BasicType, T1.BasicType));
        }
        else if (T0.IsObject && T1.IsObject)
        {
          output.Write("return System.Object.ReferenceEquals(i0, i1);");
        }
        else if (T0 == T1)
        {
          output.Write("return i0 == i1;");
        }
        else if (T0.IsString && T1.IsString)
        {//They are not the same, so we are dealing with string & char
          output.Write("return Run(Convert.ToString.Run(i0), Convert.ToString.Run(i1));");
        }
        else if ((T0.IsNumber && T1.IsString) || T1.IsBoolean)
          output.Write("return Run(i0, Convert.ToDouble.Run(i1));"); //TODO: use ToNumnber?
        else if ((T0.IsString && T1.IsNumber) || T0.IsBoolean)
          output.Write("return Run(Convert.ToDouble.Run(i0), i1);"); //TODO: use ToNumnber?
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue); return Run(ref pValue, i1); ");
        }
        else if (T1.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i1, ref pValue); return Run(i0, ref pValue); ");
        }
        else
        {
          data.type = null;
        }

        return data;
      });

      WriteBinaryOperation("NotEqual", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        output.Write("return !Equal.Run(i0, i1);");
        return data;
      });

      WriteBinaryOperation("Same", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0 == T1 || (T0.IsNumber && T1.IsNumber))
          output.Write("return Equal.Run(i0, i1);");
        else
          output.Write("return false;");

        return data;
      });

      WriteBinaryOperation("NotSame", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        output.Write("return !Same.Run(i0, i1);");
        return data;
      });

      WriteBinaryOperation("BitwiseAnd", "int", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt && T1.IsInt)
        {
          output.Write("return i0 & i1;");
          data.type = "int";
        }

        return data;
      });

      WriteBinaryOperation("BitwiseOr", "int", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt && T1.IsInt)
        {
          output.Write("return i0 | i1;");
          data.type = "int";
        }

        return data;
      });

      WriteBinaryOperation("BitwiseXor", "int", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt && T1.IsInt)
        {
          output.Write("return i0 ^ i1;");
          data.type = "int";
        }

        return data;
      });
      ////WriteBinaryOperation("LogicalAnd", null);
      ////WriteBinaryOperation("LogicalOr", null);
      CloseNamespace();

      #endregion

      #region Unary Operations

      OpenNamespace("mjr.Operations.Convert");

      WriteUnaryOperation("ToPrimitive", null, ", bool stringHint = false", (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (!T0.IsObject)
        {
          output.Write("return i0;");
          data.type = T0.BasicType;
        }

        return data;
      });

      WriteUnaryOperation("ToBoolean", "bool", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        if (T0.IsUndefined || T0.IsNull)
          output.Write("return false;");
        else if (T0.IsBoolean)
          output.Write("return i0;");
        else if (T0.IsDouble)
          output.Write("return !double.IsNaN(i0) && (i0 != 0);");
        else if (T0.IsNumber || T0.IsChar)
          output.Write("return i0 != 0;");
        else if (T0.IsString)
          output.Write("return !string.IsNullOrEmpty(i0);");
        else if (T0.IsObject)
          output.Write("return true;");
        else
          output.Write("return false;");

        return data;
      });

      WriteUnaryOperation("ToNumber", null, null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsUndefined)
        {
          output.Write("return double.NaN;");
          data.type = "double";
        }
        else if (T0.IsNull)
        {
          output.Write("return 0;");
          data.type = "sbyte";
        }
        else if (T0.IsBoolean)
        {
          output.Write("return (sbyte)(i0 ? 1 : 0);");
          data.type = "sbyte";
        }
        else if (T0.IsString)
        {
          if (T0.IsChar)
            output.Write("JSParser.ParseNumber(Convert.ToString.Run(i0), ref result);");
          else
            output.Write("JSParser.ParseNumber(i0, ref result);");
          data.type = "void";
        }
        else if (T0.IsNumber)
        {
          output.Write("return i0;");
          data.type = T0.BasicType;
        }
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); Run(ref pValue, ref result);");
          data.type = "void";
        }

        return data;
      });

      WriteUnaryOperation("ToDouble", "double", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "double";

        if (T0.IsUndefined)
          output.Write("return double.NaN;");
        else if (T0.IsNull)
          output.Write("return 0;");
        else if (T0.IsBoolean)
          output.Write("return i0 ? 1 : 0;");
        else if (T0.IsNumber)
          output.Write("return i0;");
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        }
        else if (T0.IsString)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToNumber.Run(i0, ref pValue); return Run(ref pValue);");
        }
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToFloat", "float", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "float";

        if (T0.IsUndefined)
          output.Write("return float.NaN;");
        else if (T0.IsNull)
          output.Write("return 0;");
        else if (T0.IsBoolean)
          output.Write("return i0 ? 1 : 0;");
        else if (T0.IsDouble)
          output.Write("return (float)i0;");
        else if (T0.IsNumber)
          output.Write("return i0;");
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        }
        else if (T0.IsString)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToNumber.Run(i0, ref pValue); return Run(ref pValue);");
        }
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToInt32", "int", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "int";
        
        if (T0.IsUndefined || T0.IsNull)
          output.Write("return 0;");
        else if (T0.IsBoolean)
          output.Write("return i0 ? 1 : 0;");
        else if (T0.IsNumber)
        {
          if (T0.IsDouble)
            output.Write("return (double.IsNaN(i0) || double.IsInfinity(i0))? 0 : (int)i0;");
          else
            // output.Write("return Convert.ToInt32(i0);");
            data.type = null;
        }
        else if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        else if (T0.IsString)
          output.Write("var pValue = new mdr.DValue(); Convert.ToNumber.Run(i0, ref pValue); return Run(ref pValue);");
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToUInt16", "ushort", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "ushort";

        if (T0.IsUndefined || T0.IsNull)
          output.Write("return 0;");
        else if (T0.IsBoolean)
          output.Write("return (ushort)(i0 ? 1 : 0);");
        else if (T0.IsNumber)
            output.Write("return (ushort)i0;");
        else if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        else if (T0.IsString)
          output.Write("var pValue = new mdr.DValue(); Convert.ToNumber.Run(i0, ref pValue); return Run(ref pValue);");
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToChar", "char", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "char";

        if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        else if (T0.IsNumber)
          //output.Write("return Convert.ToChar(i0);");
          data.type = null;
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToUInt32", "uint", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "uint";

        if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        else if (T0.IsString)
          output.Write("var number = new mdr.DValue(); Convert.ToNumber.Run(i0, ref number); return Run(ref number);");
        else if (T0.IsNumber)
        {
          if (T0.IsDouble)
            output.Write("return (double.IsNaN(i0) || double.IsInfinity(i0))? 0 : (uint)(i0);");
          else
            output.Write("return (uint)(i0);");
        }
        else
          output.Write("return (uint)(Convert.ToNumber.Run(i0));");

        return data;
      });

      WriteUnaryOperation("ToInt64", "long", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "long";

        if (T0.IsUndefined || T0.IsNull)
          output.Write("return 0;");
        else if (T0.IsBoolean)
          output.Write("return i0 ? 1 : 0;");
        else if (T0.IsNumber)
          data.type = null;
        else if (T0.IsObject)
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); return Run(ref pValue);");
        else if (T0.IsString)
          output.Write("var number = new mdr.DValue(); Convert.ToNumber.Run(i0, ref number); return Run(ref number);");
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToString", "string", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "string";

        if (T0.IsUndefined)
          output.Write(@"return ""undefined"";");
        else if (T0.IsNull)
          output.Write(@"return ""null"";");
        else if (T0.IsBoolean)
          output.Write(@"return i0 ? ""true"" : ""false"";");
        else if (T0.IsNumber || T0.IsChar)
          output.Write(@"return System.Convert.ToString(i0);");
        else if (T0.IsString)
          output.Write(@"return i0;");
        else if (T0.IsObject)
          output.Write(@"var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, true); return Run(ref pValue);");
        return data;
      });

      WriteUnaryOperation("ToObject", "mdr.DObject", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "mdr.DObject";

        if (T0.IsUndefined)
          output.Write("Error.TypeError(); return null;");
        else if (T0.IsString)
          if (T0.IsChar)
            output.Write("var obj = new mdr.DString(Convert.ToString.Run(i0)); return obj;");
          else
            output.Write("var obj = new mdr.DString(i0); return obj;");
        else if (T0.IsBoolean)
          output.Write("var obj = new mdr.DObject(mdr.Runtime.Instance.DBooleanMap); obj.PrimitiveValue.Set(i0); return obj;");
        else if (T0.IsNumber)
          output.Write("var obj = new mdr.DObject(mdr.Runtime.Instance.DNumberMap); obj.PrimitiveValue.Set(i0); return obj;");
        else if (T0.IsObject && !T0.IsArray && !T0.IsFunction)
        {
          output.Write("return i0;");
        }
        else
          data.type = null; // custom case

        return data;
      });

      WriteUnaryOperation("ToFunction", "mdr.DFunction", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "mdr.DFunction";

        if (T0.IsObject)
        {
          if (T0.IsFunction)
            output.Write("return i0;");
          else
            output.Write("return i0.ToDFunction();");
        }
        else
          output.Write("Error.TypeError(); return null;");

        return data;
      });

      CloseNamespace();

      OpenNamespace("mjr.Operations.Unary");

      WriteUnaryOperation("Delete", "bool", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "bool";

        output.Write("return true;");
        return data;
      });
      WriteBinaryOperation("DeleteProperty", "bool", null, (T0, T1, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsObject)
        {
          output.Write("return (i0.DeletePropertyDescriptor(i1) != mdr.PropertyMap.DeleteStatus.NotDeletable);");
          data.type = "bool";
        }

        return data;
      });

      WriteUnaryOperation("Void", "mdr.DUndefined", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "mdr.DUndefined";

        output.Write("return mjr.JSRuntime.Instance.DefaultDUndefined;");
        return data;
      });

      WriteUnaryOperation("Typeof", "string", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = "string";
        
        if (T0.IsUndefined)
          output.Write(@"return ""undefined"";");
        else if (T0.IsNull)
          output.Write(@"return ""object"";");
        else if (T0.IsBoolean)
          output.Write(@"return ""boolean"";");
        else if (T0.IsNumber)
          output.Write(@"return ""number"";");
        else if (T0.IsString)
          output.Write(@"return ""string"";");
        else if (T0.IsObject)
        {
          if (T0.IsArray)
            output.Write(@"return ""object"";");
          else if (T0.IsFunction)
            output.Write(@"return ""function"";");
          else
            output.Write(@"return (i0 is mdr.DFunction) ? ""function"" : ""object"";");
        }

        return data;
      });

      WriteUnaryOperation("Positive", null, null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        //This is the copy of the ToNumber!
        if (T0.IsUndefined)
        {
          output.Write("return double.NaN;");
          data.type = "double";
        }
        else if (T0.IsNull)
        {
          output.Write("return 0;");
          data.type = "sbyte";
        }
        else if (T0.IsBoolean)
        {
          output.Write("return (sbyte)(i0 ? 1 : 0);");
          data.type = "sbyte";
        }
        else if (T0.IsString)
        {
          output.Write("return mjr.Operations.Convert.ToDouble.Run(i0);");
          data.type = "double";
        }
        else if (T0.IsNumber)
        {
          output.Write("return i0;");
          data.type = T0.BasicType;
        }
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); Run(ref pValue, ref result);");
          data.type = "void";
        }

        return data;
      });

      WriteUnaryOperation("Negative", null, null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        //This is the copy of the ToNumber!
        if (T0.IsUndefined)
        {
          output.Write("return double.NaN;");
          data.type = "double";
        }
        else if (T0.IsNull)
        {
          output.Write("return 0;");
          data.type = "sbyte";
        }
        else if (T0.IsBoolean)
        {
          output.Write("return (sbyte)(i0 ? -1 : 0);");
          data.type = "sbyte";
        }
        else if (T0.IsString)
        {
          output.Write("return -Convert.ToDouble.Run(i0);");
          data.type = "double";
        }
        else if (T0.IsNumber)
        {
          if (T0.IsULong || T0.IsUShort)
          {
            output.Write("return -(double)i0;");
            data.type = "double";
          }
          else
          {
            output.Write("return -i0;");
            if (T0.IsUInt)
              data.type = "long";
            else
              data.type = T0.BasicType;
          }
        }
        else if (T0.IsObject)
        {
          output.Write("var pValue = new mdr.DValue(); Convert.ToPrimitive.Run(i0, ref pValue, false); Run(ref pValue, ref result);");
          data.type = "void";
        }

        return data;
      });

      WriteUnaryOperation("BitwiseNot", "int", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsInt)
        {
          output.Write("return ~i0;");
          data.type = "int";
        }

        return data;
      });

      WriteUnaryOperation("LogicalNot", "bool", null, (T0, output) =>
      {
        ReturnData data = new ReturnData();
        data.Aggressive = true;
        data.type = null;

        if (T0.IsBoolean)
        {
          output.Write("return !i0;");
          data.type = "bool";
        }

        return data;
      });

      CloseNamespace();

      #endregion

    }

    void OpenNamespace(string fullNamespace)
    {
      Write(@"
namespace ${fullNamespace}
{
".FormatWith(new
 {
   fullNamespace = fullNamespace
 }));
    }

    void CloseNamespace()
    {
      Write(@"
}
");
    }

    void WriteUnaryOperation(string name, string returnType, string argsSuffix = null, Func<TypeInfo, System.IO.TextWriter, ReturnData> genBody = null)
    {
      Write(@"
  public static partial class ${ClassName}
  {
".FormatWith(new
 {
   ClassName = name,
 }));

      var returnTypes = new UnaryReturnType();

      #region T Run(T0 i0)
      if (genBody != null)
      {
        Write(@"
    #region /**** Customized implementations *****************************************/
");
        foreach (var t0 in Types)
        {
          if (string.IsNullOrEmpty(t0.BasicType)) continue;

          var body = new System.IO.StringWriter();
          ReturnData variantReturnType = genBody(t0, body);
          returnTypes[t0] = variantReturnType.type;
          string Comment = "";
          if (variantReturnType.type == null && _generateCommentedStubs)
          {
            Comment = "//";
            variantReturnType.type = "T";
            body.Write("throw new NotImplementedException();");
          }

          // If the Aggressive flag is false, comment out the flags
          if (!variantReturnType.Aggressive)
          {
            Comment = "//";
          }

          if (variantReturnType.type != null)
            Write(@"

    ${Comment}[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static ${ReturnType} Run(${Arg0Type} i0${Result}) { ${Body} }
".FormatWith(new
 {
   Comment = Comment,
   ReturnType = returnType ?? variantReturnType.type,
   Arg0Type = t0.BasicType,
   Result = ", ref mdr.DValue result".If(returnType == null && returnTypes[t0] == "void"),
   Body = body.ToString(),
 }));
        }
        Write(@"
    #endregion /**** Customized implementations *****************************************/
");
      }
      #endregion


      #region Run(/*const*/ ref mdr.DValue i0)
      Write(@"
    #region /**** generic implementations *****************************************/
");
      Write(@"
    private static ${ReturnType} Run<T0>(T0 i0) { throw new System.NotImplementedException(string.Format(""Method {0}.Run({1}) is not implemented"", typeof(${ClassName}).FullName, typeof(T0).FullName)); }
    private static void Run<T0>(T0 i0, ref mdr.DValue result) { throw new System.NotImplementedException(string.Format(""Method {0} is not implemented"", (new System.Diagnostics.StackFrame()).GetMethod())); }

".FormatWith(new
 {
   ClassName = name,
   ReturnType = returnType ?? "mdr.DUndefined" //It is going to throw, so does not matter what type!
 }));

      Write(@"
    public static ${ReturnType} Run(/*const*/ ref mdr.DValue i0${ResultArg}${ArgsSuffix})
    {
      switch (i0.ValueType)
      {
".FormatWith(new
 {
   ReturnType = returnType ?? "void",
   ResultArg = ", ref mdr.DValue result".If(returnType == null),
   ArgsSuffix = argsSuffix,
 }));

      foreach (var t in Types)
      {
        var snippet = (returnType != null)
          ? @"
        case mdr.ValueTypes.${TypeEnum}: return Run(${Arg}${Value});
"
          : (returnTypes[t] == "void")
          ? @"
        case mdr.ValueTypes.${TypeEnum}: Run(${Arg}${Value}, ref result); break;
"
          : @"
        case mdr.ValueTypes.${TypeEnum}: result.Set(Run(${Arg}${Value})); break;
";
        Write(snippet.FormatWith(new
        {
          TypeEnum = t.TypeEnum,
          Arg = "i0".If(t.Accessor.StartsWith(".")),
          Value = t.Accessor
        }));
      }
      Write(@"
        default:
          throw new System.InvalidOperationException(string.Format(""Invalid operand type {0}"", i0.ValueType));
      }
    }

");
      Write(@"
    #endregion /**** generic implementations *****************************************/
");
      #endregion

      Write(@"
  }
");
    }

    void WriteBinaryOperation(string name, string returnType, string argsSuffix = null, Func<TypeInfo, TypeInfo, System.IO.TextWriter, ReturnData> genBody = null)
    {
      Write(@"
  public static partial class ${ClassName}
  {
".FormatWith(new
 {
   ClassName = name,
 }));

      var returnTypes = new BinaryReturnType();

      #region T Run(T0 i0, T1 i1)

      if (genBody != null)
      {
        Write(@"
    #region /**** Customized implementations *****************************************/
");
        foreach (var t0 in Types)
        {
          if (string.IsNullOrEmpty(t0.BasicType)) continue;

          foreach (var t1 in Types)
          {
            if (string.IsNullOrEmpty(t1.BasicType)) continue;

            var body = new System.IO.StringWriter();
            var variantReturnType = genBody(t0, t1, body);
            returnTypes[t0, t1] = variantReturnType.type;

            string Comment = "";
            if (variantReturnType.type == null && _generateCommentedStubs)
            {
              Comment = "//";
              variantReturnType.type = "T";
              body.Write("throw new NotImplementedException();");
            }

            // If the Aggressive flag is false, comment out the flags
            if (!variantReturnType.Aggressive)
            {
              Comment = "//";
            }

            if (variantReturnType.type != null)
              Write(@"

    ${Comment}[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static ${ReturnType} Run(${Arg0Type} i0, ${Arg1Type} i1${Result}) { ${Body} }
".FormatWith(new
 {
   Comment = Comment,
   ReturnType = returnType ?? variantReturnType.type,
   Arg0Type = t0.BasicType,
   Arg1Type = t1.BasicType,
   Result = ", ref mdr.DValue result".If(returnType == null && returnTypes[t0, t1] == "void"),
   Body = body.ToString(),
 }));
          }
        }
        Write(@"
    #endregion /**** Customized implementations *****************************************/
");
      }
      #endregion


      Write(@"
    #region /**** generic implementations *****************************************/
");
      Write(@"
    private static ${ReturnType} Run<T0, T1>(T0 i0, T1 i1) { throw new System.NotImplementedException(string.Format(""Method {0}.Run({1}, {2}) is not implemented"", typeof(${ClassName}).FullName, typeof(T0).FullName, typeof(T1).FullName)); }
    private static void Run<T0, T1>(T0 i0, T1 i1, ref mdr.DValue result) { throw new System.NotImplementedException(string.Format(""Method {0} is not implemented"", (new System.Diagnostics.StackFrame()).GetMethod())); }

".FormatWith(new
 {
   ClassName = name,
   ReturnType = returnType ?? "mdr.DUndefined" //It is going to throw, so does not matter what type!
 }));

      #region Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1)

      Write(@"
    public static ${ReturnType} Run(/*const*/ ref mdr.DValue i0, /*const*/ ref mdr.DValue i1${ResultArg}${ArgsSuffix})
    {
      switch (i0.ValueType)
      {
".FormatWith(new
 {
   ClassName = name,
   ReturnType = returnType ?? "void",
   ResultArg = ", ref mdr.DValue result".If(returnType == null),
   ArgsSuffix = argsSuffix,
 }));

      foreach (var t in Types)
      {
        var snippet = (returnType != null)
          ? @"
        case mdr.ValueTypes.${TypeEnum}: return Run(${Arg}${Value}, ref i1);
"
          : @"
        case mdr.ValueTypes.${TypeEnum}: Run(${Arg}${Value}, ref i1, ref result); break;
";
        Write(snippet.FormatWith(new
        {
          TypeEnum = t.TypeEnum,
          Arg = "i0".If(t.Accessor.StartsWith(".")),
          Value = t.Accessor
        }));
      }
      Write(@"
        default:
          throw new System.InvalidOperationException(string.Format(""Invalid operand type {0}"", i0.ValueType));
      }
    }

");

      #endregion

      #region T Run(/*const*/ ref mdr.DValue i0, T1 i1)

      foreach (var t1 in Types)
      {
        if (string.IsNullOrEmpty(t1.BasicType)) continue;

        Write(@"
    public static ${ReturnType} Run(/*const*/ ref mdr.DValue i0, ${Arg1Type} i1${ResultArg}${ArgsSuffix})
    {
      switch (i0.ValueType)
      {
".FormatWith(new
 {
   ReturnType = returnType ?? "void",
   Arg1Type = t1.BasicType,
   ResultArg = ", ref mdr.DValue result".If(returnType == null),
   ArgsSuffix = argsSuffix,
 }));

        foreach (var t in Types)
        {
          var snippet = (returnType != null)
            ? @"
        case mdr.ValueTypes.${TypeEnum}: return Run(${Arg}${Value}, i1);
"
            : (returnTypes[t, t1] == "void")
            ? @"
        case mdr.ValueTypes.${TypeEnum}: Run(${Arg}${Value}, i1, ref result); break;
"
            : @"
        case mdr.ValueTypes.${TypeEnum}: result.Set(Run(${Arg}${Value}, i1)); break;
";
          Write(snippet.FormatWith(new
          {
            TypeEnum = t.TypeEnum,
            Arg = "i0".If(t.Accessor.StartsWith(".")),
            Value = t.Accessor
          }));
        }

        Write(@"
        default:
          throw new System.InvalidOperationException(string.Format(""Invalid operand type {0}"", i0.ValueType));
      }
    }

");
      }

      #endregion

      #region T Run(T0 i0, /*const*/ ref mdr.DValue i1)

      foreach (var t0 in Types)
      {
        if (string.IsNullOrEmpty(t0.BasicType)) continue;

        Write(@"
    public static ${ReturnType} Run(${Arg0Type} i0, /*const*/ ref mdr.DValue i1${ResultArg}${ArgsSuffix})
    {
      switch (i1.ValueType)
      {
".FormatWith(new
 {
   ReturnType = returnType ?? "void",
   Arg0Type = t0.BasicType,
   ResultArg = ", ref mdr.DValue result".If(returnType == null),
   ArgsSuffix = argsSuffix,
 }));

        foreach (var t in Types)
        {
          var snippet = (returnType != null)
            ? @"
        case mdr.ValueTypes.${TypeEnum}: return Run(i0, ${Arg}${Value});
"
            : (returnTypes[t0, t] == "void")
            ? @"
        case mdr.ValueTypes.${TypeEnum}: Run(i0, ${Arg}${Value}, ref result); break;
"
            : @"
        case mdr.ValueTypes.${TypeEnum}: result.Set(Run(i0, ${Arg}${Value})); break;
";
          Write(snippet.FormatWith(new
          {
            TypeEnum = t.TypeEnum,
            Arg = "i1".If(t.Accessor.StartsWith(".")),
            Value = t.Accessor
          }));
        }

        Write(@"
        default:
          throw new System.InvalidOperationException(string.Format(""Invalid operand type {0}"", i1.ValueType));
      }
    }

");
      }
      #endregion
      Write(@"
    #endregion /**** generic implementations *****************************************/
");


      Write(@"
  }
");

    }
  }
}
