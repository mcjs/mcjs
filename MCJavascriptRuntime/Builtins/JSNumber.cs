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
using System.Text;
using System.Globalization;

using m.Util.Diagnose;

namespace mjr.Builtins
{
    class JSNumber : JSBuiltinConstructor
    {
        public JSNumber()
            : base(mdr.Runtime.Instance.DNumberPrototype, "Number")
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                mdr.DValue number = new mdr.DValue();
                if (callFrame.PassedArgsCount > 0)
                {
                  Operations.Convert.ToNumber.Run(ref callFrame.Arg0, ref number);
                    //double arg = callFrame.Arg0.ToDouble();
                    ////if (Math.Floor(arg) == arg) //this is an int (FIXME: What if it is passed as 23.0? Should we still treat it as int?)
                    ////    number.Set((int)arg);
                    ////else
                    //number.Set(arg);
                }
                else
                    number.Set(0);

                if (IsConstrutor)
                {
                    mdr.DObject objNumber = new mdr.DObject(TargetPrototype);
                    objNumber.PrimitiveValue = number;
                    //objNumber.Class = "Number";
                    callFrame.This = (objNumber);
                }
                else
                    callFrame.Return.Set(ref number);
            };

            this.DefineOwnProperty("MAX_VALUE", 1.7976931348623157E308, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("MIN_VALUE", 5E-324, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("NaN", double.NaN, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("NEGATIVE_INFINITY", double.NegativeInfinity, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("POSITIVE_INFINITY", double.PositiveInfinity, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int radix = 10;
                if (callFrame.PassedArgsCount > 0)
                {
                    radix = callFrame.Arg0.AsInt32();
                }
                var number = Operations.Convert.ToDouble.Run(callFrame.This);
                callFrame.Return.Set(ToStringImpl(number, radix));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("toLocaleString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                double number = callFrame.This.ToDouble();
                callFrame.Return.Set(ToStringImpl(number, 10));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("valueOf", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                {
                    if (callFrame.This.ValueType == mdr.ValueTypes.Int32)
                        callFrame.Return.Set(callFrame.This.ToInt32());
                    else
                        callFrame.Return.Set(callFrame.This.ToDouble());
                }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("toFixed", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int numFractionDigits = 0;
                if (callFrame.PassedArgsCount > 0)
                {
                    numFractionDigits = callFrame.Arg0.AsInt32();
                    if (numFractionDigits < 0 || numFractionDigits > 20)
                        throw new ArgumentOutOfRangeException();
                }
                double number = callFrame.This.ToDouble();
                if (double.IsNaN(number))
                    callFrame.Return.Set("NaN");
                else
                    callFrame.Return.Set(number.ToString("f" + numFractionDigits, CultureInfo.InvariantCulture));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("toExponential", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                int numFractionDigits = 0;
                if (callFrame.PassedArgsCount > 0)
                {
                    numFractionDigits = callFrame.Arg0.AsInt32();
                    if (numFractionDigits < 0 || numFractionDigits > 20)
                        throw new ArgumentOutOfRangeException();
                }
                double number = callFrame.This.ToDouble();
                if (double.IsNaN(number))
                    callFrame.Return.Set("NaN");
                else
                {
                    string format = String.Concat("#.", new String('0', numFractionDigits), "e+0");
                    callFrame.Return.Set(number.ToString(format, CultureInfo.InvariantCulture));
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            TargetPrototype.DefineOwnProperty("toPrecision", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                if (callFrame.PassedArgsCount == 0)
                {
                    callFrame.Return.Set(callFrame.This.ToDouble().ToString());
                }
                else
                {
                    double number = callFrame.This.ToDouble();
                    if (double.IsNaN(number))
                        callFrame.Return.Set("NaN");
                    else if (double.IsPositiveInfinity(number))
                        callFrame.Return.Set("Infinity");
                    else if (double.IsNegativeInfinity(number))
                        callFrame.Return.Set("-Infinity");
                    else
                    {
                        int precision = 0;
                        precision = callFrame.Arg0.AsInt32();
                        if (precision < 1 || precision > 21)
                            throw new ArgumentOutOfRangeException();

                        //TODO: make sure the following is correct implementation!
                        // Get the number of decimals
                        string str = number.ToString("e23", CultureInfo.InvariantCulture);
                        int decimals = str.IndexOfAny(new char[] { '.', 'e' });
                        decimals = decimals == -1 ? str.Length : decimals;
                        precision -= decimals;
                        precision = precision < 1 ? 1 : precision;
                        callFrame.Return.Set(number.ToString("f" + precision, CultureInfo.InvariantCulture));
                    }
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

        }

        string ToStringImpl(double number, int radix)
        {
            if (radix < 2 || radix > 36)
                Trace.Fail("RangeError");
            if (Double.IsNaN(number))
            {
                return ("NaN");
            }

            if (Double.IsNegativeInfinity(number))
            {
                return ("-Infinity");
            }

            if (Double.IsPositiveInfinity(number))
            {
                return ("Infinity");
            }

            if (radix == 10)
            {
                return (number.ToString());
            }

            return new DoubleToRadixCString(number, radix).Result;
        }
       
      ///The following is copied from v8/src/conversions.cc: char* DoubleToRadixCString(double value, int radix)
        struct DoubleToRadixCString
        {
          // Character array used for conversion.
          static char[] chars = "0123456789abcdefghijklmnopqrstuvwxyz".ToCharArray();

          // Buffer for the integer part of the result. 1024 chars is enough
          // for max integer value in radix 2.  We need room for a sign too.
          const int kBufferSize = 1100;

          public DoubleToRadixCString(double value, int radix)
          {
            var integer_buffer = new char[kBufferSize];
            integer_buffer[kBufferSize - 1] = '\0';

            // Buffer for the decimal part of the result.  We only generate up
            // to kBufferSize - 1 chars for the decimal part.
            var decimal_buffer = new char[kBufferSize];
            decimal_buffer[kBufferSize - 1] = '\0';

            // Make sure the value is positive.
            bool is_negative = value < 0.0;
            if (is_negative) value = -value;

            // Get the integer part and the decimal part.
            double integer_part = Math.Floor(value);
            double decimal_part = value - integer_part;

            // Convert the integer part starting from the back.  Always generate
            // at least one digit.
            int integer_pos = kBufferSize - 2;
            do
            {
              integer_buffer[integer_pos--] = chars[(int)(integer_part % radix)];
              integer_part /= radix;
            } while (integer_part >= 1.0);

            // Sanity check.
            Debug.Assert(integer_pos > 0);
            // Add sign if needed.
            if (is_negative) integer_buffer[integer_pos--] = '-';

            // Convert the decimal part.  Repeatedly multiply by the radix to
            // generate the next char.  Never generate more than kBufferSize - 1
            // chars.
            //
            // TODO(1093998): We will often generate a full decimal_buffer of
            // chars because hitting zero will often not happen.  The right
            // solution would be to continue until the string representation can
            // be read back and yield the original value.  To implement this
            // efficiently, we probably have to modify dtoa.
            int decimal_pos = 0;
            while ((decimal_part > 0.0) && (decimal_pos < kBufferSize - 1))
            {
              decimal_part *= radix;
              var decimal_part_floor = Math.Floor(decimal_part);
              decimal_buffer[decimal_pos++] = chars[(int)decimal_part_floor];
              decimal_part -= decimal_part_floor;
            }
            decimal_buffer[decimal_pos] = '\0';

            // Compute the result size.
            int integer_part_size = kBufferSize - 2 - integer_pos;
            // Make room for zero termination.
            var result_size = integer_part_size + decimal_pos;
            // If the number has a decimal part, leave room for the period.
            if (decimal_pos > 0) result_size++;
            // Allocate result and fill in the parts.
            var builder = new StringBuilder(result_size + 1);
            builder.Append(integer_buffer, integer_pos + 1, integer_part_size);
            if (decimal_pos > 0) builder.Append('.');
            builder.Append(decimal_buffer, 0, decimal_pos);
            Result = builder.ToString();
          }
          public string Result;
        }
    }
}
