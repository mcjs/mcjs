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
using System.Reflection.Emit;

namespace MCJavascript
{
    class CodeSourceGenerator : CodeGenerator
    {
        class MdrDumper : mdr.IMdrVisitor
        {
            //HashSet<mdr.DObject> _dumpedObjects = new HashSet<mdr.DObject>();
            Dictionary<mdr.DObject, string> _dumpedObjects = new Dictionary<mdr.DObject, string>();
            bool MarkAndVisit(mdr.DObject obj)
            {
                if (obj == null) return false;

                string res = null;
                _dumpedObjects.TryGetValue(obj, out res);
                if (res != null)
                {
                    if (res == "")
                        res = null;
                    _result = res;
                    return false;
                }
                else
                {
                    _dumpedObjects.Add(obj, "");
                    obj.Accept(this);
                    if (_result != null)
                        _dumpedObjects[obj] = _result;
                    return true;
                }
            }

            System.IO.TextWriter _output;
            public MdrDumper(System.IO.TextWriter output)
            {
                _output = output;
            }

            #region IMdrVisitor Members

            int _varIndex = 0;
            string GetVar() { return string.Format("t{0}", _varIndex++); }
            string _result;

            void WriteDClass(string varName, mdr.DObject obj)
            {
                for (int i = 0; i <= obj.DType.FieldIndex; ++i)
                {
                    var f = obj.Fields[i];
                    if (f != null)
                        MarkAndVisit(f);
                        //f.Accept(this);
                    if (_result != null)
                        _output.WriteLine("{0}.SetField(\"{1}\", {2});  //@[{3}]", varName, obj.DType.FieldNameOf(i), _result, i);
                    else
                        _output.WriteLine("{0}.GetOrAddFieldIndex(\"{1}\"); //@[{2}]", varName, obj.DType.FieldNameOf(i), i); //To make sure indexes remain correct

                }

            }

            //public void Visit(mdr.DVar obj)
            //{
            //    MarkAndVisit(obj.Object);
            //    if (_result == null)
            //        return;

            //    var tmpVar = GetVar();
            //    _output.WriteLine(
            //        "var {0} = new mdr.DVar({1});",
            //        tmpVar,
            //        _result
            //    );
            //    _result = tmpVar;
            //}

            public void Visit(mdr.DObject obj)
            {
                if (obj == mdr.DObject.Undefined)
                {
                    _result = "mdr.DObject.Undefined";
                    return;
                }

                string tmpVar;
                if (obj == JSLangImp.GlobalObj)
                    tmpVar = "JSLangImp.GlobalObj";
                else
                {
                    tmpVar = GetVar();
                    _output.WriteLine(
                        "var {0} = new mdr.DObject();",
                        tmpVar
                    );
                }
                WriteDClass(tmpVar, obj);
                _result = tmpVar;
            }
            public void Visit(mdr.DUndefined obj) { _result = "mdr.DObject.Undefined"; }
            public void Visit(mdr.DProperty obj) { _result = null; }

            public void Visit<T>(mdr.DValue<T> obj)
            {
                throw new NotImplementedException();
            }

            //public void Visit(mdr.DVarArray obj)
            //{
            //    var varName = GetVar();
            //    _output.WriteLine("var {0}=new mdr.DVarArray();", varName);

            //    for (int i = 0; i < obj.Length; ++i)
            //    {
            //        var f = obj._items[i];
            //        if (f != null)
            //            f.Accept(this);
            //        if (_result != null)
            //            _output.WriteLine("{0}._items[{1}].Set({2});", varName, i, _result);
            //    }

            //    WriteDClass(varName, obj);
            //    _result = varName;
            //}

            public void Visit(mdr.DString obj)
            {
                _result = GetVar();
                _output.WriteLine("var {0}=new mdr.DString(\"{1}\");", _result, obj.Value);
            }

            public void Visit(mdr.DDouble obj)
            {
                _result = GetVar();
                _output.WriteLine("var {0}=new mdr.DDouble({1});", _result, obj.Value);
            }

            public void Visit(mdr.DLong obj)
            {
                _result = GetVar();
                _output.WriteLine("var {0}=new mdr.DLong({1});", _result, obj.Value);
            }

            public void Visit(mdr.DInt obj)
            {
                _result = GetVar();
                _output.WriteLine("var {0}=new mdr.DInt({1});", _result, obj.Value);
            }

            public void Visit(mdr.DBoolean obj)
            {
                _result = GetVar();
                _output.WriteLine("var {0}=new mdr.DBoolean({1});", _result, obj.Value.ToString().ToLower());
            }

            public void Visit(mdr.DArray obj)
            {
                throw new NotImplementedException();
            }

            public void Visit(mdr.DType obj)
            {
                throw new NotImplementedException();
            }

            public void Visit(mdr.DFunction obj)
            {
                if (obj.Implementation is JSBuiltinFunctionImp)
                {
                    _result = null;
                    return;
                }
                MarkAndVisit(obj.Implementation);
                //if (!_dumpedObjects.Contains(obj.Implementation))
                //    Visit(obj.Implementation);

                var tmpVar = GetVar();
                _output.WriteLine(
                    "var {0} = new mdr.DFunction({1});",
                    tmpVar,
                    obj.Implementation.FullName
                );

                WriteDClass(tmpVar, obj);
                _result = tmpVar;
            }

            public void Visit(mdr.DFunctionImplementation obj)
            {
                var funcImp = obj as JSFunctionImp;
                if (funcImp == null || obj is JSBuiltinFunctionImp)
                {
                    _result = null;
                    return;
                }
                //_dumpedObjects.Add(obj);
                _output.WriteLine(
                   "var {0} = new JSFunctionImp(\"{0}\", {1}, null);",
                   funcImp.FullName,
                   (funcImp.ParentFunction != null) ? funcImp.ParentFunction.FullName : "null"
                );
                foreach (var f in funcImp.SubFunctions)
                {
                    MarkAndVisit(f);
                    if (_result != null)
                        _output.WriteLine("{0}.SubFunctions.Add({1});", funcImp.FullName, _result);
                }
                WriteDClass(funcImp.FullName, obj);
                _result = funcImp.FullName;
            }

            #endregion
        }

        System.IO.TextWriter _output;
        System.IO.TextWriter _initObject;
        System.IO.TextWriter _initCache;

        ILGen.CSharpILGenerator _csharpGen;
        string _outputDir;
        //string _filename;
        Label _bodyStart;

        public CodeSourceGenerator(string inputFilename, string outputDir)
            : base()
        {
            _outputDir = outputDir;
            //_filename = inputFilename;
            _filename = System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(inputFilename));
        }

        internal override void StartAssembly()
        {
            base.BeginAssembly();
            _output = new System.IO.StreamWriter(_filename + ".cs");

            _initObject = new System.IO.StringWriter();
            _initCache = new System.IO.StringWriter();

            _output.Write(@"
using System;
using MCJavascript;

namespace MDRSrcGen 
{
    class Program 
    {
        static void Main(string[] args)
        {

            System.Console.WriteLine(""==================================================================="");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(""------- 1st run -------\n"");
            Console.ResetColor();

            var stopWatch = new System.Diagnostics.Stopwatch();

            System.Console.WriteLine(""-------------------------------------------------------------------"");
            stopWatch.Start();

            var prgImp = Init();
            var prgFunc = new mdr.DFunction(prgImp);
            prgFunc.Context = new mdr.DObject(JSLangImp.GlobalObj);
            prgFunc.Call(null);

            stopWatch.Stop();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(""---> {0} ms"", stopWatch.Elapsed.TotalMilliseconds);
            System.Console.ResetColor();
            stopWatch.Reset();

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(""------- 2nd run -------\n"");
            Console.ResetColor();

            System.Console.WriteLine(""-------------------------------------------------------------------"");
            stopWatch.Start();

            prgImp = Init();
            prgFunc = new mdr.DFunction(prgImp);
            prgFunc.Context = new mdr.DObject(JSLangImp.GlobalObj);
            prgFunc.Call(null);

            stopWatch.Stop();
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(""---> {0} ms"", stopWatch.Elapsed.TotalMilliseconds);
            System.Console.ResetColor();
            stopWatch.Reset();
        }
");

        }
        internal override void FinishAssembly()
        {
            base.EndAssembly();
            var mdrDumper = new MdrDumper(_initObject);
            var prgImp = JSLangImp.GlobalObj.Main.Implementation;
            mdrDumper.Visit(prgImp);
            mdrDumper.Visit(JSLangImp.GlobalObj);
            _output.Write(@"
        static JSFunctionImp Init()
        {{
//Initializing objects
{0}
//Initializing code caches
{1}
return {2};
        }}
    }}
}}
", _initObject.ToString(), _initCache.ToString(), prgImp.FullName);

            _output.Flush();
            _output.Close();
            WriteBuilds(_filename);
        }

        private void WriteBuilds(string inputFilename)
        {
            var file = System.IO.Path.GetFileName(inputFilename);
            var solution = string.Format(
@"Microsoft Visual Studio Solution File, Format Version 10.00
# Visual Studio 2008
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{0}"", ""{0}.csproj"", ""{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
", file);
            using (var sln = new System.IO.StreamWriter(inputFilename + ".sln"))
            {
                sln.WriteLine(solution);
                sln.Close();
            }
            var execDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location));
            if (execDir.EndsWith("Debug"))
                execDir = execDir.Replace("Debug", "Release");
            var project = string.Format(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""3.5"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>9.0.30729</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{{A6EE344E-0A3C-4363-8A4E-48F45D9E099E}}</ProjectGuid>
    <OutputType>Exe</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>{0}</RootNamespace>
    <AssemblyName>{0}</AssemblyName>
    <TargetFrameworkVersion>v3.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin/Debug/</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin/Release/</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""Jint, Version=0.8.8.0, Culture=neutral, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>{1}/Jint.dll</HintPath>
    </Reference>
    <Reference Include=""MCDynamicRuntime, Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>{1}/MCDynamicRuntime.dll</HintPath>
    </Reference>
    <Reference Include=""MCJavascript, Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>{1}/MCJavascript.exe</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"">
      <RequiredTargetFramework>3.5</RequiredTargetFramework>
    </Reference>
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""{0}.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)/Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>
", file, execDir);
            using (var csproj = new System.IO.StreamWriter(inputFilename + ".csproj"))
            {
                csproj.WriteLine(project);
                csproj.Close();
            }
        }

        protected override void StartMethod(string methodName)
        {
            base.BeginMethod(methodName);
            _output.WriteLine("static mdr.DObject {0}(mdr.DFunction func, mdr.DObject dis, mdr.DFuncImpInstance inst){{", methodName);
            _ilGen = _csharpGen = new ILGen.CSharpILGenerator(_ilGen.MsilGen, _output);
        }
        protected override void FinishMethod(string methodName)
        {
            base.EndMethod(methodName);
            _output.WriteLine(@"}");
            _initCache.WriteLine(
                "var {0}_inst = new mdr.DFuncImpInstance({0}, new mdr.DFuncImpInstance.Guard[]{{{1}}});",
                methodName,
                string.Join(",", _funcInst.Guards.Select(c =>
                    string.Format("new mdr.DFuncImpInstance.Guard{{Container=mdr.DFuncImpInstance.ContainerType.{0}, Index={1}, ValueType=typeof({2})}}",
                        c.Container, c.Index, c.ValueType)).ToArray()));
            //if (_funcInst.CachedObjType !=null)
            //{
            //    _initCache.WriteLine("{0}_inst.CachedPropArray = new mdr.DVar[{1}][];", methodName, _funcInst.CachedPropArray.Length);
            //    _initCache.WriteLine("{0}_inst.CachedPropIndex = new int[{1}];", methodName, _funcInst.CachedPropIndex.Length);
            //    _initCache.WriteLine("{0}_inst.CachedObjType = new mdr.DType[{1}];", methodName, _funcInst.CachedObjType.Length);
            //    for (int i = 0; i < _funcInst.CachedObjType.Length; ++i)
            //        _initCache.WriteLine("{0}_inst.CachedObjType[{1}] = mdr.DType.EmptyType;", methodName, i);
            //}
            if (_funcInst.CacheLength > 0)
                _initCache.WriteLine("{0}_inst.InitCache({1});", methodName, _funcInst.CacheLength);

            _initCache.WriteLine("{0}.Cache.Add({1}_inst);", _currFuncImp.FullName, methodName);
            //_initCache.WriteLine(
            //    "{0}.Cache.Add(new mdr.DFuncImpInstance({1}, new mdr.DFuncImpInstance.Guard[]{{{2}}}));",
            //    _currFuncImp.FullName,
            //    methodName,
            //    string.Join(",", _funcInst.Guards.Select(c =>
            //        string.Format("new mdr.DFuncImpInstance.Guard{{Container=mdr.DFuncImpInstance.ContainerType.{0}, Index={1}, ValueType=typeof({2})}}",
            //            c.Container, c.Index, c.ValueType)).ToArray()));
            System.Diagnostics.Debug.Assert(_csharpGen.StackSize == 0, string.Format("Stuff left on the stack in function {0}", methodName));
        }
        protected override void GenProlog()
        {
            _output.WriteLine("//<Prolog>");
            base.GenProlog();
            //if (_funcInst.CachedObjType != null)
            //{
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_typeCache), "typeCache", _typeCache.LocalType.ToString());
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_indexCache), "indexCache", _indexCache.LocalType.ToString());
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_fieldsCache), "fieldsCache", _fieldsCache.LocalType.ToString());
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_tempDObject), "tempDObject", _tempDObject.LocalType.ToString());
            //}
            _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_tempDObject), "tempDObject", _tempDObject.LocalType.ToString());

            foreach (var symbol in _currFuncImp.Symbols)
                _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(symbol.LocalVar), symbol.Name, string.Join(", ", symbol.Types.Select(t => t.ToString()).ToArray()));

            //if (_tempDouble != null)
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_tempDouble), "tempDouble", _tempDouble.LocalType.ToString());
            //if (_tempInt != null)
            //    _output.WriteLine("//{0}={1}:{2}", _csharpGen.GetVar(_tempInt), "tempInt", _tempInt.LocalType.ToString());

            _output.WriteLine("//</Prolog>");
        }
        protected override void GenBody()
        {
            _output.WriteLine("//<Body>");
            base.GenBody();
            _output.WriteLine("//</Body>");
        }
        protected override void GenEpilog()
        {
            _output.WriteLine("//<Epilog>");
            //TODO: should we write the temporary names here?
            base.GenEpilog();
            _output.WriteLine("//</Epilog>");
        }


        #region IJintVisitor Members

        //public void Visit(Jint.Expressions.BinaryExpression expression)
        //{
        //    Visit(expression.LeftExpression);
        //    var left = _result;
        //    Visit(expression.RightExpression);
        //    var right = _result;
        //    _result = GetVar();
        //    var op = "";
        //    switch (expression.Type)
        //    {
        //        case Jint.Expressions.BinaryExpressionType.And: op = "&&"; break;
        //        case Jint.Expressions.BinaryExpressionType.Or: op = "||"; break;
        //        case Jint.Expressions.BinaryExpressionType.NotEqual: op = "!="; break;
        //        case Jint.Expressions.BinaryExpressionType.LesserOrEqual: op = "<="; break;
        //        case Jint.Expressions.BinaryExpressionType.GreaterOrEqual: op = ">="; break;
        //        case Jint.Expressions.BinaryExpressionType.Lesser: op = "<"; break;
        //        case Jint.Expressions.BinaryExpressionType.Greater: op = ">"; break;
        //        case Jint.Expressions.BinaryExpressionType.Equal: op = "=="; break;
        //        case Jint.Expressions.BinaryExpressionType.Minus: op = "-"; break;
        //        case Jint.Expressions.BinaryExpressionType.Plus: op = "+"; break;
        //        case Jint.Expressions.BinaryExpressionType.Modulo: op = "%"; break;
        //        case Jint.Expressions.BinaryExpressionType.Div: op = "/"; break;
        //        case Jint.Expressions.BinaryExpressionType.Times: op = "*"; break;
        //        case Jint.Expressions.BinaryExpressionType.Pow: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.BitwiseAnd: op = "&"; break;
        //        case Jint.Expressions.BinaryExpressionType.BitwiseOr: op = "|"; break;
        //        case Jint.Expressions.BinaryExpressionType.BitwiseXOr: op = "^"; break;
        //        case Jint.Expressions.BinaryExpressionType.Same: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.NotSame: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.LeftShift: op = "<<"; break;
        //        case Jint.Expressions.BinaryExpressionType.RightShift: op = ">>"; break;
        //        case Jint.Expressions.BinaryExpressionType.UnsignedRightShift: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.InstanceOf: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.In: op = ""; break;
        //        case Jint.Expressions.BinaryExpressionType.Unknown: op = ""; break;
        //    }
        //    if (op != "")
        //        _output.WriteLine("var {0} = {1} {2} {3};", _result, left, op, right);
        //    else
        //        _output.WriteLine("var {0} = {1}({2}, {3});", _result, expression.Type, left, right);
        //}

        //public override void Visit(Jint.Expressions.MethodCall expression)
        //{
        //    System.Diagnostics.Debug.Assert(_resultType == Types.DVar.TypeOf, "Invalid type on the stack!");
        //    var func = _result;
        //    _result = GetVar();
        //    _output.WriteLine("var {0} = {1}.Object.ToDFunction();", _result, func);
        //    func = _result;
        //    var argIndex = 0;
        //    _output.WriteLine("{0}.Args.Length={1};", func, expression.Arguments.Count);
        //    foreach (var p in expression.Arguments)
        //    {
        //        Visit(p);
        //        if (_resultType == Types.DVar.TypeOf)
        //            _result += ".Object";
        //        _output.WriteLine("{0}.Args.Get({1}).Set({2});", func, argIndex++, _result);
        //    }
        //    _output.WriteLine("{0}.Call(null);", func);
        //    _result = GetVar();
        //    _output.WriteLine("var {0} = {1}.Return;", _result, func);
        //}

        #endregion

    }
}
