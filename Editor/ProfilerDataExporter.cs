using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
#if !UNITY_2019_1_OR_NEWER
using UnityEditorInternal.Profiling;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace ProfilerDataExporter
{
#if UNITY_2019_1_OR_NEWER
    public enum ProfilerColumn
    {
        FunctionName = 0,
        TotalPercent = 1,
        SelfPercent = 2,
        Calls = 3,
        GCMemory = 4,
        TotalTime = 5,
        SelfTime = 6,
        DontSort = -1,
    }

    public enum ProfilerViewType
    {
        Hierarchy = 0
    }
#endif

    public enum SortType
    {
        FunctionName,
        TotalPercent,
        SelfPercent,
        Calls,
        GCMemory,
        TotalTime,
        SelfTime,
    }

    public class ProfilerDataExporter : EditorWindow
    {
        private static readonly ProfilerColumn[] ColumnsToShow = new ProfilerColumn[]
        {
            ProfilerColumn.FunctionName,
            ProfilerColumn.TotalPercent,
            ProfilerColumn.SelfPercent,
            ProfilerColumn.Calls,
            ProfilerColumn.GCMemory,
            ProfilerColumn.TotalTime,
            ProfilerColumn.SelfTime,
        };

        private static readonly Dictionary<ProfilerColumn, string> ColumnHeaders = new Dictionary<ProfilerColumn, string>
        {
            { ProfilerColumn.FunctionName, "Function"},
            { ProfilerColumn.TotalPercent, "Total"},
            { ProfilerColumn.SelfPercent, "Self"},
            { ProfilerColumn.Calls, "Calls"},
            { ProfilerColumn.GCMemory, "GC Alloc"},
            { ProfilerColumn.TotalTime, "Time ms"},
            { ProfilerColumn.SelfTime, "Self ms"},
        };

        private static readonly Dictionary<SortType, ProfilerColumn> SortTypeToProfilerColum = new Dictionary<SortType, ProfilerColumn>
        {
            { SortType.FunctionName, ProfilerColumn.FunctionName},
            { SortType.TotalPercent, ProfilerColumn.TotalPercent},
            { SortType.SelfPercent, ProfilerColumn.SelfPercent},
            { SortType.Calls, ProfilerColumn.Calls},
            { SortType.GCMemory, ProfilerColumn.GCMemory},
            { SortType.TotalTime, ProfilerColumn.TotalTime},
            { SortType.SelfTime, ProfilerColumn.SelfTime},
        };

        private string filePath = @"profiler_data.json";
        private Type profilerWindowType;
        private FieldInfo currentFrameFieldInfo;
        private Vector2 scrollPosition;
        private IAllocator<List<string>> listPool = new ListPool<string>(new ListFactory<string>(ColumnsToShow.Length), 50);
        private List<List<string>> functionStats = new List<List<string>>(50);
        private TableGUILayout.ITableState functionStatsTableState;
        private StatsType statsType;
        private SortType sortType = SortType.SelfTime;

        private string format = "json";
        private EditorWindow profilerWindow;

        [MenuItem("Window/Profiler Data Exporter")]
        private static void Init()
        {
            var window = GetWindow<ProfilerDataExporter>("Profiler Data Exporter");
            window.Show();
        }

        private void OnEnable()
        {
            Assembly assem = typeof(Editor).Assembly;
            profilerWindowType = assem.GetType("UnityEditor.ProfilerWindow");
            currentFrameFieldInfo = profilerWindowType.GetField("m_CurrentFrame", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private void OnGUI()
        {
            if (!profilerWindow)
            {
                profilerWindow = GetWindow(profilerWindowType);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            //DrawStats();
            DrawFormatSelection();
            DrawFilePath();
            DrawExportButtons();

            EditorGUILayout.EndScrollView();
        }

        private void DrawStats()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Statistics", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            statsType = (StatsType)EditorGUILayout.EnumPopup(statsType, GUILayout.ExpandWidth(false));
            var calulateStatistics = GUILayout.Button("Calculate", GUILayout.ExpandWidth(false));

            GUILayout.Label("Sort By", EditorStyles.boldLabel, GUILayout.ExpandWidth(false));
            var oldSortType = sortType;
            sortType = (SortType)EditorGUILayout.EnumPopup(sortType, GUILayout.ExpandWidth(false));
            bool sortTypeChanged = oldSortType != sortType;

            GUILayout.EndHorizontal();

            if (calulateStatistics || sortTypeChanged)
            {
                //using (Profiler.AddSample(Profiler.SamplerType.CalculateStatsTotal))
                {
                    var statsCalculator = StatsCalculatorProvider.GetStatsCalculator(statsType);
                    var sortColumn = SortTypeToProfilerColum[sortType];
                    var stats = statsCalculator.CalculateStats(ColumnsToShow, sortColumn);
                    UpdateFunctionStats(stats);
                }
            }

            if (functionStatsTableState == null)
            {
                var sortHeader = ColumnHeaders[SortTypeToProfilerColum[sortType]];
                functionStatsTableState = new FunctionTableState(ColumnsToShow, ColumnHeaders, sortHeader);
            }

            if (sortTypeChanged)
            {
                functionStatsTableState.SortHeader = ColumnHeaders[SortTypeToProfilerColum[sortType]];
            }

            TableGUILayout.BeginTable(functionStatsTableState, GUI.skin.GetStyle("OL Box"), GUILayout.MinHeight(100f), GUILayout.MaxHeight(500f));
            for (var i = 0; i < functionStats.Count; ++i)
            {
                var functionData = functionStats[i];
                TableGUILayout.AddRow(functionStatsTableState, i, functionData);
            }
            TableGUILayout.EndTable();
        }

        private void UpdateFunctionStats(IList<FunctionData> stats)
        {
            //using (Profiler.AddSample(Profiler.SamplerType.UpdateFunctionStats))
            {
                listPool.Free(functionStats);
                functionStats.Clear();

                var statsLength = stats.Count;
                var columnsToShowLength = ColumnsToShow.Length;
                for (int i = 0; i < statsLength; ++i)
                {
                    var stat = stats[i];
                    var functionStat = listPool.Allocate();
                    for (int j = 0; j < columnsToShowLength; ++j)
                    {
                        var column = ColumnsToShow[j];
                        var value = stat.GetValue(column);
                        functionStat.Add(value);
                    }
                    functionStats.Add(functionStat);
                }
            }
        }
        private void DrawFormatSelection()
        {
            GUILayout.Label("Format", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();

            string[] formats = new string[] { "json", "csv" };
            int previousSelectedFormatIndex = format == "json" ? 0 : 1;
            int newSelectedFormatIndex = EditorGUILayout.Popup("File format", previousSelectedFormatIndex, formats);
            
            if(previousSelectedFormatIndex != newSelectedFormatIndex)
            {
                format = formats[newSelectedFormatIndex];
                filePath = $"{ filePath.Substring(0, filePath.LastIndexOf("."))}.{ format}";

            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void DrawExportButtons()
        {
            GUILayout.Label("Export", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Profiler Data"))
            {
                if (FileOverwriteCheck())
                {
                    ExportProfilerData();
                }
            }
            var currentframe = (int)currentFrameFieldInfo.GetValue(profilerWindow);
            GUI.enabled = currentframe > 0;
            if (GUILayout.Button("Current Frame Data"))
            {
                if (FileOverwriteCheck())
                {
                    ExportCurrentFrameData(currentframe);
                }                
            }
            GUI.enabled = true;
            GUI.enabled = !string.IsNullOrEmpty(ProfilerDriver.selectedPropertyPath);
            if (GUILayout.Button("Selected Function Data"))
            {
                if (FileOverwriteCheck())
                {
                    ExportSelectedFunctionData();
                }               
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private bool FileOverwriteCheck()
        {
            return !File.Exists(filePath) || EditorUtility.DisplayDialog("File already exists", "Do you wish to overwrite it?", "Yes", "No")
        }

        private void DrawFilePath()
        {
            GUILayout.Label("File path", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            var currentDirectory = Directory.GetCurrentDirectory();
            var displayedFilePath = Path.GetFullPath(filePath).Replace(currentDirectory, "");
            GUILayout.Label(displayedFilePath, GUILayout.ExpandWidth(false));
            if (GUILayout.Button("Open", GUILayout.ExpandWidth(false)) && File.Exists(filePath))
            {
                Process.Start(filePath);
            }
            if (GUILayout.Button("Edit", GUILayout.ExpandWidth(false)))
            {
                var currentPathDirectory = Path.GetDirectoryName(filePath);
                var currentName = Path.GetFileNameWithoutExtension(filePath);
                var newfilePath = EditorUtility.SaveFilePanel("Select file path", currentPathDirectory, currentName, format);
                var isPathValid = !string.IsNullOrEmpty(newfilePath);
                if (isPathValid)
                {
                    filePath = newfilePath;
                }
            }

            GUILayout.EndHorizontal();
        }

        private void ExportProfilerData()
        {
            
         
            var firstFrameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            ExtractData(firstFrameIndex, lastFrameIndex, format:format);
        }

        private void ExportCurrentFrameData(int currentframe)
        {
            if (currentframe > -1)
            {
                ExtractData(currentframe, currentframe);
            }
        }

        private void ExportSelectedFunctionData()
        {
            var firstFrameIndex = ProfilerDriver.firstFrameIndex;
            var lastFrameIndex = ProfilerDriver.lastFrameIndex;
            ExtractData(firstFrameIndex, lastFrameIndex, ProfilerDriver.selectedPropertyPath);
        }

        private void ExtractData(int firstFrameIndex, int lastFrameIndex, string selectedPropertyPath = "", string format="json")
        {
            var profilerData = ProfilerData.GetProfilerData(firstFrameIndex, lastFrameIndex, selectedPropertyPath);
            profilerData.format = format;
            File.WriteAllText(filePath, profilerData.ToString());
            profilerData.Clear();
        }
    }

}