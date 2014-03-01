// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿
namespace mjr.Builtins
{
    class JSError : JSBuiltinConstructor
    {
        public JSError() : base(new mdr.DObject(), "Error")
        {
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                var error = new mdr.DObject(TargetPrototype);
                switch(callFrame.PassedArgsCount)
                {
                    case 0:
                        break;
                    case 1:
                        error.SetField("message", callFrame.Arg0.AsString());
                        goto case 0;
                    case 2:
                        error.SetField("fileName", callFrame.Arg1.AsString());
                        goto case 1;
                    case 3:
                        error.SetField("lineNumber", callFrame.Arg2.AsString());
                        goto case 2;
                }
                if (IsConstrutor)
                    callFrame.This = (error);
                else
                    callFrame.Return.Set(error);
            };

            TargetPrototype.DefineOwnProperty("message", "", mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("name", "Error", mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction((ref mdr.CallFrame callFrame) => 
            {
                var name = callFrame.This.GetField("name").AsString();
                var message = callFrame.This.GetField("message").AsString();
                callFrame.Return.Set(string.Format("{0}: {1}", name, message));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

        }
    }
}
