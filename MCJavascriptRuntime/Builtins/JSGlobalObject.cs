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
using System.Text;
using mjr.Builtins.TypedArray;
using System.IO;

using m.Util.Diagnose;

namespace mjr.Builtins
{
    public static class JSGlobalObject
    {
#if DEBUG
        [ThreadStatic]
        static System.IO.StreamWriter printOutFile = null;
#endif
        internal static void EvalString(string inputString, ref mdr.DValue result, mdr.DFunction callerFunction = null, mdr.DObject callerContext = null, mdr.DObject thisArg = null)
        {
          var funcMetadata = JSParser.ParseScript(inputString).Expression.Metadata;
          var func = new mdr.DFunction(funcMetadata, null);

          var tempCallFrame = new mdr.CallFrame();
          bool isDirectEval = callerContext != null;

          if (isDirectEval)
          {
            //function will behave as if it was the caller
            Debug.Assert(thisArg != null && callerFunction != null && callerContext != null, "Invalid situation! Direct eval call must have thisArg, callerFunction, callerContext set");
            funcMetadata.Scope.IsProgram = false;
            funcMetadata.Scope.IsEvalFunction = true;
            funcMetadata.ParentFunction = (JSFunctionMetadata)callerFunction.Metadata;
            tempCallFrame.CallerContext = callerContext;
            tempCallFrame.This = thisArg;
          }
          else
          {
            //This will behave just like a program code
            tempCallFrame.CallerContext = mdr.Runtime.Instance.GlobalContext;
            tempCallFrame.This = (mdr.Runtime.Instance.GlobalContext);
          }

          //TODO: find a way to assign a name to this
          //funcMetadata.Name += "_eval"; //After we know the ParentFunction

          tempCallFrame.Function = func;
          tempCallFrame.Signature = mdr.DFunctionSignature.EmptySignature;
          func.Call(ref tempCallFrame);
          result.Set(ref tempCallFrame.Return);
        }

        internal static mdr.DFunction BuiltinEval = new mdr.DFunction((ref mdr.CallFrame callFrame) =>
          {
            if (callFrame.PassedArgsCount < 1)
              return;
            if (callFrame.Arg0.ValueType != mdr.ValueTypes.String)
            {
              callFrame.Return.Set(ref callFrame.Arg0);
              return;
            }

            var inputString = callFrame.Arg0.AsString();
            EvalString(inputString, ref callFrame.Return, callFrame.CallerFunction, callFrame.CallerContext, callFrame.This);
          });

        private static void assert(bool condition) { assert(condition, null); }
        private static void assert(bool condition, string message)
        {
            if (!condition)
            {
                if (message != null)
                    Trace.Fail(message);
                else
                    Trace.Fail("Error in the script");
            }
        }

        internal static void Init(mdr.DObject obj)
        {

            obj.SetField("global", obj);
            //obj.SetField("null", mdr.Runtime.Instance.DefaultDNull);
            obj.DefineOwnProperty("undefined", mdr.Runtime.Instance.DefaultDUndefined, mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.NotConfigurable);
            obj.DefineOwnProperty("NaN", double.NaN, mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.NotConfigurable);
            obj.DefineOwnProperty("Infinity", double.PositiveInfinity, mdr.PropertyDescriptor.Attributes.Data | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.NotConfigurable);

            obj.SetField("Object", new JSObject());
            obj.SetField("Function", new JSFunction());
            obj.SetField("Array", new JSArray());
            obj.SetField("ArrayBuffer", new JSArrayBuffer());
            obj.SetField("Int8Array", new JSInt8Array());
            obj.SetField("Uint8Array", new JSUint8Array());
            obj.SetField("Int16Array", new JSInt16Array());
            obj.SetField("Uint16Array", new JSUint16Array());
            obj.SetField("Int32Array", new JSInt32Array());
            obj.SetField("Uint32Array", new JSUint32Array());
            obj.SetField("Float32Array", new JSFloat32Array());
            obj.SetField("Float64Array", new JSFloat64Array());

            obj.SetField("Math", new JSMath());
            obj.SetField("String", new JSString());
            obj.SetField("Number", new JSNumber());
            obj.SetField("Date", new JSDate());
            obj.SetField("Boolean", new JSBoolean());
            obj.SetField("Error", new JSError());
            obj.SetField("RegExp", new JSRegExp());

            obj.SetField("eval", BuiltinEval);

            AddStandardMethods(obj);
            AddExtendedMethods(obj);
        }

        // TODO: These are currently unused; can we remove them?
        /*private static char[] reservedEncoded = new char[] { ';', ',', '/', '?', ':', '@', '&', '=', '+', '$', '#' };
        private static char[] reservedEncodedComponent = new char[] { '-', '_', '.', '!', '~', '*', '\'', '(', ')', '[', ']' };*/

        private static void AddStandardMethods(mdr.DObject obj)
        {
            obj.SetField("ToNumber", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                if (callFrame.PassedArgsCount > 0)
                    callFrame.Return.Set(Operations.Convert.ToDouble.Run(ref callFrame.Arg0));
                else
                    callFrame.Return.Set(double.NaN);
            }));

            obj.SetField("isNaN", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                if (callFrame.PassedArgsCount > 0)
                    callFrame.Return.Set(double.IsNaN(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)));
                else
                    callFrame.Return.Set(false);
            }));

            obj.SetField("isFinite", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                if (callFrame.PassedArgsCount > 0)
                {
                    double arg = Operations.Convert.ToDouble.Run(ref callFrame.Arg0);
                    callFrame.Return.Set(double.IsNaN(arg) || !double.IsInfinity(arg));
                }
                else
                    callFrame.Return.Set(false);
            }));

            obj.SetField("assert", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                for (int i = 0; i < callFrame.PassedArgsCount; ++i)
                {
                  var b = Operations.Convert.ToBoolean.Run(ref callFrame.Arg0);
                    if (!b)
                        throw new Exception("Error in script");
                }
            }));

            //ECMA-262: 15.1.2.2
            obj.SetField("parseInt", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
              if (callFrame.PassedArgsCount == 1 && mdr.ValueTypesHelper.IsNumber(callFrame.Arg0.ValueType))
                callFrame.Return.Set(Operations.Convert.ToInt32.Run(ref callFrame.Arg0));
              else
              {
                string stringArg = Operations.Convert.ToString.Run(ref callFrame.Arg0).TrimStart();
                int sign = 1;
                if (stringArg[0] == '-' || stringArg[0] == '+')
                {
                  if (stringArg[0] == '-')
                    sign = -1;
                  stringArg = stringArg.Remove(0, 1);
                }

                bool stripPrefix = true;
                int radix = 10;
                if (callFrame.PassedArgsCount > 1)
                {
                  radix = Operations.Convert.ToInt32.Run(ref callFrame.Arg1);
                  if (radix != 0)
                  {
                    if (radix < 2 || radix > 36)
                    {
                      callFrame.Return.Set(double.NaN);
                      return;
                    }
                    if (radix != 16)
                      stripPrefix = false;
                  }
                  else
                    radix = 10;
                }

                if (stripPrefix && (stringArg.StartsWith("0x") || stringArg.StartsWith("0X")))
                {
                  stringArg = stringArg.Remove(0, 2);
                  radix = 16;
                }

                callFrame.Return.Set(MathInt(stringArg, radix) * sign);
              }
            }));

            //ECMA-262: 15.1.2.3
            obj.SetField("parseFloat", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                string stringArg = (Operations.Convert.ToString.Run(ref callFrame.Arg0)).TrimStart();
                char[] charArg = stringArg.TrimEnd().ToCharArray();
                int len = charArg.Length;

                if (len == 0)
                {
                    callFrame.Return.Set(double.NaN);
                    return;
                }

                if (len == 1)
                {
                    if (char.IsDigit(charArg[0]))
                    {
                        callFrame.Return.Set((double)(charArg[0] - '0'));
                        return;
                    }
                }

                int i = 0;
                if (charArg[0] == '-' || charArg[0] == '+')
                    i = 1;

                bool punctation = false;
                for (; i < charArg.Length; ++i)
                {
                    if (!char.IsLetterOrDigit(charArg[i]) && charArg[i] != '.')
                        break;
                    if (charArg[i] == '-')
                        break;
                    if (punctation && charArg[i] == '.')
                        break;
                    if (charArg[i] == '.')
                        punctation = true;
                }

                stringArg = stringArg.Substring(0, i);

                double value = 0;
                if (Double.TryParse(stringArg, out value))
                {
                    callFrame.Return.Set(value);
                    return;
                }
                callFrame.Return.Set(double.NaN);
            }));

            //ECMA-262: 15.1.3.1
            obj.SetField("decodeURI", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.decodeURI");
                string uri = callFrame.Arg0.AsString();
                callFrame.Return.Set(URIHandling.Decode(uri, URIHandling.uriReserved + "#"));
            }));

            //ECMA-262: 15.1.3.2
            obj.SetField("decodeURIComponent", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.decodeURIComponent");
                if (callFrame.PassedArgsCount < 1 || callFrame.Arg0.ValueType == mdr.ValueTypes.Undefined)
                {
                    callFrame.Return.Set("");
                    return;
                }
                callFrame.Return.Set(URIHandling.Decode(callFrame.Arg0.AsString(), string.Empty));
            }));

            //ECMA-262: 15.1.3.3
            obj.SetField("encodeURI", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.encodeURI");
                string uri = callFrame.Arg0.AsString();
                callFrame.Return.Set(URIHandling.Encode(uri, URIHandling.UriUnescaped + URIHandling.uriReserved + "#"));
            }));

            //ECMA-262: 15.1.3.4
            obj.SetField("encodeURIComponent", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.encodeURIComponent");
                if (callFrame.PassedArgsCount < 1 || callFrame.Arg0.ValueType == mdr.ValueTypes.Undefined)
                {
                    callFrame.Return.Set("");
                    return;
                }
                callFrame.Return.Set(URIHandling.Encode(callFrame.Arg0.AsString(), URIHandling.UriUnescaped));
            }));

            obj.SetField("escape", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.escape");              
                if (callFrame.PassedArgsCount == 1)
                {
                  string stringArg = callFrame.Arg0.AsString();
                  if (stringArg == null)
                  {
                    callFrame.Return.Set("");
                  }
                  else
                  {
                    string escArg = URIHandling.Escape(stringArg);
                    callFrame.Return.Set(escArg);
                  }
                }
                else
                {
                  callFrame.Return.Set("");
                }
            }));

            obj.SetField("unescape", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("Calling JSGlobalObject.unescape");
                string stringArg = callFrame.Arg0.AsString();
                string unescArg = URIHandling.Unescape(stringArg);
                callFrame.Return.Set(unescArg);
            }));
        }

        private static double MathInt(string strNumber, int radix)
        {
            double result = 0;
            bool returnNan = true;
            //converting the string to number
            foreach (char c in strNumber)
            {
                int digit;
                if (c >= '0' && c <= '9')
                    digit = c - '0';
                else
                {
                    var loweredC = char.ToLower(c);
                    if (loweredC >= 'a' && loweredC <= 'z')
                        digit = 10 + loweredC - 'a';
                    else
                    {
                        digit = radix + 1; //to make the digit illegal
                        break;
                    }
                }

                if (digit < radix)
                {
                    result = result * radix + digit;
                    returnNan = false;
                }
                else
                    break;
            }

            return returnNan ? double.NaN : result;
        }

        private static void AddExtendedMethods(mdr.DObject obj)
        {
            #region Mozilla intrinsics
            obj.SetField("assertTrue", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int argsLen = callFrame.PassedArgsCount;
                if (argsLen > 0)
                {
                    var b = Operations.Convert.ToBoolean.Run(ref callFrame.Arg0);
                    assert(b, argsLen > 1 ? callFrame.Arg1.AsString() : null);
                }
                else
                    Trace.Fail("Not enough arguments");
            }));

            obj.SetField("assertFalse", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int argsLen = callFrame.PassedArgsCount;
                if (argsLen > 0)
                {
                    var b = Operations.Convert.ToBoolean.Run(ref callFrame.Arg0);
                    assert(!b, argsLen > 1 ? callFrame.Arg1.AsString() : null);
                }
                else
                    Trace.Fail("Not enough arguments");
            }));

            obj.SetField("assertEquals", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int argsLen = callFrame.PassedArgsCount;
                if (argsLen > 1)
                {
                    bool b = Operations.Binary.Equal.Run(ref callFrame.Arg0, ref callFrame.Arg1);
                    assert(b, argsLen > 2 ? callFrame.Arg2.AsString() : null);
                }
                else
                    Trace.Fail("Not enough arguments");
            }));

            obj.SetField("assertArrayEquals", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int argsLen = callFrame.PassedArgsCount;
                if (argsLen > 1)
                {
                    var arrayA = callFrame.Arg0.AsDArray();
                    var arrayB = callFrame.Arg1.AsDArray();
                    bool areEqual = true;
                    if (arrayA != null && arrayB != null && arrayA.Length == arrayB.Length)
                    {
                        for (int i = 0; i < arrayA.Length; i++)
                            if (Operations.Binary.Equal.Run(ref arrayA.Elements[i], ref arrayB.Elements[i]))
                            {
                                areEqual = false;
                                break;
                            }
                    }
                    else if (arrayA != arrayB)
                        areEqual = false;
                    assert(areEqual, argsLen > 2 ? callFrame.Arg2.AsString() : null);
                }
                else
                    Trace.Fail("Not enough arguments");
            }));
            #endregion

            // FIXME: The below causes an infinite recursion in CodeSourceGenerator. Commenting for now. - SF
            //SetField("global", this); //to enable access to global scope directly!
            obj.SetField("print", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                var l = callFrame.PassedArgsCount;
                for (var i = 0; i < l; ++i)
                {
                    var arg = callFrame.Arg(i);
                    string s = ToString(ref arg);
                    Console.Write("{0}{1}", s, (i < l - 1) ? " " : "");

                }
                Console.WriteLine();
            }));

            obj.SetField("load", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                var l = callFrame.PassedArgsCount;
                if (l < 1)
                    throw new Exception("load must have an argument");
                var filename = callFrame.Arg0.AsString();
                JSRuntime.Instance.RunScriptFile(filename);
            }));

            #region __mcjs__ object
            {
                var mcjs = new mdr.DObject(mdr.Runtime.Instance.EmptyPropertyMapMetadata.Root);
                obj.SetField("__mcjs__", mcjs);
                mcjs.SetField("SetSwitch", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                {
                    var switchName = callFrame.Arg0.AsString();
                    var switchValue = Operations.Convert.ToBoolean.Run(ref callFrame.Arg1);
                    var prop = typeof(JSRuntimeConfiguration).GetProperty(switchName, CodeGen.Types.ClrSys.Boolean);
                    if (prop != null)
                        prop.GetSetMethod().Invoke(JSRuntime.Instance.Configuration, new object[] { switchValue });
                    else
                        Debug.WriteLine("JSRuntime.Instance.Configuration does not contain the switch '{0}'", switchName);
                }));

                mcjs.SetField("PrintDump", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                {
                    var l = callFrame.PassedArgsCount;
                    if (l != 1)
                        throw new Exception("PrintDump must have one argument");
                    Debug.WriteLine("##JS: {0}", callFrame.Arg0.AsString());
#if DEBUG
                    //Check for android log directory
                    if (printOutFile == null)
                    {
                      printOutFile = System.IO.File.CreateText(System.IO.Path.Combine(JSRuntime.Instance.Configuration.OutputDir, "mcprint" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString() + ".out"));
                    }

                    printOutFile.Write("{0}", callFrame.Arg0.AsString());
                    printOutFile.Flush();
                    //Debug.WriteLine("MCPRINTVAR: {0}={1}", callFrame.Arg1.ToString(), s);
#endif
                }));

            }
            #endregion

        }

        public static string ToString(ref mdr.DValue arg)
        {
          //TODO: it seems eventually, this is the right implementation, but for now, we use the special implementation
          //mdr.DValue output;
          //if (Operations.Internals.CallToStringProperty(Operations.Convert.ToObject.Run(ref arg), out output))
          //  return output.AsString();
          //else
          //  return Operations.Convert.ToString.Run(ref arg);

          string s;
          if (!mdr.ValueTypesHelper.IsObject(arg.ValueType))
          {
            s = Operations.Convert.ToString.Run(ref arg);
          }
          else
          {
            var argObj = arg.AsDObject(); //TODO: should we call ToObject(arg) here instead?!
            var toString = argObj.GetField("toString");
            if (toString.ValueType == mdr.ValueTypes.Function)
            {
              var cf = new mdr.CallFrame();
              cf.Function = toString.AsDFunction();
              cf.Signature = mdr.DFunctionSignature.EmptySignature;
              cf.This = argObj;
              cf.Arguments = null;
              cf.Function.Call(ref cf);
              s = Operations.Convert.ToString.Run(ref cf.Return);
            }
            else
            {
              s = arg.AsString();
            }
          }
          return s;
        }

    }


    /**
     * URIHandling Class
     */
    public static class URIHandling
    {
        private static string uriAlpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz";
        private static string uriMark = "-_.!~*'()";
        private static string DecimalDigit = "1234567890";

        public static string uriReserved { get { return ";/?:@&=+$,"; } private set { } }
        public static string UriUnescaped
        {
            get { return uriAlpha + uriMark + DecimalDigit; }
            private set { }
        }
        internal static int HexDigit(char c)
        {
            if ((c >= '0') && (c <= '9'))
            {
                return (c - '0');
            }
            if ((c >= 'A') && (c <= 'F'))
            {
                return (('\n' + c) - 0x41);
            }
            if ((c >= 'a') && (c <= 'f'))
            {
                return (('\n' + c) - 0x61);
            }
            return -1;
        }
        private static byte HexValue(char ch1, char ch2)
        {
            int num;
            int num2;
            if (((num = HexDigit(ch1)) < 0) || ((num2 = HexDigit(ch2)) < 0))
            {
                throw new InvalidOperationException("URI Decode Error");
            }
            return (byte)((num << 4) | num2);
        }

        /**
         * function AppendInHex used for encodeURI (from Microsoft.JScript)
         */
        private static void AppendInHex(StringBuilder bs, int value)
        {
            bs.Append((char)37);
            int num = value >> 4 & 15;
            char help1 = (char)(num >= 10 ? num - 10 + 65 : num + 48);
            bs.Append(help1);
            num = value & 15;
            char help2 = (char)(num >= 10 ? num - 10 + 65 : num + 48);
            bs.Append(help2);
        }

        /**
         * ECMA-262: 15.1.3
         */
        public static string Encode(string uri, string unescapedSet = null)
        {
            Trace.Assert(uri != null, "Invalid null URI");
            string do_not_encode;
            if (string.IsNullOrEmpty(unescapedSet))
                do_not_encode = UriUnescaped;
            else
                do_not_encode = unescapedSet;

            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < uri.Length; i++)
            {
                char curchar = uri.ToCharArray()[i];
                if (do_not_encode.IndexOf(curchar) != -1)
                {
                    stringBuilder.Append(curchar);
                    continue;
                }
                int num = curchar;
                if (num >= 0 && num <= 127)
                {
                    AppendInHex(stringBuilder, num);
                    continue;
                }
                if (num >= 128 && num <= 2047)
                {
                    AppendInHex(stringBuilder, num >> 6 | 192);
                    AppendInHex(stringBuilder, num & 63 | 128);
                    continue;
                }
                if (num < 55296 || num > 57343)
                {
                    AppendInHex(stringBuilder, num >> 12 | 224);
                    AppendInHex(stringBuilder, num >> 6 & 63 | 128);
                    AppendInHex(stringBuilder, num & 63 | 128);
                    continue;
                }

                if (num >= 56320 && num <= 57343)
                    throw new Exception("URIEncodeErrer");
                if (i++ >= uri.Length)
                    throw new Exception("URIEncodeErrer");
                char nextChar = uri.ToCharArray()[i];
                if (nextChar < 56320 || nextChar > 57343)
                    throw new Exception("URIEncodeErrer");

                num = (num - 55296 << 10) + nextChar + 9216;
                AppendInHex(stringBuilder, num >> 18 | 240);
                AppendInHex(stringBuilder, num >> 12 & 63 | 128);
                AppendInHex(stringBuilder, num >> 6 & 63 | 128);
                AppendInHex(stringBuilder, num & 63 | 128);
            }
            return stringBuilder.ToString();
        }

        /**
         * ECMA-262: 15.1.3 (Mostly decompiled from JScritp)
         */
        public static string Decode(string str, string reservedSet = null)
        {
            Trace.Assert(str != null, "Invalid null URI");
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];
                if (ch != '%')
                {
                    builder.Append(ch);
                }
                else
                {
                    char ch2;
                    int startIndex = i;
                    if ((i + 2) >= str.Length)
                    {
                        throw new InvalidOperationException("URI Decode Error");
                    }
                    byte num3 = HexValue(str[i + 1], str[i + 2]);
                    i += 2;
                    if ((num3 & 0x80) == 0)
                    {
                        ch2 = (char)num3;
                    }
                    else
                    {
                        int num4 = 1;
                        while (((num3 << num4) & 0x80) != 0)
                        {
                            num4++;
                        }
                        if (((num4 == 1) || (num4 > 4)) || ((i + ((num4 - 1) * 3)) >= str.Length))
                        {
                            throw new InvalidOperationException("URI Decode Error");
                        }
                        int num5 = num3 & (((int)0xff) >> (num4 + 1));
                        while (num4 > 1)
                        {
                            if (str[i + 1] != '%')
                            {
                                throw new InvalidOperationException("URI Decode Error");
                            }
                            num3 = HexValue(str[i + 2], str[i + 3]);
                            i += 3;
                            if ((num3 & 0xc0) != 0x80)
                            {
                                throw new InvalidOperationException("URI Decode Error");
                            }
                            num5 = (num5 << 6) | (num3 & 0x3f);
                            num4--;
                        }
                        if ((num5 >= 0xd800) && (num5 < 0xe000))
                        {
                            throw new InvalidOperationException("URI Decode Error");
                        }
                        if (num5 < 0x10000)
                        {
                            ch2 = (char)num5;
                        }
                        else
                        {
                            if (num5 > 0x10ffff)
                            {
                                throw new InvalidOperationException("URI Decode Error");
                            }
                            builder.Append((char)((((num5 - 0x10000) >> 10) & 0x3ff) + 0xd800));
                            builder.Append((char)(((num5 - 0x10000) & 0x3ff) + 0xdc00));
                            goto Label_01D4;
                        }
                    }
                    if (reservedSet.IndexOf(ch2) != -1)
                    {
                        builder.Append(str, startIndex, (i - startIndex) + 1);
                    }
                    else
                    {
                        builder.Append(ch2);
                    }
                Label_01D4: ;
                }
            }
            return builder.ToString();
        }

        /**
         * This is decompiled from Microsoft.JScript.escape
         */
        public static string Escape(string str)
        {
            string str2 = "0123456789ABCDEF";
            int length = str.Length;
            StringBuilder builder = new StringBuilder(length * 2);
            int num3 = -1;
            while (++num3 < length)
            {
                char ch = str[num3];
                int num2 = ch;
                if ((((0x41 > num2) || (num2 > 90)) &&
                     ((0x61 > num2) || (num2 > 0x7a))) &&
                     ((0x30 > num2) || (num2 > 0x39)))
                {
                    switch (ch)
                    {
                        case '@':
                        case '*':
                        case '_':
                        case '+':
                        case '-':
                        case '.':
                        case '/':
                            goto Label_0125;
                    }
                    builder.Append('%');
                    if (num2 < 0x100)
                    {
                        builder.Append(str2[num2 / 0x10]);
                        ch = str2[num2 % 0x10];
                    }
                    else
                    {
                        builder.Append('u');
                        builder.Append(str2[(num2 >> 12) % 0x10]);
                        builder.Append(str2[(num2 >> 8) % 0x10]);
                        builder.Append(str2[(num2 >> 4) % 0x10]);
                        ch = str2[num2 % 0x10];
                    }
                }
            Label_0125:
                builder.Append(ch);
            }
            return builder.ToString();
        }

        /**
         * This is decompiled from Microsoft.JScript.unescape
         */
        public static string Unescape(string str)
        {
            int length = str.Length;
            StringBuilder builder = new StringBuilder(length);
            int num6 = -1;
            while (++num6 < length)
            {
                char ch = str[num6];
                if (ch == '%')
                {
                    int num2;
                    int num3;
                    int num4;
                    int num5;
                    if (((((num6 + 5) < length) && (str[num6 + 1] == 'u')) && (((num2 = HexDigit(str[num6 + 2])) != -1) && ((num3 = HexDigit(str[num6 + 3])) != -1))) && (((num4 = HexDigit(str[num6 + 4])) != -1) && ((num5 = HexDigit(str[num6 + 5])) != -1)))
                    {
                        ch = (char)((((num2 << 12) + (num3 << 8)) + (num4 << 4)) + num5);
                        num6 += 5;
                    }
                    else if ((((num6 + 2) < length) && ((num2 = HexDigit(str[num6 + 1])) != -1)) && ((num3 = HexDigit(str[num6 + 2])) != -1))
                    {
                        ch = (char)((num2 << 4) + num3);
                        num6 += 2;
                    }
                }
                builder.Append(ch);
            }
            return builder.ToString();
        }
    }

}
