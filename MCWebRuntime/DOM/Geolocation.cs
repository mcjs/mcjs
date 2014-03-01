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

namespace mwr.DOM
{
    public partial class Geolocation
    {
        static bool GeoTrackingEnabled = false;
        mwr.PositionListeners geoListeners = new mwr.PositionListeners(false);
        mwr.PositionListeners geoWatches = new mwr.PositionListeners(true);

        public static void getCurrentPosition(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("++$> calling Geolocation:getCurrentPosition(this, arg0, arg1, arg2)");
            var handler = callFrame.Arg0.AsDFunction();
            Geolocation geo = callFrame.This as Geolocation;
            if (geo != null)
            {
                int id;
                geo.geoListeners.AddNewListener(new PositionListener(handler, null, null), out id);
                Debug.WriteLine("Adding a new position listener to the current Geolocation with id {0}", id);
                if (!GeoTrackingEnabled)
                {
                    //Enable Geotracking
                    GeoTrackingEnabled = true;
                    HTMLRuntime.Instance.StartLocationTracking();
                    Debug.WriteLine("Enabling Geo Tracking", id);
                }
            }
            
        }
        public static void watchPosition(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("++$> calling Geolocation:watchPosition(this, arg0, arg1, arg2)");
            var handler = callFrame.Arg0.AsDFunction();
            Geolocation geo = callFrame.This as Geolocation;
            if (geo != null)
            {
                int id;
                geo.geoWatches.AddNewListener(new PositionListener(handler, null, null), out id);
                callFrame.Return.Set(id);
                Debug.WriteLine("Adding a new position watch to the current Geolocation with id {0}", id);
                if (!GeoTrackingEnabled)
                {
                    //Enable Geotracking
                    GeoTrackingEnabled = true;
                    HTMLRuntime.Instance.StartLocationTracking();
                    Debug.WriteLine("Enabling Geo Tracking");
                }
            }
        }
        public static void clearWatch(ref mdr.CallFrame callFrame)
        {
            Debug.WriteLine("++$> calling watchPosition:clearWatch(this, arg0)");
            int id = callFrame.Arg0.AsInt32();
            Geolocation geo = callFrame.This as Geolocation;
            if (geo != null)
            {
                geo.geoWatches.RemoveListener(id);
                Debug.WriteLine("Removing position watch with id {0} listeners {1} watches {2}", id,
                    geo.geoListeners.ListenerCount(), geo.geoWatches.ListenerCount());
                if (GeoTrackingEnabled && geo.geoListeners.ListenerCount() == 0 && geo.geoWatches.ListenerCount() == 0)
                {
                    //Disable Geotracking
                    Debug.WriteLine("Disabling Geo Tracking");
                    HTMLRuntime.Instance.StopLocationTracking();
                    GeoTrackingEnabled = false;
                }
            }
        }
        static public void ProcessPositionEvent(IntPtr workItem)
        {
            Debug.WriteLine("Calling Geolocation ProcessPositionEvent, workItem is {0}", workItem);
            UILocationWorkItem wItem = new UILocationWorkItem(workItem);
            JSPosition position = new JSPosition(new IntPtr(0));

            Debug.WriteLine("Calling UIWorkItem getPositionUpdate");
            wItem.GetPositionUpdate(ref position.Data);
            ContentWindow window = mdr.Runtime.Instance.GlobalContext as ContentWindow;
            Debug.WriteLine("Position update: lat {0} long {1}", position.Data.latitude, position.Data.longitude);
            Geolocation geo = window.Navigator.Geolocation;
            if (geo != null)
            {
                geo.geoListeners.Dispatch(position);
                geo.geoWatches.Dispatch(position);
                if (GeoTrackingEnabled && geo.geoListeners.ListenerCount() == 0 && geo.geoWatches.ListenerCount() == 0)
                {
                    //Disable Geotracking
                    Debug.WriteLine("Disabling Geo Tracking");
                    HTMLRuntime.Instance.StopLocationTracking();
                    GeoTrackingEnabled = false;
                }
            }
        }

    }
}
