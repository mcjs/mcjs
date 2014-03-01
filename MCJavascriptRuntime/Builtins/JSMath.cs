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
using System.Runtime.CompilerServices;

using m.Util.Diagnose;

namespace mjr.Builtins
{
    class JSMath : mdr.DObject //JSMath is not contrcutor, just has some properties
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double SinWrapper(double d);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern double CosWrapper(double d); 

       
        public static System.Random _rand;
        //Caching angles and array index
        double prevAngle = 0;
        int prevIndex = 0;
        double tableSin(double d)
        {
            d %= PI2;  // In case that the angle is larger than 2pi 
            if (d < 0) 
                d += PI2; // in case that the angle is negative 
            int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
            return _SineDoubleTable[index]; // get the value from the table 
        }
        double tableCos(double d)
        {
            d %= PI2;  // In case that the angle is larger than 2pi 
            if (d < 0)
                d += PI2; // in case that the angle is negative 
            int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
            return _CosineDoubleTable[index]; // get the value from the table 
        }

        private const double PI2 = Math.PI * 2.0; 
        private const int TABLE_SIZE = 1024 * 4; 
        private const double TABLE_SIZE_D = (double)TABLE_SIZE; 
        private const double FACTOR = TABLE_SIZE_D / PI2; 
        private static double[] _CosineDoubleTable; 
        private static double[] _SineDoubleTable; 

        static JSMath()
        {
            if (JSRuntime.Instance.Configuration.RandomSeed >= 0) {
                _rand = new Random(JSRuntime.Instance.Configuration.RandomSeed);
            } else {
                _rand = new Random();
            }
            //Initializing the sine and cosine arrays
            if (JSRuntime.Instance.Configuration.MathOptimization)
            {
                _CosineDoubleTable = new double[TABLE_SIZE];
                _SineDoubleTable = new double[TABLE_SIZE];
                for (int i = 0; i < TABLE_SIZE; i++)
                {
                    double Angle = ((double)i / TABLE_SIZE_D) * PI2;
                    _SineDoubleTable[i] = Math.Sin(Angle);
                    _CosineDoubleTable[i] = Math.Cos(Angle);
                }
            }
        }
        public JSMath()
            : base()
        {


            if (!JSRuntime.Instance.Configuration.MathOptimization)
            {
                this.DefineOwnProperty("abs", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Abs(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            }
            else
            {
                this.DefineOwnProperty("abs", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                {
                    if (callFrame.Arg0.AsDouble() > 0)
                        callFrame.Return.Set(callFrame.Arg0.AsDouble());
                    else
                        callFrame.Return.Set(-callFrame.Arg0.AsDouble());
                }
                ), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            }

            this.DefineOwnProperty("acos", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Acos(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("asin", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Asin(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("atan", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Atan(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("ceil", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Ceiling(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            if (!JSRuntime.Instance.Configuration.MathOptimization)
            {
                this.DefineOwnProperty("cos", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Cos(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            }
            else
            {
                Debug.WriteLine("Setting up opt math libs");
                this.DefineOwnProperty("cos", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
                {
//                    double d = callFrame.Arg0.DoubleValue;// % PI2;  // In case that the angle is larger than 2pi 
//                    if (d < 0)
//                        d += PI2; // in case that the angle is negative 
//                    int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
                    double d = callFrame.Arg0.AsDouble();// % PI2;  // In case that the angle is larger than 2pi 
                    if (d != prevAngle)
                    {
                        int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
                        index %= TABLE_SIZE;
                        if (index < 0)
                            index += TABLE_SIZE;
                        prevAngle = d;
                        prevIndex = index;
                        callFrame.Return.Set(_CosineDoubleTable[index]);
                    }
                    else
                    {
                        callFrame.Return.Set(_CosineDoubleTable[prevIndex]);
                    }
                    
                }
                ), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
 
            }

            this.DefineOwnProperty("exp", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Exp(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("floor", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Floor(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("log", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Log(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("random", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(_rand.NextDouble())), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
//            /*#*/this.DefineOwnProperty("random", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(new Random().NextDouble())));
            this.DefineOwnProperty("round", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Round(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            if (!JSRuntime.Instance.Configuration.MathOptimization)
            {
                this.DefineOwnProperty("sin", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Sin(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            }
            else
            {
                this.DefineOwnProperty("sin", new mdr.DFunction((ref mdr.CallFrame callFrame) =>

                {
//                    double d = callFrame.Arg0.DoubleValue % PI2;  // In case that the angle is larger than 2pi 
//                    if (d < 0)
//                        d += PI2; // in case that the angle is negative 
//                    int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
                    double d = callFrame.Arg0.AsDouble();// % PI2;  // In case that the angle is larger than 2pi 
                    if (d != prevAngle)
                    {
                        int index = (int)(d * FACTOR); //from radians to index and casted in to an int 
                        index %= TABLE_SIZE;
                        if (index < 0)
                            index += TABLE_SIZE;
                        prevAngle = d;
                        prevIndex = index;
                        callFrame.Return.Set(_SineDoubleTable[index]);
                    }
                    else
                    {
                        callFrame.Return.Set(_SineDoubleTable[prevIndex]);
                    }
                }
                ), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
 
            }

            this.DefineOwnProperty("sqrt", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Sqrt(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("tan", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Tan(Operations.Convert.ToDouble.Run(ref callFrame.Arg0)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("atan2", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Atan2(Operations.Convert.ToDouble.Run(ref callFrame.Arg0), Operations.Convert.ToDouble.Run(ref callFrame.Arg1)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("pow", new mdr.DFunction((ref mdr.CallFrame callFrame) => callFrame.Return.Set(Math.Pow(Operations.Convert.ToDouble.Run(ref callFrame.Arg0), Operations.Convert.ToDouble.Run(ref callFrame.Arg1)))), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("max", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                double max = Operations.Convert.ToDouble.Run(ref callFrame.Arg0);
                for (var i = 1; i < callFrame.PassedArgsCount; i++)
                {
                  var arg = callFrame.Arg(i);
                  var argVal = Operations.Convert.ToDouble.Run(ref arg);
                    if (argVal > max)
                        max = argVal;
                }
                callFrame.Return.Set(max);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("min", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                double min = Operations.Convert.ToDouble.Run(ref callFrame.Arg0);
                for (var i = 1; i < callFrame.PassedArgsCount; i++)
                {
                  var arg = callFrame.Arg(i);
                  var argVal = Operations.Convert.ToDouble.Run(ref arg);
                    if (argVal < min)
                        min = argVal;
                }
                callFrame.Return.Set(min);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // Constants
            this.DefineOwnProperty("E", Math.E, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("LN2", Math.Log(2), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("LN10", Math.Log(10), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("LOG2E", 1.0 / Math.Log(2), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("LOG10E", Math.Log10(Math.E), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("PI", Math.PI, mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("SQRT1_2", Math.Sqrt(0.5), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("SQRT2", Math.Sqrt(2), mdr.PropertyDescriptor.Attributes.NotConfigurable | mdr.PropertyDescriptor.Attributes.NotWritable | mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
        }

    }
}
