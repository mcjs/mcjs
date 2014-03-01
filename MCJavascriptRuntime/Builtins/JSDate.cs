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
using System.Globalization;

using m.Util.Diagnose;

namespace mjr.Builtins
{
    class JSDate : JSBuiltinConstructor
    {
        //class DDate : mdr.DObject
        //{
        //    private double value;
        //    public DDate(mdr.DObject prototype) : base(prototype) { }
        //    public override double ToDouble() { return value; }
        //    public override mdr.DObject Set(double v) { value = v; return this; }
        //    public override mdr.DObject Set(long v) { value = v; return this; }
        //}

        public const string FORMAT = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'zzz";
        public const string FORMATUTC = "ddd, dd MMM yyyy HH':'mm':'ss 'UTC'";
        public const string DATEFORMAT = "ddd, dd MMM yyyy";
        public const string TIMEFORMAT = "HH':'mm':'ss 'GMT'zzz";
        public const string FORMATISO = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";


        // Convert Js time to .Net time
        public static long JsToNetTime(long jsTime)
        {
            return (jsTime * TICKSFACTOR) + OFFSET_1970;
        }

        // create a DateTime object from a javascript time value
        public static DateTime CreateDateTime(long jsTime)
        {
            return new DateTime(JsToNetTime(jsTime));
        }

        private void SetMillisecond(ref DateTime time, double ms)
        {
            time.AddMilliseconds(-time.Millisecond); //reset miliseond to zero
            time.AddMilliseconds(ms);
        }

        private void SetSecond(ref DateTime time, double sec)
        {
            time.AddSeconds(-time.Second); //reset second to zero
            time.AddSeconds(sec);
        }

        private void SetMinute(ref DateTime time, double min)
        {
            time.AddMinutes(-time.Minute); //reset minute to zero
            time.AddMinutes(min);
        }

        private void SetHour(ref DateTime time, double hour)
        {
            time.AddHours(-time.Hour); //reset hour to zero
            time.AddHours(hour);
        }

        private void SetDay(ref DateTime time, double day)
        {
            time.AddDays(-time.Day); //reset day to zero
            time.AddDays(day);
        }

        private void SetMonth(ref DateTime time, double month)
        {
            time.AddMonths(-time.Month); //reset month to zero
            time.AddMonths((int)month);
        }

        private void SetYear(ref DateTime time, double year)
        {
            time.AddYears((int)year - time.Year);
        }

        const int HOURS_PER_DAY = 24;
        const int MINUTES_PER_HOUR = 60;
        const int SECONDS_PER_MINUTE = 60;
        const int MS_PER_SECOND = 1000;
        const int MS_PER_MINUTE = MS_PER_SECOND * SECONDS_PER_MINUTE;
        const int MS_PER_HOUR = MS_PER_MINUTE * MINUTES_PER_HOUR;
        const int MS_PER_DAY = MS_PER_HOUR * HOURS_PER_DAY;

        internal readonly static int[] DAY_FROM_MONTH = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        internal readonly static int[] DAY_FROM_MONTH_LEAP = { 0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335 };

#if DEBUG
        internal readonly static long OFFSET_1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
#else
        internal const long OFFSET_1970 = 621355968000000000;
#endif
        internal const int TICKSFACTOR = 10000;
        public static long ToNumber(DateTime date)
        {
            /*
             * DataTime.Ticks returns number of 100 nanosec units elapsed since Jan 1, 2001.
             * In Javascrtip time is measured as the number of miliseconds since Jan 1, 1970.
             */
            return (date.Ticks - OFFSET_1970) / TICKSFACTOR;
        }

        /*
         * CheckAndCall functions check the first input argument and call one of the methods.
         * They act like macros in C++. The reason that we have these is to avoid repeating input
         * checking in all functions and also because many of the JSDate methods call one another.
         * So, we couldn't only implement them inside SetField's.
         */
        private delegate long longFunc<T>(T input);
        private delegate int intFunc<T>(T input);
        private delegate double doubleFunc<T>(T input);
        private delegate string stringFunc<T>(T input);

        private mdr.DValue CheckAndCall(ref mdr.CallFrame callFrame, longFunc<long> localFunc)
        {
            double firstArg = callFrame.Arg0.AsDouble();
            if (double.IsNaN(firstArg))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set(localFunc((long)firstArg));
            return callFrame.Return;
        }

        private mdr.DValue CheckAndCall(ref mdr.CallFrame callFrame, longFunc<int> localFunc)
        {
            double firstArg = callFrame.Arg0.AsDouble();
            if (double.IsNaN(firstArg))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set((double)localFunc((int)firstArg));
            return callFrame.Return;
        }

        private mdr.DValue CheckAndCall(ref mdr.CallFrame callFrame, intFunc<int> localFunc)
        {
            double firstArg = callFrame.Arg0.AsDouble();
            if (double.IsNaN(firstArg))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set(localFunc((int)firstArg));
            return callFrame.Return;
        }

        private mdr.DValue CheckAndCall(ref mdr.CallFrame callFrame, intFunc<long> localFunc)
        {
            double firstArg = callFrame.Arg0.AsDouble();
            if (double.IsNaN(firstArg))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set(localFunc((long)firstArg));
            return callFrame.Return;
        }

        // like above functions, but the followings check the value of "callFrame.This" instead of the args
        private mdr.DValue CheckValueAndCall(ref mdr.CallFrame callFrame, longFunc<long> localFunc)
        {
            double thisValue = callFrame.This.ToDouble();
            if (double.IsNaN(thisValue))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set((double)localFunc((long)thisValue));
            return callFrame.Return;
        }

        private mdr.DValue CheckValueAndCall(ref mdr.CallFrame callFrame, stringFunc<long> localFunc)
        {
            double thisValue = callFrame.This.ToDouble();
            if (double.IsNaN(thisValue))
                callFrame.Return.Set(double.NaN);
            else
                callFrame.Return.Set(localFunc((long)thisValue));
            return callFrame.Return;
        }

        //Checks callFrame.Arguments and returns true if all are finite        
        private Boolean CheckArgsFinite(ref mdr.CallFrame callframe, int numArgs = 0)
        {
            if (numArgs != 0 && callframe.PassedArgsCount < numArgs)
                return false;

            for (int i = 0; i < callframe.PassedArgsCount; i++)
            {
                if (double.IsInfinity(callframe.Arg(i).AsDouble()))
                    return false;
            }
            return true;
        }

        //Checks callFrame.Arguments and returns true if all are finite
        private Boolean IsValueNaN(mdr.DObject obj)
        {
            return double.IsNaN(obj.ToDouble()) ? true : false;
        }

        public JSDate()
            : base(new mdr.DObject(), "Date")
        {
            // ECMA 262 - 15.9.2
            JittedCode = (ref mdr.CallFrame callFrame) =>
            {
                if (IsConstrutor)
                {
                    mdr.DObject date = new mdr.DObject(TargetPrototype);
                    double pvalue = constructDate(ref callFrame);
                    date.PrimitiveValue.Set(pvalue);
                    callFrame.This = (date);
                }
                else
                    callFrame.Return.Set(toString(now()));
            };

            this.DefineOwnProperty("TimeWithinDay", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<long>)TimeWithinDay); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("Day", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<long>)Day); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DaysInYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<int>)DaysInYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DayFromYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<int>)DayFromYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("TimeFromYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<int>)TimeFromYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("YearFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)YearFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("InLeapYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)InLeapYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DayFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)DayFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DayWithinYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)DayWithinYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("TimeInYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<int>)TimeInYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("MonthFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)MonthFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DayWithinYear", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)DayWithinYear); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("DateFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)DateFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("WeekDay", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)WeekDay); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("UTC", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<long>)UTC); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("LocalTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (longFunc<long>)LocalTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("HourFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)HourFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("MinFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)MinFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("SecFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)SecFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            this.DefineOwnProperty("msFromTime", new mdr.DFunction((ref mdr.CallFrame callFrame) => { CheckAndCall(ref callFrame, (intFunc<long>)msFromTime); }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
            // ECMA-262 section 15.9.1.11
            TargetPrototype.DefineOwnProperty("MakeTime", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.MakeTime");
                if (CheckArgsFinite(ref callFrame, 4))
                    callFrame.Return.Set(double.NaN);
                else
                    callFrame.Return.Set(MakeTime(callFrame.Arg0.AsInt32(),
                                        callFrame.Arg1.AsInt32(),
                                        callFrame.Arg2.AsInt32(),
                                        callFrame.Arg3.AsInt32()));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.9.1.12
            TargetPrototype.DefineOwnProperty("MakeDay", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.MakeDay");
                if (CheckArgsFinite(ref callFrame, 3))
                    callFrame.Return.Set(double.NaN);
                else
                {
                    int year = callFrame.Arg0.AsInt32();
                    int month = callFrame.Arg1.AsInt32();
                    int date = callFrame.Arg2.AsInt32();
                    callFrame.Return.Set(MakeDay(year, month, date));
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.9.1.13
            TargetPrototype.DefineOwnProperty("MakeDate", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.MakeDate");
                if (CheckArgsFinite(ref callFrame, 2))
                {
                    callFrame.Return.Set(double.NaN);
                }
                else
                {
                    int day = callFrame.Arg0.AsInt32();
                    long time = (long)callFrame.Arg1.AsDouble();
                    callFrame.Return.Set((double)MakeDate(day, time));
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.9.1.13
            TargetPrototype.DefineOwnProperty("TimeClip", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.TimeClip");
                if (CheckArgsFinite(ref callFrame, 1))
                    callFrame.Return.Set(double.NaN);
                else
                {
                    Double time = callFrame.Arg0.AsDouble();
                    callFrame.Return.Set(TimeClip(time));
                }
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA-262 section 15.9.4.2
            TargetPrototype.DefineOwnProperty("parse", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.parse");
                string inputString = callFrame.Arg0.AsString();
                long time = parse(inputString);

                if (time > 0)
                    callFrame.Return.Set(time);
                else
                    callFrame.Return.Set(double.NaN);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.4.3
            TargetPrototype.DefineOwnProperty("UTC", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.UTC");
                var year = callFrame.Arg0.AsInt32();
                var month = callFrame.Arg1.AsInt32();
                var argc = callFrame.PassedArgsCount;
                var date = argc > 2 ? callFrame.Arg2.AsInt32() : 1;
                var hours = argc > 3 ? callFrame.Arg3.AsInt32() : 0;
                var minutes = argc > 4 ? callFrame.Arg(4).AsInt32() : 0;
                var seconds = argc > 5 ? callFrame.Arg(5).AsInt32() : 0;
                var ms = argc > 6 ? callFrame.Arg(6).AsInt32() : 0;
                year = (!double.IsNaN(year) && 0 <= year && year <= 99)
                    ? 1900 + year : year;
                var day = MakeDay(year, month, date);
                var time = MakeTime(hours, minutes, seconds, ms);
                callFrame.Return.Set(TimeClip(MakeDate(day, time)));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.4.4
            TargetPrototype.DefineOwnProperty("now", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.now");
                callFrame.Return.Set((double)now());
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.2
            TargetPrototype.DefineOwnProperty("toString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.3
            TargetPrototype.DefineOwnProperty("toDateString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toDateString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toDateString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.4
            TargetPrototype.DefineOwnProperty("toTimeString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toTimeString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toTimeString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.5
            TargetPrototype.DefineOwnProperty("toLocaleString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toLocaleString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toLocaleString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.6
            TargetPrototype.DefineOwnProperty("toLocaleDateString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toLocaleDateString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toLocaleDateString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.7
            TargetPrototype.DefineOwnProperty("toLocaleTimeString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toLocaleTimeString");
                CheckValueAndCall(ref callFrame, (stringFunc<long>)toLocaleTimeString);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.8
            TargetPrototype.DefineOwnProperty("valueOf", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.valueOf");
                CheckValueAndCall(ref callFrame, (longFunc<long>)valueOf);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.9
            TargetPrototype.DefineOwnProperty("getTime", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getTime");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.10
            TargetPrototype.DefineOwnProperty("getFullYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getFullYear");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getFullYear);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - B.2.4
            TargetPrototype.DefineOwnProperty("getYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getYear");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getYear);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.11
            TargetPrototype.DefineOwnProperty("getUTCFullYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCFullYear");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCFullYear);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.12
            TargetPrototype.DefineOwnProperty("getMonth", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getMonth");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getMonth);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.13
            TargetPrototype.DefineOwnProperty("getUTCMonth", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCMonth");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCMonth);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.14
            TargetPrototype.DefineOwnProperty("getDate", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getDate");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getDate);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.15
            TargetPrototype.DefineOwnProperty("getUTCDate", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCDate");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCDate);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.16
            TargetPrototype.DefineOwnProperty("getDay", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getDay");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getDay);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.17
            TargetPrototype.DefineOwnProperty("getUTCDay", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCDay");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCDay);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.18
            TargetPrototype.DefineOwnProperty("getHours", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getHours");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getHours);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.19
            TargetPrototype.DefineOwnProperty("getUTCHours", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCHours");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCHours);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.20
            TargetPrototype.DefineOwnProperty("getMinutes", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getMinutes");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getMinutes);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.21
            TargetPrototype.DefineOwnProperty("getUTCMinutes", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCMinutes");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCMinutes);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.22
            TargetPrototype.DefineOwnProperty("getSeconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getSeconds");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getSeconds);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.23
            TargetPrototype.DefineOwnProperty("getUTCSeconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCSeconds");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCSeconds);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.24
            TargetPrototype.DefineOwnProperty("getMilliseconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getMilliseconds");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getMilliseconds);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.25
            TargetPrototype.DefineOwnProperty("getUTCMilliseconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getUTCMilliseconds");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getUTCMilliseconds);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.26
            TargetPrototype.DefineOwnProperty("getTimezoneOffset", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.getTimezoneOffset");
                CheckValueAndCall(ref callFrame, (longFunc<long>)getTimezoneOffset);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.27
            TargetPrototype.DefineOwnProperty("setTime", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setTime");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No time specified");

                long time = (long)TimeClip(callFrame.Arg0.AsDouble());
                callFrame.This.PrimitiveValue.Set(time);
                callFrame.Return.Set(time);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.28
            TargetPrototype.DefineOwnProperty("setMilliseconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setMilliseconds");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No millisecond specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetMillisecond(ref thisDateTime, callFrame.Arg0.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.29
            TargetPrototype.DefineOwnProperty("setUTCMilliseconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCMilliseconds");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No millisecond specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetMillisecond(ref thisDateTime, callFrame.Arg0.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.30
            TargetPrototype.DefineOwnProperty("setSeconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setSeconds");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No Second specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetSecond(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMillisecond(ref thisDateTime, callFrame.Arg1.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.31
            TargetPrototype.DefineOwnProperty("setUTCSeconds", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCSeconds");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No Second specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetSecond(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMillisecond(ref thisDateTime, callFrame.Arg1.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.32
            TargetPrototype.DefineOwnProperty("setMinutes", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setMinutes");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No Minute specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetMinute(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetSecond(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetMillisecond(ref thisDateTime, callFrame.Arg2.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.33
            TargetPrototype.DefineOwnProperty("setUTCMinutes", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCMinutes");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No Second specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetMinute(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetSecond(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetMillisecond(ref thisDateTime, callFrame.Arg2.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.34
            TargetPrototype.DefineOwnProperty("setHours", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setHours");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No hour specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetHour(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMinute(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetSecond(ref thisDateTime, callFrame.Arg2.AsDouble());
                if (callFrame.PassedArgsCount > 3)
                    SetMillisecond(ref thisDateTime, callFrame.Arg3.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.35
            TargetPrototype.DefineOwnProperty("setUTCHours", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCHours");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No hour specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetHour(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMinute(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetSecond(ref thisDateTime, callFrame.Arg2.AsDouble());
                if (callFrame.PassedArgsCount > 3)
                    SetMillisecond(ref thisDateTime, callFrame.Arg3.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.36
            TargetPrototype.DefineOwnProperty("setDate", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setDate");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No day specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetDay(ref thisDateTime, callFrame.Arg0.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.37
            TargetPrototype.DefineOwnProperty("setUTCDate", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCDate");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No day specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetDay(ref thisDateTime, callFrame.Arg0.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.38
            TargetPrototype.DefineOwnProperty("setMonth", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setMonth");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No month specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetMonth(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetDay(ref thisDateTime, callFrame.Arg1.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.39
            TargetPrototype.DefineOwnProperty("setUTCMonth", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCMonth");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No month specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetMonth(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetDay(ref thisDateTime, callFrame.Arg1.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.40
            TargetPrototype.DefineOwnProperty("setFullYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setFullYear");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No year specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetYear(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMonth(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetDay(ref thisDateTime, callFrame.Arg2.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - B.2.5
            TargetPrototype.DefineOwnProperty("setYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setYear");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No year specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime).ToLocalTime();
                SetYear(ref thisDateTime, Operations.Convert.ToDouble.Run(ref callFrame.Arg0));
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.41
            TargetPrototype.DefineOwnProperty("setUTCFullYear", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.setUTCFullYear");
                if (callFrame.PassedArgsCount == 0)
                    throw new ArgumentException("No year specified");

                long thisTime = (long)callFrame.This.PrimitiveValue.AsDouble();
                DateTime thisDateTime = CreateDateTime(thisTime);
                SetYear(ref thisDateTime, callFrame.Arg0.AsDouble());
                if (callFrame.PassedArgsCount > 1)
                    SetMonth(ref thisDateTime, callFrame.Arg1.AsDouble());
                if (callFrame.PassedArgsCount > 2)
                    SetDay(ref thisDateTime, callFrame.Arg2.AsDouble());
                thisTime = ToNumber(thisDateTime);
                callFrame.This.PrimitiveValue.Set(thisTime);
                callFrame.Return.Set(thisTime);
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.42
            TargetPrototype.DefineOwnProperty("toUTCString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toUTCString");
                double thisTime = callFrame.This.PrimitiveValue.AsDouble();
                if (double.IsNaN(thisTime))
                {
                    callFrame.Return.Set(double.NaN.ToString());
                }

                callFrame.Return.Set(CreateDateTime((long)thisTime).ToString(JSDate.FORMATUTC, CultureInfo.InvariantCulture));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.42
            TargetPrototype.DefineOwnProperty("toISOString", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toISOString");
                double thisTime = callFrame.This.PrimitiveValue.AsDouble();
                if (double.IsNaN(thisTime))
                    callFrame.Return.Set(double.NaN.ToString());
                else if (double.IsInfinity(thisTime))
                    throw ArgumentOutOfRangeException("out of range");

                callFrame.Return.Set(CreateDateTime((long)thisTime).ToString(JSDate.FORMATISO, CultureInfo.InvariantCulture));
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);

            // ECMA 262 - 15.9.5.44
            TargetPrototype.DefineOwnProperty("toJSON", new mdr.DFunction((ref mdr.CallFrame callFrame) =>
            {
                Debug.WriteLine("calling JSDate.toJSON");
                throw new NotSupportedException("toJSON");
            }), mdr.PropertyDescriptor.Attributes.NotEnumerable | mdr.PropertyDescriptor.Attributes.Data);
        }

        private Exception ArgumentOutOfRangeException(string p)
        {
            throw new NotImplementedException();
        }


        // ECMA 262 - 15.9.1.2
        private long TimeWithinDay(long time)
        {
            return time % MS_PER_DAY;
        }

        private long Day(long time)
        {
            return time / MS_PER_DAY;
        }

        // ECMA 262 - 15.9.1.3
        private int DaysInYear(int year)
        {
            int days = 366;
            if ((year % 4 != 0) ||
                (year % 100 == 0) && (year % 400 != 0))
            {
                days = 365;
            }
            return days;
        }

        private int DayFromYear(int year)
        {
            return 365 * (year - 1970)
                + ((year - 1969) / 4)
                - ((year - 1901) / 100)
                + ((year - 1601) / 400);
        }

        private long TimeFromYear(int year)
        {
            return MS_PER_DAY * DayFromYear(year);
        }

        private int YearFromTime(long time)
        {
            return new DateTime(JsToNetTime(time)).Year;
        }

        private int InLeapYear(long time)
        {
            return DaysInYear(YearFromTime(time)) == 366 ? 1 : 0;
        }

        private int DayFromTime(long time)
        {
            return new DateTime(JsToNetTime(time)).Day;
        }

        private long TimeInYear(int year)
        {
            return DaysInYear(year) * MS_PER_DAY;
        }

        // ECMA 262 - 15.9.1.4
        private int MonthFromTime(long time)
        {
            return new DateTime(JsToNetTime(time)).Month;
        }

        private int DayWithinYear(long time)
        {
            return new DateTime(JsToNetTime(time)).DayOfYear;
        }

        // ECMA 262 - 15.9.1.5
        private int DateFromTime(long time)
        {
            return new DateTime(JsToNetTime(time)).Day;
        }

        // ECMA 262 - 15.9.1.6
        private int WeekDay(long time)
        {
            return (int)((Day(time) + 4) % 7);
        }

        // ECMA 262 - 15.9.1.9
        private long UTC(long time)
        {
            return (int)((Day(time) + 4) % 7);
        }

        private long LocalTime(long time)
        {
            return ToNumber(new DateTime(JsToNetTime(time)).ToLocalTime());
        }

        // ECMA 262 - 15.9.1.10
        private int HourFromTime(long time)
        {
            return (int)((time / MS_PER_HOUR) % HOURS_PER_DAY);
        }

        private int MinFromTime(long time)
        {
            return (int)((time / MS_PER_MINUTE) % MINUTES_PER_HOUR);
        }

        private int SecFromTime(long time)
        {
            return (int)((time / MS_PER_SECOND) % SECONDS_PER_MINUTE);
        }

        private int msFromTime(long time)
        {
            return (int)(time % MS_PER_SECOND);
        }

        private double TimeClip(double time)
        {
            if (Math.Abs(time) > 8.6E15 || double.IsInfinity(time))
                return double.NaN;
            else
                return time;
        }

        // ECMA 262 - 15.9.1.11
        private long MakeTime(int hour, int minute, int second, int ms)
        {
            return (hour * MS_PER_HOUR +
                minute * MS_PER_MINUTE +
                second * MS_PER_SECOND +
                ms);
        }


        // ECMA 262 - 15.9.1.12
        private int MakeDay(int year, int month, int day)
        {
            year += month / 12;
            month %= 12;
            if (month < 0)
            {
                year--;
                month += 12;
            }

            Debug.Assert(month >= 0);
            Debug.Assert(month < 12);

            // year_delta is an arbitrary number such that:
            // a) year_delta = -1 (mod 400)
            // b) year + year_delta > 0 for years in the range defined by
            //    ECMA 262 - 15.9.1.1, i.e. upto 100,000,000 days on either side of
            //    Jan 1 1970. callFrame.This is required so that we don't run into integer
            //    division of negative numbers.
            // c) there shouldn't be an overflow for 32-bit integers in the following
            //    operations.
            const int year_delta = 399999;
            const int base_day = 365 * (1970 + year_delta) +
                                        (1970 + year_delta) / 4 -
                                        (1970 + year_delta) / 100 +
                                        (1970 + year_delta) / 400;

            int year1 = year + year_delta;
            int day_from_year = 365 * year1 +
                                year1 / 4 -
                                year1 / 100 +
                                year1 / 400 -
                                base_day;

            if ((year % 4 != 0) || (year % 100 == 0 && year % 400 != 0))
            {
                return day_from_year + DAY_FROM_MONTH[month] + day - 1;
            }

            return day_from_year + DAY_FROM_MONTH_LEAP[month] + day - 1;
        }

        private long MakeDate(long day, long time)
        {
            return (day * MS_PER_DAY + time);
        }

        // ECMA-262 section 15.9.4.2
        private long parse(string inputString)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            DateTime date = new DateTime(0, DateTimeKind.Utc);

            if (DateTime.TryParse(inputString, culture, DateTimeStyles.None, out date))
            {
                return ToNumber(date);
            }
            else if (DateTime.TryParseExact(inputString, FORMAT, culture, DateTimeStyles.None, out date))
            {
                return ToNumber(date);
            }

            DateTime ld;
            if (DateTime.TryParseExact(inputString, DATEFORMAT, culture, DateTimeStyles.None, out ld))
            {
                date = date.AddTicks(ld.Ticks);
            }

            if (DateTime.TryParseExact(inputString, TIMEFORMAT, culture, DateTimeStyles.None, out ld))
            {
                date = date.AddTicks(ld.Ticks);
            }

            if (date.Ticks > 0)
            {
                return ToNumber(date);
            }
            else
            {
                return -1;
            }
        }

        // ECMA-262 section 15.9.4.4
        private long now()
        {
            return ToNumber(DateTime.Now.ToUniversalTime());
        }

        // ECMA 262 - 15.9.5.2
        private string toString(long time)
        {
            return new DateTime(JsToNetTime(time)).ToString(JSDate.FORMAT, CultureInfo.InvariantCulture);
        }

        // ECMA 262 - 15.9.5.3
        private string toDateString(long time)
        {
            return new DateTime(JsToNetTime(time)).ToString(JSDate.DATEFORMAT, CultureInfo.InvariantCulture);
        }


        // ECMA 262 - 15.9.5.4
        private string toTimeString(long time)
        {

            return new DateTime(JsToNetTime(time)).ToString(JSDate.TIMEFORMAT, CultureInfo.InvariantCulture);
        }

        // ECMA 262 - 15.9.5.5
        private string toLocaleString(long time)
        {
            return new DateTime(JsToNetTime(time)).ToLocalTime().ToString(JSDate.FORMAT);
        }

        // ECMA 262 - 15.9.5.6
        private string toLocaleDateString(long time)
        {
            return new DateTime(JsToNetTime(time)).ToLocalTime().ToString(JSDate.DATEFORMAT);
        }

        // ECMA 262 - 15.9.5.7
        private string toLocaleTimeString(long time)
        {
            return new DateTime(JsToNetTime(time)).ToLocalTime().ToString(JSDate.TIMEFORMAT);
        }

        // ECMA 262 - 15.9.5.8
        private long valueOf(long time)
        {
            return time;
        }

        // ECMA 262 - 15.9.5.9
        private long getTime(long time)
        {
            return (new DateTime(JsToNetTime(time)).TimeOfDay.Ticks / JSDate.TICKSFACTOR);
        }

        // ECMA 262 - 15.9.5.10
        private long getFullYear(long time)
        {
            return new DateTime(JsToNetTime(time)).ToLocalTime().Year;
        }

        // ECMA 262 - B.2.4
        private long getYear(long time)
        {
            return getFullYear(time) - 1900;
        }

        // ECMA 262 - 15.9.5.11
        private long getUTCFullYear(long time)
        {
            return new DateTime(JsToNetTime(time)).Year;
        }

        // ECMA 262 - 15.9.5.12
        private long getMonth(long time)
        {
            return (new DateTime(JsToNetTime(time)).ToLocalTime().Month) - 1;
        }

        // ECMA 262 - 15.9.5.13
        private long getUTCMonth(long time)
        {
            return (new DateTime(JsToNetTime(time)).Month) - 1;
        }

        // ECMA 262 - 15.9.5.14
        private long getDate(long time)
        {
            return new DateTime(JsToNetTime(time)).ToLocalTime().Day;
        }

        // ECMA 262 - 15.9.5.15
        private long getUTCDate(long time)
        {
            return new DateTime(JsToNetTime(time)).Day;
        }

        // ECMA 262 - 15.9.5.16
        private long getDay(long time)
        {
            return (int)new DateTime(JsToNetTime(time)).ToLocalTime().DayOfWeek;
        }

        // ECMA 262 - 15.9.5.17
        private long getUTCDay(long time)
        {
            return (long)new DateTime(JsToNetTime(time)).DayOfWeek;
        }

        // ECMA 262 - 15.9.5.18
        private long getHours(long time)
        {
            return (int)new DateTime(JsToNetTime(time)).ToLocalTime().Hour;
        }

        // ECMA 262 - 15.9.5.19
        private long getUTCHours(long time)
        {
            return (long)new DateTime(JsToNetTime(time)).Hour;
        }

        // ECMA 262 - 15.9.5.20
        private long getMinutes(long time)
        {
            return (int)new DateTime(JsToNetTime(time)).ToLocalTime().Minute;
        }

        // ECMA 262 - 15.9.5.21
        private long getUTCMinutes(long time)
        {
            return (long)new DateTime(JsToNetTime(time)).Minute;
        }

        // ECMA 262 - 15.9.5.22
        private long getSeconds(long time)
        {
            return (int)new DateTime(JsToNetTime(time)).ToLocalTime().Second;
        }

        // ECMA 262 - 15.9.5.23
        private long getUTCSeconds(long time)
        {
            return (long)new DateTime(JsToNetTime(time)).Second;
        }

        // ECMA 262 - 15.9.5.24
        private long getMilliseconds(long time)
        {
            return (int)new DateTime(JsToNetTime(time)).ToLocalTime().Millisecond;
        }

        // ECMA 262 - 15.9.5.25
        private long getUTCMilliseconds(long time)
        {
            return (long)new DateTime(JsToNetTime(time)).Millisecond;
        }

        // ECMA 262 - 15.9.5.26
        private long getTimezoneOffset(long time)
        {
            return (long)(-TimeZone.CurrentTimeZone.GetUtcOffset(new DateTime()).TotalMinutes);
        }

        // ECMA 262 - 15.9.3
        private double constructDate(ref mdr.CallFrame callframe)
        {
            double date = 0;

            if (callframe.PassedArgsCount == 0)
                date = (double)now();
            else if (callframe.PassedArgsCount == 1)
            {
                if (callframe.Arg0.ValueType == mdr.ValueTypes.String)
                {
                    long time = parse(callframe.Arg0.AsString());
                    date = time > 0 ? time : double.NaN;
                }
                else if (callframe.Arg0.ValueType == mdr.ValueTypes.Int32 || callframe.Arg0.ValueType == mdr.ValueTypes.Double)
                {
                    date = TimeClip(callframe.Arg0.AsDouble());
                }
            }
            else // more than one argument
            {
                DateTime d = new DateTime(0);

                if (callframe.PassedArgsCount > 0)
                {
                    int year = (int)callframe.Arg0.AsInt32() - 1;
                    if (year < 100)
                    {
                        year += 1900;
                    }

                    d = d.AddYears(year);
                }

                if (callframe.PassedArgsCount > 1)
                {
                    d = d.AddMonths((int)callframe.Arg1.AsInt32());
                }

                if (callframe.PassedArgsCount > 2)
                {
                    d = d.AddDays((int)callframe.Arg2.AsInt32() - 1);
                }

                if (callframe.PassedArgsCount > 3)
                {
                    d = d.AddHours((int)callframe.Arg3.AsInt32());
                }

                if (callframe.PassedArgsCount > 4)
                {
                    d = d.AddMinutes((int)callframe.Arg(4).AsInt32());
                }

                if (callframe.PassedArgsCount > 5)
                {
                    d = d.AddSeconds((int)callframe.Arg(5).AsInt32());
                }

                if (callframe.PassedArgsCount > 6)
                {
                    d = d.AddMilliseconds((int)callframe.Arg(6).AsInt32());
                }

                date = ToNumber(d);
            }

            return date;
        }
    }
}
