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

using mjr.IR;
using m.Util.Diagnose;

namespace mjr.CodeGen
{
  internal class LabelInfoManager<TargetType>
  {
    //This is used to capture TryStatement, CatchClause, etc. for proper branch handling
    Stack<object> _protectedRegion = new Stack<object>();

    public object CurrProtectedRegion { get { return (_protectedRegion.Count > 0) ? _protectedRegion.Peek() : null; } }

    public void PushProtectedRegion(object o) { _protectedRegion.Push(o); }

    public void PopProtectedRegion(object o)
    {
      var curr = _protectedRegion.Pop();
      Debug.Assert(o == curr, "miss matched between protected regions!");
    }


    public class LabelInfo
    {
      public LabelStatement Label;
      public object ProtectedRegion;
      public TargetType BreakTarget;
      TargetType _continueTarget;
      public bool HasContinueTarget { get; private set; } //Since label is ValueType, we need something else to show it was assigned. 
      public TargetType ContinueTarget
      {
        get { return _continueTarget; }
        set
        {
          HasContinueTarget = true;
          _continueTarget = value;
        }
      }
    }

    List<LabelInfo> _labels = new List<LabelInfo>();

    private LabelInfo InternalPushLabel(LabelStatement label, TargetType breakTarget)
    {
      var info = new LabelInfo()
      {
        Label = label,
        BreakTarget = breakTarget,
        ProtectedRegion = CurrProtectedRegion
      };
      _labels.Add(info);
      return info;
    }
    public void PushLabel(LabelStatement label, TargetType breakTarget) { InternalPushLabel(label, breakTarget); }

    public void PopLabel(LabelStatement label)
    {
      Debug.Assert(label == _labels[_labels.Count - 1].Label, "Missmatch between labels");
      _labels.RemoveAt(_labels.Count - 1);
    }

    public void PushLoop(LoopStatement loop, TargetType breakTarget, TargetType continueTarget)
    {
      //update the continue label of the loop's label
      Statement statement = loop;
      for (var i = _labels.Count - 1; i >= 0; --i)
      {
        var info = _labels[i];
        if (info.Label != null && info.Label.Target == statement)
        {
          info.ContinueTarget = continueTarget;
          statement = info.Label;
        }
        else
          break;
      }
      InternalPushLabel(null, breakTarget).ContinueTarget = continueTarget;
    }
    public void PopLoop(LoopStatement loop)
    {
      PopLabel(null);
    }

    public LabelInfo GetLabelInfo(string labelName)
    {
      for (var i = _labels.Count - 1; i >= 0; --i)
      {
        var info = _labels[i];
        if (labelName == null)
        {
          if (info.Label == null)
            return info;
        }
        else
        {
          if (info.Label != null && info.Label.Name == labelName)
            return info;
        }
      }
      return null;
    }

    public void Clear()
    {
      Debug.Assert(_protectedRegion.Count == 0, "Invalid situation, protected region is not closed!");
      Debug.Assert(_labels.Count == 0, "Invalid situation, labels are not closed!");

      _protectedRegion.Clear();
      _labels.Clear();
    }

    public bool IsEmpty()
    {
      return (_protectedRegion.Count == 0) && (_labels.Count == 0);
    }
  }
}
