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

namespace mjr.Operations
{
  public struct Stack
  {
    public mdr.DValue[] Items;
    public int Sp;

    /*
     * The following functions are static to simplify the code generation
     * otherwise they could be instance methods and not need to pass ref Stack all the time
     */ 

    public static void Pop(ref Stack stack)
    {
      stack.Sp--;
    }
    public static void Dup(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex + 1;
      var items = stack.Items;
      items[resultIndex] = items[inputIndex];
      stack.Sp = resultIndex + 1;
    }
    public static int Reserve(int itemCount, ref Stack stack)
    {
      var slotBeginIndex = stack.Sp;
      stack.Sp += itemCount;
      return slotBeginIndex;
    }

    public static mdr.DObject CreateContext(ref mdr.CallFrame callFrame, ref Stack stack)
    {
      mdr.DObject context;
      var contextMap = callFrame.Function.Metadata.ContextMap;
      if (contextMap != null)
      {
        context = new mdr.DObject(contextMap);
      }
      else
      {
        var outerContext = callFrame.Function.OuterContext;
        context = new mdr.DObject(outerContext);
        callFrame.Function.Metadata.ContextMap = context.Map; //This will at least prevent the lookup in the DObject
      }
      return context;
    }
    //public static void LoadContext(ref mdr.CallFrame callFrame, ref Stack stack)
    //{
    //    //Debug.WriteLine("calling Exec.LoadContext");
    //    stack.Items[stack.Sp++].Set(callFrame.Function.Context);
    //}

    public static void Return(ref mdr.CallFrame callFrame, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Return");
      callFrame.Return = stack.Items[--stack.Sp];
    }
    public static void Throw(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Throw");
      //JSRuntime.Instance.CurrentException.Set(ref stack.Items[--stack.Sp]);
      //throw new JSException();
      throw new JSException(ref stack.Items[--stack.Sp]);
    }

    public static void CreateFunction(ref mdr.CallFrame callFrame, int funcDefIndex, mdr.DObject context, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.CreateFunction");
      var funcDef = ((JSFunctionMetadata)callFrame.Function.Metadata).SubFunctions[funcDefIndex];
      var func = new mdr.DFunction(funcDef, context);
      stack.Items[stack.Sp++].Set(func); ;
    }
    public static void CreateArray(int itemsCount, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.CreateArray");
      var sp = stack.Sp - 1;
      var array = new mdr.DArray(itemsCount);
      for (var i = itemsCount - 1; i >= 0; --i, --sp)
        array.Elements[i] = stack.Items[sp];
      stack.Items[sp + 1].Set(array);
      stack.Sp = sp + 2;
    }
    public static void CreateJson(int itemsCount, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.CreateJson");
      var obj = new mdr.DObject();
      var resultIndex = stack.Sp - itemsCount * 2;
      //TODO: here we know things are string, so just push their fieldIds and user then here!
      if (itemsCount > 0)
      {
        var sp = resultIndex;
        for (var i = itemsCount - 1; i >= 0; --i, sp += 2)
          obj.SetField(ref stack.Items[sp], ref stack.Items[sp + 1]);
      }
      stack.Items[resultIndex].Set(obj);
      stack.Sp = resultIndex + 1; ;
    }
    public static void CreateRegexp(string regex, string options, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.CreateRegexp");
      stack.Items[stack.Sp++].Set(new mdr.DRegExp(regex, options));
    }

    static void ReadArguments(ref mdr.CallFrame callFrame, int argsCount, ref Stack stack)
    {
      callFrame.PassedArgsCount = argsCount;
      var sp = stack.Sp - 1;
      var argIndex = argsCount - 1;
      switch (argsCount)
      {
        case 0:
          break;
        case 1:
          callFrame.Arg0 = stack.Items[sp];
          callFrame.Signature.InitArgType(argIndex--, callFrame.Arg0.ValueType);
          goto case 0;
        case 2:
          callFrame.Arg1 = stack.Items[sp--];
          callFrame.Signature.InitArgType(argIndex--, callFrame.Arg1.ValueType);
          goto case 1;
        case 3:
          callFrame.Arg2 = stack.Items[sp--];
          callFrame.Signature.InitArgType(argIndex--, callFrame.Arg2.ValueType);
          goto case 2;
        case 4:
          callFrame.Arg3 = stack.Items[sp--];
          callFrame.Signature.InitArgType(argIndex--, callFrame.Arg3.ValueType);
          goto case 3;
        default:
          {
            var remainintArgsCount = argsCount - mdr.CallFrame.InlineArgsCount;
            callFrame.Arguments = JSFunctionArguments.Allocate(remainintArgsCount);
            for (var i = remainintArgsCount - 1; i >= 0; --i)
            {
              callFrame.Arguments[i] = stack.Items[sp--];
              callFrame.Signature.InitArgType(argIndex--, callFrame.Arguments[i].ValueType);
            }
          }
          goto case 4;
      }

    }
    public static void New(int argsCount, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.New");
      var callFrame = new mdr.CallFrame();
      ReadArguments(ref callFrame, argsCount, ref stack);

      int funcIndex = stack.Sp - argsCount - 1;
      callFrame.Function = stack.Items[funcIndex].AsDFunction();
      callFrame.Function.Construct(ref callFrame);
      stack.Items[funcIndex].Set(callFrame.This);
      stack.Sp = funcIndex + 1;
    }
    public static void Call(ref mdr.CallFrame callerFrame, mdr.DObject context, int argsCount, bool hasThis, bool isDirectEvalCall, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Call");
      var calleeFrame = new mdr.CallFrame();
      ReadArguments(ref calleeFrame, argsCount, ref stack);

      int thisIndex = stack.Sp - argsCount - 1;
      int funcIndex;

      if (hasThis)
      {
        calleeFrame.This = Convert.ToObject.Run(ref stack.Items[thisIndex]);
        funcIndex = thisIndex - 1;
      }
      else
      {
        funcIndex = thisIndex;
        if (isDirectEvalCall)// && calleeFrame.Function == Builtins.JSGlobalObject.BuiltinEval) //We don't need to check for built in eval, ..
        {
          calleeFrame.CallerFunction = callerFrame.Function;
          calleeFrame.CallerContext = context;
          calleeFrame.This = callerFrame.This;
        }
        else
          calleeFrame.This = (mdr.Runtime.Instance.GlobalContext);
      }
      calleeFrame.Function = stack.Items[funcIndex].AsDFunction();
      int returnIndex = funcIndex;
      calleeFrame.Function.Call(ref calleeFrame);
      stack.Items[returnIndex] = calleeFrame.Return;
      stack.Sp = returnIndex + 1;
    }

    public static void TernaryOperation(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.TernaryOperation");
      var leftIndex = stack.Sp - 3;
      var middleIndex = leftIndex + 1;
      var rightIndex = middleIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      Ternary.Conditional.Run(
          ref items[leftIndex]
          , ref items[middleIndex]
          , ref items[rightIndex]
          , ref items[resultIndex]
      );
      stack.Sp = middleIndex;
    }

    #region Binary ops
    //delegate void BinaryOp1(ref mdr.DValue i0, ref mdr.DValue i1, ref mdr.DValue result);
    //static void BinaryOperation1(BinaryOp1 run, ref Stack stack)
    //{
    //    var leftIndex = stack.Sp - 2;
    //    var rightIndex = leftIndex + 1;
    //    var resultIndex = leftIndex;
    //    var items = stack.Items;
    //    run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
    //    stack.Sp = rightIndex;
    //}
    //public static void And(ref Stack stack)
    //{
    //    //Debug.WriteLine("calling Exec.And");
    //    //BinaryOperation1(Binary.LogicalAnd.Run, ref stack);
    //    var leftIndex = stack.Sp - 2;
    //    var rightIndex = leftIndex + 1;
    //    var resultIndex = leftIndex;
    //    var items = stack.Items;
    //    Binary.LogicalAnd.Run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
    //    stack.Sp = rightIndex;
    //}
    //public static void Or(ref Stack stack)
    //{
    //    //Debug.WriteLine("calling Exec.Or");
    //    var leftIndex = stack.Sp - 2;
    //    var rightIndex = leftIndex + 1;
    //    var resultIndex = leftIndex;
    //    var items = stack.Items;
    //    Binary.LogicalOr.Run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
    //    stack.Sp = rightIndex;
    //}
    public static void NotEqual(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.NotEqual");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.NotEqual.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void LesserOrEqual(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LesserOrEqual");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.LessThanOrEqual.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void GreaterOrEqual(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.GreaterOrEqual");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.GreaterThanOrEqual.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Lesser(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Lesser");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.LessThan.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Greater(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Greater");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.GreaterThan.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Equal(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Equal");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.Equal.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Minus(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Minus");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      Binary.Subtraction.Run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
      stack.Sp = rightIndex;
    }
    public static void Plus(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Plus");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      Binary.Addition.Run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
      stack.Sp = rightIndex;
    }
    public static void Modulo(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Modulo");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.Remainder.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Div(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Div");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.Divide.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Times(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Times");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      Binary.Multiply.Run(ref items[leftIndex], ref items[rightIndex], ref items[resultIndex]);
      stack.Sp = rightIndex;
    }
    public static void Pow(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Pow");
      Trace.Fail(new NotImplementedException());
      //Binary.Po.Run(ref items[stack.Sp - 2], ref items[stack.Sp - 1], ref items[stack.Sp - 2]);
      //stack.Sp -= 1;
    }
    public static void BitwiseAnd(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.BitwiseAnd");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.BitwiseAnd.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void BitwiseOr(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.BitwiseOr");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.BitwiseOr.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void BitwiseXOr(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.BitwiseXOr");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.BitwiseXor.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void Same(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Same");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.Same.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void NotSame(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.NotSame");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(!Binary.Same.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void LeftShift(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LeftShift");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.LeftShift.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void RightShift(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.RightShift");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.RightShift.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void UnsignedRightShift(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.UnsignedRightShift");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.UnsignedRightShift.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void InstanceOf(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.InstanceOf");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.InstanceOf.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
    }
    public static void In(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.In");
      var leftIndex = stack.Sp - 2;
      var rightIndex = leftIndex + 1;
      var resultIndex = leftIndex;
      var items = stack.Items;
      items[resultIndex].Set(Binary.In.Run(ref items[leftIndex], ref items[rightIndex]));
      stack.Sp = rightIndex;
      //stack.Items[stack.Sp - 2].Set(!Binary.In.Run(ref items[stack.Sp - 2], ref items[stack.Sp - 1]));
      //stack.Sp -= 1;
    }
    #endregion

    #region Convert ops
    public static void ToPrimitive(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      Convert.ToPrimitive.Run(ref items[inputIndex], ref items[resultIndex]);
    }
    public static void ToBoolean(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToBoolean.Run(ref items[inputIndex]));
    }
    public static void ToNumber(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      Convert.ToNumber.Run(ref items[inputIndex], ref items[resultIndex]);
    }
    public static void ToDouble(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToDouble.Run(ref items[inputIndex]));
    }
    public static void ToInteger(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToInt32.Run(ref items[inputIndex]));
    }
    public static void ToInt32(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToInt32.Run(ref items[inputIndex]));
    }
    public static void ToUInt32(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToUInt32.Run(ref items[inputIndex]));
    }
    public static void ToUInt16(ref Stack stack)
    {
      // TODO: Unused.
      /*var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToUInt32.Run(ref items[inputIndex]));*/
      throw new NotImplementedException();
    }
    public static void ToString(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToString.Run(ref items[inputIndex]));
    }
    public static void ToObject(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToObject.Run(ref items[inputIndex]));
    }
    public static void ToFunction(ref Stack stack)
    {
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Convert.ToObject.Run(ref items[inputIndex]).ToDFunction());
    }

    #endregion
    #region Unary ops
    public static void Delete(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Delete");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Unary.Delete.Run(ref items[inputIndex]));
    }
    public static void DeleteProperty(ref Stack stack)
    {
      var baseIndex = stack.Sp - 2;
      var fieldIndex = stack.Sp - 1;
      var valueIndex = baseIndex;

      stack.Items[valueIndex].Set(Unary.DeleteProperty.Run(stack.Items[baseIndex].AsDObject(), ref stack.Items[fieldIndex]));
      stack.Sp = fieldIndex;
    }
    public static void DeleteVariable(ref Stack stack)
    {
      var baseIndex = stack.Sp - 2;
      var fieldIndex = stack.Sp - 1;
      var valueIndex = baseIndex;

      stack.Items[valueIndex].Set(Unary.DeleteVariable.Run(stack.Items[baseIndex].AsDObject(), stack.Items[fieldIndex].AsInt32()));
      stack.Sp = fieldIndex;
    }
    public static void Void(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Void");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Unary.Void.Run(ref items[inputIndex]));
    }
    public static void TypeOf(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.TypeOf");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Operations.Unary.Typeof.Run(ref items[inputIndex]));
    }
    public static void Positive(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Positive");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      Unary.Positive.Run(ref items[inputIndex], ref items[resultIndex]);
    }
    public static void Negate(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.Negate");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      Unary.Negative.Run(ref items[inputIndex], ref items[resultIndex]);
    }
    public static void BitwiseNot(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.BitwiseNot");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Unary.BitwiseNot.Run(ref items[inputIndex]));
    }
    public static void LogicalNot(ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LogicalNot");
      var inputIndex = stack.Sp - 1;
      var resultIndex = inputIndex;
      var items = stack.Items;
      items[resultIndex].Set(Unary.LogicalNot.Run(ref items[inputIndex]));
    }
    //public static void PrefixPlusPlus(int returnIndex, ref Stack stack)
    //{
    //  //Debug.WriteLine("calling Exec.PrefixPlusPlus");
    //  var inputIndex = stack.Sp - 1;
    //  var resultIndex = inputIndex;
    //  var items = stack.Items;
    //  Unary.IncDec.AddConst(ref items[resultIndex], ref items[inputIndex], 1, false, ref items[returnIndex]);
    //}
    //public static void PrefixMinusMinus(int returnIndex, ref Stack stack)
    //{
    //  //Debug.WriteLine("calling Exec.PrefixMinusMinus");
    //  var inputIndex = stack.Sp - 1;
    //  var resultIndex = inputIndex;
    //  var items = stack.Items;
    //  Unary.IncDec.AddConst(ref items[resultIndex], ref items[inputIndex], -1, false, ref items[returnIndex]);
    //}
    //public static void PostfixPlusPlus(int returnIndex, ref Stack stack)
    //{
    //  //Debug.WriteLine("calling Exec.PostfixPlusPlus");
    //  var inputIndex = stack.Sp - 1;
    //  var resultIndex = inputIndex;
    //  var items = stack.Items;
    //  Unary.IncDec.AddConst(ref items[resultIndex], ref items[inputIndex], 1, true, ref items[returnIndex]);
    //}
    //public static void PostfixMinusMinus(int returnIndex, ref Stack stack)
    //{
    //  //Debug.WriteLine("calling Exec.PostfixMinusMinus");
    //  var inputIndex = stack.Sp - 1;
    //  var resultIndex = inputIndex;
    //  var items = stack.Items;
    //  Unary.IncDec.AddConst(ref items[resultIndex], ref items[inputIndex], -1, true, ref items[returnIndex]);
    //}

    #endregion

    public static void LoadFieldByFieldId(int fieldId, int popOperandCount, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LoadFieldByFieldId");
      var baseIndex = stack.Sp - 1;
      int valueIndex = stack.Sp - popOperandCount;
      stack.Items[baseIndex].AsDObject().GetFieldByFieldId(fieldId, ref stack.Items[valueIndex]);
      //stack.Items[valueIndex] = stack.Items[baseIndex].GetFieldContainer().GetFieldByFieldId(fieldId);
      stack.Sp = valueIndex + 1;
    }
    public static void LoadField(int popOperandCount, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LoadField");
      var baseIndex = stack.Sp - 2;
      var fieldIndex = stack.Sp - 1;
      int valueIndex = stack.Sp - popOperandCount;
      stack.Items[baseIndex].AsDObject().GetField(ref stack.Items[fieldIndex], ref stack.Items[valueIndex]);
      //stack.Items[valueIndex] = stack.Items[baseIndex].GetFieldContainer().GetField(ref stack.Items[fieldIndex]);
      stack.Sp = valueIndex + 1;
    }

    public static void StoreFieldByFieldId(int fieldId, bool pushBackResult, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.StoreFieldByFieldId");
      var baseIndex = stack.Sp - 2;
      var valueIndex = stack.Sp - 1;
      stack.Items[baseIndex].AsDObject().SetFieldByFieldId(fieldId, ref stack.Items[valueIndex]);
      if (pushBackResult)
      {
        stack.Items[baseIndex] = stack.Items[valueIndex];
        stack.Sp = valueIndex;
      }
      else
        stack.Sp = baseIndex;
    }
    public static void StoreField(bool pushBackResult, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.StoreField");
      var baseIndex = stack.Sp - 3;
      var fieldIndex = stack.Sp - 2;
      var valueIndex = stack.Sp - 1;

      stack.Items[baseIndex].AsDObject().SetField(ref stack.Items[fieldIndex], ref stack.Items[valueIndex]);
      if (pushBackResult)
      {
        stack.Items[baseIndex] = stack.Items[valueIndex];
        stack.Sp = fieldIndex;
      }
      else
        stack.Sp = baseIndex;
    }

    public static void LoadArg(ref mdr.CallFrame callFrame, int argIndex, ref Stack stack)
    {
      //Debug.WriteLine("calling Exec.LoadArg");
      if (argIndex >= callFrame.PassedArgsCount)
      {
        stack.Items[stack.Sp++].Set(mdr.Runtime.Instance.DefaultDUndefined);
        return;
      }
      switch (argIndex)
      {
        case 0: stack.Items[stack.Sp++].Set(ref callFrame.Arg0); break;
        case 1: stack.Items[stack.Sp++].Set(ref callFrame.Arg1); break;
        case 2: stack.Items[stack.Sp++].Set(ref callFrame.Arg2); break;
        case 3: stack.Items[stack.Sp++].Set(ref callFrame.Arg3); break;
        default: stack.Items[stack.Sp++].Set(ref callFrame.Arguments[argIndex - mdr.CallFrame.InlineArgsCount]); break;
      }
    }
    public static void StoreArg(ref mdr.CallFrame callFrame, int argIndex, bool pushBackResult, ref Stack stack)
    {
      var valueIndex = stack.Sp - 1;
      Debug.WriteLine("calling Exec.StoreArg index {0} vindex {1} cf arg count {2}", argIndex, valueIndex, callFrame.PassedArgsCount);
      ///Since an arguments may be written and then read again in the same function
      ///we should make sure the value is stored properly. 
      ///for the first 4 arguments, we have a storage in the call frame
      ///beyond that is tricky! We can extend the Arguments array, but we have to make sure
      ///no one has a pointer/reference to any of its elements
      ///For now, we fail, but we should fix this later
      switch (argIndex)
      {
        case 0: callFrame.Arg0.Set(ref stack.Items[valueIndex]); break;
        case 1: callFrame.Arg1.Set(ref stack.Items[valueIndex]); break;
        case 2: callFrame.Arg2.Set(ref stack.Items[valueIndex]); break;
        case 3: callFrame.Arg3.Set(ref stack.Items[valueIndex]); break;
        default:
          if (argIndex < callFrame.PassedArgsCount)
          {
            callFrame.Arguments[argIndex - mdr.CallFrame.InlineArgsCount].Set(ref stack.Items[valueIndex]); break;
          }
          else
          {
            Debug.Fail("argIndex {0} > argsCount {1}", argIndex, callFrame.PassedArgsCount);
          }
          break;
      }
      if (!pushBackResult)
        stack.Sp = valueIndex;
    }


    public static void LoadThis(ref mdr.CallFrame callFrame, ref Stack stack) { stack.Items[stack.Sp++].Set(callFrame.This); }
    public static void DeclareVariable(mdr.DObject context, string field, int fieldId, ref Stack stack)
    {
      //we may be looking at a global or this might be second time we call this function with the same context, so following assert will fail (incorrectly)
      //Debug.Assert(!context.HasOwnPropertyByFieldId(fieldId), "Cannot redeclare local variable {0}", field);

      context.AddOwnPropertyDescriptorByFieldId(fieldId, mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotConfigurable);
    }
    public static void LoadVariable(mdr.DObject context, int fieldId, int ancestorDistance, ref Stack stack)
    {
      mdr.PropertyDescriptor pd = null;

      //TODO: If we do not create a prototype for GlobalContext, the following code would have been enough!
      //var pd = context.GetPropertyDescriptorByLineId(fieldId);
      //pd.Get(context, ref stack.Items[stack.Sp++]);

      if (ancestorDistance < 0)
      {//We are dealing with unknown symbol type
        while (context != mdr.Runtime.Instance.GlobalContext)
        {
          pd = context.Map.GetPropertyDescriptorByFieldId(fieldId);
          if (pd != null)
            break;
          context = context.Prototype;
        }
      }
      else
      {//we are dealing with known symbol type
        for (var i = 0; i < ancestorDistance && context != mdr.Runtime.Instance.GlobalContext; ++i)
        {
          context = context.Prototype;
        }
      }
      if (pd == null)
        pd = context.GetPropertyDescriptorByFieldId(fieldId);
      pd.Get(context, ref stack.Items[stack.Sp++]);
    }
    public static void StoreVariable(mdr.DObject context, int fieldId, int ancestorDistance, bool pushBackResult, ref Stack stack)
    {
      var valueIndex = stack.Sp - 1;

      //TODO: If we do not create a prototype for GlobalContext, the following code would have been enough!
      //var pd = context.Map.GetPropertyDescriptorByFieldId(fieldId);
      //if (pd != null)
      //    pd.Set(context, ref stack.Items[stack.Sp - 1]);
      //else
      //    mdr.Runtime.Instance.GlobalContext.SetFieldByFieldId(fieldId, ref stack.Items[stack.Sp - 1]); //DOTO: this is very expensive!

      mdr.PropertyDescriptor pd = null;

      if (ancestorDistance < 0)
      {//We are dealing with unknown symbol type
        //while (context != mdr.Runtime.Instance.GlobalContext)
        //{
        //    pd = context.Map.GetCachedOwnPropertyDescriptorByFieldId(fieldId);
        //    if (pd != null)
        //        break;
        //    context = context.Prototype;
        //}
        //if (pd != null)
        //    pd.Set(context, ref stack.Items[stack.Sp - 1]);
        //else
        //    context.SetFieldByFieldId(fieldId, ref stack.Items[stack.Sp - 1]);

        pd = context.Map.GetPropertyDescriptorByFieldId(fieldId);
        if (pd != null && !pd.IsUndefined)
          pd.Set(context, ref stack.Items[valueIndex]);
        else
          mdr.Runtime.Instance.GlobalContext.SetFieldByFieldId(fieldId, ref stack.Items[valueIndex]);

      }
      else
      {//we are dealing with known symbol type
        for (var i = 0; i < ancestorDistance && context != mdr.Runtime.Instance.GlobalContext; ++i)
        {
          context = context.Prototype;
        }
        pd = context.Map.GetPropertyDescriptorByFieldId(fieldId);
        Debug.Assert(pd != null && pd.IsDataDescriptor, "Invalid situation, variable is undeclared in current context");
        pd.Set(context, ref stack.Items[valueIndex]);
      }

      if (!pushBackResult)
        stack.Sp = valueIndex;
    }

    public static void LoadUndefined(ref Stack stack) { stack.Items[stack.Sp++].Set(mdr.Runtime.Instance.DefaultDUndefined); }
    public static void LoadNull(ref Stack stack) { stack.Items[stack.Sp++].Set(mdr.Runtime.Instance.DefaultDNull); }
    public static void LoadString(string value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadDouble(double value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadInt(int value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadBoolean(bool value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadDObject(mdr.DObject value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadAny(object value, ref Stack stack) { stack.Items[stack.Sp++].Set(value); }
    public static void LoadDValue(ref mdr.DValue value, ref Stack stack) { stack.Items[stack.Sp++].Set(ref value); }

  }
}
