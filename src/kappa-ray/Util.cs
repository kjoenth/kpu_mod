﻿using System;
using System.Collections.Generic;

namespace kapparay
{
    public static class Util
    {
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            return (value.CompareTo(min) < 0) ? min : (value.CompareTo(max) > 0) ? max : value;
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
        public static void Exception(Exception exc)
        {
            Log(exc.Message + '\n' + exc.StackTrace.ToString(), true);
        }

        public static void Log(string message, bool msg=true)
        {
            string ut = "";
            try
            {
                int t = (int)Planetarium.GetUniversalTime();
                ut = KSPUtil.PrintDate(t, true, true);
            }
            catch(NullReferenceException)
            {
            }
            UnityEngine.Debug.Log("kapparay: [" + ut + "] " + (msg ? "" : "Log: ") + message);
            #if DEBUG
            if(msg) Message("kapparay: debug: " + message, false);
            #endif
        }
    }
}

