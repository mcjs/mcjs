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
using System.Reflection;

namespace mjr
{
  public class JSFunctionCode : mdr.DFunctionCode
  {
    public MethodInfo GenericMethodHandle { get { return MethodHandle; } set { MethodHandle = value; } }
    public JittedMethod GenericMethod { get { return Method; } set { Method = value; } }

    public MethodInfo SpecializedMethodHandle;
    public JittedMethod SpecializedMethod;

    private JSFunctionMetadata Metadata;
    public CodeGen.Profiler Profiler;
    //public CodeGen.Profiler Profiler { get; private set; } //TODO: fix this later, 

    public bool IsHot { get { return Profiler != null && Profiler.ExecutionCount > 6; } }
    public bool IsSpecialized { get { return SpecializedMethodHandle != null; } }

    public JSFunctionCode(JSFunctionMetadata funcMetadata, ref mdr.DFunctionSignature signature)
      : base(ref signature)
    {
      Metadata = funcMetadata;
    }

    public override void Execute(ref mdr.CallFrame callFrame)
    {
        if (SpecializedMethod != null && !Metadata.IsBlackListed)
        SpecializedMethod(ref callFrame);
      else
      {
        ///NOTE: don't try to use interpreter instances since we may run this function recursively.
        var currProfiler = Profiler;
        if (Profiler != null)
        {
          var canProfile =
            //Profiler.ExecutionCount > 0 &&//We don't want to profile in the very first execution
            SpecializedMethodHandle == null &&//to be sure function is not being Jitted
            !Metadata.IsBlackListed
          ;

          ++Profiler.ExecutionCount;

          if (canProfile)
            Profiler.Prepare();
          else
            Profiler = null; //Remove it to prevent profiling
        }

        if (GenericMethod != null)
          GenericMethod(ref callFrame);
        else if (JSRuntime.Instance.Configuration.EnableRecursiveInterpreter)
          (new CodeGen.Interpreter()).Execute(ref callFrame);
        else
          Operations.ICMethods.Execute(ref callFrame, 0, Metadata.InlineCache.Length -1);

        if (currProfiler == null && JSRuntime.Instance.Configuration.EnableProfiling && !Metadata.IsBlackListed)
        {
          //This was the first excution
          currProfiler = new CodeGen.Profiler(Metadata);
          currProfiler.ExecutionCount = 1;
        }
        Profiler = currProfiler; //put it back
      }
    }

    //TODO: who put this here! Should be removed
    //public void GetMapsPDsIndices(ref mdr.PropertyMap[] maps, ref mdr.PropertyDescriptor[] pds, ref int[] indices)
    //{
    //  if (Profiler != null && Profiler.HasMaps)
    //    Profiler.GetMapsPDsIndices(ref maps, ref pds, ref indices);
    //}

    //public void UpdateMapsPDsIndices(ref mdr.PropertyMap[] maps, ref mdr.PropertyDescriptor[] pds, ref int[] indices, int profIndex,
    //                                 mdr.PropertyMap map, mdr.PropertyDescriptor pd)
    //{
    //  maps[profIndex] = map;
    //  pds[profIndex] = pd;
    //  indices[profIndex] = pd.Index;
    //}
  }
}
