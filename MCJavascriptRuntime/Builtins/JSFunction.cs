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
    class JSFunction : JSBuiltinConstructor
    {
        public JSFunction()
            : base(mdr.Runtime.Instance.DFunctionPrototype, "Function")
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                //ECMA 15.3.2.1
                Debug.WriteLine("calling new Function");
                StringBuilder properFunctionDeclaration = new StringBuilder("return function ");
                int argsCount = 0;
                string body;
                if (callFrame.PassedArgsCount == 0){
                    argsCount = 0;
                    body = "";
                }
                else
                if (callFrame.PassedArgsCount == 1){
                    argsCount = 0;
                    body = callFrame.Arg(0).AsString();
                }
                else{
                    argsCount = callFrame.PassedArgsCount - 1;
                    body = callFrame.Arg(callFrame.PassedArgsCount - 1).AsString();
                }

                properFunctionDeclaration.Append("(");
                for (int i = 0; i < argsCount;i++ )
                {
                    properFunctionDeclaration.Append(callFrame.Arg(i).AsString());
                    if (i < argsCount - 1)
                    {
                        properFunctionDeclaration.Append(",");
                    }
                }
                properFunctionDeclaration.Append(")");

                properFunctionDeclaration.Append("{").Append(body).Append("}");
                string properFunctionDecString = properFunctionDeclaration.ToString();
                Debug.WriteLine("new Function: {0}", properFunctionDecString);
                JSGlobalObject.EvalString(properFunctionDecString, ref callFrame.Return);
                if (IsConstrutor)
                    callFrame.This = callFrame.Return.AsDFunction();
            };            

            // toString is implementd in DObject
            //TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            //{
            //    Debug.WriteLine("calling JSFunction.toString");
            //    var function = callFrame.This as mdr.DFunction;
            //    if (function == null)
            //        Trace.Fail("TypeError");
            //    var funcStream = new System.IO.StringWriter();
            //    funcStream.WriteLine("{0} {{", Declaration(function));
            //    var astWriter = new mjr.Expressions.AstWriter(funcStream);
            //    astWriter.Execute((JSFunctionMetadata)function.Metadata);
            //    funcStream.WriteLine("}");
            //    callFrame.Return.Set(funcStream.ToString());
            //    //callFrame.Return.Set(function.ToString());
            //}), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("apply", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSFunction.apply");
                var tmpCallFrame = new mdr.CallFrame();
                tmpCallFrame.Function = callFrame.This.ToDFunction();
                if (tmpCallFrame.Function == null)
                    throw new InvalidOperationException(".apply should be called on a Function object");
                if (mdr.ValueTypesHelper.IsDefined(callFrame.Arg0.ValueType))
                    tmpCallFrame.This = Operations.Convert.ToObject.Run(ref callFrame.Arg0);
                else
                    tmpCallFrame.This = mdr.Runtime.Instance.GlobalContext;
                if (callFrame.PassedArgsCount > 1)
                {
                    if (callFrame.Arg1.ValueType != mdr.ValueTypes.Array)
                        throw new InvalidOperationException("second argument of .apply should be an array");
                    var args = callFrame.Arg1.AsDArray();
                    tmpCallFrame.PassedArgsCount = args.Length;
                    switch (tmpCallFrame.PassedArgsCount)
                    {  //putting goto case x will crash the mono on linux
                        case 0:
                            break;
                        case 1:
                            tmpCallFrame.Arg0 = args.Elements[0];
                            break;
                        case 2:
                            tmpCallFrame.Arg1 = args.Elements[1];
                            goto case 1;
                            //tmpCallFrame.Arg0 = args.Elements[0];
                            //break;
                        case 3:
                            tmpCallFrame.Arg2 = args.Elements[2];
                            goto case 2;
                            //tmpCallFrame.Arg1 = args.Elements[1];
                            //tmpCallFrame.Arg0 = args.Elements[0];
                            //break;
                        case 4:
                            tmpCallFrame.Arg3 = args.Elements[3];
                            goto case 3;
                            //tmpCallFrame.Arg2 = args.Elements[2];
                            //tmpCallFrame.Arg1 = args.Elements[1];
                            //tmpCallFrame.Arg0 = args.Elements[0];
                            //break;
                        default:
                            tmpCallFrame.Arguments = JSFunctionArguments.Allocate(tmpCallFrame.PassedArgsCount - mdr.CallFrame.InlineArgsCount);
                            Array.Copy(args.Elements, mdr.CallFrame.InlineArgsCount, tmpCallFrame.Arguments, 0, tmpCallFrame.PassedArgsCount - mdr.CallFrame.InlineArgsCount);
                            goto case 4;
                            //tmpCallFrame.Arg3 = args.Elements[3];
                            //tmpCallFrame.Arg2 = args.Elements[2];
                            //tmpCallFrame.Arg1 = args.Elements[1];
                            //tmpCallFrame.Arg0 = args.Elements[0];
                            //break;
                    }
                    tmpCallFrame.Signature = new mdr.DFunctionSignature(args.Elements, tmpCallFrame.PassedArgsCount);
                }
                else
                    tmpCallFrame.PassedArgsCount = 0;
                tmpCallFrame.Function.Call(ref tmpCallFrame);
                if (tmpCallFrame.Arguments != null)
                    JSFunctionArguments.Release(tmpCallFrame.Arguments);

                callFrame.Return = tmpCallFrame.Return;
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("call", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSFunction.call");
                var tmpCallFrame = new mdr.CallFrame();
                tmpCallFrame.Function = callFrame.This.ToDFunction();
                if (tmpCallFrame.Function == null)
                    throw new InvalidOperationException(".call should be called on a Function object");

                switch (callFrame.PassedArgsCount)
                {
                    case 0:
                        tmpCallFrame.PassedArgsCount = 0;
                        tmpCallFrame.This = mdr.Runtime.Instance.GlobalContext;
                        //throw new InvalidOperationException(".call should be called with at least one parameter");
                        break;
                    case 1:
                        //tmpCallFrame.This = callFrame.Arg0;
                        if (mdr.ValueTypesHelper.IsDefined(callFrame.Arg0.ValueType))
                            tmpCallFrame.This = Operations.Convert.ToObject.Run(ref callFrame.Arg0);
                        else
                            tmpCallFrame.This = mdr.Runtime.Instance.GlobalContext;
                        tmpCallFrame.PassedArgsCount = callFrame.PassedArgsCount - 1;
                        break;
                    case 2:
                        tmpCallFrame.Arg0 = callFrame.Arg1;
                        goto case 1;
                        //tmpCallFrame.This = callFrame.Arg0;
                        //tmpCallFrame.ArgsCount = callFrame.ArgsCount - 1;
                        //break;
                    case 3:
                        tmpCallFrame.Arg1 = callFrame.Arg2;
                        goto case 2;
                        //tmpCallFrame.Arg0 = callFrame.Arg1;
                        //tmpCallFrame.This = callFrame.Arg0;
                        //tmpCallFrame.ArgsCount = callFrame.ArgsCount - 1;
                        //break;
                    case 4:
                        tmpCallFrame.Arg2 = callFrame.Arg3;
                        goto case 3;
                        //tmpCallFrame.Arg1 = callFrame.Arg2;
                        //tmpCallFrame.Arg0 = callFrame.Arg1;
                        //tmpCallFrame.This = callFrame.Arg0;
                        //tmpCallFrame.ArgsCount = callFrame.ArgsCount - 1;
                        //break;
                    case 5:
                        tmpCallFrame.Arg3 = callFrame.Arguments[0];
                        goto case 4;
                        //tmpCallFrame.Arg2 = callFrame.Arg3;
                        //tmpCallFrame.Arg1 = callFrame.Arg2;
                        //tmpCallFrame.Arg0 = callFrame.Arg1;
                        //tmpCallFrame.This = callFrame.Arg0;
                        //tmpCallFrame.ArgsCount = callFrame.ArgsCount - 1;
                        //break;
                    default:
                        tmpCallFrame.Arguments = JSFunctionArguments.Allocate(tmpCallFrame.PassedArgsCount - mdr.CallFrame.InlineArgsCount);
                        Array.Copy(tmpCallFrame.Arguments, 0, callFrame.Arguments, 1, callFrame.PassedArgsCount - 1 - mdr.CallFrame.InlineArgsCount);
                        tmpCallFrame.Arg3 = callFrame.Arguments[0];
                        goto case 5;
                        //tmpCallFrame.Arg2 = callFrame.Arg3;
                        //tmpCallFrame.Arg1 = callFrame.Arg2;
                        //tmpCallFrame.Arg0 = callFrame.Arg1;
                        //tmpCallFrame.This = callFrame.Arg0;
                        //tmpCallFrame.ArgsCount = callFrame.ArgsCount - 1;
                        //break;
                }
                tmpCallFrame.Signature = new mdr.DFunctionSignature(ref tmpCallFrame, tmpCallFrame.PassedArgsCount);
                tmpCallFrame.Function.Call(ref tmpCallFrame);
                if (tmpCallFrame.Arguments != null)
                    JSFunctionArguments.Release(tmpCallFrame.Arguments);

                callFrame.Return = tmpCallFrame.Return;
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            /*TargetPrototype.DefineOwnProperty("bind", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSFunction.bind");
                Trace.Fail("Unimplemented");
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);*/
        }

        //string Declaration(mdr.DFunction function)
        //{
        //    var jsmetadata = function.Metadata as JSFunctionMetadata;
        //    if (jsmetadata == null)
        //        return null;
        //    if (jsmetadata.HasName)
        //        return string.Format("function {0}({1})", jsmetadata.Name, jsmetadata.Parameters != null ? string.Join(", ", jsmetadata.Parameters.ToArray()) : "");
        //    else
        //        return string.Format("function ({0})", jsmetadata.Parameters != null ? string.Join(", ", jsmetadata.Parameters.ToArray()) : "");
        //}

    }
}
