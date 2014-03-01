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

using mjr.IR;
using mjr.CodeGen;
using m.Util.Diagnose;

//#define SHOW_CALL_TREE

namespace mjr
{
  public class JSFunctionMetadata : mdr.DFunctionMetadata
  {
    #region Code cache
    mdr.DFunctionCodeCache<JSFunctionCode> _cache = new mdr.DFunctionCodeCache<JSFunctionCode>();
    public mdr.DFunctionCodeCache<JSFunctionCode> Cache { get { return _cache; } }

    #endregion

    #region Flags
    [Flags]
    enum Flags
    {
      None,
      EnableTypeInference = 1 << 0,
      EnableMethodCallResolution = 1 << 1,
      EnableInlineCache = 1 << 2,
      EnableProfiling = 1 << 3,
    }
    Flags _flags;
    bool HasFlags(Flags flags) { return (_flags & flags) != 0; }
    void SetFlags(Flags flags, bool set)
    {
      if (set)
        _flags |= flags;
      else
        _flags &= ~flags;
    }
    public bool EnableTypeInference { get { return HasFlags(Flags.EnableTypeInference); } set { SetFlags(Flags.EnableTypeInference, value); } }
    public bool EnableProfiling { get { return HasFlags(Flags.EnableProfiling); } set { SetFlags(Flags.EnableProfiling, value); } }
    public bool EnableMethodCallResolution { get { return HasFlags(Flags.EnableMethodCallResolution); } set { SetFlags(Flags.EnableMethodCallResolution, value); } }
    public bool EnableInlineCache { get { return HasFlags(Flags.EnableInlineCache); } set { SetFlags(Flags.EnableInlineCache, value); } }
    #endregion

    public Scope Scope { get { return FunctionIR.Scope; } }
    public FunctionExpression FunctionIR { get; private set; }
    public int ParametersCount { get { return (FunctionIR.Parameters != null) ? FunctionIR.Parameters.Count : 0; } }
    private string ParametersName { get { return string.Format("({0})", FunctionIR.Parameters != null ? string.Join(", ", FunctionIR.Parameters.ConvertAll(i => i.Symbol.Name).ToArray()) : ""); } }
    public override string Declaration { get { return string.Format("{0}{1}", FullName, ParametersName); } }
    string Name { get { return (FunctionIR != null && FunctionIR.Name != null) ? FunctionIR.Name.Symbol.Name : ""; } }

    /// <summary>
    /// This is the number of total symbols in all inner scopes of the function 
    /// we will need this to provide proper value index to symbols, which is used during interpreter
    /// </summary>
    public int TotalSymbolCount { get; set; }

#region Everything related to specialized JIT

    public int GuardProfileSize { get; set; }
    public int MapProfileSize { get; set; }
    public int CallProfileSize { get; set; }

    public int GuardedCastNodeCount;
    public int ReferenceNodeCount;
    public int InvocationNodeCount;
    public int GetProfileIndex(GuardedCast node) { return (node.ProfileIndex == -1) ? (node.ProfileIndex = GuardedCastNodeCount++) : node.ProfileIndex; }
    public int GetProfileIndex(Reference node) { return (node.ProfileIndex == -1) ? (node.ProfileIndex = ReferenceNodeCount++) : node.ProfileIndex; }
    public int GetProfileIndex(Invocation node) { return (node.ProfileIndex == -1) ? (node.ProfileIndex = InvocationNodeCount++) : node.ProfileIndex; }

    public int TemporariesStartIndex { get; set; }
    public int ValuesLength { get; set; }
    public bool IsBlackListed { get; set; }
#endregion

    public override string ToString()
    {
      var funcStream = new System.IO.StringWriter();
      var declaration = string.Format("function {0}{1}", Name, ParametersName);
      funcStream.WriteLine("{0} {{", declaration);
      //var astWriter = new AstWriter(funcStream);
      //astWriter.Execute(this);
      funcStream.WriteLine("}");
      return funcStream.ToString();
    }

    #region Function declaration tree

    JSFunctionMetadata _parent;
    public JSFunctionMetadata ParentFunction
    {
      get { return _parent; }
      set
      {
        Debug.Assert(_parent == null, "Cannot change the parent of {0} to {1}", FullName, (value == null) ? "null" : value.FullName);
        _parent = value;
        if (_parent != null)
        {
          _parent.AddSubFunction(this);
          Debug.Assert(FuncDefinitionIndex == _parent.SubFunctions.Count - 1, "Invalid situation. AddSubFunction should have setup the index correctly");
          FullName = string.Format("{0}_{1}_{2}", _parent.FullName, FuncDefinitionIndex, Name);
        }
        else
        {
          FullName = Name;
          FuncDefinitionIndex = mdr.Runtime.InvalidIndex;
        }
      }
    }
    public List<JSFunctionMetadata> SubFunctions { get; private set; }
    public JSFunctionMetadata GetSubFunction(int index) { return SubFunctions[index]; } //This is to simplify code generation
    void AddSubFunction(JSFunctionMetadata subFunc)
    {
      Debug.Assert(subFunc.FuncDefinitionIndex == mdr.Runtime.InvalidIndex || SubFunctions[subFunc.FuncDefinitionIndex] == subFunc, "Opps! Invalid situation in function tree!");
      subFunc.FuncDefinitionIndex = this.SubFunctions.Count;
      this.SubFunctions.Add(subFunc);
    }

    /// <summary>
    /// Show the order in which this function was defined. 
    /// Show index of this func in its parents
    /// For ProgramFunction:
    ///     this.ParentFunction=null, JSRuntime.Scripts[this.FuncDefinitionIndex] = this
    /// For EvalFunction
    ///     this.ParentFunction!=null, this.FuncDefinitionIndex=-1
    /// For others
    ///     this.ParentFunction.SubFunctions[this.FuncDefinitionIndex] = this
    /// </summary>
    public int FuncDefinitionIndex { get; private set; }

    #endregion

    public JSFunctionMetadata(FunctionExpression functionIR, JSFunctionMetadata parent = null)
      : base()
    {
      Execute = FirstExecute;
      SubFunctions = new List<JSFunctionMetadata>();
      FunctionIR = functionIR;
      FunctionIR.Scope.SetContainer(this);
      ParentFunction = parent;
      GuardProfileSize = 0;
      MapProfileSize = 0;
      CallProfileSize = 0;
    }

    #region Status

    public enum Status : byte
    {
      None,
      //Parsing,
      //Parsed,
      Analyzing,
      Analyzed,
      Preparing,
      Prepared,
      Jitting,
      Jitted,
      Executing,
      Executed,
    }
#if SHOW_CALL_TREE
        static int _callLevel = 1;
        static string _callPrefix = ".";
#endif
    Status _currentStatus;
    public Status CurrentStatus
    {
      get
      {
        return _currentStatus;
      }
      private set
      {
        //FIXME: what should be the contidtion to check? We should not be Xing and then enter Xing again!
        //Debug.Assert(value >= _currentStatus);
        _currentStatus = value;
#if SHOW_CALL_TREE
                Console.ForegroundColor = ConsoleColor.DarkGray;
                switch (_currentStatus)
                {
                    case Status.Executing:
                        Console.Write(_callPrefix);
                        _callPrefix = new string('.', ++_callLevel);
                        break;
                    case Status.Executed:
                        _callPrefix = new string('.', --_callLevel);
                        Console.Write(_callPrefix);
                        break;
                    default:
                        Console.Write(_callPrefix);
                        break;
                }

                Debug.WriteLine("{0} function {1}({2}) with parent {3}", _currentStatus, FullName, Parameters != null ? string.Join(", ", Parameters.ToArray()) : "", ParentFunction != null ? ParentFunction.Name : "<null>");
                Console.ResetColor();
#endif
      }
    }
    //public bool IsParsing { get { return CurrentStatus == Status.Parsing; } }
    //public bool IsParsed { get { return CurrentStatus >= Status.Parsed; } }
    public bool IsAnalyzing { get { return CurrentStatus == Status.Analyzing; } }
    public bool IsAnalyzed { get { return CurrentStatus >= Status.Analyzed; } }
    public bool IsPreparing { get { return CurrentStatus == Status.Preparing; } }
    public bool IsPrepared { get { return CurrentStatus >= Status.Prepared; } }
    public bool IsJitting { get { return CurrentStatus == Status.Jitting; } }
    public bool IsJitted { get { return CurrentStatus >= Status.Jitted; } }

    #endregion

    ///We use this mask to remove the extra parameter types from the list of input signature
    ///The mask has 0xF for valid arguments, and 0x0 for the rest
    ///Practically the mask shows what parameters will be used function customization (TI, code$, ...)
    public mdr.DFunctionSignature ParametersMask;

    ///To avoid repeated computations, we store the final result of ParametersMask and the effect of different
    ///conditions and optimizationsin the following. This should be use in as many places as possible. 
    public mdr.DFunctionSignature SignatureMask;

    /// <summary>
    /// Analyze() is always called for none global functions (even if func is not executed) since we need the information
    /// </summary>
    public void Analyze()
    {
      if (CurrentStatus < Status.Analyzing)
      {
        if (ParentFunction != null && ParentFunction.CurrentStatus < Status.Analyzing)
          ParentFunction.Analyze();

        CurrentStatus = Status.Analyzing;

        Analyzer.Execute(this);

        CurrentStatus = Status.Analyzed;
      }
    }

    /// <summary>
    /// Prepare() is called once before the first execution of function.
    /// </summary>
    void Prepare()
    {
      if (CurrentStatus < Status.Preparing)
      {
        Analyze();

        var configuration = JSRuntime.Instance.Configuration;

        CurrentStatus = Status.Preparing;

        ParametersMask.Value = mdr.DFunctionSignature.GetMask(ParametersCount);

        //TODO: heuristic to disable TypeInference, we can also see if the func does not have any argument AND the "arguments" variable is not referenced in the functiob body
        EnableTypeInference =
          configuration.EnableTypeInference
          && configuration.EnableJIT
          && (ParametersCount <= mdr.DFunctionSignature.TypesPerElement)
          && (Scope.HasLoop || Scope.Symbols.Count > 0)
          ;

        //if (ParametersCount <= mdr.DFunctionSignature.TypesPerElement)
        if (EnableTypeInference)
          SignatureMask.Value = ParametersMask.Value;
        else
          SignatureMask.Value = mdr.DFunctionSignature.EmptySignature.Value;


        //TODO: make this a configuration parameter.
        EnableProfiling = configuration.EnableProfiling && !IsBlackListed;

        FunctionDeclarationHoister.Execute(this);

        if (configuration.EnableMethodCallResolution)
          MethodResolver.Execute(this);

        EnableInlineCache = configuration.EnableInlineCache;

        var timer =
          configuration.ProfileFunctionTime
          ? JSRuntime.StartTimer(configuration.ProfileJitTime, "JS/SymInit/" + Declaration)
          : JSRuntime.StartTimer(configuration.ProfileJitTime, "JS/SymInit");
        try
        {
          var symbols = Scope.Symbols;

          for (var i = symbols.Count - 1; i >= 0; --i)
          {
            var symbol = symbols[i];

            if (symbol.SymbolType == JSSymbol.SymbolTypes.ClosedOnLocal
              || symbol.SymbolType == JSSymbol.SymbolTypes.ParentLocal
              || symbol.SymbolType == JSSymbol.SymbolTypes.Global
              || symbol.SymbolType == JSSymbol.SymbolTypes.Unknown
              )
              symbol.AssignFieldId();
            Debug.Assert(
              !symbol.IsParameter
              ||
              (symbol.Index == i
              && (
                 (symbol.ParameterIndex == i && (Scope.IsFunctionDeclaration || FunctionIR.Name == null))
                 || (symbol.ParameterIndex == (i - 1) && !Scope.IsFunctionDeclaration && FunctionIR.Name != null)
                 )
              )
              , "Invalid situation!, symbol {0} should be paramter with parameter index {1} instead of {2}", symbol.Name, i, symbol.ParameterIndex);
          }
        }
        finally
        {
          JSRuntime.StopTimer(timer);
        }

        CurrentStatus = Status.Prepared;
      }

    }

    void FirstExecute(ref mdr.CallFrame callFrame)
    {
      ///We may have multiple function objects point to the same function
      ///therefore, this code might be executed multiple times, but once for each function object. 
      Prepare();
      Execute = NormalExecute;
      callFrame.Function.JittedCode = Execute; //To make sure from now on, it goes to the right function, also to prevent instanciating a new delegate object.
      NormalExecute(ref callFrame);
    }

    System.Threading.Tasks.Task _task;

    private bool IsFullMatch(JSFunctionCode funcCode, ref mdr.DFunctionSignature signature)
    {
      ///We might have a case that for example, 2 out of 4 parameters was passed before, but now, we have all parameters passed 
      ///but the first 2 matches the previous ones. Therefore, we check to make sure passed signature matches exactly with 
      ///funcCode (found in code cache).
      bool isFullMatch;
      isFullMatch = (funcCode != null) && (funcCode.Signature.Value == (signature.Value & SignatureMask.Value));
      return isFullMatch;
    }

    static int FuncId = 0;
    ILGen.BaseILGenerator CreateStub(JSFunctionCode funcCode, bool enableSpeculation)
    {
      var ilGen = JSRuntime.Instance.AsmGenerator.GetILGenerator();
      if (enableSpeculation)
      {
        var methodName = string.Format("{0}_{1}_s_", FullName, FuncId++);
        funcCode.SpecializedMethodHandle = ilGen.BeginJittedMethod(methodName);
      }
      else
      {
        var methodName = string.Format("{0}_{1}", FullName, FuncId++);
        funcCode.GenericMethodHandle = ilGen.BeginJittedMethod(methodName);
      }
      return ilGen;
    }

    void GenerateCode(JSFunctionCode funcCode, ILGen.BaseILGenerator ilGen, bool enableSpeculation)
    {
      var cgInfo = new CodeGenerationInfo(this, funcCode, ilGen);

      ///This is where we are giong to make different decistions aboud different Phases and passes

      if (JSRuntime.Instance.Configuration.EnableLightCompiler)
      {
        CodeGeneratorLight.Execute(cgInfo);
      }
      else
      {
        if (JSRuntime.Instance.Configuration.EnableFunctionInlining)
          FunctionInliner.Execute(cgInfo);
        if (EnableTypeInference)
          TypeInferer.Execute(cgInfo);

        if (enableSpeculation && !IsBlackListed)
        {
          CodeGen.CodeGeneratorWithInlineCache.Execute(this);
          try {
              CodeGeneratorWithSpecialization.Execute(cgInfo);
          }
          catch(JSDeoptFailedException e) {
              IsBlackListed = true;
	            ilGen = CreateStub(funcCode, false);
	            cgInfo = new CodeGenerationInfo(this, funcCode, ilGen);
              funcCode.Profiler = null;
              if (EnableTypeInference)
                  TypeInferer.Execute(cgInfo);
	      CodeGenerator.Execute(cgInfo);
          }
        }
        else
        {
          if (this.EnableProfiling && !IsBlackListed)
            CodeGeneratorWithProfiling.Execute(cgInfo);
          else
            CodeGenerator.Execute(cgInfo);
        }
        var method = ilGen.EndJittedMethod(this, funcCode);
        if (enableSpeculation && !IsBlackListed)
          funcCode.SpecializedMethod = method;
        else
          funcCode.GenericMethod = method;
      }
    }

    /// <summary>
    /// The following is called if we know about the signature or we know a function is going to be hot in advance
    /// In this case, we are only interested in the full match case
    /// </summary>
    public JSFunctionCode JitSpeculatively(ref mdr.DFunctionSignature signature)
    {
      Debug.Warning("This is not yet implemented!");
      return null;
    }


    JSFunctionCode Jit(ref mdr.DFunctionSignature signature, out bool isFullMatch)
    {
      var config = JSRuntime.Instance.Configuration;
      lock (this)
      {
        var useGenericJITInsteadOfInterpreter = false;

        var funcCode = Cache.Get(ref signature);
        var oldFuncCode = funcCode; //This is to keep a second copy in case we wanted to change the funcCode but return this result
        isFullMatch = IsFullMatch(funcCode, ref signature);
        if (isFullMatch)
        {
          if (funcCode.SpecializedMethod != null) //Everything is already done
            return funcCode;

          if (funcCode.GenericMethod != null //Already has a generic method jitted that we can use
            && !funcCode.IsHot) //Not ready to do specialized JIT yet
            return funcCode;

          if (config.EnableInterpreter)
          {
            if (!funcCode.IsHot) //Not ready to JIT yet
              return funcCode;

            if (funcCode.SpecializedMethodHandle != null) //IsHot and specializing JIT is already in progress
            {
              Trace.Write("Interpreting {0} instead of waiting", Declaration);
              //Instead of waiting, we can just run another round with interpreter
              return funcCode;
            }

            if (!config.EnableJIT) //IsHot but JIT disabled, Not a common case
              return funcCode;
          }
          else if (funcCode.SpecializedMethodHandle != null)
          {
            Debug.Assert(funcCode.GenericMethod != null && funcCode.IsHot, "Invalid situation, this is only reason we could be here!");
            //this is for unliketly case that we disable interpreter and enable parallel JIT
            ///The JIT must have started on it already, but it's .Method was not ready when we where checking above.
            Trace.Assert(_task != null, "Invalid situation, {0} must have already a compilation task", Declaration);
            //We not have to wait for this JIT to finish since we don't have any other choice. 
            Trace.Write("Waiting for JIT of {0} to finish", Declaration);
            _task.Wait();
            return funcCode;
          }
        }
        else
        {
          //this menas, we have not seen this signature before and we should start the process for this one again
          var funcSignature = signature;
          funcSignature.Value &= SignatureMask.Value;
          funcCode = new JSFunctionCode(this, ref funcSignature);
          Cache.Add(funcCode);

          useGenericJITInsteadOfInterpreter = //We should add all heuristics here
            Scope.HasLoop || IsBlackListed
          ;

          if (config.EnableInterpreter
            && (
              !config.EnableJIT
              || !useGenericJITInsteadOfInterpreter
              || !config.EnableRecursiveInterpreter
              )
            )
          {
            isFullMatch = true; //In other words, when interpreter is enabled, we always return full match

            if (!config.EnableRecursiveInterpreter)
              CodeGen.CodeGeneratorWithInlineCache.Execute(this);

            return funcCode;
          }
        }

        Debug.Assert(
          funcCode != null //already in Cache
          && config.EnableJIT //We can jit
          && funcCode.SpecializedMethodHandle == null //no previous JIT started
          && (!config.EnableInterpreter //We could not interpret
            || useGenericJITInsteadOfInterpreter //We do not want to interpret
            || funcCode.IsHot //Time to start the JIT for this function
            )
          , "Invalid situation!");


        CurrentStatus = Status.Jitting;

        Debug.WriteLine("Jitting {0} with sig=0x{1:X}-->{2} from 0x{3:X}", Declaration, funcCode.Signature.Value, string.Join(",", funcCode.Signature.Types), signature.Value);

        var enableSpeculation =
          JSRuntime.Instance.Configuration.EnableSpeculativeJIT
          && funcCode.Profiler != null
          && funcCode.IsHot
          && !IsBlackListed;

        ///During code generation, we may again want to refer this same function (e.g. recursive)
        ///Therefore, we first add the funcCode to the Cache, and then generate code.
        ///Also, .MethodInfo of the funcCode should be assigned before the code gen begins (or at the begining) 
        ///so a look up in the cache will return all thre result that we might need during the code geration itself.
        var ilGen = CreateStub(funcCode, enableSpeculation);

        if (config.EnableParallelJit
          && !useGenericJITInsteadOfInterpreter
          && (config.EnableInterpreter || oldFuncCode != funcCode)//otherwise, there is no point in creating background task and then waiting
          //&& (funcCode != null || !isNeededForImmediateExecution) 
            )
        {
          ///In this case we either have some result to return, or no one is going to immediately need the results
          ///therefore, we can create background tasks for compilation. 

          //TODO: if interpreter is enabled, we should disable profiler to make sure it is not changed during JIT
          var runtime = JSRuntime.Instance;
          var currTask = _task;
          if (currTask != null)
          {
            _task = currTask.ContinueWith(t =>
            {
              mdr.Runtime.Instance = JSRuntime.Instance = runtime;
              GenerateCode(funcCode, ilGen, enableSpeculation);
            });
            //TODO: do we need to make sure while setting up continuation, currTask did not finish and leave us hanging?
          }
          else
            _task = System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
              mdr.Runtime.Instance = JSRuntime.Instance = runtime;
              GenerateCode(funcCode, ilGen, enableSpeculation);
            });
          if (!config.EnableInterpreter)
            return oldFuncCode;
        }
        else
        {
          GenerateCode(funcCode, ilGen, enableSpeculation);
          isFullMatch = true; //We generated a full match
        }
        CurrentStatus = Status.Jitted;
        return funcCode;
      }
    }

    /// <summary>
    ///This function is called in the following cases
    /// -A DFunction is executed for the first time
    /// -A DFunctionCode detects that signature has changed and a new specialized version is needed
    /// -A DFunction was used last time speculatively, but we may have a better one now!
    ///In both of these cases, we create a new code and set it in the DFunction
    /// </summary>
    public void NormalExecute(ref mdr.CallFrame callFrame)
    {
      //NOTE: we need to timing measurement in the generated code for execute to support recursive calls properly.

      Debug.Assert(
        (callFrame.Signature.Value & ParametersMask.Value) == new mdr.DFunctionSignature(ref callFrame, ParametersCount).Value
        , string.Format("passed signature {0:X} does not match the arguments in {1} with default sigmask {2:X}", callFrame.Signature.Value, Declaration, SignatureMask.Value)
      );


      var funcCode = Cache.Get(ref callFrame.Signature);
      var isFullMatch = IsFullMatch(funcCode, ref callFrame.Signature);

      ///While we were looking up and checking stuff, another JIT task might have updated cache
      ///Also, if funcCode==null, we don't want to create one and change the code$ here and potentially cause race
      if (!isFullMatch
        || (funcCode.IsHot && !funcCode.IsSpecialized))
        funcCode = Jit(ref callFrame.Signature, out isFullMatch);

      callFrame.Function.Code = funcCode;

      if (isFullMatch && funcCode.SpecializedMethod != null)
      {
        ///This means we've found an exact match and don't need to worry about a better match added to cache later
        //callFrame.Function.Code = funcCode;
        callFrame.Function.EnableSignature = true;
        callFrame.Function.JittedCode = funcCode.SpecializedMethod;
      }
      else// if (callFrame.Function.Code != null)
      {
        ///This means we have a matching signature before, and not it has changed. So, we should reset the state of the function until a later time
        ///If we don't change the followings, we will do another unnecessary signature check and potentially jump to this function again. 
        //callFrame.Function.Code = null;
        callFrame.Function.EnableSignature = false;
        callFrame.Function.JittedCode = this.Execute;
      }

      CurrentStatus = Status.Executing;
#if SHOW_CALL_TREE
      Console.ForegroundColor = ConsoleColor.DarkGray;
      Debug.WriteLine("{0}{1}.sig={2:X}", _callPrefix, FullName, callFrame.Signature.Value);
      Console.ResetColor();
#endif
      funcCode.Execute(ref callFrame);
      CurrentStatus = Status.Executed;
    }

    public override void BlackList(mdr.DFunction func)
    {
        var code = func.Code as JSFunctionCode;
        IsBlackListed = true;
        if (code != null)
        {
            Cache.Remove(code);
            code.SpecializedMethod = null;
            code.SpecializedMethodHandle = null;
        }
        func.JittedCode = NormalExecute;
    }

  }
}
