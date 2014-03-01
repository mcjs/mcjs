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
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using IDLCodeGen.IDL;
using IDLCodeGen.Conversions;
using IDLCodeGen.Util;

namespace IDLCodeGen.Targets
{
  class AutoBindings : IDLTarget
  {
    public override string Filename { get { return "AutoBindings.cs"; }  }

    protected override void Generate(Writer Write, IDLXML idl)
    {
#if ENABLE_ZOOMM_MONO_EXTENSIONS
      // Only use .NET 4.5 MethodImplOptions on Mono platforms (UNIX / Mac OS X) for now.
      var os = (int) Environment.OSVersion.Platform;
      var methodImplOptions = "[MethodImplAttribute(MethodImplOptions.AggressiveInlining)]"
                              .If(os == 4 || os == 6 || os == 128);
#else
      var methodImplOptions = "";
#endif

      Write(@"
using System;
using System.Runtime.CompilerServices;
using mwr.Extensions;

// Disable warnings related to new and override.
#pragma warning disable 108, 109, 114

namespace mwr.DOM
{
");

      foreach (var iface in idl.Interfaces)
      {
        Write(@"
public partial class ${Name}
{
  public new partial class Bindings
  {
".FormatWith(iface));

        foreach (var op in (from o in iface.Operations where !o.IsRuntime select o))
        {
            var argList = string.Join("", op.Args.Select(a => ", " + a.Type.AsCSInternalArg() + " arg" + a.Index));
            var callArgList = string.Join("", op.Args.Select(a => ", " + "ref ".If(a.Type.ByRef) + "arg" + a.Index));
            Write(@"
    ${implOpts}
    public static ${returnType} ${oName}(IntPtr domObject${args})
    {
      ${retValDef}
      m.Util.Timers.Timer timer = null;
      try
      {
        if (HTMLRuntime.Instance != null) 
           timer = HTMLRuntime.StartTimer(HTMLRuntime.Instance.Configuration.ProfileBindingTime, ""JS/Binding/${iName}.Internal.${oName}"");
        #if (ENABLE_HOOKS${forceDisableHooks})
          #if ENABLE_RR
            if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.RecordEnabled)
            {
               ${retAssign}${iName}.Internal.${oName}(domObject${callArgs});
               RecordReplayManager.Instance.Record(""${iName}"", ${retVal}, ""${oName}"", true, domObject${callArgs});
            }
            else if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.ReplayEnabled)
            {
               ${retAssign}${retCast}RecordReplayManager.Instance.OutputBindingReplay(""${iName}"", ""${oName}"", domObject${callArgs});
            }
            else
            {
               ${retAssign}${iName}.Internal.${oName}(domObject${callArgs});
            }
          #endif  
        #else
          ${retAssign}${iName}.Internal.${oName}(domObject${callArgs});
        #endif
      }
      finally
      {
        if (HTMLRuntime.Instance != null) 
           HTMLRuntime.StopTimer(timer);
      }
      ${retValReturn}
    }

".FormatWith(new
 {
     implOpts = methodImplOptions,
     returnType = op.RetType.AsCSRet(),
     oName = op.Name,
     args = argList,
     forceDisableHooks = " && HOOKS_DISABLED_FOR_THIS_OPERATION".If(op.DisableHooks),
     iName = iface.Name,
     callArgs = callArgList,
     retAssign = "retVal = ".If(!op.RetType.IsVoid),
     ret = "return ".If(!op.RetType.IsVoid),
     retValDef = (op.RetType.AsCSArg() + " retVal;").If(!op.RetType.IsVoid),
     retVal = (op.RetType.IsVoid) ? "null" : "retVal",
     retValReturn = "return retVal;".If(!op.RetType.IsVoid),
     retCast = ("(" + op.RetType.AsCSArg() + ")").If(!op.RetType.IsVoid),
 }));
        }

        foreach (var attr in (from a in iface.Attributes where !a.IsEventHandler select a))
        {
          if (!attr.IsReadOnly)
            Write(@"
    ${implOpts}
    public static void ${aName}Setter(IntPtr domObject, ${argType} arg)
    {
      m.Util.Timers.Timer timer = null;
      try
      {
        if (HTMLRuntime.Instance != null) 
           timer = HTMLRuntime.StartTimer(HTMLRuntime.Instance.Configuration.ProfileBindingTime, ""JS/Binding/${iName}.Internal.${aName}Setter"");
        #if ENABLE_HOOKS
          #if ENABLE_RR
            if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.RecordEnabled)
            {
               ${iName}.Internal.${aName}Setter(domObject, arg);
               RecordReplayManager.Instance.Record(""${iName}"", null, ""${aName}Setter"", true, domObject, arg);
            }
            else if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.ReplayEnabled)
            {
               RecordReplayManager.Instance.OutputBindingReplay(""${iName}"", ""${aName}Setter"", domObject, arg);
            }
            else
            {
               ${iName}.Internal.${aName}Setter(domObject, arg);
            }
          #endif  
        #else
          ${iName}.Internal.${aName}Setter(domObject, arg);
        #endif
      }
      finally
      {       
        if (HTMLRuntime.Instance != null) 
           HTMLRuntime.StopTimer(timer);
      }
    }

".FormatWith(new { implOpts = methodImplOptions,
                   aName = attr.Name,
                   argType = attr.SetterType.AsCSInternalArg(),
                   iName = iface.Name }));

          Write(@"
    ${implOpts}
    public static ${returnType} ${aName}Getter(IntPtr domObject)
    {
      ${retValDef}
      m.Util.Timers.Timer timer = null;
      try
      {
        if (HTMLRuntime.Instance != null) 
           timer = HTMLRuntime.StartTimer(HTMLRuntime.Instance.Configuration.ProfileBindingTime, ""JS/Binding/${iName}.Internal.${aName}Getter"");
        #if ENABLE_HOOKS
          #if ENABLE_RR
            if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.RecordEnabled)
            {
               ${retAssign}${iName}.Internal.${aName}Getter(domObject);
               RecordReplayManager.Instance.Record(""${iName}"", ${retVal}, ""${aName}Getter"", true, domObject);
            }
            else if (mwr.RecordReplayManager.Instance != null && mwr.RecordReplayManager.Instance.ReplayEnabled)
            {
               ${retAssign}${retCast}RecordReplayManager.Instance.OutputBindingReplay(""${iName}"", ""${aName}Getter"", domObject);
            }
            else
            {
               ${retAssign}${iName}.Internal.${aName}Getter(domObject);
            }
          #endif  
        #else
          ${retAssign}${iName}.Internal.${aName}Getter(domObject);
        #endif
      }
      finally
      {
        if (HTMLRuntime.Instance != null) 
           HTMLRuntime.StopTimer(timer);
      }
      ${retValReturn}
    }

".FormatWith(new { implOpts = methodImplOptions,
                   returnType = attr.GetterType.AsCSRet(),
                   aName = attr.Name,
                   iName = iface.Name,
                   retValDef = (attr.GetterType.AsCSArg() + " retVal;").If(!attr.GetterType.IsVoid),
                   retAssign = "retVal = ".If(!attr.GetterType.IsVoid),
                   retVal = (attr.GetterType.IsVoid) ? "null" : "retVal",
                   retValReturn = "return retVal;".If(!attr.GetterType.IsVoid),
                   retCast = ("(" + attr.GetterType.AsCSArg() + ")").If(!attr.GetterType.IsVoid),
 }));
        }
        Write(@"
  }
}

");
      }
      Write(@"
}
");
    }
  }
}
