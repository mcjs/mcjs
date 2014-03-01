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
using m.Util.Diagnose;

using mjr.IR;

namespace mjr.CodeGen
{
  #region ProfileRecord class storing the type record
  public class ProfileRecord
  {
    struct Record
    {
      public mdr.ValueTypes type;
      public int freq;
    }
    Record[] records;

    /* Maximum size */
    int size;

    /* Actual length */
    public int length;

    /* Number of times the type seen is not among the top "size" number of hot types. */
    int missCount = 0;

    static int MAX_MISS_COUNT = 3;
    static double HOTNESS_THRESHOLD = .8;

    /* Constructor */
    public ProfileRecord(int noItem)
    {
      records = new Record[noItem];
      size = noItem;
    }

    /* Function to add as well as update the records array */
    public void UpdateType(mdr.ValueTypes type)
    {
      /* We do not track Undefined and Null values right now */
      if (type == mdr.ValueTypes.Undefined || type == mdr.ValueTypes.Null)
        return;

      for (var i = 0; i < length; ++i)
      {
        if (records[i].type == type)
        {
          records[i].freq++;
          return;
        }
      }

      if (length < size)
      {
        records[length].type = type;
        records[length++].freq = 1;
        return;
      }

      missCount++;

    }

    /* Returns the hotType if its freq crosses HOTNESS_THRESHOLD */
    public mdr.ValueTypes GetHotType()
    {
      int maxCount = 0;
      int totalCount = 0;
      mdr.ValueTypes retVal = mdr.ValueTypes.DValueRef;

      if (missCount > MAX_MISS_COUNT)
        return mdr.ValueTypes.DValueRef;

      for (var i = 0; i < length; ++i)
      {
        if (records[i].freq > maxCount)
        {
          retVal = records[i].type;
          maxCount = records[i].freq;
        }
        totalCount += records[i].freq;
      }

      if (totalCount != 0 && maxCount / totalCount > HOTNESS_THRESHOLD)
        return retVal;
      else
        return mdr.ValueTypes.DValueRef;
    }
  }
  #endregion

  public abstract class NodeProfile { }

  #region MapNodeProfile
  public class MapNodeProfile : NodeProfile
  {
    public mdr.PropertyMap Map;
    public mdr.PropertyDescriptor PD;
    public int index;
    public bool IsTooDynamic;

    public MapNodeProfile() { }
    //public MapNodeProfile(mdr.PropertyMap map, mdr.PropertyDescriptor pd)
    //{
    //  Map = map;
    //  PD = pd;
    //  if (pd != null)
    //  {
    //    index = PD.Index;
    //  }
    //}

    /* Updates the map, pd and index */
    public void UpdateNodeProfile(mdr.PropertyMap map, mdr.PropertyDescriptor pd)
    {
      if (!IsTooDynamic)
      {
        if (Map == null)
        {
          Map = map;
          PD = pd;
          index = pd.Index;
        }
        else
        {
          if (Map != map || PD != pd)
          {
            IsTooDynamic = true;
            Map = null;
            PD = null;
            index = -1;
          }
        }
      }
    }
  }
  #endregion

  #region CallNodeProfile
  public class CallNodeProfile : NodeProfile
  {
    public mdr.DFunction Target;
    public bool IsPolymorphicCallSite;

    public CallNodeProfile() { }

    //public CallNodeProfile(mdr.DFunction target)
    //{
    //  Target = target;
    //}

    public void UpdateNodeProfile(mdr.DFunction target)
    {
      if (!IsPolymorphicCallSite)
      {
        if (Target == null)
          Target = target;
        else
          if (Target != target)
          {
            IsPolymorphicCallSite = true;
            Target = null;
          }
      }
    }

  }
  #endregion

  #region GuardNodeProfile
  public class GuardNodeProfile : NodeProfile
  {
    public ProfileRecord TypeProfile = new ProfileRecord(2); //TODO: Add this to some configuration
    private mdr.ValueTypes _hotType = mdr.ValueTypes.Unknown;

    public GuardNodeProfile() { }

    ////Constructor
    //public GuardNodeProfile(mdr.ValueTypes type)
    //{
    //  TypeProfile.UpdateType(type);
    //}

    public void UpdateNodeProfile(mdr.ValueTypes type)
    {
      TypeProfile.UpdateType(type);
    }

    public mdr.ValueTypes GetHotPrimitiveType()
    {
      if (_hotType == mdr.ValueTypes.Unknown)
      {
        var hotType = GetTypeWithHighestFreq(TypeProfile);
        if (hotType != mdr.ValueTypes.DValueRef
          && mdr.ValueTypesHelper.IsPrimitive(hotType)
          && hotType != mdr.ValueTypes.Undefined
          && hotType != mdr.ValueTypes.Null)
          _hotType = hotType;
        else
          _hotType = mdr.ValueTypes.DValueRef;
      }

      return _hotType;
    }

    public mdr.ValueTypes GetHotType()
    {
      if (_hotType == mdr.ValueTypes.Unknown)
      {
        _hotType = GetTypeWithHighestFreq(TypeProfile);
      }

      return _hotType;
    }

    //Private member functions
    private mdr.ValueTypes GetTypeWithHighestFreq(ProfileRecord record)
    {
      Debug.Assert(record != null, "Record cannot be null!");

      return TypeProfile.GetHotType();
    }
  }
  #endregion

  #region Profiler
  public class Profiler
  {
    private List<GuardNodeProfile> GuardProfileData;
    private List<MapNodeProfile> MapProfileData;
    private List<CallNodeProfile> CallProfileData;

    public int ExecutionCount = 0;
    public int BackedgeCount = 0;
    public bool HasMaps = false;
    private JSFunctionMetadata _currFuncMetadata;

    public Profiler(JSFunctionMetadata funcMetadata)
    {
      _currFuncMetadata = funcMetadata;
    }

    public void Prepare()
    {
      if (GuardProfileData == null)
      {
        GuardProfileData = new List<GuardNodeProfile>(_currFuncMetadata.GuardProfileSize);
        MapProfileData = new List<MapNodeProfile>(_currFuncMetadata.MapProfileSize);
        CallProfileData = new List<CallNodeProfile>(_currFuncMetadata.CallProfileSize);
      }
    }

    private T GetOrAddNodeProfile<T>(int index, List<T> container) where T : NodeProfile, new()
    {
      Debug.Assert(index > -1, "Cannot locate profile info for index {0}", index);
      //If we use arrays and Array.Resize, the following might be faster
      for (var i = container.Count; i <= index; ++i)
        container.Add(null);
      var nodeProfile = container[index];
      if (nodeProfile == null)
      {
        nodeProfile = new T();
        container[index] = nodeProfile;
      }
      return nodeProfile;
    }

    public GuardNodeProfile GetOrAddGuardNodeProfile(int index) { return GetOrAddNodeProfile(index, GuardProfileData); }
    public CallNodeProfile GetOrAddCallNodeProfile(int index) { return GetOrAddNodeProfile(index, CallProfileData); }
    public MapNodeProfile GetOrAddMapNodeProfile(int index) { return GetOrAddNodeProfile(index, MapProfileData); }

    public GuardNodeProfile GetOrAddNodeProfile(GuardedCast node) { return GetOrAddGuardNodeProfile(_currFuncMetadata.GetProfileIndex(node)); }
    public CallNodeProfile GetOrAddNodeProfile(Invocation node) { return GetOrAddCallNodeProfile(_currFuncMetadata.GetProfileIndex(node)); }
    public MapNodeProfile GetOrAddNodeProfile(Reference node) { return GetOrAddMapNodeProfile(_currFuncMetadata.GetProfileIndex(node)); }

    private T GetNodeProfile<T>(int index, List<T> container) where T : NodeProfile
    {
      if (index != -1 && index < container.Count)
        return container[index];
      else
        return null;
    }

    public GuardNodeProfile GetNodeProfile(GuardedCast node) { return GetNodeProfile(node.ProfileIndex, GuardProfileData); }
    public CallNodeProfile GetNodeProfile(Invocation node) { return GetNodeProfile(node.ProfileIndex, CallProfileData); }
    public MapNodeProfile GetNodeProfile(Reference node) { return GetNodeProfile(node.ProfileIndex, MapProfileData); }

#if OLD
    private int GuardProfilerCounter = 0;
    private int CallProfilerCounter = 0;
    private int MapProfilerCounter = 0;

    private void _resizeProfileData<T>(List<T> ProfileData, int newSize, T item)
    {
      var rangeSize = newSize - ProfileData.Count;

      if (newSize == 0)
        return;
      else if (rangeSize == 1)
      {
        ProfileData.Add(item);
        return;
      }

      var tempList = new List<T>(rangeSize);
      for (var i = 0; i < rangeSize; i++)
        tempList.Add(item);

      ProfileData.AddRange(tempList);
    }

    public int GetGuardNodeIndex(GuardedCast node)
    {
      //Debug.Assert(node.ProfileIndex <= GuardProfileData.Count, "Assignment happens in codegen time");
      if (node.ProfileIndex == -1)
      {
        node.ProfileIndex = GuardProfilerCounter;
        int index = node.ProfileIndex;
        if (index >= GuardProfileData.Count)
        {
          _resizeProfileData<GuardNodeProfile>(GuardProfileData, index + 1, null);
          GuardProfileData[index] = new GuardNodeProfile(mdr.ValueTypes.Unknown);
        }
        GuardProfilerCounter++;
        if (GuardProfilerCounter > jsMD.GuardProfileSize)
          jsMD.GuardProfileSize = GuardProfilerCounter;
      }
      return node.ProfileIndex;
    }

    public int GetMapNodeIndex(ReadIndexerExpression node)
    {
      //Debug.Assert(node.ProfileIndex <= MapProfileData.Count, "Assignment happens in codegen time");
      if (node.ProfileIndex == -1)
      {
        node.ProfileIndex = MapProfilerCounter;
        int index = node.ProfileIndex;
        if (index >= MapProfileData.Count)
        {
          _resizeProfileData<MapNodeProfile>(MapProfileData, index + 1, null);
          MapProfileData[index] = new MapNodeProfile(null, null);
        }
        MapProfilerCounter++;
        if (MapProfilerCounter > jsMD.MapProfileSize)
          jsMD.MapProfileSize = MapProfilerCounter;
      }
      return node.ProfileIndex;
    }

    public int GetCallNodeIndex(Invocation node)
    {
      //Debug.Assert(node.ProfileIndex <= CallProfileData.Count, "Assignment happens in codegen time");
      if (node.ProfileIndex == -1)
      {
        node.ProfileIndex = CallProfilerCounter;
        int index = node.ProfileIndex;
        if (index == CallProfileData.Count)
        {
          _resizeProfileData<CallNodeProfile>(CallProfileData, index + 1, null);
          CallProfileData[index] = new CallNodeProfile(null);
        }
        CallProfilerCounter++;
        if (CallProfilerCounter > jsMD.CallProfileSize)
          jsMD.CallProfileSize = CallProfilerCounter;
      }
      return node.ProfileIndex;
    }

    public GuardNodeProfile GetGuardNodeProfile(int index)
    {
      if (index >= GuardProfileData.Count)
      {
        _resizeProfileData<GuardNodeProfile>(GuardProfileData, index + 1, null);
        GuardProfileData[index] = new GuardNodeProfile(mdr.ValueTypes.Unknown);
      }
      if (GuardProfileData[index] == null)
      {
        GuardProfileData[index] = new GuardNodeProfile(mdr.ValueTypes.Unknown);
      }
      Debug.Assert(index > -1 && index < GuardProfileData.Count, "Assignment happens in codegen time");
      return GuardProfileData[index];
    }

    public MapNodeProfile GetMapNodeProfile(int index)
    {
      if (index >= MapProfileData.Count)
      {
        _resizeProfileData<MapNodeProfile>(MapProfileData, index + 1, null);
        MapProfileData[index] = new MapNodeProfile(null, null);
      }
      if (MapProfileData[index] == null)
      {
        MapProfileData[index] = new MapNodeProfile(null, null);
      }
      Debug.Assert(index > -1 && index < MapProfileData.Count, "Assignment happens in codegen time");
      return MapProfileData[index];
    }

    public CallNodeProfile GetCallNodeProfile(int index)
    {
      if (index >= CallProfileData.Count)
      {
        _resizeProfileData<CallNodeProfile>(CallProfileData, index + 1, null);
        CallProfileData[index] = new CallNodeProfile(null);
      }
      if (CallProfileData[index] == null)
      {
        CallProfileData[index] = new CallNodeProfile(null);
      }
      Debug.Assert(index > -1 && index < CallProfileData.Count, "Assignment happens in codegen time");
      return CallProfileData[index];
    }

    //Public functions
    public void CreateNewProfile(Invocation node, mdr.DFunction target)
    {
      CallNodeProfile prof = new CallNodeProfile(target);
      if (node.ProfileIndex == -1)
      {
        node.ProfileIndex = CallProfileData.Count;
        CallProfileData.Add(prof);
        jsMD.CallProfileSize++;
      }
      else
      {
        _resizeProfileData<CallNodeProfile>(CallProfileData, node.ProfileIndex + 1, null);
        CallProfileData[node.ProfileIndex] = prof;
      }
    }

    public void CreateNewProfile(ReadIndexerExpression node, mdr.PropertyMap map, mdr.PropertyDescriptor pd)
    {
      MapNodeProfile prof = new MapNodeProfile(map, pd);
      if (node.ProfileIndex == -1)
      {
        node.ProfileIndex = MapProfileData.Count;
        MapProfileData.Add(prof);
        jsMD.MapProfileSize++;
      }
      else
      {
        _resizeProfileData<MapNodeProfile>(MapProfileData, node.ProfileIndex + 1, null);
        MapProfileData[node.ProfileIndex] = prof;
      }
      HasMaps = true;
    }

    public void UpdateTypeProfile(GuardedCast node, mdr.ValueTypes type)
    {
      if (node.ProfileIndex < GuardProfileData.Count && node.ProfileIndex != -1)
      {
        var prof = GuardProfileData[node.ProfileIndex];
        prof.UpdateNodeProfile(type);
      }
      else
      {
        GuardNodeProfile prof = new GuardNodeProfile(type);
        if (node.ProfileIndex == -1)
        {
          node.ProfileIndex = GuardProfileData.Count;
          GuardProfileData.Add(prof);
          jsMD.GuardProfileSize++;
        }
        else
        {
          _resizeProfileData<GuardNodeProfile>(GuardProfileData, node.ProfileIndex + 1, null);
          GuardProfileData[node.ProfileIndex] = prof;
        }
      }
    }
#endif

    public mdr.ValueTypes GetHotType(GuardedCast node)
    {
      Debug.Assert(node != null, "Node cannot be null!");

      var nodeProfile = GetNodeProfile(node);
      if (nodeProfile == null)
        return mdr.ValueTypes.DValueRef;

      return (nodeProfile as GuardNodeProfile).GetHotType();
    }

    public mdr.ValueTypes GetHotPrimitiveType(GuardedCast node)
    {
      Debug.Assert(node != null, "Node cannot be null!");

      var nodeProfile = GetNodeProfile(node);
      if (nodeProfile == null)
        return mdr.ValueTypes.DValueRef;

      return (nodeProfile as GuardNodeProfile).GetHotPrimitiveType();
    }

    public void GetMapsPDsIndices(ref mdr.PropertyMap[] maps, ref mdr.PropertyDescriptor[] pds, ref int[] indices)
    {
      var length = MapProfileData.Count;
      maps = new mdr.PropertyMap[length];
      pds = new mdr.PropertyDescriptor[length];
      indices = new int[length];

      for (var i = 0; i < length; ++i)
      {
        maps[i] = MapProfileData[i] == null ? null : MapProfileData[i].Map;
        pds[i] = MapProfileData[i] == null ? null : MapProfileData[i].PD;
        indices[i] = MapProfileData[i] == null ? -1 : MapProfileData[i].index;
      }
    }

    //TODO: Fix this for later.
    public override string ToString()
    {
      var profStream = new System.IO.StringWriter();
      //foreach (var nodeProfile in ProfileData)
      //{
      //  //profStream.Write("{0},", nodeProfile.Key.GetType());
      //  int typeCount = 0;
      //  int objTypeCount = 0;
      //  int funcTargetCount = 0;

      //  //typeCount = nodeProfile.Value.TypeProfile.length;
      //  //if (nodeProfile.Value.Map != null)
      //  //  objTypeCount = 1;
      //  //if (nodeProfile.Value.Target != null)
      //  //  funcTargetCount = 1;

      //  profStream.WriteLine("{0},{1},{2},{3},{4}", typeCount, objTypeCount, funcTargetCount, ExecutionCount, BackedgeCount);
      //}
      return profStream.ToString();
    }
  }
  #endregion
}
