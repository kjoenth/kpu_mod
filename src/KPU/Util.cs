﻿using System;
using System.Collections.Generic;

namespace KPU
{
    public static class Util
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
        }
        public static string formatSI(double value, string unit)
        {
            Dictionary<int,string> prefixes = new Dictionary<int,string>(){
                {0, ""},
                {1, "k"},
                {2, "M"},
                {3, "G"},
                {4, "T"},
                {-1, "m"},
                {-2, "µ"},
                {-3, "n"},
                {-4, "p"},
            };
            int offset = (int)Math.Floor(Math.Log10(Math.Abs(value)) / 3);
            offset = Clamp(offset, -4, 4);
            value /= Math.Pow(1000, offset);
            return value.ToString("F") + prefixes[offset] + unit;
        }
    }
    public static class Logging
    {
        public static void Message(string message, bool log=true)
        {
            if (log) Log(message, false);
            try
            {
                var msg = new ScreenMessage(message, 4f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(msg);
            }
            catch (Exception)
            {
            }
        }
        public static void Log(string message, bool msg=true)
        {
            UnityEngine.Debug.Log("KPU: " + (msg ? "" : "Log: ") + message);
            #if DEBUG
            if(msg) Message("KPU: debug: " + message, false);
            #endif
        }
    }
}

