using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using static Codice.CM.Common.CmCallContext;

public class DialogueGraphEditor : EditorWindow
{
    private MyScenes scenes;
    private int selectedSceneIndex = 0;
    private Vector2 scrollPosition;
    private Vector2 zoomOrigin = Vector2.zero;
    private float zoomScale = 1f;
    private double lastClickTime = 0;
    private const double doubleClickThreshold = 0.3;
    const float nodeWidth = 300;
    private DialogueNode editingNode = null;
    private DialogueLink editingLink = null;
    private Rect editingRect;
    private string editBuffer = "";
    private Dictionary<int, DialogueNode> nodeMap;
    private Dictionary<int, Vector2> nodePositions;
    private bool editingActive = false;
    private DialogueNode draggedNode = null;
    private Vector2 dragOffset;
    private string jsonPath = null;
    private string JsonPathKey = Application.dataPath + "/dialogue_graph.json";

    [MenuItem("Tools/PlotTalkAI/Dialogue Graph")]
    public static void OpenWindow()
    {
        Debug.Log(StorageApi.GetInstance().IsLoggedIn());
        var window = GetWindow<DialogueGraphEditor>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.Show();
    }

    private void OnEnable()
    {
        nodeMap = new Dictionary<int, DialogueNode>();
        nodePositions = new Dictionary<int, Vector2>();
        if (EditorPrefs.HasKey(JsonPathKey))
        {
            jsonPath = EditorPrefs.GetString(JsonPathKey);
            if (File.Exists(jsonPath))
            {
                string json = File.ReadAllText(jsonPath);
                var data = JsonUtility.FromJson<MyScenes>("{\"scene\":" + json + "}");
                if (data != null && data.scene != null)
                {
                    scenes = data;
                    return;
                }
            }
        }
        scenes = null;
        Debug.LogWarning("Граф не загружен. Загрузите JSON вручную.");
    }

    private void DrawJsonControls()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(30));
        if (GUILayout.Button("Загрузить JSON", GUILayout.Width(140))) LoadFromJson();
        GUI.enabled = !string.IsNullOrEmpty(jsonPath);
        if (GUILayout.Button("Сохранить JSON", GUILayout.Width(140))) SaveToJson();
        GUI.enabled = true;
        GUILayout.FlexibleSpace();
        if (!string.IsNullOrEmpty(jsonPath)) GUILayout.Label($"Файл: {Path.GetFileName(jsonPath)}", EditorStyles.miniLabel);
        GUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void OnGUI()
    {
        DrawJsonControls();
        if (scenes == null || scenes.scene == null || scenes.scene.Count == 0) return;
        DrawSceneSelector();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        Matrix4x4 oldMatrix = GUI.matrix;
        Matrix4x4 translation = Matrix4x4.TRS(zoomOrigin, Quaternion.identity, Vector3.one);
        Matrix4x4 scaling = Matrix4x4.Scale(Vector3.one * zoomScale);
        GUI.matrix = translation * scaling * GUI.matrix;
        DrawGraphContent();
        GUI.matrix = oldMatrix;
        EditorGUILayout.EndScrollView();
        Repaint();
    }

    private void DrawSceneSelector()
    {
        Rect popupRect = new Rect(290, 5, 490, 20);
        if (scenes.scene == null || scenes.scene.Count == 0) return;
        string[] options = scenes.scene.ConvertAll(s => $"{scenes.scene.IndexOf(s) + 1}. {s.npc_name}").ToArray();
        selectedSceneIndex = EditorGUI.Popup(popupRect, selectedSceneIndex, options);
    }

    private void ApplyEditing()
    {
        if (editingNode != null) editingNode.line = editBuffer;
        else if (editingLink != null) editingLink.line = editBuffer;
        EndEditing();
    }

    private void CancelEditing()
    {
        EndEditing();
    }

    private void EndEditing()
    {
        editingNode = null;
        editingLink = null;
        editBuffer = "";
        editingActive = false;
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
    }

    private bool IsEditing()
    {
        return editingNode != null || editingLink != null;
    }

    private float CalculateNodeHeight(string text)
    {
        GUIStyle style = EditorStyles.helpBox;
        float width = nodeWidth - style.padding.horizontal - style.margin.horizontal;
        return style.CalcHeight(new GUIContent(text), width) + 5;
    }

    private void DrawGraphContent()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Space(1500);
        GUILayout.EndHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Space(1500); 
        GUILayout.EndVertical();
        var scene = scenes.scene[selectedSceneIndex];
        var nodes = scene.data;
        nodeMap.Clear();
        nodePositions.Clear();
        float startX = 100, startY = 100, spacingY = 200;
        float spacingX = 100;
        for (int i = 0; i < nodes.Count; i++)
        {
            DialogueNode node = nodes[i];
            nodeMap[node.id] = node;
            Vector2 pos = node.meta != null ? new Vector2(node.meta.x, node.meta.y) : new Vector2(startX, startY + i * spacingY);
            nodePositions[node.id] = pos;
            float currentNodeHeight = CalculateNodeHeight($"ID: {node.id}\n{node.line}");
            var rect = new Rect(pos.x + spacingX, pos.y, nodeWidth, currentNodeHeight);
            GUI.Box(rect, $"ID: {node.id}\n{node.line}", EditorStyles.helpBox);
            Event e = Event.current;
            if (!IsEditing())
            {
                if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
                {
                    double time = EditorApplication.timeSinceStartup;
                    if (time - lastClickTime < doubleClickThreshold)
                    {
                        editingNode = node;
                        editingLink = null;
                        editBuffer = node.line;
                        editingRect = rect;
                        editingActive = true;
                        GUI.FocusControl(null);
                        e.Use();
                    }
                    else
                    {
                        draggedNode = node;
                        dragOffset = e.mousePosition - pos;
                        e.Use();
                    }
                    lastClickTime = time;
                }
                if (e.type == EventType.MouseDrag && draggedNode == node && e.button == 0)
                {
                    Vector2 newPos = e.mousePosition - dragOffset;
                    node.meta.x = newPos.x;
                    node.meta.y = newPos.y;
                    nodePositions[node.id] = newPos;
                    e.Use();
                    GUI.changed = true;
                }
                if (e.type == EventType.MouseUp && draggedNode == node)
                {
                    draggedNode = null;
                    e.Use();
                }
            }
        }
        foreach (var node in nodes)
        {
            if (!nodePositions.TryGetValue(node.id, out Vector2 fromPos)) continue;
            float fromNodeHeight = CalculateNodeHeight($"ID: {node.id}\n{node.line}");
            var fromRect = new Rect(fromPos.x, fromPos.y, nodeWidth, fromNodeHeight);
            foreach (var link in node.to)
            {
                if (!nodePositions.TryGetValue(link.id, out Vector2 toPos)) continue;
                DialogueNode targetNode = nodeMap[link.id];
                float toNodeHeight = CalculateNodeHeight($"ID: {targetNode.id}\n{targetNode.line}");
                var toRect = new Rect(toPos.x, toPos.y, nodeWidth, toNodeHeight);
                Vector2 start = new Vector2(fromRect.center.x + spacingX, fromRect.yMax);
                Vector2 end = new Vector2(toRect.center.x + spacingX, toRect.yMin);
                Handles.DrawBezier(
                    start,
                    end,
                    start + Vector2.down * 50,
                    end + Vector2.up * 50,
                    Color.cyan,
                    null,
                    2f
                );
                if (!string.IsNullOrEmpty(link.line))
                {
                    Vector2 labelPos = (start + end) / 2;
                    var labelRect = new Rect(labelPos.x - 50, labelPos.y - 10, 100, 20);
                    GUI.Label(labelRect, link.line, EditorStyles.miniLabel);
                    if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                    {
                        double time = EditorApplication.timeSinceStartup;
                        if (time - lastClickTime < doubleClickThreshold)
                        {
                            editingLink = link;
                            editingNode = null;
                            editBuffer = link.line;
                            editingRect = labelRect;
                            editingActive = true;
                            GUI.FocusControl(null);
                            Event.current.Use();
                        }
                        lastClickTime = time;
                    }
                }
            }
        }
        if (IsEditing())
        {
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            float editHeight = textAreaStyle.CalcHeight(new GUIContent(editBuffer), nodeWidth);
            Rect textAreaRect = new Rect(editingRect.x, editingRect.y, nodeWidth, editHeight);
            textAreaStyle.wordWrap = true;
            GUI.SetNextControlName("EditField");
            editBuffer = EditorGUI.TextArea(textAreaRect, editBuffer, textAreaStyle);
            if (!editingActive)
            {
                EditorGUI.FocusTextInControl("EditField");
                editingActive = true;
            }
            float buttonWidth = 60f;
            float buttonHeight = 24f;
            float spacing = 8f;
            Rect okButtonRect = new Rect(textAreaRect.x + nodeWidth + spacing, textAreaRect.y, buttonWidth + 20f, buttonHeight);
            Rect cancelButtonRect = new Rect(okButtonRect.x, okButtonRect.y + buttonHeight + spacing, buttonWidth, buttonHeight);
            if (GUI.Button(okButtonRect, "Сохранить")) ApplyEditing();
            if (GUI.Button(cancelButtonRect, "Отмена")) CancelEditing();
            if (editingNode != null)
            {
                float fieldsY = cancelButtonRect.y + buttonHeight + spacing;
                float fieldWidth = 90f;
                Rect itemLabelRect = new Rect(
                    okButtonRect.x,
                    fieldsY,
                    fieldWidth,
                    buttonHeight
                );
                GUI.Label(itemLabelRect, "Предмет (ID):");
                Rect itemFieldRect = new Rect(
                    okButtonRect.x,
                    fieldsY + buttonHeight,
                    fieldWidth,
                    buttonHeight
                );
                editingNode.goal_achieved.item = EditorGUI.IntField(itemFieldRect, editingNode.goal_achieved.item);
                Rect infoLabelRect = new Rect(
                    okButtonRect.x,
                    fieldsY + buttonHeight * 2 + spacing,
                    fieldWidth,
                    buttonHeight
                );
                GUI.Label(infoLabelRect, "Информация:");
                Rect infoFieldRect = new Rect(
                    okButtonRect.x,
                    fieldsY + buttonHeight * 3 + spacing,
                    fieldWidth,
                    buttonHeight
                );
                editingNode.goal_achieved.info = EditorGUI.IntField(infoFieldRect, editingNode.goal_achieved.info);
                if (editingNode.goal_achieved.item == -1)
                {
                    Rect hintRect = new Rect(
                        okButtonRect.x,
                        fieldsY + buttonHeight * 4 + spacing,
                        fieldWidth,
                        buttonHeight
                    );
                    GUI.Label(hintRect, "( -1 = ничего )", EditorStyles.miniLabel);
                }
            }
            return;
        }
    }

    private void LoadFromJson()
    {
        string path = EditorUtility.OpenFilePanel("Загрузить файл", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            string json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<MyScenes>("{\"scene\":" + json + "}");
            if (data != null && data.scene != null)
            {
                scenes = data;
                jsonPath = path;
                EditorPrefs.SetString(JsonPathKey, path);
                Repaint();
                Debug.Log("Загружено: " + path);
            }
            else Debug.LogError("Не удалось распарсить JSON как список сцен.");
        }
    }

    [Serializable]
    public class SceneListWrapper<T>
    {
        public List<T> scene;
        public SceneListWrapper(List<T> scene)
        {
            this.scene = scene;
        }
    }

    private void SaveToJson()
    {
        if (string.IsNullOrEmpty(jsonPath))
        {
            Debug.LogWarning("Файл не выбран. Сначала загрузите или выберите файл.");
            return;
        }
        string rawJson = JsonUtility.ToJson(new SceneListWrapper<MyScene>(scenes.scene), true);
        int index = rawJson.IndexOf("\"scene\"");
        if (index != -1)
        {
            int start = rawJson.IndexOf('[', index);
            int end = rawJson.LastIndexOf(']');
            if (start != -1 && end != -1 && end > start)
            {
                string json = rawJson.Substring(start, end - start + 1);
                File.WriteAllText(jsonPath, json);
                Debug.Log("Сохранено в: " + jsonPath);
                return;
            }
        }
        Debug.LogError("Не удалось сохранить JSON: структура повреждена.");
    }
}