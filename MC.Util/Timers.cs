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
using Stopwatch = System.Diagnostics.Stopwatch;

using m.Util.Diagnose;

namespace m.Util
{
  public class Timers
  {
    internal static readonly long NanoSecondsPerTick = -1;

    ///We want to ensure multiple runtimes etc can run simultaneously.
    ///To get consistent values, we use one global static StopWatch that no one should ever mess with.
    static readonly Stopwatch StopWatch;
    static readonly long OriginTimeNanosecond;
    public static long GetTicks() { return Timers.StopWatch.ElapsedTicks; }

    static Timers()
    {
      NanoSecondsPerTick = (1000L * 1000L * 1000L) / Stopwatch.Frequency;
      DateTime originTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
      var originElapseTime = DateTime.UtcNow - originTime;
      OriginTimeNanosecond = originElapseTime.Ticks * NanoSecondsPerTick; // * 0;
      StopWatch = new Stopwatch();
      StopWatch.Start();
    }

    public class SimpleTimer
    {
      public readonly string Name;

      long BeginTicks;
      //long EndTicks;
      int StartCount;
      public long ElapsedTicks { get; private set; }

      public long ElapsedNanoseconds { get { return ElapsedTicks * Timers.NanoSecondsPerTick; } }
      public double ElapsedMilliseconds { get { return ElapsedNanoseconds / 1000000.0; } }
      public long BeginNanoseconds { get { return BeginTicks * Timers.NanoSecondsPerTick; } }
      public long EndNanoseconds { get { return BeginNanoseconds + ElapsedNanoseconds; } }

      public SimpleTimer(string name)
      {
        Name = name;
        ElapsedTicks = 0;
        StartCount = 0;
      }
      public void Start()
      {
        if (StartCount == 0)
          BeginTicks = Timers.StopWatch.ElapsedTicks;
        ++StartCount;
      }
      public void Stop()
      {
        --StartCount;
        if (StartCount == 0)
        {
          var EndTicks = Timers.StopWatch.ElapsedTicks;
          ElapsedTicks += EndTicks - BeginTicks;
        }
      }
      public void ToXml(System.IO.TextWriter output)
      {
        output.WriteLine(
            " <T n='{0}' b='{1}' d='{2}' e='{3}' />"
            , Name
            , BeginNanoseconds + OriginTimeNanosecond
            , ElapsedNanoseconds
            , EndNanoseconds + OriginTimeNanosecond
        );
      }
    }

    /// <summary>
    /// We won't have too many instances of this class. But the timer's start/stop methods may be called many times. 
    /// </summary>
    public class Timer
    {
      public const string NameSeparator = "/";
      public readonly string Name;


      /// <summary>
      /// The ranges of each instance and its parents will belong to the same thread
      /// </summary>
      class ActiveRangeCollection
      {
        struct ActiveRange
        {
          public long BeginTicks;
          public long EndTicks;

          public long ElapsedTicks { get { return EndTicks - BeginTicks; } }

          public long BeginNanoseconds { get { return BeginTicks * Timers.NanoSecondsPerTick; } }
          public long EndNanoseconds { get { return EndTicks * Timers.NanoSecondsPerTick; } }
          public long ElapsedNanoseconds { get { return ElapsedTicks * Timers.NanoSecondsPerTick; } }

          public double BeginMilliseconds { get { return BeginNanoseconds / 1000000.0; } }
          public double EndMilliseconds { get { return EndNanoseconds / 1000000.0; } }
          public double ElapsedMilliseconds { get { return ElapsedNanoseconds / 1000000.0; } }
        }

        ///We store the ranges in a linked list of arrays. That way we spend less time creating nodes in memory. 
        ///RangesPerLine determines number of elements per array (i.e. line)
        const int RangesPerLine = 256;
        LinkedList<ActiveRange[]> _activeRanges;
        int _currentActiveRangeIndex;
        int _startCount;

        public ActiveRangeCollection()
        {
          _activeRanges = new LinkedList<ActiveRange[]>();
          _currentActiveRangeIndex = RangesPerLine;
          _startCount = 0;
        }

        public void Start(long currTicks)
        {
          if (_startCount == 0)
          {
            ActiveRange[] ranges;
            if (_currentActiveRangeIndex >= RangesPerLine)
            {
              ranges = new ActiveRange[RangesPerLine];
              _activeRanges.AddLast(ranges);
              _currentActiveRangeIndex = 0;
            }
            else
              ranges = _activeRanges.Last.Value;
            ranges[_currentActiveRangeIndex].BeginTicks = currTicks;
          }
          ++_startCount;
        }

        public void Stop(long currTicks)
        {
          --_startCount;
          if (_startCount == 0)
          {
            Debug.Assert(_activeRanges.Last != null && _currentActiveRangeIndex < RangesPerLine, "No Timer in {0} is already runing!", "this range");// FullName);
            ActiveRange[] ranges = _activeRanges.Last.Value;
            ranges[_currentActiveRangeIndex].EndTicks = currTicks;
            //ElapsedTicks += ranges[_currentActiveRangeIndex].ElapsedTicks;
            ++_currentActiveRangeIndex;
          }
        }

        public void Halt(Timer timer, int threadId, System.IO.TextWriter output)
        {
          if (_startCount != 0)
          {
            var activeCount = _startCount;
            _startCount = 1; //This is to make sure Stop() will work correctly
            Stop(Timers.StopWatch.ElapsedTicks);

            var ranges = _activeRanges.Last.Value;
            var index = ((_currentActiveRangeIndex >= RangesPerLine) ? RangesPerLine : _currentActiveRangeIndex) - 1;
            output.WriteLine(
                " <T n='{0}-{4}-EXCEPTION' b='{1}' d='{2}' e='{3}' t='{4}' active='{5}'/>"
                , timer.Name
                , ranges[index].BeginNanoseconds + OriginTimeNanosecond
                , ranges[index].ElapsedNanoseconds
                , ranges[index].EndNanoseconds + OriginTimeNanosecond
                , threadId
                , activeCount
            );
          }
        }

        public void ToXml(Timer timer, int threadId, System.IO.TextWriter output)
        {
          var ranges = _activeRanges.First;
          while (ranges != null)
          {
            var lastRangeIndex = (ranges == _activeRanges.Last) ? _currentActiveRangeIndex : RangesPerLine;
            for (var r = 0; r < lastRangeIndex; ++r)
            {
              output.WriteLine(
                  " <T n='{0}' b='{1}' d='{2}' e='{3}' t='{4}'/>"
                  , timer.Name
                  , ranges.Value[r].BeginNanoseconds + OriginTimeNanosecond
                  , ranges.Value[r].ElapsedNanoseconds
                  , ranges.Value[r].EndNanoseconds + OriginTimeNanosecond
                  , threadId
              );
            }
            ranges = ranges.Next;
          }
        }
      }
      System.Collections.Concurrent.ConcurrentDictionary<int, ActiveRangeCollection> _activeRanges = new System.Collections.Concurrent.ConcurrentDictionary<int, ActiveRangeCollection>();
      ActiveRangeCollection GetActiveRangeCollection(int threadId)
      {
        ActiveRangeCollection collection;
        if (!_activeRanges.TryGetValue(threadId, out collection))
          collection = _activeRanges.GetOrAdd(threadId, new ActiveRangeCollection());
        return collection;
      }

      public Timer(string name)
      {
        Name = name;
      }
      public void Start()
      {
        StartTimersOverhead();
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var currTicks = Timers.StopWatch.ElapsedTicks;
        GetActiveRangeCollection(threadId).Start(currTicks);
        StopTimersOverhead();
      }
      public void Stop()
      {
        StartTimersOverhead();
        var threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        var currTicks = Timers.StopWatch.ElapsedTicks;
        GetActiveRangeCollection(threadId).Stop(currTicks);
        StopTimersOverhead();
      }
      public void Halt(System.IO.TextWriter output)
      {
        foreach (var t in _activeRanges)
          t.Value.Halt(this, t.Key, output);
      }
      public void ToXml(System.IO.TextWriter output)
      {
        foreach (var t in _activeRanges)
          t.Value.ToXml(this, t.Key, output);
      }
    }

    public static SimpleTimer TimersOverhead = new SimpleTimer("TimerOverhaed");
    [System.Diagnostics.ConditionalAttribute("DIAGNOSE")]
    static void StartTimersOverhead() { TimersOverhead.Start(); }
    [System.Diagnostics.ConditionalAttribute("DIAGNOSE")]
    static void StopTimersOverhead() { TimersOverhead.Stop(); }

    readonly string Name;

    /// <summary>
    /// The following contains all timers, we want to make timer lookup based on id as fast as possible. 
    /// We guarantee that each parent timer is before all its children in the list
    /// </summary>
    readonly List<Timer> _timers = new List<Timer>();
    readonly Dictionary<string, int> _timerNames = new Dictionary<string, int>();

    readonly List<SimpleTimer> _simpleTimers = new List<SimpleTimer>();

    public Timers(string name = null)
    {
      if (string.IsNullOrEmpty(name))
        Name = "Timers";
      else
        Name = name;
    }

    /// <summary>
    /// This function should not be called often. Instead, its results should be cached and then GetTimer(int) used instead. 
    /// The function itself incorporate thread id in the name for timing parallel code
    /// </summary>
    /// <param name="names">names of timer hierarchy</param>
    /// <returns>id of the timer to be used with GetTimer</returns>
    public int GetTimerId(string name)
    {
      int timerId;
      StartTimersOverhead();
      lock (_timerNames)
      {
        if (!_timerNames.TryGetValue(name, out timerId))
        {
          timerId = _timers.Count;
          _timers.Add(new Timer(name));
          _timerNames[name] = timerId;
        }
      }
      StopTimersOverhead();
      return timerId;
    }
    /// <summary>
    /// We use this to speedup timer access during execution
    /// </summary>
    public Timer GetTimer(int timerId) { return _timers[timerId]; }
    public Timer GetTimer(string name) { return GetTimer(GetTimerId(name)); }

    public SimpleTimer GetSimpleTimer(string name)
    {
      var timer = new SimpleTimer(name);
      _simpleTimers.Add(timer);
      return timer;
    }

    public void ToXml(System.IO.TextWriter output)
    {
      output.WriteLine("<{0} NanosecondsPerTick='{1}'>", Name, NanoSecondsPerTick);

      for (var i = _timers.Count - 1; i >= 0; --i)
      {
        var timer = _timers[i];
        timer.Halt(Console.Out);// output);
      }

      for (var i = 0; i < _timers.Count; ++i)
      {
        var timer = _timers[i];
        timer.ToXml(output);
      }

      for (var i = 0; i < _simpleTimers.Count; ++i)
      {
        var timer = _simpleTimers[i];
        timer.ToXml(output);
      }

      //TimersOverhead.ToXml(output);
      output.WriteLine("</{0}>", Name);
    }

  }
}
