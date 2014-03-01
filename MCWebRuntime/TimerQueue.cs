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

namespace mwr
{
    public class TimeoutEvent
    {
        private static int _id;
        public readonly int Id;
        long _interval;
        bool isInterval;
        public DateTime ActivationTime;
        public mdr.CallFrame CallFrame;
        public bool cleared;
        public bool IsInterval { get { return isInterval; } }
        public long Interval { get { return _interval; } }
 
        public TimeoutEvent(long timeout, bool isInterval)
        {
            ActivationTime = DateTime.UtcNow.AddMilliseconds(timeout);
            if (isInterval)
            {
                Id = _id++;
                _interval = timeout;
                this.isInterval = true;
            }
            else
            {
                //                Id = -1;
                Id = _id++;
                _interval = 0;
                this.isInterval = false;
            }
            cleared = false;
        }
        public void UpdateActivationTime()
        {
            ActivationTime = ActivationTime.AddMilliseconds(_interval);
        }
        public void UpdateActivationTime(int Intervals)
        {
            ActivationTime = ActivationTime.AddMilliseconds(_interval * Intervals);
        }
    }

    public class TimerQueue
    {
        SortedDictionary<DateTime, TimeoutEvent> _queue = new SortedDictionary<DateTime, TimeoutEvent>();
        
        public int QueueSize { get { return _queue.Count; } }

        public void SetTimeoutOrInterval(TimeoutEvent timeout)
        {
            _queue.Add(timeout.ActivationTime, timeout);
        }
        
        public void ClearTimeout(int id)
        {
            var t = _queue.Values.FirstOrDefault(item => item.Id == id);
            if (t != default(TimeoutEvent))
            {
                _queue.Remove(t.ActivationTime);
                t.cleared = true;
            }
        }

        public void ClearInterval(int id)
        {
            var t = _queue.Values.FirstOrDefault(item => item.Id == id);
            if (t != default(TimeoutEvent))
            {
                _queue.Remove(t.ActivationTime);
                t.cleared = true;

            }
        }

        public DateTime NextActivationTime()
        {
            if (_queue.Count == 0)
                return DateTime.UtcNow.AddSeconds(1000); //Should be a big enough number to avoid triggering the timeout during typical use, although if it triggers that is ok
            else
                return _queue.Keys.FirstOrDefault();
        }

        public void ProcessEvents()
        {
            DateTime currTime = DateTime.UtcNow;
            while (true)
            {
                var timer = _queue.Values.FirstOrDefault();
                if (_queue.Count() == 0 || timer == default(TimeoutEvent) || timer == null || timer.ActivationTime > currTime)
                {
                    return;
                }
                try
                {
                  timer.CallFrame.This = HTMLRuntime.Instance.GlobalContext;
                  Debug.Assert(timer.CallFrame.Function != null, "The handler function of timer {0} is null", timer.Id);
                  if (timer.CallFrame.Function != null) //TODO: this is here to avoid Trace.ASSERT which slow things down.
                    timer.CallFrame.Function.Call(ref timer.CallFrame);
                }
                catch (Exception ex)
                {
                  //In debug mode stop execution by forwarding the exception to the top level! 
                  //If not in debug mode, just ignore the exception and contiue with the timer maintainace code
                  Diagnostics.WriteException(ex, "when processing timer");
                }
                finally
                {
                  _queue.Remove(timer.ActivationTime);//Replace it with sth that remove the first element
                }
                if (timer.IsInterval && !timer.cleared)
                {

                    TimeSpan diff = DateTime.UtcNow - timer.ActivationTime;
                    int passed_intervals = (int)(diff.Ticks/(10000*timer.Interval));
                    if (passed_intervals == 0)
                        passed_intervals = 1;
                    timer.UpdateActivationTime(passed_intervals);
                    _queue.Add(timer.ActivationTime, timer);
                }
            }
        }
    }
}
