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

namespace MCJavascript
{

		class JSMathImp : JSGlobalObject.BuiltinObjConstructor
		{
            public override mdr.DObject Construct(mdr.DFunction func)
            {
                throw new Exception("Cannot create instance of Math");
            }
            public JSMathImp()
                : base()
            {

                SetField("abs", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Abs(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("acos", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Acos(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("asin", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Asin(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("atan", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Atan(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("atan2", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Atan2(func.Arguments[0].ToDouble(), func.Arguments[1].ToDouble()));
                    return func.Return;
                })));
                SetField("ceil", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Ceiling(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("cos", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Cos(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("exp", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Exp(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("floor", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Floor(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("log", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Log(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("max", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
		    double max = func.Arguments[0].ToDouble();
		    for(var i = 1; i < func.Arguments.Length; i++)
		       if(func.Arguments[i].ToDouble() > max)
		          max = func.Arguments[i].ToDouble();
                    func.Return = func.Return.Set(max);
                    return func.Return;
                })));
                SetField("min", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
		    double min = func.Arguments[0].ToDouble();
		    for(var i = 1; i < func.Arguments.Length; i++)
		       if(func.Arguments[i].ToDouble() < min)
		          min = func.Arguments[i].ToDouble();
                    func.Return = func.Return.Set(min);
                    return func.Return;
                })));
                SetField("random", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(new Random().NextDouble());
                    return func.Return;
                })));
                SetField("round", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Round(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("pow", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Pow(func.Arguments[0].ToDouble(), func.Arguments[1].ToDouble()));
                    return func.Return;
                })));
                SetField("sin", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Sin(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("sqrt", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Sqrt(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
                SetField("tan", new mdr.DFunction(new JSBuiltinFunctionImp((func, inst) =>
                {
                    func.Return = func.Return.Set(Math.Tan(func.Arguments[0].ToDouble()));
                    return func.Return;
                })));
			
		// Constants
		SetField("E", new mdr.DDouble(Math.E));
		SetField("LN2", new mdr.DDouble(Math.Log(2)));
		SetField("LN10", new mdr.DDouble(Math.Log(10)));
		SetField("LOG2E", new mdr.DDouble(1.0/Math.Log(2)));
		SetField("LOG10E", new mdr.DDouble(Math.Log10(Math.E)));
                SetField("PI", new mdr.DDouble(Math.PI));
		SetField("SQRT1_2", new mdr.DDouble(Math.Sqrt(0.5)));
		SetField("SQRT2", new mdr.DDouble(Math.Sqrt(2)));
		}
	}

}
