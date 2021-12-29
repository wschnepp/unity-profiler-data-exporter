using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor.Profiling;
using UnityEditorInternal;
#if !UNITY_2019_1_OR_NEWER
using UnityEditorInternal.Profiling;
#endif
using UnityEngine;
using static UnityEditor.Profiling.FrameDataView;

namespace ProfilerDataExporter
{
    [Serializable]
    public class ProfilerData
    {
        public List<FrameData> frames = new List<FrameData>(300);

        public string format;

        private string ToCsv()
        {

            StringBuilder builder = new StringBuilder(1024*1024);
            builder.Append("Frame;CPU;GPU; PluginRenderTime\n");
            for (int fid = 0; fid < frames.Count; fid++)
            {
                var frame = frames[fid];

                builder.Append($"{fid};{frame.frameTimeCPU};{frame.frameTimeGPU};\n");
            }
            return builder.ToString();
        }

        public override string ToString()
        {
            if (format == "csv")
            {
                return ToCsv();
            }
            else
            {
                return JsonUtility.ToJson(this);
            }
        }

        private static IAllocator<ProfilerData> profilerDataAllocator = new ObjectPool<ProfilerData>(new BaseFactory<ProfilerData>(), 1);

        public static ProfilerData GetProfilerData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "")
        {
            {
                var profilerData = profilerDataAllocator.Allocate();
                for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
                {
                    RawFrameDataView view = ProfilerDriver.GetRawFrameDataView(frameIndex, 1);
                    var sampleCount = view.sampleCount;

                    var frameData = FrameData.Create();

                    frameData.frameTimeCPU = view.frameTimeMs;
                    frameData.frameTimeGPU = view.frameGpuTimeMs;
                    view.Dispose();

                    profilerData.frames.Add(frameData);
                }
                return profilerData;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < frames.Count; ++i)
            {
                frames[i].Clear();
            }
            frames.Clear();
            profilerDataAllocator.Free(this);
        }
    }

    [Serializable]
    public class FrameData
    {
        public List<FunctionData> functions = new List<FunctionData>(50);
        public float frameTimeCPU;
        public float frameTimeGPU;

        private static IAllocator<FrameData> frameDataAllocator = new ObjectPool<FrameData>(new BaseFactory<FrameData>(), 300);

        public static FrameData Create()
        {
            return frameDataAllocator.Allocate();
        }

        public void Clear()
        {
            for (int i = 0; i < functions.Count; ++i)
            {
                functions[i].Clear();
            }
            functions.Clear();
            frameDataAllocator.Free(this);
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }

    [Serializable]
    public class FunctionData
    {
        private static readonly string[] columnNames = Enum.GetNames(typeof(ProfilerColumn));
        private static readonly ProfilerColumn[] columns = (ProfilerColumn[])Enum.GetValues(typeof(ProfilerColumn));

        private static IAllocator<FunctionData> functionDataAllocator = new ObjectPool<FunctionData>(new BaseFactory<FunctionData>(), 300 * 50);

        public string functionPath;
        public FunctionDataValue[] values = new FunctionDataValue[columnNames.Length];

        public string GetValue(ProfilerColumn column)
        {
            var columnName = columnNames[(int)column];
            return FindDataValue(columnName).value;
        }

        private FunctionDataValue FindDataValue(string columnName)
        {
            int length = values.Length;
            for (int i = 0; i < length; ++i)
            {
                var value = values[i];
                if (value.column == columnName)
                {
                    return value;
                }
            }
            return default(FunctionDataValue);
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }

        public void Clear()
        {
            for (int i = 0; i < values.Length; ++i)
            {
                var functionDataValue = values[i];
                if (functionDataValue != null)
                {
                    functionDataValue.Clear();
                }
            }
            functionPath = string.Empty;
            functionDataAllocator.Free(this);
        }

        public static FunctionData Create(ProfilerProperty property)
        {
            var functionData = functionDataAllocator.Allocate();
            functionData.functionPath = property.propertyPath;
            for (int i = 0; i < columns.Length; ++i)
            {
                var column = columns[i];
#if UNITY_5_5_OR_NEWER
                if (column == ProfilerColumn.DontSort)
                {
                    continue;
                }
#endif
                var functionDataValue = FunctionDataValue.Create();
                functionDataValue.column = columnNames[i];
#if UNITY_2019_1_OR_NEWER
                functionDataValue.value = property.GetColumn((int)column);
#else
                functionDataValue.value = property.GetColumn(column);
#endif

                functionData.values[i] = functionDataValue;
            }
            return functionData;
        }
    }

    [Serializable]
    public class FunctionDataValue
    {
        public string column;
        public string value;

        private static IAllocator<FunctionDataValue> functionDataValueAllocator = new ObjectPool<FunctionDataValue>(new BaseFactory<FunctionDataValue>(), 300 * 50 * Enum.GetValues(typeof(ProfilerColumn)).Length);

        public static FunctionDataValue Create()
        {
            return functionDataValueAllocator.Allocate();
        }

        public void Clear()
        {
            functionDataValueAllocator.Free(this);
            column = string.Empty;
            value = string.Empty;
        }
    }
}