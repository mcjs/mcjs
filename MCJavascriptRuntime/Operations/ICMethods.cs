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
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using m.Util.Diagnose;

namespace mjr.Operations
{
  /// <summary>
  /// These are helper methods to make CodeGeneratorWithInlineCache simpler
  /// sometimes the order of arguments chosen in a way to simplify code gen
  /// also, we try to concentrate most decisions (such as index in .Values) here
  /// </summary>
  static class ICMethods
  {
    public static void Execute(ref mdr.CallFrame callFrame, int fromIndex, int toIndex)
    {
      var ics = callFrame.Function.Metadata.InlineCache;
      var i = fromIndex;
      while (i <= toIndex)
      {
        Debug.WriteLine("Running {0}:{1}", i, ics[i].Method.Name);
        i = ics[i](ref callFrame, i);
      }
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static mdr.DObject GetContext(ref mdr.CallFrame callFrame) { return callFrame.Values[0].AsDObject(); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void SetContext(ref mdr.CallFrame callFrame, mdr.DObject context) { callFrame.Values[0].Set(context); }


    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static mdr.DArray GetArguments(ref mdr.CallFrame callFrame) { return callFrame.Values[1].AsDArray(); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void SetArguments(ref mdr.CallFrame callFrame, mdr.DArray arguments) { callFrame.Values[1].Set(arguments); }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void CreateArgumentsObject(ref mdr.CallFrame callFrame, int argumentsIndex)
    {
      var arguments = JSFunctionArguments.CreateArgumentsObject(ref callFrame, GetContext(ref callFrame));
      callFrame.Values[argumentsIndex].Set(arguments);
      SetArguments(ref callFrame, arguments);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void Return(ref mdr.CallFrame callFrame, int resultIndex)
    {
      callFrame.Return = callFrame.Values[resultIndex];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void CreateArray(ref mdr.CallFrame callFrame, int resultIndex, int valuesCount)
    {
      var values = callFrame.Values;
      var array = new mdr.DArray(valuesCount);
      for (var i = valuesCount - 1; i >= 0; --i)
        array.Elements[i] = values[resultIndex + i];
      values[resultIndex].Set(array);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void CreateObject(ref mdr.CallFrame callFrame, int resultIndex, int fieldsCount)
    {
      //Here we assume we have (fieldId, value) pairs on the stack starting at resultIndex
      var values = callFrame.Values;
      var obj = new mdr.DObject();
      var lastSP = resultIndex + fieldsCount * 2;
      for (var sp = resultIndex; sp < lastSP; sp += 2)
        obj.SetFieldByFieldId(values[sp].AsInt32(), ref values[sp + 1]);
      values[resultIndex].Set(obj);
    }

    static void ReadArguments(ref mdr.CallFrame callerFrame, int firstArgIndex, int argsCount, ref mdr.CallFrame calleeFrame)
    {
      var values = callerFrame.Values;
      calleeFrame.PassedArgsCount = argsCount;
      var argIndex = argsCount - 1;
      var sp = firstArgIndex + argIndex;
      switch (argsCount)
      {
        case 0:
          break;
        case 1:
          calleeFrame.Arg0 = values[sp];
          calleeFrame.Signature.InitArgType(argIndex--, calleeFrame.Arg0.ValueType);
          goto case 0;
        case 2:
          calleeFrame.Arg1 = values[sp--];
          calleeFrame.Signature.InitArgType(argIndex--, calleeFrame.Arg1.ValueType);
          goto case 1;
        case 3:
          calleeFrame.Arg2 = values[sp--];
          calleeFrame.Signature.InitArgType(argIndex--, calleeFrame.Arg2.ValueType);
          goto case 2;
        case 4:
          calleeFrame.Arg3 = values[sp--];
          calleeFrame.Signature.InitArgType(argIndex--, calleeFrame.Arg3.ValueType);
          goto case 3;
        default:
          {
            var remainintArgsCount = argsCount - mdr.CallFrame.InlineArgsCount;
            calleeFrame.Arguments = JSFunctionArguments.Allocate(remainintArgsCount);
            for (var i = remainintArgsCount - 1; i >= 0; --i)
            {
              calleeFrame.Arguments[i] = values[sp--];
              calleeFrame.Signature.InitArgType(argIndex--, calleeFrame.Arguments[i].ValueType);
            }
          }
          goto case 4;
      }

    }

    public static void New(ref mdr.CallFrame callFrame, int argsCount, int resultIndex)
    {
      var values = callFrame.Values;
      var calleeFrame = new mdr.CallFrame();
      var sp = resultIndex;
      calleeFrame.Function = values[sp++].AsDFunction();
      ReadArguments(ref callFrame, sp, argsCount, ref calleeFrame);
      calleeFrame.Function.Construct(ref calleeFrame);
      values[resultIndex].Set(calleeFrame.This);
    }

    public static void Call(ref mdr.CallFrame callFrame, int argsCount, int resultIndex, bool hasThis, bool isDirectEvalCall)
    {
      var values = callFrame.Values;
      var calleeFrame = new mdr.CallFrame();
      var sp = resultIndex;
      calleeFrame.Function = values[sp++].AsDFunction();
      if (hasThis)
        calleeFrame.This = values[sp++].AsDObject();
      else
      {
        if (isDirectEvalCall)
        {
          calleeFrame.CallerFunction = callFrame.Function;
          calleeFrame.CallerContext = GetContext(ref callFrame);
          calleeFrame.This = callFrame.This;
        }
        else
          calleeFrame.This = mdr.Runtime.Instance.GlobalContext;
      }
      ReadArguments(ref callFrame, sp, argsCount, ref calleeFrame);
      calleeFrame.Function.Call(ref calleeFrame);
      values[resultIndex] = calleeFrame.Return;
    }

    static void LoadValue(ILGen.BaseILGenerator ilGen, System.Reflection.Emit.LocalBuilder values, int index, mdr.ValueTypes valueType)
    {
      ilGen.Ldloc(values);
      ilGen.Ldc_I4(index);
      ilGen.Ldelema(CodeGen.Types.DValue.TypeOf);
      if (valueType != mdr.ValueTypes.DValueRef)
        ilGen.Call(CodeGen.Types.DValue.As(valueType));
    }

    public static void CreateUnaryOperationIC(ILGen.BaseILGenerator ilGen, IR.NodeType nodeType, int resultIndex, mdr.ValueTypes i0Type, bool i0TypeCheck)
    {
      ilGen.WriteComment("IC method for {0}({1}, {2}) written to {3}", nodeType, i0Type, i0Type, resultIndex);

      var values = ilGen.DeclareLocal(CodeGen.Types.DValue.ArrayOf);
      ilGen.Ldarg_CallFrame();
      ilGen.Ldfld(CodeGen.Types.CallFrame.Values);
      ilGen.Stloc(values);

      var t0 = ilGen.DeclareLocal(CodeGen.Types.ClrSys.Int32);
      //if (i0TypeCheck)
      {
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        ilGen.Call(CodeGen.Types.DValue.GetValueType);
        ilGen.Stloc(t0);
      }

      var guardFail = ilGen.DefineLabel();
      var done = ilGen.DefineLabel();

      var operation = CodeGen.Types.Operations.Unary.Get(nodeType);

      if (i0Type == mdr.ValueTypes.DValueRef) //Just a better guess!
        i0Type = operation.ReturnType(i0Type); //we will need to repeat the lookup again

      ilGen.Ldloc(t0);
      ilGen.Ldc_I4((int)i0Type);
      ilGen.Bne_Un(guardFail);

      var mi = operation.Get(i0Type);
      var returnType = operation.ReturnType(i0Type);

      if (returnType == mdr.ValueTypes.DValueRef)
      {
        Debug.Assert(mi.GetParameters().Length == 2 && mi.GetParameters()[1].ParameterType == CodeGen.Types.TypeOf(mdr.ValueTypes.DValueRef), "Invalid situation, method {0} must get a second parameter of type 'ref DValue'", mi);
        LoadValue(ilGen, values, resultIndex, i0Type);
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        ilGen.Call(mi);
      }
      else
      {
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        LoadValue(ilGen, values, resultIndex, i0Type);
        ilGen.Call(mi);
        ilGen.Call(CodeGen.Types.DValue.Set.Get(returnType));
      }
      ilGen.Br(done);

      ilGen.MarkLabel(guardFail);
      ilGen.Ldarg_CallFrame();
      ilGen.Ldarg_1();
      ilGen.Ldc_I4((int)nodeType);
      ilGen.Ldc_I4(resultIndex);
      ilGen.Ldloc(t0);
      ilGen.Ldc_I4(i0TypeCheck);
      ilGen.Call(CodeGen.Types.Operations.ICMethods.RunAndUpdateUnaryOperationIC);
      ilGen.Ret();

      ilGen.MarkLabel(done);
    }

    public static int RunAndUpdateUnaryOperationIC(ref mdr.CallFrame callFrame, int index, IR.NodeType nodeType, int resultIndex, mdr.ValueTypes i0Type, bool i0TypeCheck)
    {
      var ilGen = JSRuntime.Instance.AsmGenerator.GetILGenerator();
      var funcMetadata = callFrame.Function.Metadata;
      var methodName = funcMetadata.FullName + "__" + index.ToString();
      ilGen.BeginICMethod(methodName);
      CreateUnaryOperationIC(ilGen, nodeType, resultIndex, i0Type, i0TypeCheck);
      ilGen.Ldc_I4(index + 1);
      ilGen.Ret();
      ilGen.WriteComment("Reinstalling this at index {0}", index);

      var m = ilGen.EndICMethod((JSFunctionMetadata)funcMetadata);
      funcMetadata.InlineCache[index] = m;
      return m(ref callFrame, index);
    }

    public static void CreateBinaryOperationIC(ILGen.BaseILGenerator ilGen, IR.NodeType nodeType, int resultIndex, mdr.ValueTypes i0Type, mdr.ValueTypes i1Type, bool i0TypeCheck, bool i1TypeCheck)
    {
      ilGen.WriteComment("IC method for {0}({1}, {2}) written to {3}", nodeType, i0Type, i0Type, resultIndex);

      var values = ilGen.DeclareLocal(CodeGen.Types.DValue.ArrayOf);
      ilGen.Ldarg_CallFrame();
      ilGen.Ldfld(CodeGen.Types.CallFrame.Values);
      ilGen.Stloc(values);

      ///Try to make a better guess on the unknown types. 
      if (i0Type == mdr.ValueTypes.DValueRef)
      {
        if (i1Type != mdr.ValueTypes.DValueRef)
          i0Type = i1Type;
        else
          i0Type = i1Type = mdr.ValueTypes.Int32; //Just a guess! 
      }
      else if (i1Type == mdr.ValueTypes.DValueRef)
        i1Type = i0Type;

      var t0 = ilGen.DeclareLocal(CodeGen.Types.ClrSys.Int32);
      //if (i0TypeCheck)
      {
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        ilGen.Call(CodeGen.Types.DValue.GetValueType);
        ilGen.Stloc(t0);
      }

      var t1 = ilGen.DeclareLocal(CodeGen.Types.ClrSys.Int32);
      //if (i0TypeCheck)
      {
        LoadValue(ilGen, values, resultIndex + 1, mdr.ValueTypes.DValueRef);
        ilGen.Call(CodeGen.Types.DValue.GetValueType);
        ilGen.Stloc(t1);
      }

      var guardFail = ilGen.DefineLabel();
      var done = ilGen.DefineLabel();

      ilGen.Ldloc(t0);
      ilGen.Ldc_I4(8);
      ilGen.Shl();
      ilGen.Ldloc(t1);
      ilGen.Or();
      ilGen.Ldc_I4(((int)i0Type << 8) | (int)i1Type);
      ilGen.Bne_Un(guardFail);

      var operation = CodeGen.Types.Operations.Binary.Get(nodeType);
      var mi = operation.Get(i0Type, i1Type);
      var returnType = operation.ReturnType(i0Type, i1Type);
      if (returnType == mdr.ValueTypes.DValueRef)
      {
        Debug.Assert(mi.GetParameters().Length == 3 && mi.GetParameters()[2].ParameterType == CodeGen.Types.TypeOf(mdr.ValueTypes.DValueRef), "Invalid situation, method {0} must get a third parameter of type 'ref DValue'", mi);
        LoadValue(ilGen, values, resultIndex, i0Type);
        LoadValue(ilGen, values, resultIndex + 1, i1Type);
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        ilGen.Call(mi);
      }
      else
      {
        LoadValue(ilGen, values, resultIndex, mdr.ValueTypes.DValueRef);
        LoadValue(ilGen, values, resultIndex, i0Type);
        LoadValue(ilGen, values, resultIndex + 1, i1Type);
        ilGen.Call(mi);
        ilGen.Call(CodeGen.Types.DValue.Set.Get(returnType));
      }
      ilGen.Br(done);

      ilGen.MarkLabel(guardFail);
      ilGen.Ldarg_CallFrame();
      ilGen.Ldarg_1();
      ilGen.Ldc_I4((int)nodeType);
      ilGen.Ldc_I4(resultIndex);
      ilGen.Ldloc(t0);
      ilGen.Ldloc(t1);
      ilGen.Ldc_I4(i0TypeCheck);
      ilGen.Ldc_I4(i1TypeCheck);
      ilGen.Call(CodeGen.Types.Operations.ICMethods.RunAndUpdateBinaryOperationIC);
      ilGen.Ret();

      ilGen.MarkLabel(done);
    }

    public static int RunAndUpdateBinaryOperationIC(ref mdr.CallFrame callFrame, int index, IR.NodeType nodeType, int resultIndex, mdr.ValueTypes i0Type, mdr.ValueTypes i1Type, bool i0TypeCheck, bool i1TypeCheck)
    {
      var ilGen = JSRuntime.Instance.AsmGenerator.GetILGenerator();
      var funcMetadata = callFrame.Function.Metadata;
      var methodName = funcMetadata.FullName + "__" + index.ToString();
      ilGen.BeginICMethod(methodName);
      CreateBinaryOperationIC(ilGen, nodeType, resultIndex, i0Type, i1Type, i0TypeCheck, i1TypeCheck);
      ilGen.Ldc_I4(index + 1);
      ilGen.Ret();
      ilGen.WriteComment("Reinstalling this at index {0}", index);

      var m = ilGen.EndICMethod((JSFunctionMetadata)funcMetadata);
      funcMetadata.InlineCache[index] = m;
      return m(ref callFrame, index);
    }


    /// <summary>
    /// We use this function that may not be inlined and also enabled the common case to be inlined
    /// </summary>
    public static mdr.PropertyDescriptor GetPropertyDescriptor_Slow(ref mdr.CallFrame callFrame, int valueIndex, int fieldId)
    {
      var values = callFrame.Values;
      Debug.Assert(values[valueIndex].ValueType == mdr.ValueTypes.Undefined, "callFrame.Values[{0}].ValueType = {1} and is not undefined", valueIndex, values[valueIndex].ValueType);
      //first time visit
      var pd = GetContext(ref callFrame).GetPropertyDescriptorByFieldId(fieldId);
      values[valueIndex].Set(pd);
      return pd;
    }
    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static mdr.PropertyDescriptor GetPropertyDescriptor(ref mdr.CallFrame callFrame, int valueIndex, int fieldId)
    {
      var values = callFrame.Values;
      mdr.PropertyDescriptor pd;
      if (values[valueIndex].ValueType != mdr.ValueTypes.Undefined)
        pd = (mdr.PropertyDescriptor)values[valueIndex].AsObject();
      else
        pd = GetPropertyDescriptor_Slow(ref callFrame, valueIndex, fieldId);
      return pd;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void ReadFromContext(ref mdr.CallFrame callFrame, int resultIndex, int valueIndex, int fieldId)
    {
      var pd = GetPropertyDescriptor(ref callFrame, valueIndex, fieldId);
      var context = GetContext(ref callFrame);
      pd.Get(context, ref callFrame.Values[resultIndex]);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void WriteToContext(ref mdr.CallFrame callFrame, int resultIndex, int valueIndex, int fieldId)
    {
      var pd = GetPropertyDescriptor(ref callFrame, valueIndex, fieldId);
      if (pd.IsUndefined)
      {
        JSRuntime.Instance.GlobalContext.SetFieldByFieldId(fieldId, ref callFrame.Values[resultIndex]);
      }
      else
      {
        var context = GetContext(ref callFrame);
        pd.Set(context, ref callFrame.Values[resultIndex]);
      }
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void WriteValueToContext(ref mdr.CallFrame callFrame, int valueIndex, int fieldId, ref mdr.DValue value)
    {
      var pd = GetPropertyDescriptor(ref callFrame, valueIndex, fieldId);
      if (pd.IsUndefined)
      {
        JSRuntime.Instance.GlobalContext.SetFieldByFieldId(fieldId, ref value);
      }
      else
      {
        var context = GetContext(ref callFrame);
        pd.Set(context, ref value);
      }
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void CreateFunction(ref mdr.CallFrame callFrame, int resultIndex, int functionIndex)
    {
      var func = new mdr.DFunction(
       ((JSFunctionMetadata)callFrame.Function.Metadata).SubFunctions[functionIndex]
       , GetContext(ref callFrame));
      callFrame.Values[resultIndex].Set(func);
    }

    public static void TryCatchFinally(ref mdr.CallFrame callFrame, int tryBeginIndex, int tryEndIndex, int catchBeginIndex, int catchEndIndex, int finallyBeginIndex, int finallyEndIndex, int exceptionVariableIndex)
    {
      try
      {
        Execute(ref callFrame, tryBeginIndex, tryEndIndex);
      }
      catch (JSException e)
      {
        if (catchBeginIndex <= catchEndIndex)
        {
          //write e to proper .Values location
          callFrame.Values[exceptionVariableIndex] = e.Value;
          Execute(ref callFrame, catchBeginIndex, catchEndIndex);
        }
        else
          throw;
      }
      finally
      {
        Execute(ref callFrame, finallyBeginIndex, finallyEndIndex);
      }
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void ReadIndexer(ref mdr.CallFrame callFrame, int resultIndex)
    {
      //TODO: make this dynamically adaptive
      var values = callFrame.Values;
      var container = values[resultIndex].AsDObject();
      container.GetField(ref values[resultIndex + 1], ref values[resultIndex]);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void ReadProperty(ref mdr.CallFrame callFrame, int resultIndex, int fieldId)
    {
      var values = callFrame.Values;
      var container = values[resultIndex].AsDObject();
      container.GetFieldByFieldId(fieldId, ref values[resultIndex]);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void WriteIndexer(ref mdr.CallFrame callFrame, int resultIndex)
    {
      //TODO: make this dynamically adaptive
      var values = callFrame.Values;
      var container = values[resultIndex].AsDObject();
      container.SetField(ref values[resultIndex + 1], ref values[resultIndex + 2]);
      values[resultIndex] = values[resultIndex + 2];
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static void WriteProperty(ref mdr.CallFrame callFrame, int resultIndex, int fieldId)
    {
      var values = callFrame.Values;
      var container = values[resultIndex].AsDObject();
      container.SetFieldByFieldId(fieldId, ref values[resultIndex + 1]);
      values[resultIndex] = values[resultIndex + 1];
    }

  }
}
