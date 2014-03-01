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
using System.Reflection;
using System.Reflection.Emit;

namespace mjr.ILGen
{
  class CppAsmGenerator : CodeAsmGenerator
  {
    public CppAsmGenerator(string inputFilename, string outputDir)
      : base(inputFilename, outputDir)
    {
      _consoleOnly = false;
    }

    public CppAsmGenerator()
      : base()
    {
      _consoleOnly = true;
    }

    private bool _consoleOnly;
    public System.IO.TextWriter Init { get; private set; }

    public override BaseILGenerator GetILGenerator()
    {
      if (_consoleOnly)
        return new CppILGenerator(this, Console.Out);
      else
        return new CppILGenerator(this);
    }
    public override void BeginAssembly()
    {
      if (_consoleOnly)
      {
        base.BeginAssembly();
        Init = System.IO.TextWriter.Null; //This is a publi member that other classes may use!
        return;
      }

      OpenOutput(Filename + ".cpp");
      base.BeginAssembly();

      Init = new System.IO.StringWriter();

      var execDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location));
      if (execDir.EndsWith("Debug"))
        execDir = execDir.Replace("Debug", "Release");

      WriteOutput(@"
#using <System.dll>
//#using <{0}/MCDynamicRuntime.dll>
//#using <{0}/mjr.exe>

using namespace System;
using namespace mjr;

namespace MDRSrcGen 
{{
	ref class Program 
	{{
", execDir.Replace('\\', '/'));
    }
    public override void EndAssembly()
    {
      if (_consoleOnly)
      {
        base.EndAssembly();
        return;
      }

      WriteOutput(@"
		static void Init(JSRuntime^ runtime)
		{
			//Initializing objects
      auto fields = gcnew array<System::String^> 
      {
");
      for (var i = 0; i < mdr.Runtime.Instance.FieldId2NameMap.Count; ++i)
        WriteOutput("\t\t\t\tL\"{0}\", //{1}", mdr.Runtime.Instance.FieldId2NameMap[i], i);
      WriteOutput(@"
      }};
			runtime->FieldId2NameMap->Capacity = fields->Length;
			for (auto i = 0; i < fields->Length; ++i)
			{{
				auto id = runtime->GetFieldId(fields[i]);
				if (id != i) throw ""Error!"";
			}}

      //for(int i=0; i < runtime->Scripts->Count; ++i)
			//	runtime->Scripts[i]->Parse();

			//Initializing code caches
{0}
    }}//Init
", Init.ToString());

      //var inputs = InputFilename.Replace('\\', '/');
      var inputs = new System.Text.StringBuilder();
      foreach (var s in JSRuntime.Instance.Configuration.ScriptFileNames)
        inputs.AppendLine(string.Format("\t\t\t\t, L\"{0}\"", s.Replace('\\', '/')));
      foreach (var s in JSRuntime.Instance.Configuration.ScriptStrings)
        inputs.AppendLine(string.Format("\t\t\t\t, \"-e\", L\"{0}\"", s));

      WriteOutput(@"
  public:
		static void Main()
		{{
			auto config = gcnew MCJavascript::ProgramConfiguration(
				L""--cd-""
#if !defined(_DEBUG)
				, L""-t""
#endif
{0}
			);
			auto program = gcnew MCJavascript::Program();
      program->Run(gcnew MCJavascript::Program::Initializer(&Init), config);
		}}//Main
  }};//Program
}}

int main(array<System::String ^> ^args)
{{
	MDRSrcGen::Program::Main();
	return 0;
}}
", inputs.ToString());

      base.EndAssembly();
      WriteBuilds(Filename);
    }

    private void WriteBuilds(string inputFilename)
    {
      var file = System.IO.Path.GetFileName(inputFilename);
      var solution = string.Format(
@"Microsoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project(""{{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}}"") = ""{0}"", ""{0}.cpp.vcxproj"", ""{{0D0D38A5-FE02-498F-8263-87834AF17964}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Win32 = Debug|Win32
		Release|Win32 = Release|Win32
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{0D0D38A5-FE02-498F-8263-87834AF17964}}.Debug|Win32.ActiveCfg = Debug|Win32
		{{0D0D38A5-FE02-498F-8263-87834AF17964}}.Debug|Win32.Build.0 = Debug|Win32
		{{0D0D38A5-FE02-498F-8263-87834AF17964}}.Release|Win32.ActiveCfg = Release|Win32
		{{0D0D38A5-FE02-498F-8263-87834AF17964}}.Release|Win32.Build.0 = Release|Win32
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
", file);
      using (var sln = new System.IO.StreamWriter(inputFilename + ".cpp.sln"))
      {
        sln.WriteLine(solution);
        sln.Close();
      }
      var execDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location));
      //if (execDir.EndsWith("Debug"))
      //    execDir = execDir.Replace("Debug", "Release");
      if (execDir.EndsWith("Release"))
        execDir = execDir.Replace("Release", "Debug");
      var project = string.Format(
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""ProjectConfigurations"">
    <ProjectConfiguration Include=""Debug|Win32"">
      <Configuration>Debug</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include=""Release|Win32"">
      <Configuration>Release</Configuration>
      <Platform>Win32</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label=""Globals"">
    <ProjectGuid>{{0D0D38A5-FE02-498F-8263-87834AF17964}}</ProjectGuid>
    <RootNamespace>{0}</RootNamespace>
    <Keyword>ManagedCProj</Keyword>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.Default.props"" />
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"" Label=""Configuration"">
    <ConfigurationType>Application</ConfigurationType>
    <CharacterSet>Unicode</CharacterSet>
    <CLRSupport>true</CLRSupport>
    <WholeProgramOptimization>true</WholeProgramOptimization>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"" Label=""Configuration"">
    <ConfigurationType>Application</ConfigurationType>
    <CharacterSet>Unicode</CharacterSet>
    <CLRSupport>true</CLRSupport>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.props"" />
  <ImportGroup Label=""ExtensionSettings"">
  </ImportGroup>
  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"" Label=""PropertySheets"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
  </ImportGroup>
  <ImportGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"" Label=""PropertySheets"">
    <Import Project=""$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props"" Condition=""exists('$(UserRootDir)\Microsoft.Cpp.$(Platform).user.props')"" Label=""LocalAppDataPlatform"" />
  </ImportGroup>
  <PropertyGroup Label=""UserMacros"" />
  <PropertyGroup>
    <_ProjectFileVersion>10.0.30319.1</_ProjectFileVersion>
    <OutDir Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"">$(SolutionDir)$(Configuration)\</OutDir>
    <IntDir Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"">$(Configuration)\</IntDir>
    <LinkIncremental Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"">true</LinkIncremental>
    <OutDir Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"">$(SolutionDir)$(Configuration)\</OutDir>
    <IntDir Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"">$(Configuration)\</IntDir>
    <LinkIncremental Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"">false</LinkIncremental>
  </PropertyGroup>
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|Win32'"">
    <ClCompile>
      <Optimization>Disabled</Optimization>
      <PreprocessorDefinitions>_DEBUG;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <RuntimeLibrary>MultiThreadedDebugDLL</RuntimeLibrary>
      <WarningLevel>Level3</WarningLevel>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
    </ClCompile>
    <Link>
      <AdditionalDependencies>
      </AdditionalDependencies>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <AssemblyDebug>true</AssemblyDebug>
      <TargetMachine>NotSet</TargetMachine>
      <SubSystem>Console</SubSystem>
    </Link>
  </ItemDefinitionGroup>
  <ItemDefinitionGroup Condition=""'$(Configuration)|$(Platform)'=='Release|Win32'"">
    <ClCompile>
      <PreprocessorDefinitions>NDEBUG;%(PreprocessorDefinitions)</PreprocessorDefinitions>
      <RuntimeLibrary>MultiThreadedDLL</RuntimeLibrary>
      <WarningLevel>Level3</WarningLevel>
      <DebugInformationFormat>ProgramDatabase</DebugInformationFormat>
    </ClCompile>
    <Link>
      <AdditionalDependencies>
      </AdditionalDependencies>
      <GenerateDebugInformation>true</GenerateDebugInformation>
      <TargetMachine>NotSet</TargetMachine>
      <SubSystem>Console</SubSystem>
    </Link>
  </ItemDefinitionGroup>
  <ItemGroup>
    <ClCompile Include=""{0}.cpp"" />
  </ItemGroup>
  <ItemGroup>
    <Reference Include=""MC.Util, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"">
      <HintPath>MC.Util.dll</HintPath>
    </Reference>
    <Reference Include=""MCDynamicRuntime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"">
      <HintPath>{1}\MCDynamicRuntime.dll</HintPath>
    </Reference>
    <Reference Include=""MCJavascriptRuntime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"">
      <HintPath>{1}\MCJavascriptRuntime.dll</HintPath>
    </Reference>
    <Reference Include=""MCWebRuntime, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"">
      <HintPath>{1}\MCWebRuntime.dll</HintPath>
    </Reference>
    <Reference Include=""MCJavascript, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"">
      <HintPath>{1}\MCJavascript.exe</HintPath>
    </Reference>
  </ItemGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.targets"" />
  <ImportGroup Label=""ExtensionTargets"">
  </ImportGroup>
</Project>
", file, execDir);
      using (var proj = new System.IO.StreamWriter(inputFilename + ".cpp.vcxproj"))
      {
        proj.WriteLine(project);
        proj.Close();
      }
    }

  }
}
