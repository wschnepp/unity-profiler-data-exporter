﻿using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

//NOTICE: File has been modified by Wilhelm Schnepp (2021-12-29)

namespace ProfilerDataExporter
{
    /// <summary>
    /// Wrapper for unity internal SplitterState class
    /// </summary>
    public class SplitterState
    {
        private static readonly Type SplitterStateType = typeof(Editor).Assembly.GetType("UnityEditor.SplitterState");
        public object splitter = null;
        public float[] realSizes;

        private static readonly FieldInfo RealSizesInfo = SplitterStateType.GetField(
            "realSizes",
            BindingFlags.DeclaredOnly |
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance |
            BindingFlags.GetField);

        public SplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes)
        {
            splitter = SplitterStateType.InvokeMember(null,
            BindingFlags.DeclaredOnly |
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Instance | BindingFlags.CreateInstance, null, null, new object[] { relativeSizes, minSizes, maxSizes });
            object t = RealSizesInfo.GetValue(splitter); ;
            realSizes = (float[])t;
        }

        public SplitterState(object splitter)
        {
            this.splitter = splitter;
            realSizes = (float[])RealSizesInfo.GetValue(splitter);
        }

        public int ID
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("ID",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("ID",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public float xOffset
        {
            get
            {
                return (float)SplitterStateType.InvokeMember("xOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public float splitSize
        {
            get
            {
                var f = SplitterStateType.InvokeMember("splitSize",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
                return (float)f;
            }
        }
        public float[] relativeSizes
        {
            get
            {
                return (float[])SplitterStateType.InvokeMember("relativeSizes",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
        }
        public int splitterInitialOffset
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("splitterInitialOffset",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("splitterInitialOffset",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }
        public int currentActiveSplitter
        {
            get
            {
                return (int)SplitterStateType.InvokeMember("currentActiveSplitter",
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.GetField, null, splitter, null);
            }
            internal set
            {
                SplitterStateType.InvokeMember("currentActiveSplitter",
                     BindingFlags.DeclaredOnly |
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.SetField, null, splitter, new object[] { value });
            }
        }

        public void RealToRelativeSizes()
        {
            SplitterStateType.InvokeMember("RealToRelativeSizes",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, null);
        }

        public void DoSplitter(int currentActiveSplitter, int v, int num3)
        {
            SplitterStateType.InvokeMember("DoSplitter",
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.InvokeMethod, null, splitter, new object[] { currentActiveSplitter, v, num3 });
        }
    }
}
