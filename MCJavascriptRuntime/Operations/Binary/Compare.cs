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
using mdr;
using System.Runtime.CompilerServices;

namespace mjr.Operations.Binary
{
  /// <summary>
  /// ECMA-262, 11.8.5
  /// </summary>
  public static partial class Compare
  {
    public enum Result : int
    {
      Undefined,
      True,
      False
    };

    /// <summary>
    /// ECMA-262, 11.8.5: Implementation of the abstract relational comparison
    /// </summary>
    /// <param name="i0">The first operand</param>
    /// <param name="i1">The second operand</param>
    /// <param name="LeftFirst">Whether the evaluation of the left side must be done first</param>
    /// <returns>if x less than y return true, otherwise return false</returns>
    //public static Result Run(ref mdr.DValue i0, ref mdr.DValue i1, bool LeftFirst = true)
    //{
    //  mdr.DValue pLeft;
    //  mdr.DValue pRight;
    //  if (LeftFirst)
    //  {
    //    Convert.ToPrimitive.Run(ref i0, out pLeft, false);
    //    Convert.ToPrimitive.Run(ref i1, out pRight, false);
    //  }
    //  else
    //  {
    //    Convert.ToPrimitive.Run(ref i1, out pRight, false);
    //    Convert.ToPrimitive.Run(ref i0, out pLeft, false);
    //  }

    //  if (pLeft.ValueType != mdr.ValueTypes.String || pRight.ValueType != mdr.ValueTypes.String)
    //  {
    //    double numberLeft = pLeft.ToDouble();
    //    double numberRight = pRight.ToDouble();
    //    if (double.IsNaN(numberLeft) || double.IsNaN(numberRight))
    //      return Result.Undefined;
    //    if (numberLeft == numberRight)
    //      return Result.False;
    //    if (double.IsPositiveInfinity(numberLeft) || double.IsNegativeInfinity(numberRight))
    //      return Result.False;
    //    if (double.IsPositiveInfinity(numberRight) || double.IsNegativeInfinity(numberLeft))
    //      return Result.True;
    //    return (numberLeft < numberRight ? Result.True : Result.False);
    //  }
    //  else //both px and py are strings
    //  {
    //    string stringLeft = pLeft.ToString();
    //    string stringRight = pRight.ToString();
    //    int comparison = String.Compare(stringLeft, stringRight);
    //    if (comparison < 0)
    //      return Result.True;
    //    else
    //      return Result.False;
    //  }
    //}

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Result Run(string i0, string i1)
    {
      int comparison = String.Compare(i0, i1);
      if (comparison < 0)
        return Result.True;
      else
        return Result.False;
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Result Run(double i0, double i1)
    {
      if (double.IsNaN(i0) || double.IsNaN(i1))
        return Result.Undefined;
      if (i0 == i1)
        return Result.False;
      if (double.IsPositiveInfinity(i0) || double.IsNegativeInfinity(i1))
        return Result.False;
      if (double.IsPositiveInfinity(i1) || double.IsNegativeInfinity(i0))
        return Result.True;
      return (i0 < i1 ? Result.True : Result.False);
    }

    [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
    public static Result Run(mdr.DObject i0, mdr.DObject i1, bool LeftFirst)
    {
      var pLeft = new mdr.DValue();
      var pRight = new mdr.DValue();
      if (LeftFirst)
      {
        Convert.ToPrimitive.Run(i0, ref pLeft, false);
        Convert.ToPrimitive.Run(i1, ref pRight, false);
      }
      else
      {
        Convert.ToPrimitive.Run(i1, ref pRight, false);
        Convert.ToPrimitive.Run(i0, ref pLeft, false);
      }

      return Run(ref pLeft, ref pRight);
    }
  }
}
