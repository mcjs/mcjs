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
using System.Reflection;

using m.Util.Diagnose;

namespace mjr.CodeGen
{
#pragma warning disable 649

  static class Types
  {
    static class Initializer
    {
      static readonly Type _construtorInfo;
      static readonly Type _methodInfo;
      static readonly Type _fieldInfo;

      static Initializer()
      {
        _construtorInfo = typeof(ConstructorInfo);
        _methodInfo = typeof(MethodInfo);
        _fieldInfo = typeof(FieldInfo);
      }
      static bool ParametersMatch(string[] argTypeNames, ParameterInfo[] parameters)
      {
        if (argTypeNames == null)
          return parameters.Length == 0;

        if (parameters.Length != argTypeNames.Length)
          return false;
        for (var i = 0; i < parameters.Length; ++i)
        {
          var paramType = parameters[i].ParameterType;
          var paramName = paramType.Name;
          if (paramType.IsByRef)
            paramName = paramName.Replace("&", "Ref");
          else if (paramName == "Byte")
            paramName = "UInt8";
          else if (paramName == "SByte")
            paramName = "Int8";
          else if (paramName == "Single")
            paramName = "Float";
          //if (!parameters[i].ParameterType.Name.EndsWith(argTypeNames[i]))
          if (paramName != argTypeNames[i])
            return false;
        }
        return true;
      }
      static void MatchMethod(FieldInfo field, MemberTypes memberType, Type targetType)
      {
        var first_ = field.Name.IndexOf('_');
        MemberInfo matchedMember = null;
        string memberName;
        string[] argTypeNames = null;
        if (first_ > 0)
        {
          memberName = field.Name.Substring(0, first_);
          argTypeNames = field.Name.Substring(first_ + 1).Split('_');
        }
        else
        {
          memberName = field.Name;
        }
        MemberInfo[] members;
        if (memberType == MemberTypes.Constructor)
        {
          Debug.Assert(memberName == "CONSTRUCTOR", "Invalid CTOR name {0}", memberName);
          members = targetType.GetMember(".ctor", MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance);
        }
        else
        {
          Debug.Assert(memberType == MemberTypes.Method, "Invalid MemberType {0}", memberType);
          members = targetType.GetMember(memberName, MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }
        if (argTypeNames == null && members.Length == 1)
          matchedMember = members[0];
        else
          for (var memberIndex = 0; memberIndex < members.Length; ++memberIndex)
          {
            var method = members[memberIndex] as MethodBase;
            if (ParametersMatch(argTypeNames, method.GetParameters()))
            {
              matchedMember = method;
              break;
            }
          }

        //Try to see if we have a property getter or setter
        if (matchedMember == null && memberType == MemberTypes.Method)
        {
          if (memberName.StartsWith("get_"))
          {
            var propertyName = memberName.Substring(4);
            var property = targetType.GetProperty(propertyName);
            if (property != null)
              matchedMember = property.GetGetMethod();
          }
          else if (memberName.StartsWith("set_"))
          {
            var propertyName = memberName.Substring(4);
            var property = targetType.GetProperty(propertyName);
            if (property != null)
              matchedMember = property.GetGetMethod();
          }
        }
        if (matchedMember != null)
          field.SetValue(null, matchedMember);
        else
          Debug.Warning("No member of {0} matches {1}", targetType.FullName, field.Name);
      }
      internal static void Run(Type infoType, Type targetType)
      {
        var fields = infoType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        //var targetMembers = targetType.GetMembers();
        foreach (var field in fields)
        {
          switch (field.Name)
          {
            case "TypeOf": field.SetValue(null, targetType); break;
            case "RefOf": field.SetValue(null, targetType.MakeByRefType()); break;
            case "ArrayOf": field.SetValue(null, targetType.MakeArrayType()); break;
            default:
              {
                //var memberType = MemberTypes.All;

                if (field.FieldType == _construtorInfo)
                {
                  MatchMethod(field, MemberTypes.Constructor, targetType);
                }
                else if (field.FieldType == _methodInfo)
                {
                  MatchMethod(field, MemberTypes.Method, targetType);
                }
                else if (field.FieldType == _fieldInfo)
                {
                  var matchedField = targetType.GetField(field.Name);
                  if (matchedField != null)
                    field.SetValue(null, matchedField);
                  else
                    Debug.Warning("No field of {0} matches {1}", targetType.FullName, field.Name);
                  //memberType = MemberTypes.Field;
                }
                else
                {
                  Debug.Warning("{0}.{1} has unsupported type {2}", targetType.Name, field.Name, field.FieldType);
                }
                break;
              }
          }
        }
      }
    }

    /// <summary>
    /// This class is used to lookup and cache the namesake but overloaded methods in a class
    /// </summary>
    internal abstract class MethodCacheBase
    {
      readonly Type TypeOf;
      readonly string MethodName;
      readonly int NumberOfOperands;

      readonly Dictionary<int, MethodInfo> _methods;
      readonly Dictionary<int, mdr.ValueTypes> _returnTypes;
      readonly mdr.ValueTypes _uniqueReturnType;

      protected int GetKey(mdr.ValueTypes t0) { return (int)t0; }
      protected int GetKey(mdr.ValueTypes t0, mdr.ValueTypes t1) { return (int)t1 << 8 | (int)t0; }
      protected int GetKey(mdr.ValueTypes t0, mdr.ValueTypes t1, mdr.ValueTypes t2) { return (int)t2 << 16 | (int)t1 << 8 | (int)t0; }
      protected int GetKey(mdr.ValueTypes t0, mdr.ValueTypes t1, mdr.ValueTypes t2, mdr.ValueTypes t3) { return (int)t3 << 24 | (int)t2 << 16 | (int)t1 << 8 | (int)t0; }
      protected abstract int GetKey(MethodInfo mi);

      protected MethodInfo Get(int key)
      {
        MethodInfo mi = null;
        if (!_methods.TryGetValue(key, out mi))
          CannotFindMethod(key);
        return mi;
      }
      protected mdr.ValueTypes ReturnType(int key)
      {
        if (_uniqueReturnType != mdr.ValueTypes.Unknown) //same time for everything
          return _uniqueReturnType;
        else
        {
          mdr.ValueTypes returnType;
          if (!_returnTypes.TryGetValue(key, out returnType))
          {
            if (!HasUnknownArgument(key))
              CannotFindMethod(key);
            returnType = mdr.ValueTypes.Unknown;
          }
          return returnType;
        }
      }

      bool HasUnknownArgument(int key)
      {
        var t0 = (mdr.ValueTypes)((key) & 0xFF);
        var t1 = (mdr.ValueTypes)((key >> 8) & 0xFF);
        var t2 = (mdr.ValueTypes)((key >> 16) & 0xFF);
        var t3 = (mdr.ValueTypes)((key >> 24) & 0xFF);
        return (t0 == mdr.ValueTypes.Unknown) || (t1 == mdr.ValueTypes.Unknown) || (t2 == mdr.ValueTypes.Unknown) || (t3 == mdr.ValueTypes.Unknown);
      }

      void CannotFindMethod(int key)
      {
        var t0 = (mdr.ValueTypes)((key) & 0xFF);
        var t1 = (mdr.ValueTypes)((key >> 8) & 0xFF);
        var t2 = (mdr.ValueTypes)((key >> 16) & 0xFF);
        var t3 = (mdr.ValueTypes)((key >> 24) & 0xFF);

        switch (NumberOfOperands)
        {
          case 1: throw new InvalidOperationException(string.Format("Could not find {0}.{1}({2}) method", TypeOf.Name, MethodName, t0));
          case 2: throw new InvalidOperationException(string.Format("Could not find {0}.{1}({2}, {3}) method", TypeOf.Name, MethodName, t0, t1));
          case 3: throw new InvalidOperationException(string.Format("Could not find {0}.{1}({2}, {3}, {4}) method", TypeOf.Name, MethodName, t0, t1, t2));
          case 4: throw new InvalidOperationException(string.Format("Could not find {0}.{1}({2}, {3}, {4}, {5}) method", TypeOf.Name, MethodName, t0, t1, t2, t3));
        }

      }

      protected MethodCacheBase(Type declaringType, string methodName, int numberOfOperands, bool hasOutput)
      {
        TypeOf = declaringType;
        MethodName = methodName;
        NumberOfOperands = numberOfOperands;

        _methods = new Dictionary<int, MethodInfo>();
        _returnTypes = new Dictionary<int, mdr.ValueTypes>();

        _uniqueReturnType = mdr.ValueTypes.Undefined;
        foreach (var m in TypeOf.GetMethods())
        {
          if (m.Name != MethodName)
            continue;

          var parameters = m.GetParameters();
          Debug.Assert(parameters.Length >= NumberOfOperands, "Method {0} has {1} parameters which is less than expected {2}", m, parameters.Length, NumberOfOperands);

          var key = GetKey(m);
          Debug.Assert(!_methods.ContainsKey(key), "Method {0} in {1} is already in the hash!", m, m.DeclaringType.FullName);
          _methods[key] = m;

          mdr.ValueTypes returnType;
          if (hasOutput)
          {
            if (m.ReturnType == Types.ClrSys.Void || m.ReturnType == null)
            {
              Debug.Assert(parameters.Length > NumberOfOperands, "Method {0} has no return value and no reference parameter", m);
              var refParameterIndex = NumberOfOperands; //parameters.Length-1;
              returnType = ValueTypeOf(parameters[refParameterIndex].ParameterType);
              Debug.Assert(returnType == mdr.ValueTypes.DValueRef, "Method {0} has invalid reference parameter type {1} at position {2}", m, returnType, refParameterIndex);

            }
            else
              returnType = ValueTypeOf(m.ReturnType);
          }
          else
          {
            Debug.Assert(m.ReturnType == Types.ClrSys.Void || m.ReturnType == null, "Constructor says method does not have output, but {0} has a return value!", m);
            returnType = mdr.ValueTypes.Unknown;
          }
          _returnTypes[key] = returnType; //Previous assert guarantees that we don't have the key here as well

          if (_uniqueReturnType == mdr.ValueTypes.Undefined)
            _uniqueReturnType = returnType;
          else if (_uniqueReturnType != returnType)
            _uniqueReturnType = mdr.ValueTypes.Unknown;
        }
      }
    }
    internal class MethodCache1 : MethodCacheBase
    {
      protected override int GetKey(MethodInfo mi)
      {
        var parameters = mi.GetParameters();
        var firstParamType = ValueTypeOf(parameters[0].ParameterType);
        //var secondParamType = ValueTypeOf(parameters[1].ParameterType);
        var key = GetKey(firstParamType);
        return key;
      }
      internal mdr.ValueTypes ReturnType(mdr.ValueTypes t1) { return ReturnType(GetKey(t1)); }
      internal MethodInfo Get(mdr.ValueTypes t1) { return Get(GetKey(t1)); }
      internal MethodCache1(Type declaringType, string methodName, bool hasOutput = true) : base(declaringType, methodName, 1, hasOutput) { }
    }
    internal class MethodCache2 : MethodCacheBase
    {
      protected override int GetKey(MethodInfo mi)
      {
        var parameters = mi.GetParameters();
        var firstParamType = ValueTypeOf(parameters[0].ParameterType);
        var secondParamType = ValueTypeOf(parameters[1].ParameterType);
        var key = GetKey(firstParamType, secondParamType);
        return key;
      }
      internal mdr.ValueTypes ReturnType(mdr.ValueTypes t1, mdr.ValueTypes t2) { return ReturnType(GetKey(t1, t2)); }
      internal MethodInfo Get(mdr.ValueTypes t1, mdr.ValueTypes t2) { return Get(GetKey(t1, t2)); }
      internal MethodCache2(Type declaringType, string methodName, bool hasOutput = true) : base(declaringType, methodName, 2, hasOutput) { }
    }



    internal static Type TypeOf(mdr.ValueTypes t)
    {
      switch (t)
      {
        case mdr.ValueTypes.Undefined: return Types.DUndefined.TypeOf;
        case mdr.ValueTypes.String: return Types.ClrSys.String.TypeOf;
        case mdr.ValueTypes.Char: return Types.ClrSys.Char;
        case mdr.ValueTypes.Boolean: return Types.ClrSys.Boolean;
        case mdr.ValueTypes.Float: return Types.ClrSys.Float;
        case mdr.ValueTypes.Double: return Types.ClrSys.Double;
        case mdr.ValueTypes.Int8: return Types.ClrSys.Int8;
        case mdr.ValueTypes.Int16: return Types.ClrSys.Int16;
        case mdr.ValueTypes.Int32: return Types.ClrSys.Int32;
        case mdr.ValueTypes.Int64: return Types.ClrSys.Int64;
        case mdr.ValueTypes.UInt8: return Types.ClrSys.UInt8;
        case mdr.ValueTypes.UInt16: return Types.ClrSys.UInt16;
        case mdr.ValueTypes.UInt32: return Types.ClrSys.UInt32;
        case mdr.ValueTypes.UInt64: return Types.ClrSys.UInt64;
        case mdr.ValueTypes.Null: return Types.DNull.TypeOf;
        case mdr.ValueTypes.Object: return Types.DObject.TypeOf;
        case mdr.ValueTypes.Function: return Types.DFunction.TypeOf;
        case mdr.ValueTypes.Array: return Types.DArray.TypeOf;
        case mdr.ValueTypes.Property: return Types.DObject.TypeOf;
        case mdr.ValueTypes.DValue: return Types.DValue.TypeOf;
        case mdr.ValueTypes.DValueRef: return Types.DValue.RefOf;
        case mdr.ValueTypes.Any: return Types.ClrSys.Object.TypeOf;
        default: throw new InvalidOperationException(string.Format("Cannot convert {0} to a type", t));
      }
    }
    internal static mdr.ValueTypes ValueTypeOf(Type t, bool throwIfNotMatched = true)
    {
      if (t == Types.ClrSys.String.TypeOf) return mdr.ValueTypes.String;
      if (t == Types.ClrSys.Char) return mdr.ValueTypes.Char;
      if (t == Types.ClrSys.Boolean) return mdr.ValueTypes.Boolean;
      if (t == Types.ClrSys.Float) return mdr.ValueTypes.Float;
      if (t == Types.ClrSys.Double) return mdr.ValueTypes.Double;
      if (t == Types.ClrSys.Int8) return mdr.ValueTypes.Int8;
      if (t == Types.ClrSys.Int16) return mdr.ValueTypes.Int16;
      if (t == Types.ClrSys.Int32) return mdr.ValueTypes.Int32;
      if (t == Types.ClrSys.Int64) return mdr.ValueTypes.Int64;
      if (t == Types.ClrSys.UInt8) return mdr.ValueTypes.UInt8;
      if (t == Types.ClrSys.UInt16) return mdr.ValueTypes.UInt16;
      if (t == Types.ClrSys.UInt32) return mdr.ValueTypes.UInt32;
      if (t == Types.ClrSys.UInt64) return mdr.ValueTypes.UInt64;
      if (t == Types.DObject.TypeOf) return mdr.ValueTypes.Object;
      if (t == Types.DFunction.TypeOf) return mdr.ValueTypes.Function;
      if (t == Types.DArray.TypeOf) return mdr.ValueTypes.Array;
      if (t == Types.DValue.TypeOf) return mdr.ValueTypes.DValue;
      if (t == Types.DValue.RefOf) return mdr.ValueTypes.DValueRef;
      if (t == Types.DUndefined.TypeOf) return mdr.ValueTypes.Undefined;
      if (t == Types.DNull.TypeOf) return mdr.ValueTypes.Null;
      if (t is object) return mdr.ValueTypes.Any;
      if (throwIfNotMatched)
        throw new InvalidOperationException(string.Format("Cannot convert {0} to a ValueType", t));
      else
        return mdr.ValueTypes.Unknown;
    }

    internal static class ClrSys
    {
      internal static class Console
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo WriteLine_String;
        static Console()
        {
          Initializer.Run(typeof(Types.ClrSys.Console), typeof(System.Console));
        }
      }
      internal static class Object
      {
        internal static readonly Type TypeOf;
        internal static readonly Type ArrayOf;

        static Object()
        {
          Initializer.Run(typeof(Types.ClrSys.Object), typeof(System.Object));
        }
      }

      internal static class String
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo Concat_String_String;
        internal static readonly MethodInfo Compare_String_String;
        internal static readonly MethodInfo Substring_Int32_Int32;

        internal static readonly MethodInfo GetLength;
        internal static readonly MethodInfo GetItem;

        static String()
        {
          Initializer.Run(typeof(Types.ClrSys.String), typeof(System.String));
          GetLength = TypeOf.GetProperty("Length").GetGetMethod();
          foreach (PropertyInfo pi in TypeOf.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            if (pi.GetIndexParameters().Length > 0)
            {
              GetItem = pi.GetGetMethod();
              break;
            }
        }
      }
      internal static Type Void = typeof(void);
      internal static Type Char = typeof(char);
      internal static Type Boolean = typeof(bool);
      internal static Type Float = typeof(float);
      internal static Type Double = typeof(double);
      internal static Type Int8 = typeof(sbyte);
      internal static Type Int16 = typeof(short);
      internal static Type Int32 = typeof(int);
      internal static Type Int64 = typeof(long);
      internal static Type UInt8 = typeof(byte);
      internal static Type UInt16 = typeof(ushort);
      internal static Type UInt32 = typeof(uint);
      internal static Type UInt64 = typeof(ulong);

      internal static class Convert
      {
        internal static Type TypeOf;
        static Dictionary<int, MethodInfo> _cache = new Dictionary<int, MethodInfo>();
        internal static MethodInfo Get(mdr.ValueTypes fromType, mdr.ValueTypes toType)
        {
          Debug.Assert(
            (mdr.ValueTypesHelper.IsNumber(fromType) || fromType == mdr.ValueTypes.Boolean)
            && mdr.ValueTypesHelper.IsNumber(toType)
            , "Invalid situation! Cannot convert {0} to {1} directly", fromType, toType);

          var key = ((int)fromType << 8) | (int)toType;
          MethodInfo m;
          if (!_cache.TryGetValue(key, out m))
          {
            var methodName = string.Format("To{0}", toType);
            var arg = Types.TypeOf(fromType);
            m = TypeOf.GetMethod(methodName, new Type[] { arg });
            //System.Console.WriteLine("Found method {0}, from {1} to {2}", m, fromType, toType);
            Trace.Assert(m != null, "Could not find System.Convert method to convert from {0} to {1}", fromType, toType);
            Debug.Assert(m.ReturnType == Types.TypeOf(toType), "Invalid situation! return type of {0} is not {1}", m, toType);
            _cache[key] = m;
          }
          return m;
        }

        static Convert()
        {
          TypeOf = typeof(System.Convert);
        }
      }
    }

    internal static class Diagnose
    {
      internal static class Debug
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo WriteLine;

        static Debug()
        {
          //We cannot use the Initializer.Run since we have many methods with the same name in this class. 
          //Initializer.Run(typeof(Types.Diagnose.Debug), typeof(m.Util.Diagnose.Debug));

          TypeOf = typeof(m.Util.Diagnose.Debug);
          WriteLine = TypeOf.GetMethod("WriteLine", new Type[] { ClrSys.String.TypeOf, ClrSys.Object.ArrayOf });
        }
      }
    }

    internal static class JSException
    {
      internal static readonly Type TypeOf;

      internal static readonly ConstructorInfo CONSTRUCTOR;
      internal static readonly FieldInfo Value;

      internal static readonly MethodCache1 Throw;

      static JSException()
      {
        Initializer.Run(typeof(Types.JSException), typeof(mjr.JSException));
        Throw = new MethodCache1(TypeOf, "Throw", false);
      }
    }

    internal static class JSSpeculationFailedException
    {
      internal static readonly Type TypeOf;

      internal static readonly FieldInfo icIndex;
      internal static readonly FieldInfo expectedType;

      internal static readonly ConstructorInfo CONSTRUCTOR_Int32_Int32;
      internal static readonly MethodCache1 Throw;

      static JSSpeculationFailedException()
      {
        Initializer.Run(typeof(Types.JSSpeculationFailedException), typeof(mjr.JSSpeculationFailedException));
        Throw = new MethodCache1(TypeOf, "Throw", false);
      }
    }

    #region Property
    internal static class PropertyDescriptor
    {
      internal static readonly Type TypeOf;

      internal static readonly FieldInfo Index;
      internal static readonly FieldInfo Container;

      internal static readonly MethodInfo Get_DObject;
      internal static readonly MethodInfo Get_DObject_DValueRef;

      internal static readonly MethodCache2 Set;

      internal static readonly MethodInfo IsUndefined;
      internal static readonly MethodInfo IsInherited;


      static PropertyDescriptor()
      {
        Initializer.Run(typeof(PropertyDescriptor), typeof(mdr.PropertyDescriptor));

        Set = new MethodCache2(TypeOf, "Set", false);

        IsUndefined = TypeOf.GetProperty("IsUndefined").GetGetMethod();
        IsInherited = TypeOf.GetProperty("IsInherited").GetGetMethod();
      }

    }
    internal static class PropertyMap
    {
      internal static readonly Type TypeOf;

      internal static readonly FieldInfo Parent;

      internal static readonly MethodInfo AddOwnProperty_String_Int32_Attributes;
      internal static readonly MethodInfo GetOwnPropertyDescriptor;
      //internal static readonly MethodInfo GetOwnPropertyDescriptorByFieldId;
      internal static readonly MethodInfo GetPropertyDescriptor;
      internal static readonly MethodInfo GetPropertyDescriptorByFieldId;

      static PropertyMap()
      {
        Initializer.Run(typeof(PropertyMap), typeof(mdr.PropertyMap));
      }

    }
    internal static class PropertyMapMetadata
    {
      internal static readonly Type TypeOf;

      static PropertyMapMetadata()
      {
        Initializer.Run(typeof(PropertyMapMetadata), typeof(mdr.PropertyMapMetadata));
      }

    }
    #endregion

    internal static class DValue
    {
      internal static readonly Type TypeOf;
      internal static readonly Type ArrayOf;
      internal static readonly Type RefOf;

      //internal static readonly ConstructorInfo CONSTRUCTOR;

      //internal static readonly FieldInfo ValueType;
      //internal static readonly FieldInfo BooleanValue;
      //internal static readonly FieldInfo IntValue;
      //internal static readonly FieldInfo DoubleValue;
      //internal static readonly FieldInfo DObjectValue;

      internal static readonly MethodInfo GetValueType;

      internal static readonly MethodInfo AsString;
      internal static readonly MethodInfo AsChar;
      internal static readonly MethodInfo AsBoolean;
      internal static readonly MethodInfo AsFloat;
      internal static readonly MethodInfo AsDouble;
      internal static readonly MethodInfo AsInt8;
      internal static readonly MethodInfo AsInt16;
      internal static readonly MethodInfo AsInt32;
      internal static readonly MethodInfo AsInt64;
      internal static readonly MethodInfo AsUInt8;
      internal static readonly MethodInfo AsUInt16;
      internal static readonly MethodInfo AsUInt32;
      internal static readonly MethodInfo AsUInt64;
      internal static readonly MethodInfo AsDObject;
      internal static readonly MethodInfo AsDFunction;
      internal static readonly MethodInfo AsDArray;
      internal static readonly MethodInfo AsObject;
      internal static readonly MethodInfo AsDUndefined;
      internal static readonly MethodInfo AsDNull;
      internal static MethodInfo As(mdr.ValueTypes type)
      {
        switch (type)
        {
          case mdr.ValueTypes.String: return AsString;
          case mdr.ValueTypes.Char: return AsChar;
          case mdr.ValueTypes.Boolean: return AsBoolean;
          case mdr.ValueTypes.Float: return AsFloat;
          case mdr.ValueTypes.Double: return AsDouble;
          case mdr.ValueTypes.Int8: return AsInt8;
          case mdr.ValueTypes.Int16: return AsInt16;
          case mdr.ValueTypes.Int32: return AsInt32;
          case mdr.ValueTypes.Int64: return AsInt64;
          case mdr.ValueTypes.UInt8: return AsUInt8;
          case mdr.ValueTypes.UInt16: return AsUInt16;
          case mdr.ValueTypes.UInt32: return AsUInt32;
          case mdr.ValueTypes.UInt64: return AsUInt64;
          case mdr.ValueTypes.Object: return AsDObject;
          case mdr.ValueTypes.Function: return AsDFunction;
          case mdr.ValueTypes.Array: return AsDArray;
          case mdr.ValueTypes.Property: return AsDObject;
          case mdr.ValueTypes.Any: return AsObject;
          case mdr.ValueTypes.Undefined: return AsDUndefined;
          case mdr.ValueTypes.Null: return AsDNull;
          default:
            throw new InvalidOperationException(string.Format("Could not find DObject.As{0} method", type));
        }
      }

      internal static readonly MethodInfo SetUndefined;

      internal static readonly MethodCache1 Set;

      internal static readonly MethodInfo Create_Boolean;
      internal static readonly MethodInfo Create_String;
      internal static readonly MethodInfo Create_Char;
      internal static readonly MethodInfo Create_Double;
      internal static readonly MethodInfo Create_Int32;
      internal static readonly MethodInfo Create_DObject;
      internal static MethodInfo Create(mdr.ValueTypes type)
      {
        switch (type)
        {
          case mdr.ValueTypes.Boolean:
            return Create_Boolean;
          case mdr.ValueTypes.String:
            return Create_String;
          case mdr.ValueTypes.Char:
            return Create_Char;
          case mdr.ValueTypes.Double:
            return Create_Double;
          case mdr.ValueTypes.Int32:
            return Create_Int32;
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            return Create_DObject;
          default:
            throw new InvalidOperationException(string.Format("Could not find creator for {0}", type));
        }
      }

      static DValue()
      {
        Initializer.Run(typeof(Types.DValue), typeof(mdr.DValue));
        Set = new MethodCache1(TypeOf, "Set", false);
        GetValueType = TypeOf.GetProperty("ValueType").GetGetMethod();
      }
    }

    internal static class DObject
    {
      internal static readonly Type TypeOf;
      internal static readonly Type ArrayOf;
      internal static readonly Type RefOf;

      internal static readonly ConstructorInfo CONSTRUCTOR;
      internal static readonly ConstructorInfo CONSTRUCTOR_DObject;
      internal static readonly ConstructorInfo CONSTRUCTOR_PropertyMap;

      internal static readonly FieldInfo Fields;
      internal static readonly FieldInfo PrimitiveValue;
      internal static readonly FieldInfo MapId;

      internal static readonly MethodInfo GetMap;
      internal static readonly MethodInfo SetMap;
      internal static readonly MethodInfo GetPrototype;
      internal static readonly MethodInfo GetTypeOf;

      internal static readonly MethodInfo ToBoolean;
      internal static readonly MethodInfo ToInt32;
      internal static readonly MethodInfo ToDouble;
      new internal static readonly MethodInfo ToString;
      internal static readonly MethodInfo ToDArray;
      internal static readonly MethodInfo ToDFunction;
      internal static MethodInfo To(mdr.ValueTypes type)
      {
        switch (type)
        {
          case mdr.ValueTypes.Int32:
            return ToInt32;
          case mdr.ValueTypes.Double:
            return ToDouble;
          case mdr.ValueTypes.String:
            return ToString;
          case mdr.ValueTypes.Boolean:
            return ToBoolean;
          case mdr.ValueTypes.Object:
            goto default;
          case mdr.ValueTypes.Array:
            return ToDArray;
          case mdr.ValueTypes.Function:
            return ToDFunction;
          case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.Null:
          default:
            throw new InvalidOperationException(string.Format("Could not find DObject.To{0}() method", type));
        }
      }

      //internal static readonly MethodInfo Set_Int32;
      //internal static readonly MethodInfo Set_Boolean;
      //internal static readonly MethodInfo Set_Double;
      //internal static readonly MethodInfo Set_String;
      ////internal static readonly MethodInfo Set_DObject;
      //internal static MethodInfo Set(mdr.ValueTypes type)
      //{
      //    switch (type)
      //    {
      //        case mdr.ValueTypes.Int32:
      //            return Set_Int32;
      //        case mdr.ValueTypes.Double:
      //            return Set_Double;
      //        case mdr.ValueTypes.String:
      //            return Set_String;
      //        case mdr.ValueTypes.Boolean:
      //            return Set_Boolean;
      //        case mdr.ValueTypes.Object:
      //        case mdr.ValueTypes.Function:
      //        case mdr.ValueTypes.Array:
      //        case mdr.ValueTypes.Undefined:
      //        case mdr.ValueTypes.Null:
      //        //return Set_DObject;
      //        default:
      //            throw new InvalidOperationException(string.Format("Could not find DObject.Set({0}) method", type));
      //    }
      //}

      internal static readonly MethodInfo GetPropertyDescriptorByFieldId;
      internal static readonly MethodCache1 GetPropertyDescriptor;
      //internal static readonly MethodInfo GetPropertyDescriptor_String;
      //internal static readonly MethodInfo GetPropertyDescriptor_Double;
      //internal static readonly MethodInfo GetPropertyDescriptor_Int32;
      //internal static readonly MethodInfo GetPropertyDescriptor_Boolean;
      //internal static readonly MethodInfo GetPropertyDescriptor_DObject;
      //internal static readonly MethodInfo GetPropertyDescriptor_DValueRef;
      //internal static MethodInfo GetPropertyDescriptor(mdr.ValueTypes type)
      //{
      //  switch (type)
      //  {
      //    case mdr.ValueTypes.String:
      //      return GetPropertyDescriptor_String;
      //    case mdr.ValueTypes.Double:
      //      return GetPropertyDescriptor_Double;
      //    case mdr.ValueTypes.Int32:
      //      return GetPropertyDescriptor_Int32;
      //    case mdr.ValueTypes.Boolean:
      //      return GetPropertyDescriptor_Boolean;
      //    case mdr.ValueTypes.Object:
      //    case mdr.ValueTypes.Function:
      //    case mdr.ValueTypes.Array:
      //    case mdr.ValueTypes.Property:
      //      return GetPropertyDescriptor_DObject;
      //    case mdr.ValueTypes.DValueRef:
      //      return GetPropertyDescriptor_DValueRef;
      //    //case mdr.ValueTypes.DValue:
      //    //    return AddPropertyDescriptor_DValue;
      //    default:
      //      throw new InvalidOperationException(string.Format("Could not find DObject.GetPropertyDescriptor({0}) method", type));
      //  }
      //}

      internal static readonly MethodInfo AddPropertyDescriptorByFieldId;
      internal static readonly MethodCache1 AddPropertyDescriptor;
      //internal static readonly MethodInfo AddPropertyDescriptor_String;
      //internal static readonly MethodInfo AddPropertyDescriptor_Double;
      //internal static readonly MethodInfo AddPropertyDescriptor_Int32;
      //internal static readonly MethodInfo AddPropertyDescriptor_Boolean;
      //internal static readonly MethodInfo AddPropertyDescriptor_DObject;
      //internal static readonly MethodInfo AddPropertyDescriptor_DValueRef;
      //internal static MethodInfo AddPropertyDescriptor(mdr.ValueTypes type)
      //{
      //  switch (type)
      //  {
      //    case mdr.ValueTypes.String:
      //      return AddPropertyDescriptor_String;
      //    case mdr.ValueTypes.Double:
      //      return AddPropertyDescriptor_Double;
      //    case mdr.ValueTypes.Int32:
      //      return AddPropertyDescriptor_Int32;
      //    case mdr.ValueTypes.Boolean:
      //      return AddPropertyDescriptor_Boolean;
      //    case mdr.ValueTypes.Object:
      //    case mdr.ValueTypes.Function:
      //    case mdr.ValueTypes.Array:
      //    case mdr.ValueTypes.Property:
      //      return AddPropertyDescriptor_DObject;
      //    case mdr.ValueTypes.DValueRef:
      //      return AddPropertyDescriptor_DValueRef;
      //    //case mdr.ValueTypes.DValue:
      //    //    return AddPropertyDescriptor_DValue;
      //    default:
      //      throw new InvalidOperationException(string.Format("Could not find DObject.AddPropertyDescriptor({0}) method", type));
      //  }
      //}

      internal static readonly MethodInfo DeletePropertyDescriptorByFieldId;
      internal static readonly MethodCache1 DeletePropertyDescriptor;
      //internal static readonly MethodInfo DeletePropertyDescriptor_String;
      //internal static readonly MethodInfo DeletePropertyDescriptor_Double;
      //internal static readonly MethodInfo DeletePropertyDescriptor_Int32;
      //internal static readonly MethodInfo DeletePropertyDescriptor_Boolean;
      //internal static readonly MethodInfo DeletePropertyDescriptor_DObject;
      //internal static readonly MethodInfo DeletePropertyDescriptor_DValueRef;
      //internal static MethodInfo DeletePropertyDescriptor(mdr.ValueTypes type)
      //{
      //  switch (type)
      //  {
      //    case mdr.ValueTypes.String:
      //      return DeletePropertyDescriptor_String;
      //    case mdr.ValueTypes.Double:
      //      return DeletePropertyDescriptor_Double;
      //    case mdr.ValueTypes.Int32:
      //      return DeletePropertyDescriptor_Int32;
      //    case mdr.ValueTypes.Boolean:
      //      return DeletePropertyDescriptor_Boolean;
      //    case mdr.ValueTypes.Object:
      //    case mdr.ValueTypes.Function:
      //    case mdr.ValueTypes.Array:
      //    case mdr.ValueTypes.Property:
      //      return DeletePropertyDescriptor_DObject;
      //    case mdr.ValueTypes.DValueRef:
      //      return DeletePropertyDescriptor_DValueRef;
      //    //case mdr.ValueTypes.DValue:
      //    //    return DeletePropertyDescriptor_DValue;
      //    default:
      //      throw new InvalidOperationException(string.Format("Could not find DObject.DeletePropertyDescriptor({0}) method", type));
      //  }
      //}

      internal static readonly MethodInfo AddOwnPropertyDescriptorByFieldId;

      internal static readonly MethodInfo GetFieldByFieldId_Int32_DValueRef;
      internal static readonly MethodInfo GetField_String_DValueRef;
      internal static readonly MethodInfo GetField_Double_DValueRef;
      internal static readonly MethodInfo GetField_Int32_DValueRef;
      internal static readonly MethodInfo GetField_Boolean_DValueRef;
      internal static readonly MethodInfo GetField_DObject_DValueRef;
      internal static readonly MethodInfo GetField_DValueRef_DValueRef;

      //internal static readonly MethodInfo GetFieldByFieldId_Int32;
      //internal static readonly MethodInfo GetField_String;
      //internal static readonly MethodInfo GetField_Double;
      //internal static readonly MethodInfo GetField_Int32;
      //internal static readonly MethodInfo GetField_Boolean;
      //internal static readonly MethodInfo GetField_DObject;
      //internal static readonly MethodInfo GetField_DValueRef;
      //internal static readonly MethodInfo GetField_DValue;
      internal static MethodInfo GetField(mdr.ValueTypes type)
      {
        switch (type)
        {
          case mdr.ValueTypes.String:
            return GetField_String_DValueRef;
          case mdr.ValueTypes.Double:
            return GetField_Double_DValueRef;
          case mdr.ValueTypes.Int32:
            return GetField_Int32_DValueRef;
          case mdr.ValueTypes.Boolean:
            return GetField_Boolean_DValueRef;
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            return GetField_DObject_DValueRef;
          //case mdr.ValueTypes.DValue:
          //    return GetField_DValue;
          case mdr.ValueTypes.DValueRef:
            return GetField_DValueRef_DValueRef;
          default:
            throw new InvalidOperationException(string.Format("Could not find DObject.GetField({0}) method", type));
        }
      }

      internal static readonly MethodInfo SetFieldByFieldId_Int32_String;
      internal static readonly MethodInfo SetFieldByFieldId_Int32_Double;
      internal static readonly MethodInfo SetFieldByFieldId_Int32_Int32;
      internal static readonly MethodInfo SetFieldByFieldId_Int32_Boolean;
      internal static readonly MethodInfo SetFieldByFieldId_Int32_DObject;
      //internal static readonly MethodInfo SetFieldByLineId_DValue;
      internal static readonly MethodInfo SetFieldByFieldId_Int32_DValueRef;
      internal static MethodInfo SetFieldByFieldId(mdr.ValueTypes type)
      {
        switch (type)
        {
          case mdr.ValueTypes.String:
            return SetFieldByFieldId_Int32_String;
          case mdr.ValueTypes.Double:
            return SetFieldByFieldId_Int32_Double;
          case mdr.ValueTypes.Int32:
            return SetFieldByFieldId_Int32_Int32;
          case mdr.ValueTypes.Boolean:
            return SetFieldByFieldId_Int32_Boolean;
          case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            return SetFieldByFieldId_Int32_DObject;
          case mdr.ValueTypes.DValueRef:
            return SetFieldByFieldId_Int32_DValueRef;
          //case mdr.ValueTypes.DValue:
          //    return SetFieldByLineId_DValue;
          default:
            throw new InvalidOperationException(string.Format("Could not find DObject.GetField({0}) method", type));
        }
      }

      internal static readonly MethodInfo SetField_String_String;
      internal static readonly MethodInfo SetField_String_Double;
      internal static readonly MethodInfo SetField_String_Int32;
      internal static readonly MethodInfo SetField_String_Boolean;
      internal static readonly MethodInfo SetField_String_DObject;
      //internal static readonly MethodInfo SetField_String_DValue;
      internal static readonly MethodInfo SetField_String_DValueRef;
      internal static readonly MethodInfo SetField_Double_String;
      internal static readonly MethodInfo SetField_Double_Double;
      internal static readonly MethodInfo SetField_Double_Int32;
      internal static readonly MethodInfo SetField_Double_Boolean;
      internal static readonly MethodInfo SetField_Double_DObject;
      //internal static readonly MethodInfo SetField_Double_DValue;
      internal static readonly MethodInfo SetField_Double_DValueRef;
      internal static readonly MethodInfo SetField_Int32_String;
      internal static readonly MethodInfo SetField_Int32_Double;
      internal static readonly MethodInfo SetField_Int32_Int32;
      internal static readonly MethodInfo SetField_Int32_Boolean;
      internal static readonly MethodInfo SetField_Int32_DObject;
      //internal static readonly MethodInfo SetField_Int32_DValue;
      internal static readonly MethodInfo SetField_Int32_DValueRef;
      internal static readonly MethodInfo SetField_Boolean_String;
      internal static readonly MethodInfo SetField_Boolean_Double;
      internal static readonly MethodInfo SetField_Boolean_Int32;
      internal static readonly MethodInfo SetField_Boolean_Boolean;
      internal static readonly MethodInfo SetField_Boolean_DObject;
      //internal static readonly MethodInfo SetField_Boolean_DValue;
      internal static readonly MethodInfo SetField_Boolean_DValueRef;
      internal static readonly MethodInfo SetField_DObject_String;
      internal static readonly MethodInfo SetField_DObject_Double;
      internal static readonly MethodInfo SetField_DObject_Int32;
      internal static readonly MethodInfo SetField_DObject_Boolean;
      internal static readonly MethodInfo SetField_DObject_DObject;
      //internal static readonly MethodInfo SetField_DObject_DValue;
      internal static readonly MethodInfo SetField_DObject_DValueRef;
      internal static readonly MethodInfo SetField_DValueRef_String;
      internal static readonly MethodInfo SetField_DValueRef_Double;
      internal static readonly MethodInfo SetField_DValueRef_Int32;
      internal static readonly MethodInfo SetField_DValueRef_Boolean;
      internal static readonly MethodInfo SetField_DValueRef_DObject;
      //internal static readonly MethodInfo SetField_DValueRef_DValue;
      internal static readonly MethodInfo SetField_DValueRef_DValueRef;
      internal static MethodInfo SetField(mdr.ValueTypes indexType, mdr.ValueTypes valueType)
      {
        switch (indexType)
        {
          case mdr.ValueTypes.String:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_String_String;
              case mdr.ValueTypes.Double:
                return SetField_String_Double;
              case mdr.ValueTypes.Int32:
                return SetField_String_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_String_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_String_DObject;
              //case ValueTypes.DValue:
              //    return SetField_String_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_String_DValueRef;
            }
            break;
          case mdr.ValueTypes.Double:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_Double_String;
              case mdr.ValueTypes.Double:
                return SetField_Double_Double;
              case mdr.ValueTypes.Int32:
                return SetField_Double_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_Double_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_Double_DObject;
              //case ValueTypes.DValue:
              //    return SetField_Double_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_Double_DValueRef;
            }
            break;
          case mdr.ValueTypes.Int32:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_Int32_String;
              case mdr.ValueTypes.Double:
                return SetField_Int32_Double;
              case mdr.ValueTypes.Int32:
                return SetField_Int32_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_Int32_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_Int32_DObject;
              //case ValueTypes.DValue:
              //    return SetField_Int_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_Int32_DValueRef;
            }
            break;
          case mdr.ValueTypes.Boolean:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_Boolean_String;
              case mdr.ValueTypes.Double:
                return SetField_Boolean_Double;
              case mdr.ValueTypes.Int32:
                return SetField_Boolean_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_Boolean_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_Boolean_DObject;
              //case ValueTypes.DValue:
              //    return SetField_Boolean_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_Boolean_DValueRef;
            }
            break;
          case mdr.ValueTypes.Null:
          case mdr.ValueTypes.Undefined:
          case mdr.ValueTypes.Object:
          case mdr.ValueTypes.Function:
          case mdr.ValueTypes.Array:
          case mdr.ValueTypes.Property:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_DObject_String;
              case mdr.ValueTypes.Double:
                return SetField_DObject_Double;
              case mdr.ValueTypes.Int32:
                return SetField_DObject_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_DObject_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_DObject_DObject;
              //case ValueTypes.DValue:
              //    return SetField_DObject_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_DObject_DValueRef;
            }
            break;
          case mdr.ValueTypes.DValueRef:
            switch (valueType)
            {
              case mdr.ValueTypes.String:
                return SetField_DValueRef_String;
              case mdr.ValueTypes.Double:
                return SetField_DValueRef_Double;
              case mdr.ValueTypes.Int32:
                return SetField_DValueRef_Int32;
              case mdr.ValueTypes.Boolean:
                return SetField_DValueRef_Boolean;
              case mdr.ValueTypes.Null:
              case mdr.ValueTypes.Undefined:
              case mdr.ValueTypes.Object:
              case mdr.ValueTypes.Function:
              case mdr.ValueTypes.Array:
              case mdr.ValueTypes.Property:
                return SetField_DValueRef_DObject;
              //case ValueTypes.DValue:
              //    return SetField_DValueRef_DValue;
              case mdr.ValueTypes.DValueRef:
                return SetField_DValueRef_DValueRef;
            }
            break;
        }
        throw new InvalidOperationException(string.Format("Could not find DObject.SetField({0}, {1})", indexType, valueType));

      }


      static DObject()
      {
        Initializer.Run(typeof(DObject), typeof(mdr.DObject));

        GetMap = TypeOf.GetProperty("Map").GetGetMethod();
        SetMap = TypeOf.GetProperty("Map").GetSetMethod();
        GetPrototype = TypeOf.GetProperty("Prototype").GetGetMethod();
        GetTypeOf = TypeOf.GetMethod("GetTypeOf");

        GetPropertyDescriptor = new MethodCache1(TypeOf, "GetPropertyDescriptor");
        AddPropertyDescriptor = new MethodCache1(TypeOf, "AddPropertyDescriptor");
        DeletePropertyDescriptor = new MethodCache1(TypeOf, "DeletePropertyDescriptor");
      }
    }

    internal static class DString
    {
      internal static readonly Type TypeOf;

      internal static readonly ConstructorInfo CONSTRUCTOR_String;
      static DString()
      {
        Initializer.Run(typeof(DString), typeof(mdr.DString));
      }
    }

    internal class DArray
    {
      internal static readonly Type TypeOf;

      internal static readonly ConstructorInfo CONSTRUCTOR_Int32;

      internal static FieldInfo Elements;
      internal static FieldInfo ElementsLength;

      //internal static readonly MethodInfo ResizeElements;


      static DArray()
      {
        Initializer.Run(typeof(Types.DArray), typeof(mdr.DArray));
      }
    }
    internal class DRegExp
    {
      internal static readonly Type TypeOf;

      internal static readonly ConstructorInfo CONSTRUCTOR_String_String;

      static DRegExp()
      {
        Initializer.Run(typeof(Types.DRegExp), typeof(mdr.DRegExp));
      }
    }
    internal class DUndefined
    {
      internal static readonly Type TypeOf;

      //internal static readonly FieldInfo Default;

      static DUndefined()
      {
        Initializer.Run(typeof(Types.DUndefined), typeof(mdr.DUndefined));
      }
    }
    internal class DNull
    {
      internal static readonly Type TypeOf;

      //internal static readonly FieldInfo Default;

      static DNull()
      {
        Initializer.Run(typeof(Types.DNull), typeof(mdr.DNull));
        //TypeOf = typeof(mdr.DNull);

        //Default = TypeOf.GetField("Default");
      }
    }

    internal class CallFrame
    {
      internal static readonly Type TypeOf;
      internal static readonly Type RefOf;

      //internal static readonly ConstructorInfo CONSTRUCTOR;

      internal static readonly FieldInfo Signature;
      internal static readonly FieldInfo Function;
      internal static readonly FieldInfo CallerFunction;
      internal static readonly FieldInfo CallerContext;
      internal static readonly FieldInfo Return;
      internal static readonly FieldInfo This;
      internal static readonly FieldInfo PassedArgsCount;
      internal static readonly FieldInfo Arg0;
      internal static readonly FieldInfo Arg1;
      internal static readonly FieldInfo Arg2;
      internal static readonly FieldInfo Arg3;
      internal static readonly FieldInfo Arguments;

      internal static readonly MethodInfo SetExpectedArgsCount;

      internal static readonly FieldInfo Values;
      internal static readonly FieldInfo StackPointer;

      static CallFrame()
      {
        Initializer.Run(typeof(Types.CallFrame), typeof(mdr.CallFrame));
      }
    }
    internal class DFunction
    {
      internal static readonly Type TypeOf;

      internal static readonly FieldInfo Code;
      internal static readonly FieldInfo OuterContext;

      internal static readonly ConstructorInfo CONSTRUCTOR_DFunctionMetadata_DObject;

      internal static readonly MethodInfo Call;
      internal static readonly MethodInfo Construct;
      internal static readonly MethodInfo GetMetadata;
      internal static readonly MethodInfo BlackList;

      static DFunction()
      {
        Initializer.Run(typeof(Types.DFunction), typeof(mdr.DFunction));
        GetMetadata = TypeOf.GetProperty("Metadata").GetGetMethod();
      }
    }
    internal class DFunctionCode
    {
      internal static readonly Type TypeOf;

      //internal static readonly FieldInfo Signature;
      //internal static readonly FieldInfo SignatureMask;
      //internal static readonly FieldInfo CalleeSignature;
      //internal static readonly FieldInfo ArgumentsPool;

      static DFunctionCode()
      {
        Initializer.Run(typeof(Types.DFunctionCode), typeof(mdr.DFunctionCode));
      }
    }
    internal class DFunctionSignature
    {
      internal static readonly Type TypeOf;
      internal static readonly Type RefOf;

      //internal static readonly ConstructorInfo CONSTRUCTOR2;

      internal static readonly FieldInfo Value;

      internal static readonly MethodInfo InitArgType;
      //internal static readonly MethodInfo Match;

      static DFunctionSignature()
      {
        Initializer.Run(typeof(Types.DFunctionSignature), typeof(mdr.DFunctionSignature));
      }
    }
    internal class DFunctionMetadata
    {
      internal static readonly Type TypeOf;

      //internal static readonly MethodInfo Execute;

      static DFunctionMetadata()
      {
        Initializer.Run(typeof(Types.DFunctionMetadata), typeof(mdr.DFunctionMetadata));
      }
    }
    internal class JSFunctionMetadata
    {
      internal static readonly Type TypeOf;

      internal static readonly MethodInfo GetSubFunction;

      static JSFunctionMetadata()
      {
        Initializer.Run(typeof(Types.JSFunctionMetadata), typeof(mjr.JSFunctionMetadata));
      }
    }


    internal static class Runtime
    {
      internal static readonly Type TypeOf;

      //internal static readonly FieldInfo Instance;
      internal static readonly MethodInfo get_Instance;

      internal static readonly FieldInfo Timers;
      internal static readonly FieldInfo Counters;

      //internal static readonly FieldInfo DUndefinedPrototype;
      internal static readonly FieldInfo DObjectPrototype;
      internal static readonly FieldInfo DFunctionPrototype;
      internal static readonly FieldInfo DNumberPrototype;
      internal static readonly FieldInfo DStringPrototype;
      internal static readonly FieldInfo DBooleanPrototype;
      internal static readonly FieldInfo DArrayPrototype;

      internal static readonly FieldInfo DefaultDUndefined;
      internal static readonly FieldInfo DefaultDNull;

      internal static readonly FieldInfo DObjectMap;
      internal static readonly FieldInfo DFunctionMap;
      internal static readonly FieldInfo DNumberMap;
      internal static readonly FieldInfo DStringMap;
      internal static readonly FieldInfo DBooleanMap;
      internal static readonly FieldInfo DArrayMap;

      //internal static readonly FieldInfo GlobalDObject;
      internal static readonly FieldInfo GlobalContext;

      static Runtime()
      {
        Initializer.Run(typeof(Types.Runtime), typeof(mdr.Runtime));
        get_Instance = TypeOf.GetProperty("Instance").GetGetMethod();
      }
    }

    internal static class JSRuntime
    {
      internal static readonly Type TypeOf;

      //internal static readonly FieldInfo Instance;

      //internal static readonly FieldInfo GlobalObj;
      //internal static readonly FieldInfo CurrentException;

      internal static readonly MethodInfo PushLocation;
      internal static readonly MethodInfo PopLocation;

      static JSRuntime()
      {
        Initializer.Run(typeof(JSRuntime), typeof(mjr.JSRuntime));
      }
    }

    internal static class JSFunctionArguments
    {
      internal static readonly Type TypeOf;

      internal static readonly MethodInfo Allocate;
      internal static readonly MethodInfo Release;
      //internal static readonly MethodInfo CreateArgumentsObject_CallFrameRef;
      internal static readonly MethodInfo CreateArgumentsObject_CallFrameRef_DObject;

      static JSFunctionArguments()
      {
        Initializer.Run(typeof(Types.JSFunctionArguments), typeof(mjr.JSFunctionArguments));
      }
    }

    internal static class JSFunctionContext
    {
      internal static readonly Type TypeOf;

      internal static readonly MethodInfo CreateProgramContext;
      internal static readonly MethodInfo CreateEvalContext;
      internal static readonly MethodInfo CreateConstantContext;
      internal static readonly MethodInfo CreateFunctionContext;

      static JSFunctionContext()
      {
        Initializer.Run(typeof(Types.JSFunctionContext), typeof(mjr.JSFunctionContext));
      }
    }

    internal static class JSFunctionCode
    {
      internal static readonly Type TypeOf;

      internal static readonly FieldInfo Profiler;

      static JSFunctionCode()
      {
        Initializer.Run(typeof(Types.JSFunctionCode), typeof(mjr.JSFunctionCode));
      }
    }

    internal static class JSPropertyNameEnumerator
    {
      internal static readonly Type TypeOf;

      internal static readonly ConstructorInfo CONSTRUCTOR_DObject;

      internal static readonly MethodInfo MoveNext;
      internal static readonly MethodInfo GetCurrent;

      static JSPropertyNameEnumerator()
      {
        Initializer.Run(typeof(Types.JSPropertyNameEnumerator), typeof(mjr.JSPropertyNameEnumerator));
      }
    }

    internal static class Operations
    {
      internal static readonly MethodCache1 Assign;

      internal static class Convert
      {
        internal static readonly MethodCache1 ToPrimitive;
        internal static readonly MethodCache1 ToBoolean;
        internal static readonly MethodCache1 ToNumber;
        internal static readonly MethodCache1 ToDouble;
        internal static readonly MethodCache1 ToInt32;
        internal static readonly MethodCache1 ToUInt32;
        new internal static readonly MethodCache1 ToString;
        internal static readonly MethodCache1 ToObject;
        internal static readonly MethodCache1 ToFunction;

        static Convert()
        {
          //Initializer.Run(typeof(Types.Operations.Convert), typeof(mjr.Operations.Convert));
          ToPrimitive = new MethodCache1(typeof(mjr.Operations.Convert.ToPrimitive), "Run");
          ToBoolean = new MethodCache1(typeof(mjr.Operations.Convert.ToBoolean), "Run");
          ToNumber = new MethodCache1(typeof(mjr.Operations.Convert.ToNumber), "Run");
          ToDouble = new MethodCache1(typeof(mjr.Operations.Convert.ToDouble), "Run");
          ToInt32 = new MethodCache1(typeof(mjr.Operations.Convert.ToInt32), "Run");
          ToUInt32 = new MethodCache1(typeof(mjr.Operations.Convert.ToUInt32), "Run");
          ToString = new MethodCache1(typeof(mjr.Operations.Convert.ToString), "Run");
          ToObject = new MethodCache1(typeof(mjr.Operations.Convert.ToObject), "Run");
          ToFunction = new MethodCache1(typeof(mjr.Operations.Convert.ToFunction), "Run");
        }
      }

      internal static class Unary
      {
        internal static readonly MethodCache1 Delete;
        internal static readonly MethodCache2 DeleteProperty;
        internal static readonly MethodCache2 DeleteVariable;
        internal static readonly MethodCache1 Void;
        internal static readonly MethodCache1 Typeof;
        internal static readonly MethodCache1 Positive;
        internal static readonly MethodCache1 Negative;
        internal static readonly MethodCache1 BitwiseNot;
        internal static readonly MethodCache1 LogicalNot;

        //internal static class IncDec
        //{
        //  internal static readonly Type TypeOf;

        //  internal static readonly MethodInfo Run_DValueRef_Int32;
        //  internal static readonly MethodInfo AddConst;

        //  static IncDec()
        //  {
        //    Initializer.Run(typeof(Types.Operations.Unary.IncDec), typeof(mjr.Operations.Unary.IncDec));
        //  }
        //}

        internal static MethodCache1 Get(IR.NodeType nodeType)
        {
          switch (nodeType)
          {
            case IR.NodeType.ToPrimitive: return Convert.ToPrimitive;
            case IR.NodeType.ToBoolean: return Convert.ToBoolean;
            case IR.NodeType.ToNumber: return Convert.ToNumber;
            case IR.NodeType.ToDouble: return Convert.ToDouble;
            case IR.NodeType.ToInteger: throw new NotImplementedException(); // return Convert.ToInteger;
            case IR.NodeType.ToInt32: return Convert.ToInt32;
            case IR.NodeType.ToUInt32: return Convert.ToUInt32;
            case IR.NodeType.ToUInt16: throw new NotImplementedException(); // return Convert.ToUInt16;
            case IR.NodeType.ToString: return Convert.ToString;
            case IR.NodeType.ToObject: return Convert.ToObject;
            case IR.NodeType.ToFunction: return Convert.ToFunction;

            case IR.NodeType.DeleteExpression: return Delete;
            case IR.NodeType.VoidExpression: return Void;
            case IR.NodeType.TypeofExpression: return Typeof;
            case IR.NodeType.PositiveExpression: return Positive;
            case IR.NodeType.NegativeExpression: return Negative;
            case IR.NodeType.BitwiseNotExpression: return BitwiseNot;
            case IR.NodeType.LogicalNotExpression: return LogicalNot;
          }
          return null;
        }

        static Unary()
        {
          //Initializer.Run(typeof(Types.Operations.Unary), typeof(mjr.Operations.Unary));
          Delete = new MethodCache1(typeof(mjr.Operations.Unary.Delete), "Run");
          DeleteProperty = new MethodCache2(typeof(mjr.Operations.Unary.DeleteProperty), "Run");
          DeleteVariable = new MethodCache2(typeof(mjr.Operations.Unary.DeleteVariable), "Run");
          Void = new MethodCache1(typeof(mjr.Operations.Unary.Void), "Run");
          Typeof = new MethodCache1(typeof(mjr.Operations.Unary.Typeof), "Run");
          Positive = new MethodCache1(typeof(mjr.Operations.Unary.Positive), "Run");
          Negative = new MethodCache1(typeof(mjr.Operations.Unary.Negative), "Run");
          BitwiseNot = new MethodCache1(typeof(mjr.Operations.Unary.BitwiseNot), "Run");
          LogicalNot = new MethodCache1(typeof(mjr.Operations.Unary.LogicalNot), "Run");

        }
      }

      internal static class Binary
      {
        internal static readonly MethodCache2 Multiply;
        internal static readonly MethodCache2 Divide;
        internal static readonly MethodCache2 Remainder;

        internal static readonly MethodCache2 Addition;
        internal static readonly MethodCache2 Subtraction;

        internal static readonly MethodCache2 LeftShift;
        internal static readonly MethodCache2 RightShift;
        internal static readonly MethodCache2 UnsignedRightShift;

        internal static readonly MethodCache2 LessThan;
        internal static readonly MethodCache2 GreaterThan;
        internal static readonly MethodCache2 LessThanOrEqual;
        internal static readonly MethodCache2 GreaterThanOrEqual;
        internal static readonly MethodCache2 InstanceOf;
        internal static readonly MethodCache2 In;

        internal static readonly MethodCache2 Equal;
        internal static readonly MethodCache2 NotEqual;
        internal static readonly MethodCache2 Same;
        internal static readonly MethodCache2 NotSame;

        internal static readonly MethodCache2 BitwiseAnd;
        internal static readonly MethodCache2 BitwiseOr;
        internal static readonly MethodCache2 BitwiseXor;

        //internal static readonly MethodCache2 LogicalAnd;
        //internal static readonly MethodCache2 LogicalOr;

        internal static MethodCache2 Get(IR.NodeType nodeType)
        {
          switch (nodeType)
          {
            case IR.NodeType.MultiplyExpression: return Multiply;
            case IR.NodeType.DivideExpression: return Divide;
            case IR.NodeType.RemainderExpression: return Remainder;
            case IR.NodeType.AdditionExpression: return Addition;
            case IR.NodeType.SubtractionExpression: return Subtraction;
            case IR.NodeType.LeftShiftExpression: return LeftShift;
            case IR.NodeType.RightShiftExpression: return RightShift;
            case IR.NodeType.UnsignedRightShiftExpression: return UnsignedRightShift;
            case IR.NodeType.LesserExpression: return LessThan;
            case IR.NodeType.GreaterExpression: return GreaterThan;
            case IR.NodeType.LesserOrEqualExpression: return LessThanOrEqual;
            case IR.NodeType.GreaterOrEqualExpression: return GreaterThanOrEqual;
            case IR.NodeType.InstanceOfExpression: return InstanceOf;
            case IR.NodeType.InExpression: return In;
            case IR.NodeType.EqualExpression: return Equal;
            case IR.NodeType.NotEqualExpression: return NotEqual;
            case IR.NodeType.SameExpression: return Same;
            case IR.NodeType.NotSameExpression: return NotSame;
            case IR.NodeType.BitwiseAndExpression: return BitwiseAnd;
            case IR.NodeType.BitwiseOrExpression: return BitwiseOr;
            case IR.NodeType.BitwiseXorExpression: return BitwiseXor;
            //case IR.NodeType.LogicalAndExpression: return LogicalAnd;
            //case IR.NodeType.LogicalOrExpression: return LogicalOr;
          }
          return null;
        }

        static Binary()
        {
          //Initializer.Run(typeof(Types.Operations.Binary), typeof(mjr.Operations.Binary));

          Multiply = new MethodCache2(typeof(mjr.Operations.Binary.Multiply), "Run");
          Divide = new MethodCache2(typeof(mjr.Operations.Binary.Divide), "Run");
          Remainder = new MethodCache2(typeof(mjr.Operations.Binary.Remainder), "Run");

          Addition = new MethodCache2(typeof(mjr.Operations.Binary.Addition), "Run");
          Subtraction = new MethodCache2(typeof(mjr.Operations.Binary.Subtraction), "Run");

          LeftShift = new MethodCache2(typeof(mjr.Operations.Binary.LeftShift), "Run");
          RightShift = new MethodCache2(typeof(mjr.Operations.Binary.RightShift), "Run");
          UnsignedRightShift = new MethodCache2(typeof(mjr.Operations.Binary.UnsignedRightShift), "Run");

          LessThan = new MethodCache2(typeof(mjr.Operations.Binary.LessThan), "Run");
          GreaterThan = new MethodCache2(typeof(mjr.Operations.Binary.GreaterThan), "Run");
          LessThanOrEqual = new MethodCache2(typeof(mjr.Operations.Binary.LessThanOrEqual), "Run");
          GreaterThanOrEqual = new MethodCache2(typeof(mjr.Operations.Binary.GreaterThanOrEqual), "Run");
          InstanceOf = new MethodCache2(typeof(mjr.Operations.Binary.InstanceOf), "Run");
          In = new MethodCache2(typeof(mjr.Operations.Binary.In), "Run");

          Equal = new MethodCache2(typeof(mjr.Operations.Binary.Equal), "Run");
          NotEqual = new MethodCache2(typeof(mjr.Operations.Binary.NotEqual), "Run");
          Same = new MethodCache2(typeof(mjr.Operations.Binary.Same), "Run");
          NotSame = new MethodCache2(typeof(mjr.Operations.Binary.NotSame), "Run");

          BitwiseAnd = new MethodCache2(typeof(mjr.Operations.Binary.BitwiseAnd), "Run");
          BitwiseOr = new MethodCache2(typeof(mjr.Operations.Binary.BitwiseOr), "Run");
          BitwiseXor = new MethodCache2(typeof(mjr.Operations.Binary.BitwiseXor), "Run");

          //LogicalAnd = new MethodCache2(typeof(mjr.Operations.Binary.LogicalAnd), "Run");
          //LogicalOr = new MethodCache2(typeof(mjr.Operations.Binary.LogicalOr), "Run");
        }
      }

      internal static class Stack
      {
        internal static readonly Type TypeOf;

        internal static FieldInfo Items;
        internal static FieldInfo Sp;

        internal static readonly MethodInfo Pop;
        internal static readonly MethodInfo Dup;
        internal static readonly MethodInfo Reserve;

        internal static readonly MethodInfo CreateContext;
        //internal static readonly MethodInfo LoadContext;

        internal static readonly MethodInfo Return;
        internal static readonly MethodInfo Throw;

        internal static readonly MethodInfo CreateFunction;
        internal static readonly MethodInfo CreateArray;
        internal static readonly MethodInfo CreateJson;
        internal static readonly MethodInfo CreateRegexp;
        internal static readonly MethodInfo New;
        internal static readonly MethodInfo Call;

        internal static readonly MethodInfo TernaryOperation;

        #region Binary ops
        //internal static readonly MethodInfo And;
        //internal static readonly MethodInfo Or;
        internal static readonly MethodInfo NotEqual;
        internal static readonly MethodInfo LesserOrEqual;
        internal static readonly MethodInfo GreaterOrEqual;
        internal static readonly MethodInfo Lesser;
        internal static readonly MethodInfo Greater;
        internal static readonly MethodInfo Equal;
        internal static readonly MethodInfo Minus;
        internal static readonly MethodInfo Plus;
        internal static readonly MethodInfo Modulo;
        internal static readonly MethodInfo Div;
        internal static readonly MethodInfo Times;
        internal static readonly MethodInfo Pow;
        internal static readonly MethodInfo BitwiseAnd;
        internal static readonly MethodInfo BitwiseOr;
        internal static readonly MethodInfo BitwiseXOr;
        internal static readonly MethodInfo Same;
        internal static readonly MethodInfo NotSame;
        internal static readonly MethodInfo LeftShift;
        internal static readonly MethodInfo RightShift;
        internal static readonly MethodInfo UnsignedRightShift;
        internal static readonly MethodInfo InstanceOf;
        internal static readonly MethodInfo In;
        #endregion

        #region Unary ops
        internal static readonly MethodInfo Delete;
        internal static readonly MethodInfo DeleteProperty;
        internal static readonly MethodInfo DeleteVariable;
        internal static readonly MethodInfo Void;
        internal static readonly MethodInfo TypeOfOp;
        internal static readonly MethodInfo Positive;
        internal static readonly MethodInfo Negate;
        internal static readonly MethodInfo BitwiseNot;
        internal static readonly MethodInfo LogicalNot;
        //internal static readonly MethodInfo PrefixPlusPlus;
        //internal static readonly MethodInfo PrefixMinusMinus;
        //internal static readonly MethodInfo PostfixPlusPlus;
        //internal static readonly MethodInfo PostfixMinusMinus;
        #endregion

        #region Convert ops
        internal static readonly MethodInfo ToPrimitive;
        internal static readonly MethodInfo ToBoolean;
        internal static readonly MethodInfo ToNumber;
        internal static readonly MethodInfo ToDouble;
        internal static readonly MethodInfo ToInteger;
        internal static readonly MethodInfo ToInt32;
        internal static readonly MethodInfo ToUInt32;
        internal static readonly MethodInfo ToUInt16;
        internal static readonly MethodInfo ToString_StackRef;
        internal static readonly MethodInfo ToObject;
        internal static readonly MethodInfo ToFunction;
        #endregion

        internal static readonly MethodInfo LoadFieldByFieldId;
        internal static readonly MethodInfo LoadField;
        internal static readonly MethodInfo StoreFieldByFieldId;
        internal static readonly MethodInfo StoreField;

        internal static readonly MethodInfo LoadThis;
        internal static readonly MethodInfo LoadArg;
        internal static readonly MethodInfo StoreArg;

        internal static readonly MethodInfo DeclareVariable;
        internal static readonly MethodInfo LoadVariable;
        internal static readonly MethodInfo StoreVariable;


        internal static readonly MethodInfo LoadUndefined;
        internal static readonly MethodInfo LoadNull;
        internal static readonly MethodInfo LoadString;
        internal static readonly MethodInfo LoadDouble;
        internal static readonly MethodInfo LoadInt;
        internal static readonly MethodInfo LoadBoolean;
        internal static readonly MethodInfo LoadDObject;
        internal static readonly MethodInfo LoadAny;
        internal static readonly MethodInfo LoadDValue;

        static Stack()
        {
          Initializer.Run(typeof(Types.Operations.Stack), typeof(mjr.Operations.Stack));

          TypeOfOp = TypeOf.GetMethod("TypeOf");
        }

      }

      internal static class ICMethods
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo GetContext;
        internal static readonly MethodInfo SetContext;

        internal static readonly MethodInfo GetArguments;
        internal static readonly MethodInfo SetArguments;
        internal static readonly MethodInfo CreateArgumentsObject;
        internal static readonly MethodInfo Return;
        internal static readonly MethodInfo CreateArray;
        internal static readonly MethodInfo CreateObject;
        internal static readonly MethodInfo New;
        internal static readonly MethodInfo Call;
        internal static readonly MethodInfo RunAndUpdateUnaryOperationIC;
        internal static readonly MethodInfo RunAndUpdateBinaryOperationIC;
        internal static readonly MethodInfo GetPropertyDescriptor;
        internal static readonly MethodInfo ReadFromContext;
        internal static readonly MethodInfo WriteToContext;
        internal static readonly MethodInfo WriteValueToContext;
        internal static readonly MethodInfo CreateFunction;
        internal static readonly MethodInfo TryCatchFinally;
        internal static readonly MethodInfo ReadIndexer;
        internal static readonly MethodInfo ReadProperty;
        internal static readonly MethodInfo WriteIndexer;
        internal static readonly MethodInfo WriteProperty;
        internal static readonly MethodInfo Execute;

        static ICMethods()
        {
          Initializer.Run(typeof(Types.Operations.ICMethods), typeof(mjr.Operations.ICMethods));
        }
      }

      internal static class Error
      {
        internal static readonly Type TypeOf;
        internal static readonly MethodInfo ReferenceError;
        internal static readonly MethodInfo SemanticError;
        static Error()
        {
          Initializer.Run(typeof(Types.Operations.Error), typeof(mjr.Operations.Error));
        }
      }

      internal static class Internals
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo CheckSignature;

        internal static readonly MethodInfo GetFieldUsingIC;
        internal static readonly MethodInfo GetInheritFieldUsingIC;
        internal static readonly MethodInfo GetTypesSplit;
        internal static readonly MethodInfo CompareTypes;
        internal static readonly MethodInfo PrintString;

        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_String_Int32_Int32;
        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_Double_Int32_Int32;
        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_Int32_Int32_Int32;
        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_Boolean_Int32_Int32;
        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_DObject_Int32_Int32;
        //internal static readonly MethodInfo SetFieldByLineId_DValue;
        internal static readonly MethodInfo SetFieldUsingIC_DObject_Int32_DValueRef_Int32_Int32;
        internal static MethodInfo SetFieldUsingIC(mdr.ValueTypes type)
        {
          switch (type)
          {
            case mdr.ValueTypes.String:
              return SetFieldUsingIC_DObject_Int32_String_Int32_Int32;
            case mdr.ValueTypes.Double:
              return SetFieldUsingIC_DObject_Int32_Double_Int32_Int32;
            case mdr.ValueTypes.Int32:
              return SetFieldUsingIC_DObject_Int32_Int32_Int32_Int32;
            case mdr.ValueTypes.Boolean:
              return SetFieldUsingIC_DObject_Int32_Boolean_Int32_Int32;
            case mdr.ValueTypes.Undefined:
            case mdr.ValueTypes.Null:
            case mdr.ValueTypes.Object:
            case mdr.ValueTypes.Function:
            case mdr.ValueTypes.Array:
            case mdr.ValueTypes.Property:
              return SetFieldUsingIC_DObject_Int32_DObject_Int32_Int32;
            case mdr.ValueTypes.DValueRef:
              return SetFieldUsingIC_DObject_Int32_DValueRef_Int32_Int32;
            //case mdr.ValueTypes.DValue:
            //    return SetFieldByLineId_DValue;
            default:
              throw new InvalidOperationException(string.Format("Could not find DObject.SetFieldUsingIC({0}) method", type));
          }
        }


        //internal static readonly MethodInfo SetFieldUsingIC;

        internal static readonly MethodInfo UpdateMapProfile;
        internal static readonly MethodInfo UpdateMapProfileForWrite;
        internal static readonly MethodInfo UpdateCallProfile;
        internal static readonly MethodInfo UpdateGuardProfile;

        static Internals()
        {
          Initializer.Run(typeof(Types.Operations.Internals), typeof(mjr.Operations.Internals));
        }

      }
      static Operations()
      {
        //Initializer.Run(typeof(Types.Operations), typeof(mjr.Operations));
        Assign = new MethodCache1(typeof(mjr.Operations.Assign), "Run");
      }
    }

    internal static class Profiler
    {
      internal static readonly Type TypeOf;

      internal static readonly MethodInfo GetOrAddMapNodeProfile;
      internal static readonly MethodInfo GetOrAddCallNodeProfile;
      internal static readonly MethodInfo GetOrAddGuardNodeProfile;

      internal static class MapNodeProfile
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo UpdateNodeProfile;

        static MapNodeProfile()
        {
          Initializer.Run(typeof(Types.Profiler.MapNodeProfile), typeof(mjr.CodeGen.MapNodeProfile));
        }
      }

      internal static class CallNodeProfile
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo UpdateNodeProfile;

        static CallNodeProfile()
        {
          Initializer.Run(typeof(Types.Profiler.CallNodeProfile), typeof(mjr.CodeGen.CallNodeProfile));
        }
      }

      internal static class GuardNodeProfile
      {
        internal static readonly Type TypeOf;

        internal static readonly MethodInfo UpdateNodeProfile;

        static GuardNodeProfile()
        {
          Initializer.Run(typeof(Types.Profiler.GuardNodeProfile), typeof(mjr.CodeGen.GuardNodeProfile));
        }
      }

      static Profiler()
      {
        Initializer.Run(typeof(Types.Profiler), typeof(mjr.CodeGen.Profiler));
      }
    }

    internal static class Util
    {
      internal static class Timers
      {
        internal static class Timer
        {
          internal static readonly Type TypeOf;

          internal static readonly MethodInfo Start;
          internal static readonly MethodInfo Stop;

          static Timer()
          {
            Initializer.Run(typeof(Types.Util.Timers.Timer), typeof(m.Util.Timers.Timer));
          }
        }

        internal static readonly Type TypeOf;

        internal static readonly MethodInfo GetTicks;
        internal static readonly MethodInfo GetTimer_Int32;

        static Timers()
        {
          Initializer.Run(typeof(Types.Util.Timers), typeof(m.Util.Timers));
        }
      }

      internal static class Counters
      {
        internal static class Counter
        {
          internal static readonly Type TypeOf;

          internal static readonly FieldInfo Count;

          static Counter()
          {
            Initializer.Run(typeof(Types.Util.Counters.Counter), typeof(m.Util.Counters.Counter));
          }
        }

        internal static readonly Type TypeOf;

        internal static readonly MethodInfo GetCounter_Int32;

        static Counters()
        {
          Initializer.Run(typeof(Types.Util.Counters), typeof(m.Util.Counters));
        }
      }

      internal static class TimeCounter
      {
        internal static readonly Type TypeOf;

        internal static readonly ConstructorInfo CONSTRUCTOR_Int32_Counters;
        internal static readonly ConstructorInfo CONSTRUCTOR_String_String_Counters;

        internal static readonly MethodInfo Start;
        internal static readonly MethodInfo Stop;

        static TimeCounter()
        {
          Initializer.Run(typeof(TimeCounter), typeof(m.Util.TimeCounter));
        }
      }
    }

#pragma warning restore 649

  }

#pragma warning restore

}
