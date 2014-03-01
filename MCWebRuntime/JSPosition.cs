// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
// Copyright (c) [2010-2014] The Regents of the University of California
// All rights reserved.
// Redistribution and use in source and binary forms, with or without modification, are permitted (subject to the limitations in the disclaimer below) provided that the following conditions are met:
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of The Regents of the University of California nor the project name nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// NO EXPRESS OR IMPLIED LICENSES TO ANY PARTY'S PATENT RIGHTS ARE GRANTED BY THIS LICENSE. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// --~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~----~--~--~--~--
﻿using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

using mwr.DOM;

namespace mwr
{
    /// <summary>
    /// We store all event data in this struct so that we can easily transfer data between C++ & C#
    /// </summary>

    // Remember to update the C++ version of this enumeration if you make any changes.

    //Alex will complete this section
    [StructLayout(LayoutKind.Sequential)]
    public struct PositionData
    {
        #region Position
        public double latitude;
        public double longitude;
        public double altitude;
        public double accuracy;
        public double altitudeAccuracy;
        public double heading;
        public double speed;
        public string lciid;
        public UInt64 timestamp;
        public Int32 status;
        #endregion
    }


    public class JSPosition : WrappedObject
    {
        public PositionData Data = new PositionData();

        public JSPosition(IntPtr objPtr)
        : base(objPtr)
        {
        }
        public static new void PreparePrototype(mdr.DObject prototype)
        {
            prototype.DefineOwnProperty("latitude", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.latitude);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("longitude", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.longitude);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("altitude", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.altitude);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("accuracy", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.accuracy);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("altitudeAccuracy", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.altitudeAccuracy);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("heading", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.heading);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable);
            prototype.DefineOwnProperty("speed", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.speed);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable); 
            prototype.DefineOwnProperty("lciid", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.lciid);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable); 
            prototype.DefineOwnProperty("timestamp", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.timestamp);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable); 
            prototype.DefineOwnProperty("status", new mdr.DProperty()
            {
                OnGetDValue = (mdr.DObject This, ref mdr.DValue v) =>
                {
                    var ev = This.FirstInPrototypeChainAs<JSPosition>();
                    v.Set(ev.Data.status);
                },
            }, mdr.PropertyDescriptor.Attributes.NotWritable); 

        }
    }
}
