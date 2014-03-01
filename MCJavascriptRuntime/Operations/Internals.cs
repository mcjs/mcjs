// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿//#define __STAT__PD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using mdr;
using m.Util.Diagnose;

namespace mjr.Operations
{
  public static class Internals
  {
    /// <summary>
    /// ECMA-262, 8.12.8: Implements [[DefaultValue]] for JS objects
    /// </summary>
    /// <param name="input"></param>
    /// <param name="output"></param>
    /// <param name="stringHint">Specifies whether the hint is string or number. Default is number.</param>
    public static void DefaultValue(ref mdr.DValue input, out mdr.DValue output, bool stringHint = false)
    {
      Debug.Assert(ValueTypesHelper.IsObject(input.ValueType));
      DefaultValue(input.AsDObject(), out output, stringHint);
    }
    public static void DefaultValue(mdr.DObject input, out mdr.DValue output, bool stringHint = false)
    {
      if (input.PrimitiveValue.ValueType != mdr.ValueTypes.Undefined)
      {
        output = input.PrimitiveValue;
        return;
      }

      if (stringHint)
      {
        if (!CallToStringProperty(input, out output) && !CallValueOfProperty(input, out output))
          Trace.Fail(new InvalidOperationException(string.Format("TypeError: DefaultValue for {0} is unknown", input.ValueType)));
      }
      else //assume number hint
      {
        if (!CallValueOfProperty(input, out output) && !CallToStringProperty(input, out output))
          Trace.Fail(new InvalidOperationException(string.Format("TypeError: DefaultValue for {0} is unknown", input.ValueType)));
      }
    }


    /// <summary>
    /// if the toString property of the input is a callable, calls it and sets the result in the output
    /// </summary>
    /// <param name="input">input</param>
    /// <param name="output">The return value of toString property</param>
    /// <returns>true if toString is callable and returns a primitive value, otherwise return false</returns>
    public static bool CallToStringProperty(ref mdr.DValue input, out mdr.DValue output)
    {
      return CallToStringProperty(input.AsDObject(), out output);
    }
    public static bool CallProperty(mdr.DObject input, string propName, out mdr.DValue output)
    {
      if (input != null)
      {
        var propDesc = input.GetPropertyDescriptor(propName);
        var prop = new mdr.DValue();
        propDesc.Get(input, ref prop);
        mdr.DFunction func = null;
        if (prop.ValueType == mdr.ValueTypes.Function)
        {
          func = prop.AsDFunction();
          //if (toString != null)
          //{
          mdr.CallFrame callFrame = new mdr.CallFrame();
          callFrame.This = (input);
          callFrame.Function = func;
          func.Call(ref callFrame);
          if (ValueTypesHelper.IsPrimitive(callFrame.Return.ValueType))
          {
            output = callFrame.Return;
            return true;
          }
        }
      }
      output = new mdr.DValue();
      output.SetUndefined();
      return false;
    }

    public static bool CallToStringProperty(mdr.DObject input, out mdr.DValue output)
    {
      return CallProperty(input, "toString", out output);
    }

    /// <summary>
    /// if the valueOf property of the input is a callable, calls it and sets the result in the output
    /// </summary>
    /// <param name="input">input</param>
    /// <param name="output">The return value of valueOf property</param>
    /// <returns>true if valueOf is callable and returns a primitive value, otherwise return false</returns>
    public static bool CallValueOfProperty(ref mdr.DValue input, out mdr.DValue output)
    {
      return CallValueOfProperty(input.AsDObject(), out output);
    }
    public static bool CallValueOfProperty(mdr.DObject input, out mdr.DValue output)
    {
      return CallProperty(input, "valueOf", out output);
    }

    public static bool CheckSignature(ref mdr.CallFrame callFrame)
    {
      var func = callFrame.Function;
      var funcCode = func.Code;
      if (!func.EnableSignature || funcCode == null)
      {
        //If funcCode is null, that means this function was resolved at compile time and directly called in the code
        return true;
      }

      Debug.Assert(
        ((JSFunctionMetadata)callFrame.Function.Metadata).ParametersCount > 0
        && funcCode.Signature.Value != mdr.DFunctionSignature.EmptySignature.Value
        , "Invalid situaltion, we should not be checking signature for function {0}", callFrame.Function.Metadata.Declaration);

      if (funcCode.MatchSignature(ref callFrame.Signature))
        return true;
      else
      {
        callFrame.Function.Metadata.Execute(ref callFrame);
        return false;
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void SetFieldUsingIC(DObject obj, int fieldId, ref DValue v, int mapId, int fieldIndex)
    {
#if __STAT__PD 
      if (mdr.Runtime.Instance.Configuration.ProfileStats)
      {
        mdr.Runtime.Instance.Counters.GetCounter("Set IC calls").Count++;
      }
#endif
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(ref v);
//        obj.SetFieldByFieldId(fieldId, ref v);
#if __STAT__PD 
        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("Set IC hit").Count++;
          mdr.Runtime.Instance.Counters.GetCounter("Set IC hit findex_" + fieldIndex).Count++;
        }
#endif
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, ref v);
#if __STAT__PD 
        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("Set IC miss").Count++;
        }
#endif
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void SetFieldUsingIC(DObject obj, int fieldId, string v, int mapId, int fieldIndex)
    {
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(v);
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, v);
      }
    }
    public static void SetFieldUsingIC(DObject obj, int fieldId, double v, int mapId, int fieldIndex)
    {
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(v);
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, v);
      }
    }
    public static void SetFieldUsingIC(DObject obj, int fieldId, int v, int mapId, int fieldIndex)
    {
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(v);
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, v);
      }
    }
    public static void SetFieldUsingIC(DObject obj, int fieldId, bool v, int mapId, int fieldIndex)
    {
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(v);
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, v);
      }
    }
    public static void SetFieldUsingIC(DObject obj, int fieldId, DObject v, int mapId, int fieldIndex)
    {
      if (mapId == obj.MapId)
      {
        obj.Fields[fieldIndex].Set(v);
      }
      else
      {
        obj.SetFieldByFieldId(fieldId, v);
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void GetFieldUsingIC(DObject obj, int fieldId, ref DValue v, int mapId, int fieldIndex)
    {
#if __STAT__PD 
      if (mdr.Runtime.Instance.Configuration.ProfileStats)
      {
        mdr.Runtime.Instance.Counters.GetCounter("IC calls").Count++;
      }
#endif
      if (mapId == obj.MapId)
      {
        v = obj.Fields[fieldIndex];
#if __STAT__PD 
        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("IC hit").Count++;
          mdr.Runtime.Instance.Counters.GetCounter("IC hit findex_" + fieldIndex).Count++;
        }
#endif
      }
      else
      {
        obj.GetPropertyDescriptorByFieldId(fieldId).Get(obj, ref v);
#if __STAT__PD 
        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("IC miss").Count++;
        }
#endif
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void GetInheritFieldUsingIC(DObject obj, int fieldId, ref DValue v, int mapId, int fieldIndex, int inheritObjectCacheIndex)
    {

      
#if __STAT__PD 
      if (mdr.Runtime.Instance.Configuration.ProfileStats)
      {
        mdr.Runtime.Instance.Counters.GetCounter("Inh IC calls").Count++;
      } 
#endif
      if (mapId == obj.MapId)
      {
        DObject patentObj = JSRuntime._inheritPropertyObjectCache[inheritObjectCacheIndex];
        if (patentObj != null)
        {
          v = patentObj.Fields[fieldIndex];
#if __STAT__PD 
          if (mdr.Runtime.Instance.Configuration.ProfileStats)
          {
            mdr.Runtime.Instance.Counters.GetCounter("Inh IC hits").Count++;
            mdr.Runtime.Instance.Counters.GetCounter("Inh IC hit oindex_" + inheritObjectCacheIndex).Count++;
            mdr.Runtime.Instance.Counters.GetCounter("Inh IC hit findex_" + fieldIndex).Count++;
          }
#endif
        }
      }
      else
      {
        obj.GetPropertyDescriptorByFieldId(fieldId).Get(obj, ref v);
#if __STAT__PD 
        if (mdr.Runtime.Instance.Configuration.ProfileStats)
        {
          mdr.Runtime.Instance.Counters.GetCounter("Inh IC misses").Count++;
        }
#endif
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void UpdateGuardProfile(ValueTypes type, mjr.CodeGen.Profiler profiler, int profileIndex)
    {
      if (profiler != null)
      {
        profiler.GetOrAddGuardNodeProfile(profileIndex).UpdateNodeProfile(type);
      }
    }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static PropertyDescriptor UpdateMapProfile(PropertyDescriptor pd, mjr.CodeGen.Profiler profiler, int profileIndex, PropertyMap map)
    {
      if (profiler != null)
      {
        profiler.GetOrAddMapNodeProfile(profileIndex).UpdateNodeProfile(map, pd);
      }
      return pd;
    }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void UpdateMapProfileForWrite(DObject obj, mjr.CodeGen.Profiler profiler, int profileIndex, int fieldId, PropertyMap oldMap)
    {
      if (profiler != null)
      {
        if (obj.Map == oldMap) 
        {
          PropertyDescriptor pd = obj.GetPropertyDescriptorByFieldId(fieldId);
          //          obj.GetPropertyDescriptor
            //          Trace.WriteLine("YYY");
            profiler.GetOrAddMapNodeProfile(profileIndex).UpdateNodeProfile(oldMap, pd);
        }
      }
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void UpdateCallProfile(CodeGen.Profiler profiler, int profileIndex, DFunction function)
    {
      if (profiler != null)
          profiler.GetOrAddCallNodeProfile(profileIndex).UpdateNodeProfile(function);
    }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static string[] GetTypesSplit(string typeString)
    {
        return typeString.Split(',');
    }

#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static bool CompareTypes(string typeString1, string typeString2)
    {
        return typeString1.Equals(typeString2);
    }
#if __MonoCS__
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
#endif
    public static void PrintString(string str)
    {
        Console.Write(str);
    }

  }
}
