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

namespace mjr.Operations.Unary
{
  /// <summary>
  /// ECMA-262, 11.4.1; case 2 & 3.
  /// Called when deleting non-refrences
  /// This class is also used to help TypeInference
  /// </summary>
  public static partial class Delete
  {
    //public static void Run(ref mdr.DValue i0, ref mdr.DValue result)
    //{
    //    Trace.Fail(new NotSupportedException("Unsupported operation Delete"));
    //}
  }

  /// <summary>
  /// ECMA-262, 11.4.1; case 4.
  /// Called when deleting properties (o[i])
  /// </summary>
  public static partial class DeleteProperty
  {
  }

  /// <summary>
  /// ECMA-262, 11.4.1; case 5.
  /// Called when deleting declared variables (x)
  /// </summary>
  public static partial class DeleteVariable
  {
    public static bool Run(mdr.DObject i0, int i1) 
    {
      if (i1 == mdr.Runtime.InvalidFieldId)
        return false; //It is Local symbol with no FieldId

      var context = i0;
      while (context != mdr.Runtime.Instance.GlobalContext)
      {
        if (context.HasOwnPropertyByFieldId(i1))
          return false; //We cannot delete function locals
        else
          context = context.Prototype;
      }
      //Now we try to delete from GlobalContext
      //return (context.DeletePropertyDescriptorByFieldId(i1) != mdr.PropertyMap.DeleteStatus.NotDeletable); 

      Debug.Warning("Deleting global members is not supported!");
      return false; // as if it was local of a function
    }
  }

}
