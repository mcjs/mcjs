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
using System.Text;

using m.Util.Diagnose;

namespace mjr.Builtins
{
    class JSArray : JSBuiltinConstructor
    {
        public JSArray()
            : base(mdr.Runtime.Instance.DArrayPrototype, "Array")
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                mdr.DArray array;

                var argsCount = callFrame.PassedArgsCount;

                if (argsCount == 1)
                {
                    var len = Operations.Convert.ToInt32.Run(ref callFrame.Arg0);
                    array = new mdr.DArray(len);
                }
                else
                {
                    array = new mdr.DArray(argsCount);
                    switch (argsCount)
                    {
                        case 0: break;
                        case 1: break;
                        case 2:
                            array.Elements[1] = callFrame.Arg1;
                            array.Elements[0] = callFrame.Arg0;
                            break;
                        case 3:
                            array.Elements[2] = callFrame.Arg2;
                            goto case 2;
                        case 4:
                            array.Elements[3] = callFrame.Arg3;
                            goto case 3;
                        default:
                            Debug.Assert(argsCount > mdr.CallFrame.InlineArgsCount, "Code gen must be updated to support new CallFrame");
                            Array.Copy(callFrame.Arguments, 0, array.Elements, mdr.CallFrame.InlineArgsCount, argsCount - mdr.CallFrame.InlineArgsCount);
                            goto case 4;
                    }
                }
                if (IsConstrutor)
                    callFrame.This = (array);
                else
                    callFrame.Return.Set(array);
            };

            // ECMA-262 section 15.4.3.1
            this.DefineOwnProperty("isArray", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.isArray");
                if (callFrame.This.ValueType == mdr.ValueTypes.Array)
                    callFrame.Return.Set(true);
                else
                    callFrame.Return.Set(false);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.2
            TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.toString");
                var join = callFrame.This.GetField("join");
                if (join.ValueType == mdr.ValueTypes.Function)// it is callable
                {
                    var joinFun = join.AsDFunction();
                    Debug.Assert(joinFun != null, "Invalid situation!");
                    callFrame.Function = joinFun;
                    //callFrame.This is already set
                    callFrame.Signature = mdr.DFunctionSignature.EmptySignature;
                    callFrame.PassedArgsCount = 0;
                    callFrame.Arguments = null;
                    joinFun.Call(ref callFrame);
                    callFrame.Return.Set(callFrame.Return.AsString());
                }
                else
                    callFrame.Return.Set(callFrame.This.ToString());
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.3
            TargetPrototype.DefineOwnProperty("toLocaleString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                throw new NotImplementedException("Array.toLocaleString is not implemented");
                // TODO: callFrame.This is not an accurate implementation
                /*mdr.DObject array = callFrame.This;
                mdr.DFunction join = array.GetField("join").ToDObject() as mdr.DFunction;
                if (join != null) // it is callable
                {
                    callFrame.Function = join;
                    callFrame.Signature = mdr.DFunctionSignature.EmptySignature;
                    callFrame.Arguments = null;
                    join.Call(ref callFrame);
                    callFrame.Return.Set(callFrame.Return.ToString());    //TODO: is this the right implementation?
                }
                else
                    callFrame.Return.Set(array.ToString());*/
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.4
            TargetPrototype.DefineOwnProperty("concat", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.concat");
                //The most common case is calling .concat on array object with normal set of arguments
                mdr.DArray newArray;
                var destStartIndex = 0;

                var thisArray = callFrame.This as mdr.DArray;
                if(thisArray != null)
                {
                  newArray = new mdr.DArray(callFrame.PassedArgsCount + thisArray.Length);
                  Array.Copy(thisArray.Elements, 0, newArray.Elements, 0, thisArray.Length);
                  destStartIndex = thisArray.Length;
                }
                else 
                {
                  newArray = new mdr.DArray(callFrame.PassedArgsCount + 1);
                  newArray.Elements[0].Set(callFrame.This);
                  destStartIndex = 1;
                }
                for (int i = 0; i < callFrame.PassedArgsCount; ++i)
                {
                  newArray.Elements[destStartIndex] = callFrame.Arg(i); //This is the common case
                  if (newArray.Elements[destStartIndex].ValueType == mdr.ValueTypes.Array)
                  {
                    var array = newArray.Elements[destStartIndex].AsDArray();
                    //We had already accounted 1 cell for this item, so, we add the missing remaining elements
                    newArray.Length += (array.Length - 1); //Extends newArray.Elements
                    Array.Copy(array.Elements, 0, newArray.Elements, destStartIndex, array.Length);
                    destStartIndex += array.Length;
                  }
                  else
                    ++destStartIndex;
                }
                //concat(newArray, ref callFrame.Arguments[i]);

                callFrame.Return.Set(newArray);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.5
            TargetPrototype.DefineOwnProperty("join", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.join");
                string separator = (callFrame.PassedArgsCount == 0 || callFrame.Arg0.ValueType == mdr.ValueTypes.Undefined)
                    ? ","
                    : callFrame.Arg0.AsString();

                callFrame.Return.Set(join(callFrame.This, separator).ToString());
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.6
            TargetPrototype.DefineOwnProperty("pop", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.pop");
                var This = callFrame.This;
                var lenField = This.GetField("length");
                int len = Operations.Convert.ToInt32.Run(ref lenField);
                if (len == 0)
                {
                    This.SetField("length", 0);
                    callFrame.Return.Set(mdr.Runtime.Instance.DefaultDUndefined);
                }
                else if (len > 0)
                {
                    var index = len - 1;
                    mdr.DValue element = new mdr.DValue();
                    This.GetField(index, ref element);
                    This.DeletePropertyDescriptor(index.ToString());
                    This.SetField("length", index);
                    callFrame.Return.Set(ref element);
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.7
            TargetPrototype.DefineOwnProperty("push", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.push");
                var This = callFrame.This;
                var lenField = This.GetField("length");
                int len = Operations.Convert.ToInt32.Run(ref lenField);
                mdr.DValue arg = new mdr.DValue();
                for (int i = 0; i < callFrame.PassedArgsCount; ++i)
                {
                    arg = callFrame.Arg(i);
                    This.SetField(len, ref arg);
                    len++;
                }
                This.SetField("length", len);
                callFrame.Return.Set(len);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.8
            TargetPrototype.DefineOwnProperty("reverse", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.reverse");
                var This = callFrame.This;
                var lenField = This.GetField("length");
                int len = Operations.Convert.ToInt32.Run(ref lenField);
                int middle = len / 2;
                mdr.DValue lowerValue = new mdr.DValue();
                mdr.DValue upperValue = new mdr.DValue();
                bool lowerExists, upperExists;
                for (int lower = 0; lower != middle; lower++)
                {
                    int upper = len - lower - 1;
                    lowerExists = This.HasProperty(lower);
                    upperExists = This.HasProperty(upper);
                    if (lowerExists && upperExists)
                    {
                        This.GetField(lower, ref lowerValue);
                        This.GetField(upper, ref upperValue);
                        This.SetField(lower, ref upperValue);
                        This.SetField(upper, ref lowerValue);
                    }
                    else if (!lowerExists && upperExists)
                    {
                        This.GetField(upper, ref upperValue);
                        This.SetField(lower, ref upperValue);
                        This.DeletePropertyDescriptor(upper.ToString());
                    }
                    else if (lowerExists && !upperExists)
                    {
                        This.GetField(lower, ref lowerValue);
                        This.SetField(upper, ref lowerValue);
                        This.DeletePropertyDescriptor(lower.ToString());
                    }
                }
                callFrame.Return.Set(This);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.9
            TargetPrototype.DefineOwnProperty("shift", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.shift");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                if (array == null || array.Length == 0)
                    //callFrame.Return.ValueType = mdr.ValueTypes.Undefined;
                    callFrame.Return.Set(mdr.Runtime.Instance.DefaultDUndefined);
                else
                {
                    callFrame.Return.Set(ref array.Elements[0]);
                    for (int k = 1; k < len; k++)
                        array.Elements[k - 1].Set(ref array.Elements[k]);

                    array.Length--;
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.10
            TargetPrototype.DefineOwnProperty("slice", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.slice");
                /*  TODO: This is the implementation for a case that "this" is a DArray. Use this faster implementation after adding PackedArray optimization.
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                int relativeStart = callFrame.Arg0.ToInt32();
                int k = (relativeStart < 0) ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
                int relativeEnd = (callFrame.Arg1.ValueType == mdr.ValueTypes.Undefined) ? len : callFrame.Arg1.ToInt32();
                int final = (relativeEnd < 0) ? Math.Max(relativeEnd + len, 0) : Math.Min(relativeEnd, len);
                mdr.DArray newArray = new mdr.DArray((final - k > 0) ? (final - k) : 0);
                for (int n = 0; k < final; k++, n++)
                    newArray.Elements[n].Set(ref array.Elements[k]);
                callFrame.Return.Set(newArray);
                 */

                var This = callFrame.This;
                var lenField = This.GetField("length");
                int len = Operations.Convert.ToInt32.Run(ref lenField);
                int relativeStart = 0;
                if (callFrame.PassedArgsCount > 0 ) 
                  relativeStart = Operations.Convert.ToInt32.Run(ref callFrame.Arg0);
                int k = (relativeStart < 0) ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
                int relativeEnd = ((callFrame.Arg1.ValueType == mdr.ValueTypes.Undefined) || (callFrame.PassedArgsCount < 2)) ? len : Operations.Convert.ToInt32.Run(ref callFrame.Arg1 );
                int final = (relativeEnd < 0) ? Math.Max(relativeEnd + len, 0) : Math.Min(relativeEnd, len);
                mdr.DArray newArray = new mdr.DArray((final - k > 0) ? (final - k) : 0);
                mdr.DValue item = new mdr.DValue();
                for (int n = 0; k < final; k++, n++)
                {
                    This.GetField(k, ref item);
                    newArray.Elements[n].Set(ref item);
                }
                callFrame.Return.Set(newArray);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.12
            //TODO: splice is generic and can be applied to other objects
            TargetPrototype.DefineOwnProperty("splice", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.splice");
                if (callFrame.PassedArgsCount < 2)
                {
                    callFrame.Return.Set(JSRuntime.Instance.DefaultDUndefined);
                    return;
                }
                var A = new mdr.DArray();
                var This = callFrame.This as mdr.DArray;
                if (This == null)
                    throw new Exception("Object is not an array, but splice must work with generic objects. Please fix it!");
                int len = This.Length;
                int relativeStart = Operations.Convert.ToInt32.Run(ref callFrame.Arg0);
                int actualStart = relativeStart < 0 ? Math.Max(len + relativeStart, 0) : Math.Min(relativeStart, len);
                int actualDeleteCount = Math.Min(Math.Max(Operations.Convert.ToInt32.Run(ref callFrame.Arg1), 0), len - actualStart);

                A.Length = actualDeleteCount;
                for (int k = 0; k < actualDeleteCount; k++)
                {
                    int from = relativeStart + k;
                    if (from < len)
                        A.Elements[k].Set(ref This.Elements[from]);
                }

                int itemCount = callFrame.PassedArgsCount - 2;
                if (itemCount < actualDeleteCount)
                {
                    for (int k = actualStart; k < len - actualDeleteCount; k++)
                    {
                        int from = k + actualDeleteCount;
                        int to = k + itemCount;
                        // if (from < len) // This condition will always hold
                        This.Elements[to].Set(ref This.Elements[from]);
                        // from will always be less than less and therefore the element exists in the array 
                        //TODO: can we assume any index less than Length exist? When an element is deleted from middle of the Elements, is Length adjusted?
                        /*
                        else
                        {
                            This.RemoveElements(to, len - actualDeleteCount - k);
                            break;
                        }*/
                    }
                    This.RemoveElements(len - actualDeleteCount + itemCount, actualDeleteCount - itemCount);
                    This.Length = len - actualDeleteCount + itemCount;
                }
                else if (itemCount > actualDeleteCount)
                {
                    This.Length = len - actualDeleteCount + itemCount;
                    for (int k = len - actualDeleteCount; k > actualStart; k--)
                    {
                        int from = k + actualDeleteCount - 1;
                        int to = k + itemCount - 1;
                        //if (from < len) //This condition will always hold
                        This.Elements[to].Set(ref This.Elements[from]);
                    }
                }

                for (int k = 0; k < itemCount; k++)
                    This.Elements[k + actualStart] = callFrame.Arg(k + 2);

                
                callFrame.Return.Set(A);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.11
            TargetPrototype.DefineOwnProperty("sort", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.sort");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                if (len == 0)
                    callFrame.Return.Set(array);

                int argsCount = callFrame.PassedArgsCount;
                mdr.DValue function = callFrame.Arg0;

                for (uint i = 0; i < len - 1; ++i)
                {
                    mdr.DValue iObj = array.Elements[i];
                    uint themin = i;
                    mdr.DValue minObj = iObj;
                    for (uint j = i + 1; j < len; ++j)
                    {
                        mdr.DValue jObj = array.Elements[j];
                        double compareResult = 0;
                        if (jObj.ValueType == mdr.ValueTypes.Undefined)
                            compareResult = 1;
                        else if (minObj.ValueType == mdr.ValueTypes.Undefined)
                            compareResult = -1;
                        else if (argsCount == 1 && function.ValueType == mdr.ValueTypes.Function)
                        {
                            callFrame.Function = function.AsDFunction();
                            callFrame.SetArg(0, ref minObj);
                            callFrame.SetArg(1, ref jObj);
                            callFrame.PassedArgsCount = 2;
                            callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 2);
                            callFrame.Function.Call(ref callFrame);
                            compareResult = Operations.Convert.ToInt32.Run(ref callFrame.Return) <= 0 ? 1 : -1;
                        }
                        else
                        {
                            mdr.DValue v1 = mdr.DValue.Create(jObj.AsString());
                            mdr.DValue v2 = mdr.DValue.Create(minObj.AsString());
                            compareResult = Operations.Binary.LessThan.Run(ref v1, ref v2) ? -1 : 1;
                        }

                        if (compareResult < 0)
                        {
                            themin = j;
                            minObj = jObj;
                        }
                    }
                    if (themin > i)
                    {
                        array.Elements[i] = minObj;
                        array.Elements[themin] = iObj;
                    }
                }
                callFrame.Return.Set(array);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.13
            TargetPrototype.DefineOwnProperty("unshift", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.unshift");
                var thisObj = callFrame.This;
                mdr.DArray array = thisObj.ToDArray();

                int len = array.Length;
                int argsCount = callFrame.PassedArgsCount;
                array.ResizeElements(len + argsCount);
                array.Length = len + argsCount;
                if (argsCount != 0 && len != 0)
                {
                    mdr.DValue iObj;
                    for (int i = len; i > 0; --i)
                    {
                        iObj = array.Elements[i - 1];
                        array.Elements[i - 1].Set(0);
                        array.Elements[i + argsCount - 1] = iObj;

                    }
                }
                for (int k = 0; k < argsCount; ++k)
                {
                    array.Elements[k] = callFrame.Arg(k);
                }
                callFrame.Return.Set(array.Length);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.14
            TargetPrototype.DefineOwnProperty("indexOf", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.indexOf");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                int index = 0;
                if (callFrame.PassedArgsCount > 1)
                    index = Operations.Convert.ToInt32.Run(ref callFrame.Arg1);
                index = index < 0 ? Math.Max(len + index, 0) : Math.Min(index, len);
                mdr.DValue searchElem = callFrame.Arg0;
                for (; index < len; ++index)
                {
                    mdr.DValue indexElem = array.Elements[index];
                    if (indexElem.ValueType != mdr.ValueTypes.Undefined
                        && Operations.Binary.Equal.Run(ref indexElem, ref searchElem))
                    {
                        callFrame.Return.Set(index);
                        return;
                    }
                }
                callFrame.Return.Set(-1);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.15
            TargetPrototype.DefineOwnProperty("lastIndexOf", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.lastIndexOf");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                if (len == 0)
                {
                    callFrame.Return.Set(-1);
                    return;
                }
                int index = len - 1;
                if (callFrame.PassedArgsCount > 1)
                    index = Operations.Convert.ToInt32.Run(ref callFrame.Arg1);
                index = index < 0 ? len + index : Math.Min(index, len - 1);
                if (index < 0)
                {
                    callFrame.Return.Set(-1);
                    return;
                }
                mdr.DValue searchElem = callFrame.Arg0;
                do
                {
                    mdr.DValue indexElem = array.Elements[index];
                    if (indexElem.ValueType != mdr.ValueTypes.Undefined
                        && Operations.Binary.Equal.Run(ref indexElem, ref searchElem))
                    {
                        callFrame.Return.Set(index);
                        return;
                    }
                } while (index-- > 0);
                callFrame.Return.Set(-1);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.16
            TargetPrototype.DefineOwnProperty("every", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.every");
                var thisObj = callFrame.This;
                mdr.DArray array = thisObj.ToDArray();
                int len = array.Length;
                if (len == 0)
                    callFrame.Return.Set(array);

                // TODO: Commented because argsCount is unused.
                /*int argsCount = callFrame.ArgsCount;*/
                mdr.DFunction function = callFrame.Arg0.AsDFunction();
                bool result = true;

                for (int i = 0; i < len; ++i)
                {
                    if (array.Elements[i].ValueType != mdr.ValueTypes.Undefined)
                    {
                        callFrame.Function = function;
                        callFrame.SetArg(0, ref array.Elements[i]);
                        callFrame.SetArg(1, i);
                        callFrame.SetArg(2, thisObj);
                        callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 3);
                        callFrame.Function.Call(ref callFrame);
                        if (Operations.Convert.ToBoolean.Run(ref callFrame.Return) == false)
                        {
                            result = false;
                            break;
                        }
                    }
                }
                callFrame.Return.Set(result);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.17
            TargetPrototype.DefineOwnProperty("some", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.some");
                var thisObj = callFrame.This;
                mdr.DArray array = thisObj.ToDArray();
                int len = array.Length;
                if (len == 0)
                    callFrame.Return.Set(array);

                // TODO: Commented because argsCount is unused.
                /*int argsCount = callFrame.ArgsCount;*/
                mdr.DFunction function = callFrame.Arg0.AsDFunction();
                bool result = false;

                for (int i = 0; i < len; ++i)
                {
                    if (array.Elements[i].ValueType != mdr.ValueTypes.Undefined)
                    {
                        callFrame.Function = function;
                        callFrame.SetArg(0, ref array.Elements[i]);
                        callFrame.SetArg(1, i);
                        callFrame.SetArg(2, thisObj);
                        callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 3);
                        callFrame.Function.Call(ref callFrame);
                        if (Operations.Convert.ToBoolean.Run(ref callFrame.Return) == true)
                        {
                            result = true;
                            break;
                        }
                    }
                }
                callFrame.Return.Set(result);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.18
            TargetPrototype.DefineOwnProperty("forEach", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.forEach");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                if (len == 0)
                    callFrame.Return.Set(array);

                mdr.DFunction function = callFrame.Arg0.AsDFunction();
 
                // TODO: Commented because thisArg is unused.
                /*mdr.DValue thisArg;
                if (callFrame.ArgsCount > 1)
                    thisArg = callFrame.Arg1;
                else
                    thisArg = new mdr.DValue();*/

                for (int i = 0; i < len; ++i)
                {
                    if (array.Elements[i].ValueType != mdr.ValueTypes.Undefined)
                    {
                        callFrame.Function = function;
                        callFrame.PassedArgsCount = 3;
                        callFrame.SetArg(0, ref array.Elements[i]);
                        callFrame.SetArg(1, i);
                        callFrame.SetArg(2, array);
                        callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 3);
                        callFrame.Function.Call(ref callFrame);
                    }
                }
                callFrame.Return.Set(mdr.Runtime.Instance.DefaultDUndefined);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.4.4.19
            TargetPrototype.DefineOwnProperty("map", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSArray.map");
                mdr.DArray array = callFrame.This.ToDArray();
                int len = array.Length;
                if (len == 0)
                    callFrame.Return.Set(array);

                mdr.DFunction function = callFrame.Arg0.AsDFunction();
                mdr.DObject thisArg;
                if (callFrame.PassedArgsCount > 1)
                    thisArg = Operations.Convert.ToObject.Run(ref callFrame.Arg1);
                else
                    thisArg = mdr.Runtime.Instance.GlobalContext;

                mdr.DArray newarray = new mdr.DArray(len);

                for (int i = 0; i < len; ++i)
                {
                    if (array.Elements[i].ValueType != mdr.ValueTypes.Undefined)
                    {
                        callFrame.Function = function;
                        callFrame.This = thisArg;
                        callFrame.PassedArgsCount = 3;
                        callFrame.SetArg(0, ref array.Elements[i]);
                        callFrame.SetArg(1, i);
                        callFrame.SetArg(2, array);
                        callFrame.Signature = new mdr.DFunctionSignature(ref callFrame, 3);
                        callFrame.Function.Call(ref callFrame);
                        newarray.Elements[i] = callFrame.Return;
                    }
                }
                callFrame.Return.Set(newarray);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("filter", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Trace.Fail("Unimplemented");
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("reduce", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Trace.Fail("Unimplemented");
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("reduceRight", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Trace.Fail("Unimplemented");
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

        }

        // ECMA-262 section 15.4.4.5
        //private StringBuilder join(mdr.DArray array) { return join(array, null); }
        private StringBuilder join(mdr.DObject obj, string separator)
        {
            if (obj == null)
                throw new ArgumentException("Object is null!");

            if (separator == null)
                separator = ",";

            StringBuilder strBuilder = new StringBuilder(string.Empty);
            var lengthField = obj.GetFieldByFieldId(mdr.Runtime.Instance.LengthFieldId);
            var length = Operations.Convert.ToInt32.Run(ref lengthField);
            if (length == 0)
                return strBuilder;

            var element_0 = obj.GetField(0);

            if (element_0.ValueType != mdr.ValueTypes.Undefined && element_0.ValueType != mdr.ValueTypes.Null)
                strBuilder.Append(Operations.Convert.ToString.Run(ref element_0));
            for (int k = 1; k < length; k++)
            {
                strBuilder.Append(separator);
                var element_k = obj.GetField(k);
                if (element_k.ValueType != mdr.ValueTypes.Undefined && element_k.ValueType != mdr.ValueTypes.Null)
                    strBuilder.Append(Operations.Convert.ToString.Run(ref element_k));
            }

            return strBuilder;
        }

        //private void concat(mdr.DArray array, ref mdr.DValue element)
        //{
        //    //if (element.ValueType == mdr.ValueTypes.Array)
        //    //{
        //    //    mdr.DArray elemArray = element.DObjectValue.ToDArray();
        //    //    int n = array.Length;
        //    //    array.Length += elemArray.Length;
        //    //    for (int k = 0; k < elemArray.Length; k++)
        //    //    {
        //    //        array.Elements[n].Set(ref elemArray.Elements[k]);
        //    //        n++;
        //    //    }
        //    //}
        //    //else
        //    {
        //        int n = array.Length;
        //        array.Length++;
        //        array.Elements[n].Set(ref element);
        //    }
        //}
    }
}
