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
using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using System.Security.Permissions;

namespace DOMBinding
{
	public static class SampleDOMNode
	{
		static mdr.DObject prototype;

		static SampleDOMNode()
		{
		    //Here we build the prototype of DOM Object Wrapper. This prototype must have all
		    //the methods of the DOM object callable by JS.
		    prototype = new mdr.DObject();
		    prototype.SetField("GetElementByName", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
		    {
		        var o = callFrame.This as WrappedObject;
		        var domptr = o.Domptr;

				//GCHandle gchWrapperIndex = GCHandle.Alloc(new int(), GCHandleType.Pinned);
		        var elemName = callFrame.Arg0.ToString();

				WrappedObject elemPtr = GetElementByName(domptr,                 /*gchWrapperIndex.AddrOfPinnedObject(),*/elemName) as WrappedObject;

				//int wrapperindex = (int)gchWrapperIndex.Target;
		        //gchWrapperIndex.Free();

				callFrame.Return.Set(elemPtr);
		    }));

            prototype.SetField("GetName", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
		    {
		        var o = callFrame.This as WrappedObject;
		        var arg0 = o.Domptr;
		        callFrame.Return.Set(GetName(arg0));
		    }));

            prototype.SetField("Num", new mdr.DProperty()
            {
                TargetValueType = mdr.ValueTypes.Int32,
                OnGetInt = (This) =>
                {
                    Console.WriteLine("running getNum in C#");
                    Console.Out.Flush();
                    return GetNum(((WrappedObject) This).Domptr);
                },
                OnSetInt = (This, n) => SetNum(((WrappedObject) This).Domptr, n),
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) => { v.Set(GetNum(((WrappedObject) This).Domptr)); },
            });

		}

		public static WrappedObject MakeWrapper(IntPtr domptr)
		{
		    WrappedObject wrapper = new WrappedObject(domptr, prototype);
  			return wrapper;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern public static string GetName(IntPtr domObject);
	
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern public static WrappedObject GetElementByName(IntPtr domObject, string name);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public static int GetNum(IntPtr domObject);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        extern public static void SetNum(IntPtr domObject, int num);
	}
}
