using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DialogueItemsEditor : EditorWindow
{
    private ItemsDatabase database;
    private List<InventoryItem> predefinedItems => database.items;
    private Vector2 itemsScrollPos;
    private float[] columnWidths = { 50f, 140f, 250f, 120f, 70f };
    private double lastClickTime = 0;
    private const double doubleClickThreshold = 0.3;
    private int editingItemIndex = -1;
    private enum EditingField { None, Name, Description }
    private EditingField editingField = EditingField.None;
    private string editBuffer = "";
    private Rect editingRect;
    private bool editingActive = false;
    private const string DatabasePath = "Assets/DialogueItemsDatabase.asset";


    [MenuItem("Tools/PlotTalkAI/Dialogue Items")]
    public static void OpenWindow()
    {
        var window = GetWindow<DialogueItemsEditor>();
        window.titleContent = new GUIContent("Dialogue Items");
        window.Show();
    }

    private void OnEnable()
    {
        database = AssetDatabase.LoadAssetAtPath<ItemsDatabase>(DatabasePath);
        if (database == null)
        {
            database = CreateInstance<ItemsDatabase>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        if (database.items.Count == 0)
        {
            CreateInventoryItem("Ключ-карта", "Ключ-карта Сириуса", "keycard_sirius");
            CreateInventoryItem("Конспект", "Конспект лекции А. В. Гасникова \"Минимизация эмпирического риска\"", "conspects_sirius");
            CreateInventoryItem("Мерч", "Мерч Сириуса", "tshirt_sirius");
            CreateInventoryItem("Закрытый ноутбук", "Коллеги, просим вас немедлено закрыть ноутбуки!", "laptop_sirius");
            CreateInventoryItem("Бейджик", "Бейджик участника июльской программы Большие вызовы", "id_card_sirius");
            SaveDatabase();
        }
    }

    private void CreateInventoryItem(string name, string description, string texturePath)
    {
        var item = CreateInstance<InventoryItem>();
        item.name = name;
        item.description = description;
        if (!string.IsNullOrEmpty(texturePath)) item.icon = Resources.Load<Sprite>("Textures/" + texturePath);
        else item.icon = Resources.Load<Sprite>("Textures/" + name);
        AssetDatabase.AddObjectToAsset(item, database);
        database.items.Add(item);
        SaveDatabase();
    }

    private void SaveDatabase()
    {
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void OnGUI()
    {
        DrawItemsTable();
    }

    private bool IsEditing() => editingItemIndex >= 0 && editingField != EditingField.None;

    private void StartEditing(int row, EditingField field, Rect rect, string initialText)
    {
        editingItemIndex = row;
        editingField = field;
        editBuffer = initialText;
        editingRect = rect;
        editingActive = false;
    }

    private void ApplyEditing()
    {
        if (editingItemIndex >= 0 && editingItemIndex < predefinedItems.Count)
        {
            if (editingField == EditingField.Name) predefinedItems[editingItemIndex].name = editBuffer;
            else if (editingField == EditingField.Description) predefinedItems[editingItemIndex].description = editBuffer;
        }
        SaveDatabase();
        EndEditing();
    }

    private void CancelEditing()
    {
        EndEditing();
    }

    private void EndEditing()
    {
        editingItemIndex = -1;
        editingField = EditingField.None;
        editBuffer = "";
        editingActive = false;
        GUI.FocusControl(null);
        GUIUtility.keyboardControl = 0;
    }
    

    private void DrawItemsTable()
    {
        float headerRowHeight = 30f;
        float contentRowHeight = 108f;
        float iconSize = 100f;
        Color gridColor = Color.black;
        float areaWidth = columnWidths.Sum() + 25;
        float areaX = (position.width - areaWidth) / 2f;
        float areaY = 50;
        float totalHeight = headerRowHeight;
        foreach (var item in predefinedItems)
        {
            totalHeight += CalculateRowHeight(item.description, columnWidths[2], contentRowHeight);
        }
        float areaHeight = Mathf.Min(position.height - 80, 40 + totalHeight);
        GUIStyle tightWindowStyle = new GUIStyle(GUI.skin.window)
        {
            padding = new RectOffset(1, 1, 3, 3),
            margin = new RectOffset(0, 0, 0, 0)
        };

        GUILayout.BeginArea(new Rect(areaX, areaY, areaWidth, areaHeight), "", tightWindowStyle);
        GUILayout.BeginHorizontal(GUILayout.Height(30));
        {
            GUIStyle headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(3, 3, 3, 3),
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
            };
            GUILayout.Label("Таблица предметов", headerLabelStyle);
            if (GUILayout.Button("Добавить предмет", GUILayout.Width(120))) CreateInventoryItem("Новый предмет", "Описание", "");
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
        itemsScrollPos = GUI.BeginScrollView(
            new Rect(0, 26, areaWidth, areaHeight - 26),
            itemsScrollPos,
            new Rect(0, 0, areaWidth - 20, totalHeight)
        );
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(5, 5, 5, 5),
            fixedHeight = headerRowHeight - 2,
            fontSize = 12
        };
        GUIStyle centerCellStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(5, 5, 5, 5),
            wordWrap = true,
            fontSize = 12,
            clipping = TextClipping.Clip
        };
        float startX = 0;
        EditorGUI.DrawRect(new Rect(0, 0, areaWidth - 15, headerRowHeight), Color.clear);
        EditorGUILayout.BeginHorizontal(GUILayout.Height(headerRowHeight));
        for (int i = 0; i < columnWidths.Length; i++)
        {
            string header = i switch
            {
                0 => "ID",
                1 => "Название",
                2 => "Описание",
                3 => "Иконка",
                4 => "Действия",
                _ => ""
            };
            Rect headerRect = new Rect(startX + 1, 0, columnWidths[i] + i + 1, headerRowHeight);
            GUI.Label(headerRect, header, headerStyle);
            if (i < columnWidths.Length - 1)
            {
                EditorGUI.DrawRect(new Rect(startX + columnWidths[i], 0, 1, headerRowHeight), gridColor);
            }
            startX += columnWidths[i];
        }
        EditorGUILayout.EndHorizontal();
        float currentY = headerRowHeight + 1;
        for (int row = 0; row < predefinedItems.Count; row++)
        {
            InventoryItem item = predefinedItems[row];
            EditorGUI.BeginDisabledGroup(IsEditing());
            float rowHeight = CalculateRowHeight(item.description, columnWidths[2], contentRowHeight);
            Rect idRect = new Rect(1, currentY, columnWidths[0], rowHeight);
            EditorGUI.LabelField(idRect, row.ToString(), centerCellStyle);
            Rect nameRect = new Rect(columnWidths[0] + 2, currentY, columnWidths[1], rowHeight);
            Event e = Event.current;
            EditorGUI.LabelField(nameRect, item.name, centerCellStyle);
            if (e.type == EventType.MouseDown && e.button == 0 && nameRect.Contains(e.mousePosition))
            {
                double time = EditorApplication.timeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold)
                {
                    StartEditing(row, EditingField.Name, nameRect, item.name);
                    e.Use();
                }
                lastClickTime = time;
            }
            Rect descRect = new Rect(columnWidths[0] + columnWidths[1] + 3, currentY, columnWidths[2], rowHeight);
            EditorGUI.LabelField(descRect, item.description, centerCellStyle);
            if (e.type == EventType.MouseDown && e.button == 0 && descRect.Contains(e.mousePosition))
            {
                double time = EditorApplication.timeSinceStartup;
                if (time - lastClickTime < doubleClickThreshold)
                {
                    StartEditing(row, EditingField.Description, descRect, item.description);
                    e.Use();
                }
                lastClickTime = time;
            }
            Rect iconRect = new Rect(
                columnWidths[0] + columnWidths[1] + columnWidths[2] + (columnWidths[3] - iconSize) / 2,
                currentY + (rowHeight - iconSize) / 2,
                iconSize,
                iconSize
            );
            Sprite newIcon = (Sprite)EditorGUI.ObjectField(iconRect, item.icon, typeof(Sprite), false);
            if (newIcon != item.icon)
            {
                item.icon = newIcon;
                SaveDatabase();
            }
            EditorGUI.DrawRect(new Rect(0, currentY, areaWidth - 15, 1), gridColor);
            EditorGUI.DrawRect(new Rect(columnWidths[0], currentY, 1, rowHeight), gridColor);
            EditorGUI.DrawRect(new Rect(columnWidths[0] + columnWidths[1], currentY, 1, rowHeight), gridColor);
            EditorGUI.DrawRect(new Rect(columnWidths[0] + columnWidths[1] + columnWidths[2], currentY, 1, rowHeight), gridColor);
            EditorGUI.DrawRect(new Rect(columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3], currentY, 1, rowHeight), gridColor);
            Rect deleteRect = new Rect(columnWidths[0] + columnWidths[1] + columnWidths[2] + columnWidths[3] + 9,
                currentY + (rowHeight - 24) / 2, columnWidths[4] - 10, 24
            );
            if (GUI.Button(deleteRect, "Удалить"))
            {
                predefinedItems.RemoveAt(row);
                SaveDatabase();
                GUIUtility.ExitGUI();
            }
            EditorGUI.EndDisabledGroup();
            currentY += rowHeight;
        }
        if (IsEditing())
        {
            GUIStyle style = (editingField == EditingField.Name) ? EditorStyles.textField : EditorStyles.textArea;
            style.wordWrap = (editingField == EditingField.Description);
            float editHeight = (editingField == EditingField.Name) ? 22f
                : Mathf.Max(60f, style.CalcHeight(new GUIContent(editBuffer), editingRect.width));
            Rect textAreaRect = new Rect(editingRect.x - 1, editingRect.y, editingRect.width - 1, editHeight);
            GUI.SetNextControlName("EditField");
            if (editingField == EditingField.Name) editBuffer = EditorGUI.TextField(textAreaRect, editBuffer, style);
            else editBuffer = EditorGUI.TextArea(textAreaRect, editBuffer, style);
            if (!editingActive)
            {
                EditorGUI.FocusTextInControl("EditField");
                editingActive = true;
            }
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    ApplyEditing();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    CancelEditing();
                    e.Use();
                }
            }
            float buttonWidth = 75f;
            float buttonHeight = 25f;
            float spacing = 6f;
            Rect okButtonRect = new Rect(textAreaRect.x + textAreaRect.width + spacing, textAreaRect.y, buttonWidth, buttonHeight);
            Rect cancelButtonRect = new Rect(okButtonRect.x, okButtonRect.y + buttonHeight + spacing, buttonWidth, buttonHeight);
            if (GUI.Button(okButtonRect, "Сохранить")) ApplyEditing();
            if (GUI.Button(cancelButtonRect, "Отмена")) CancelEditing();
        }

        GUI.EndScrollView();
        EditorGUI.DrawRect(new Rect(0, 0, 1, totalHeight), gridColor);
        GUILayout.EndArea();
    }

    private float CalculateRowHeight(string description, float columnWidth, float minHeight)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            padding = new RectOffset(5, 5, 5, 5)
        };
        float height = style.CalcHeight(new GUIContent(description), columnWidth - 10);
        return Mathf.Max(minHeight, height + 10);
    }
}