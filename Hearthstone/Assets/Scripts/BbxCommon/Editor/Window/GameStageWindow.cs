#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;
using UnityEngine;

namespace BbxCommon.Editor
{
    public class GameStageWindow : EditorWindow
    {
        private enum EView
        {
            Runtime,
            Entries,
        }

        private const float DefaultEntryListWidth = 250f;
        private const float MinEntryListWidth = 140f;
        private const float MinEntryInspectorWidth = 260f;
        private const float EntryDividerWidth = 5f;
        private const int EntryDividerControlHint = 0x4B4A13;
        private const string SelectedEntryPathKey = "BbxCommon.GameStageWindow.SelectedEntryPath";

        internal static IGameEngine CurGameEngine;

        private readonly Dictionary<string, bool> m_FoldoutDic = new();
        private readonly List<GameStageEntryAsset> m_Entries = new();

        private EView m_View;
        private GameStageEntryAsset m_SelectedEntry;
#if ODIN_INSPECTOR
        private PropertyTree m_SelectedEntryPropertyTree;
#else
        private UnityEditor.Editor m_SelectedEntryEditor;
#endif
        private bool m_ResetInspectorKeyboardFocus;
        private float m_EntryDividerDragOffset;
        [SerializeField]
        private float m_EntryListWidth = DefaultEntryListWidth;
        private Vector2 m_EntryListScrollPos;
        private Vector2 m_InspectorScrollPos;
        private Vector2 m_RuntimeScrollPos;

        [MenuItem("BbxCommon/GameStageWindow")]
        private static void Open()
        {
            var projectBrowserType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ProjectBrowser");
            var window = projectBrowserType == null
                ? GetWindow<GameStageWindow>("Game Stage")
                : GetWindow<GameStageWindow>("Game Stage", true, projectBrowserType);
            window.titleContent = new GUIContent("Game Stage");
            window.minSize = new Vector2(520f, 220f);
            window.m_View = EView.Runtime;
            window.Show();
            window.Focus();
        }

        private void OnEnable()
        {
            RefreshEntries();
        }

        private void OnDisable()
        {
            ResetSelectedEntryInspector();
        }

        private void OnProjectChange()
        {
            RefreshEntries();
            Repaint();
        }

        private void Update()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        private void OnGUI()
        {
            if (m_ResetInspectorKeyboardFocus)
            {
                EditorGUIUtility.editingTextField = false;
                GUI.FocusControl(null);
                GUIUtility.keyboardControl = 0;
                m_ResetInspectorKeyboardFocus = false;
            }

            DrawToolbar();
            switch (m_View)
            {
                case EView.Entries:
                    DrawEntries();
                    break;
                case EView.Runtime:
                    DrawRuntime();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                m_View = (EView)GUILayout.Toolbar((int)m_View, new[] { "Runtime", "Entries" }, EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                if (m_View == EView.Entries && GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
                    RefreshEntries();
            }
        }

        private void DrawEntries()
        {
            m_EntryListWidth = ClampEntryListWidth(m_EntryListWidth);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawEntryList();
                DrawEntryDivider();
                DrawSelectedEntryInspector();
            }
        }

        private void DrawEntryList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(m_EntryListWidth), GUILayout.ExpandHeight(true)))
            {
                m_EntryListScrollPos = EditorGUILayout.BeginScrollView(m_EntryListScrollPos);
                if (m_Entries.Count == 0)
                {
                    EditorGUILayout.HelpBox("No Stage entry assets were found.", MessageType.Info);
                }
                else
                {
                    foreach (var entry in m_Entries)
                    {
                        var selected = ReferenceEquals(entry, m_SelectedEntry);
                        var style = selected ? EditorStyles.selectionRect : EditorStyles.label;
                        if (GUILayout.Button(entry.DisplayName, style, GUILayout.Height(22f)))
                        {
                            EditorGUIUtility.editingTextField = false;
                            GUI.FocusControl(null);
                            GUIUtility.keyboardControl = 0;
                            SelectEntry(entry);
                            Selection.activeObject = entry;
                            GUIUtility.ExitGUI();
                        }
                    }
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Create Entry", GUILayout.Height(26f)))
                    ShowCreateEntryMenu();
            }
        }

        private void DrawEntryDivider()
        {
            var dividerRect = GUILayoutUtility.GetRect(
                EntryDividerWidth,
                EntryDividerWidth,
                GUILayout.Width(EntryDividerWidth),
                GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(dividerRect, MouseCursor.ResizeHorizontal);

            var controlId = GUIUtility.GetControlID(
                EntryDividerControlHint,
                FocusType.Passive,
                dividerRect);
            var currentEvent = Event.current;
            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && dividerRect.Contains(currentEvent.mousePosition))
                    {
                        m_EntryDividerDragOffset = currentEvent.mousePosition.x - m_EntryListWidth;
                        GUIUtility.hotControl = controlId;
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        m_EntryListWidth = ClampEntryListWidth(
                            currentEvent.mousePosition.x - m_EntryDividerDragOffset);
                        Repaint();
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;
            }

            var lineRect = dividerRect;
            lineRect.x += Mathf.Floor((EntryDividerWidth - 1f) * 0.5f);
            lineRect.width = 1f;
            EditorGUI.DrawRect(
                lineRect,
                EditorGUIUtility.isProSkin
                    ? new Color(0.12f, 0.12f, 0.12f)
                    : new Color(0.45f, 0.45f, 0.45f));
        }

        private float ClampEntryListWidth(float width)
        {
            var maxWidth = Mathf.Max(
                MinEntryListWidth,
                position.width - MinEntryInspectorWidth - EntryDividerWidth);
            return Mathf.Clamp(width, MinEntryListWidth, maxWidth);
        }

        private void DrawSelectedEntryInspector()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                if (m_SelectedEntry == null)
                {
                    EditorGUILayout.HelpBox("Select or create a Stage entry to edit it.", MessageType.Info);
                    return;
                }

                var path = AssetDatabase.GetAssetPath(m_SelectedEntry);
                DrawEntryHeader(path);

                EditorGUILayout.Space(4f);
                m_InspectorScrollPos = EditorGUILayout.BeginScrollView(m_InspectorScrollPos);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
                {
                    var previousDisplayName = m_SelectedEntry.DisplayName;
#if ODIN_INSPECTOR
                    EnsureSelectedEntryPropertyTree();
                    if (m_SelectedEntryPropertyTree != null)
                        GameStageEntryInspectorDrawer.Draw(m_SelectedEntryPropertyTree);
#else
                    EnsureSelectedEntryEditor();
                    m_SelectedEntryEditor?.OnInspectorGUI();
#endif
                    if (!string.Equals(previousDisplayName, m_SelectedEntry.DisplayName, StringComparison.Ordinal))
                    {
                        SortEntries();
                        Repaint();
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawEntryHeader(string path)
        {
            var isValid = m_SelectedEntry.ValidateEntry(out var error);
            if (!GameStageEntryLauncher.IsEntryAssetPath(path))
            {
                isValid = false;
                error = $"Entry asset must be stored under {GameStageEntryAsset.AssetFolder}.";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(m_SelectedEntry.DisplayName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || !isValid))
                {
                    if (GUILayout.Button("Play", GUILayout.Width(72f), GUILayout.Height(22f)))
                    {
                        try
                        {
                            GameStageEntryLauncher.Start(m_SelectedEntry);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogException(exception);
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(
                           EditorApplication.isPlaying ||
                           !GameStageEntryLauncher.IsEntryAssetPath(path)))
                {
                    if (GUILayout.Button("Delete", GUILayout.Width(72f), GUILayout.Height(22f)) &&
                        TryDeleteSelectedEntry(path))
                    {
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (!isValid)
                EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        private bool TryDeleteSelectedEntry(string path)
        {
            var entry = m_SelectedEntry;
            if (entry == null || !GameStageEntryLauncher.IsEntryAssetPath(path))
                return false;

            var confirmed = EditorUtility.DisplayDialog(
                "Delete Stage Entry",
                $"Delete Stage entry '{entry.DisplayName}'?\n\n{path}\n\nThis action cannot be undone.",
                "Delete",
                "Cancel");
            if (!confirmed)
                return false;

            if (ReferenceEquals(Selection.activeObject, entry))
                Selection.activeObject = null;

            ResetSelectedEntryInspector();
            m_SelectedEntry = null;
            SessionState.EraseString(SelectedEntryPathKey);

            var deleted = AssetDatabase.DeleteAsset(path);
            RefreshEntries();
            if (m_SelectedEntry != null)
                Selection.activeObject = m_SelectedEntry;
            Repaint();

            if (deleted)
                return true;

            EditorUtility.DisplayDialog(
                "Delete Stage Entry Failed",
                $"Unity could not delete the Stage entry asset at:\n\n{path}",
                "OK");
            return false;
        }

        private void RefreshEntries()
        {
            var selectedPath = m_SelectedEntry == null
                ? SessionState.GetString(SelectedEntryPathKey, string.Empty)
                : AssetDatabase.GetAssetPath(m_SelectedEntry);

            m_Entries.Clear();
            if (AssetDatabase.IsValidFolder(GameStageEntryAsset.AssetFolder))
            {
                var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { GameStageEntryAsset.AssetFolder });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(path);
                    if (entry != null)
                        m_Entries.Add(entry);
                }
            }

            SortEntries();

            var selection = m_Entries.FirstOrDefault(entry =>
                AssetDatabase.GetAssetPath(entry).Equals(selectedPath, StringComparison.OrdinalIgnoreCase));
            SelectEntry(selection ?? m_Entries.FirstOrDefault());
        }

        private void SortEntries()
        {
            m_Entries.Sort((left, right) =>
                string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
        }

        private void SelectEntry(GameStageEntryAsset entry)
        {
            if (ReferenceEquals(entry, m_SelectedEntry) && HasSelectedEntryInspector())
                return;

            ResetSelectedEntryInspector();
            m_SelectedEntry = entry;
            m_ResetInspectorKeyboardFocus = true;
            m_InspectorScrollPos = Vector2.zero;
            Repaint();

            if (entry == null)
            {
                SessionState.EraseString(SelectedEntryPathKey);
                return;
            }

            SessionState.SetString(SelectedEntryPathKey, AssetDatabase.GetAssetPath(entry));
        }

#if ODIN_INSPECTOR
        private void EnsureSelectedEntryPropertyTree()
        {
            if (m_SelectedEntry != null && m_SelectedEntryPropertyTree == null)
                m_SelectedEntryPropertyTree = PropertyTree.Create(new SerializedObject(m_SelectedEntry));
        }
#else
        private void EnsureSelectedEntryEditor()
        {
            if (m_SelectedEntryEditor != null && !ReferenceEquals(m_SelectedEntryEditor.target, m_SelectedEntry))
                ResetSelectedEntryInspector();

            if (m_SelectedEntry != null && m_SelectedEntryEditor == null)
                m_SelectedEntryEditor = UnityEditor.Editor.CreateEditor(m_SelectedEntry);
        }
#endif

        private bool HasSelectedEntryInspector()
        {
#if ODIN_INSPECTOR
            return m_SelectedEntryPropertyTree != null;
#else
            return m_SelectedEntryEditor != null;
#endif
        }

        private void ResetSelectedEntryInspector()
        {
#if ODIN_INSPECTOR
            m_SelectedEntryPropertyTree = null;
#else
            if (m_SelectedEntryEditor == null)
                return;

            DestroyImmediate(m_SelectedEntryEditor);
            m_SelectedEntryEditor = null;
#endif
        }

        private void ShowCreateEntryMenu()
        {
            var menu = new GenericMenu();
            var entryTypes = TypeCache.GetTypesDerivedFrom<GameStageEntryAsset>()
                .Where(type => !type.IsAbstract && !type.IsGenericType)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();

            if (entryTypes.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("No entry types found"));
            }
            else
            {
                foreach (var type in entryTypes)
                {
                    var capturedType = type;
                    var label = ObjectNames.NicifyVariableName(type.Name.Replace("Asset", string.Empty));
                    menu.AddItem(new GUIContent(label), false, () => CreateAndSelectEntryAsset(capturedType));
                }
            }

            menu.ShowAsContext();
        }

        private void CreateAndSelectEntryAsset(Type entryType)
        {
            var entry = CreateEntryAsset(entryType);
            var path = AssetDatabase.GetAssetPath(entry);
            RefreshEntries();
            SelectEntry(AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(path));
        }

        public static GameStageEntryAsset CreateEntryAsset(Type entryType)
        {
            EnsureAssetFolder();

            var entry = CreateInstance(entryType) as GameStageEntryAsset;
            if (entry == null)
                throw new InvalidOperationException($"Could not create GameStage entry type {entryType.FullName}.");

            entry.PrepareForEditorStorage();
            var fileName = entryType.Name.EndsWith("Asset", StringComparison.Ordinal)
                ? entryType.Name.Substring(0, entryType.Name.Length - "Asset".Length)
                : entryType.Name;
            var path = AssetDatabase.GenerateUniqueAssetPath(
                $"{GameStageEntryAsset.AssetFolder}/{fileName}.asset");
            AssetDatabase.CreateAsset(entry, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return entry;
        }

        internal static void EnsureAssetFolder()
        {
            const string resourcesFolder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesFolder))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(GameStageEntryAsset.AssetFolder))
                AssetDatabase.CreateFolder(resourcesFolder, "Editor");
        }

        private void DrawRuntime()
        {
            if (CurGameEngine == null)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to inspect active GameStages.", MessageType.Info);
                return;
            }

            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            m_RuntimeScrollPos = EditorGUILayout.BeginScrollView(m_RuntimeScrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField(CurGameEngine.GetType().Name, EditorStyles.boldLabel);

            foreach (var stage in CurGameEngine.GetEnabledGameStage())
            {
                m_FoldoutDic.GetOrAdd(stage.StageName, out var isFoldout);
                m_FoldoutDic[stage.StageName] = EditorGUILayout.Foldout(isFoldout, stage.StageName);
                if (!m_FoldoutDic[stage.StageName])
                    continue;

                DrawLoadItems(stage, loadingTimeData);
                DrawStageSystems(stage);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawLoadItems(GameStage stage, LoadingTimeData loadingTimeData)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20f);
                m_FoldoutDic.GetOrAdd(stage.StageName + "Load Items", out var isFoldout);
                m_FoldoutDic[stage.StageName + "Load Items"] = EditorGUILayout.Foldout(isFoldout, "Load Items");
            }

            if (!m_FoldoutDic[stage.StageName + "Load Items"] || loadingTimeData == null)
                return;

            foreach (var pair in loadingTimeData.GetStageItemDic(stage.StageName))
            {
                var parts = pair.Key.Split('.');
                var key = pair.Key.TryRemoveStart(parts[0] + ".");
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(40f);
                    GUILayout.Label(key, GUILayout.Width(350f));
                    GUILayout.Label($"{pair.Value / 1000000f}ms", GUILayout.Width(150f));
                }
            }
        }

        private void DrawStageSystems(GameStage stage)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(20f);
                m_FoldoutDic.GetOrAdd(stage.StageName + "System", out var isFoldout);
                m_FoldoutDic[stage.StageName + "System"] = EditorGUILayout.Foldout(isFoldout, "System");
            }

            if (!m_FoldoutDic[stage.StageName + "System"])
                return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(40f);
                GUILayout.Label("System", EditorStyles.miniBoldLabel, GUILayout.Width(260f));
                GUILayout.Label("Loop", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
                GUILayout.Label("Execution", EditorStyles.miniBoldLabel, GUILayout.Width(160f));
                GUILayout.Label("Last Time", EditorStyles.miniBoldLabel, GUILayout.Width(120f));
            }

            DrawSystems(stage.UpdateSystems, "Update");
            DrawSystems(stage.FixedUpdateSystems, "FixedUpdate");
        }

        private void DrawSystems(IReadOnlyList<EcsSystemBase> systems, string updateLoop)
        {
            foreach (var system in systems)
            {
                var isRegistered = CurGameEngine.TryGetSystemOrder(system.GetType(), out _);
                var hasExecutionOrder = CurGameEngine.TryGetSystemExecutionOrder(system, out var executionOrder);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(40f);
                    GUILayout.Label(system.GetType().Name, GUILayout.Width(260f));
                    GUILayout.Label(updateLoop, GUILayout.Width(90f));
                    GUILayout.Label(
                        hasExecutionOrder
                            ? $"#{executionOrder + 1}{(isRegistered ? string.Empty : " (unregistered)")}"
                            : "Not loaded",
                        GUILayout.Width(160f));
                    GUILayout.Label($"{system.LastUpdateTimeNs / 1000000f:F3} ms", GUILayout.Width(120f));
                }
            }
        }
    }
}
#endif
