﻿using System;
using System.Collections.Generic;
using ByteSizeLib;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;

namespace ProfilerDataExporter
{
    public abstract class StatsCalculatorBase
    {
        private static readonly ProfilerColumn[] ProfilerColumns = (ProfilerColumn[])Enum.GetValues(typeof(ProfilerColumn));
        private static readonly string[] ProfilerColumnNames = Enum.GetNames(typeof(ProfilerColumn));

        public static ProfilerColumn sSortCollum = ProfilerColumn.SelfTime;
        // return sort value according to column
        private static readonly Func<FunctionData, float> GetSortValue = f => {
            string str = f.GetValue(sSortCollum);

            // unify gc value
            int factor = 1;
            if (sSortCollum == ProfilerColumn.GCMemory)
            {
                int spaceIndex = str.IndexOf(' ');
                if (str[spaceIndex + 1] == 'K')
                    factor = 1024;
                else if (str[spaceIndex + 1] == 'M')
                    factor = 1024 * 1024;

                str = str.Substring(0, spaceIndex);
            }
            return float.Parse(str) * factor;
        };


        private static Func<FunctionData, float>[] getFunctionValues;

        private ProfilerColumn[] columnsToShow;

        private List<float> values = new List<float>();
        private Dictionary<string, List<FunctionData>> functionsDataByName = new Dictionary<string, List<FunctionData>>();

        protected StatsCalculatorBase()
        {
            if (getFunctionValues == null)
            {
                getFunctionValues = new Func<FunctionData, float>[ProfilerColumns.Length];
                for (var i = 0; i < ProfilerColumns.Length; ++i)
                {
                    var column = ProfilerColumns[i];
                    if (column != ProfilerColumn.GCMemory)
                    {
                        getFunctionValues[i] = f => float.Parse(f.GetValue(column).Replace("%", ""));
                    }
                    else
                    {
                        getFunctionValues[i] = f => (float)ByteSize.Parse(f.GetValue(column)).Bytes;
                    }
                }
            }
        }

        public IList<FunctionData> CalculateStats(ProfilerColumn[] columnsToShow)
        {
            //using (Profiler.AddSample(Profiler.SamplerType.CalculateStats))
            {
                var firstFrameIndex = ProfilerDriver.firstFrameIndex;
                var lastFrameIndex = ProfilerDriver.lastFrameIndex;
                var profilerData = ProfilerData.GetProfilerData(firstFrameIndex, lastFrameIndex);

                var frames = profilerData.frames;
                for (int i = 0; i < frames.Count; ++i)
                {
                    var frameData = frames[i];
                    var functions = frameData.functions;
                    for (int j = 0; j < functions.Count; ++j)
                    {
                        var functionData = functions[j];
                        var functionName = functionData.GetValue(ProfilerColumn.FunctionName);
                        List<FunctionData> functionsData;
                        if (!functionsDataByName.TryGetValue(functionName, out functionsData))
                        {
                            functionsData = new List<FunctionData>();
                            functionsDataByName.Add(functionName, functionsData);
                        }
                        functionsData.Add(functionData);
                    }
                }

                this.columnsToShow = columnsToShow;

                var functionStats = new List<FunctionData>(functionsDataByName.Count);
                foreach (var pair in functionsDataByName)
                {
                    var functionName = pair.Key;
                    var functionsData = pair.Value;
                    functionStats.Add(AggregateFunction(functionName, functionsData));
                }
                functionStats.Sort((x, y) => GetSortValue(y).CompareTo(GetSortValue(x)));

                functionsDataByName.Clear();
                profilerData.Clear();
                return functionStats;
            }
        }

        public static void SetSortType(SortType sortType)
        {
            switch (sortType)
            {
                case SortType.TotalPercent:
                    sSortCollum = ProfilerColumn.TotalPercent;
                    break;
                case SortType.SelfPercent:
                    sSortCollum = ProfilerColumn.SelfPercent;
                    break;
                case SortType.Calls:
                    sSortCollum = ProfilerColumn.Calls;
                    break;
                case SortType.GCMemory:
                    sSortCollum = ProfilerColumn.GCMemory;
                    break;
                case SortType.TotalTime:
                    sSortCollum = ProfilerColumn.TotalTime;
                    break;
                case SortType.SelfTime:
                    sSortCollum = ProfilerColumn.SelfTime;
                    break;
            }
        }

        private FunctionData AggregateFunction(string functionName, IList<FunctionData> functionsData)
        {
            var function = new FunctionData { values = new FunctionDataValue[columnsToShow.Length] };
            function.values[0] = new FunctionDataValue
            {
                column = "FunctionName",
                value = functionName
            };
            for (var i = 1; i < columnsToShow.Length; ++i)
            {
                var column = columnsToShow[i];
                var functionDataValue = GetValue(functionsData, column);
                function.values[i] = functionDataValue;
            }
            return function;
        }

        private FunctionDataValue GetValue(IList<FunctionData> framesData, ProfilerColumn column)
        {
            var functionDataValue = new FunctionDataValue { column = ProfilerColumnNames[(int)column] };
            var getFunctionValue = getFunctionValues[(int)column];

            for (int i = 0; i < framesData.Count; ++i)
            {
                values.Add(getFunctionValue(framesData[i]));
            }

            if (column != ProfilerColumn.GCMemory)
            {
                functionDataValue.value = AggregateValues(values).ToString("F2");
            }
            else
            {
                functionDataValue.value = ByteSize.FromBytes(AggregateValues(values)).ToString();
            }

            values.Clear();
            return functionDataValue;
        }

        protected abstract float AggregateValues(IList<float> values);
    }
}