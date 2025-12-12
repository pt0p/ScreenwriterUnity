using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class PlotTalkAI : EditorWindow
{
    // pages
    public enum Page
    {
        Login,
        Register,
        Main,
        GameDetail,
        EditGame,
        EditCharacters,
        EditCharacter,
        EditScene,
        EditScript,
        GraphEditor
    }

    private const float MIN_ZOOM = 0.3f;
    private const float MAX_ZOOM = 2.0f;
    private const float GRAPH_PADDING = 50f;
    
    private GUIStyle addCardStyle;
    private GUIStyle arrowButtonStyle;
    private GUIStyle buttonStyle;
    private GUIStyle lowButtonStyle;
    private GUIStyle cardStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle centeredItalicLabelStyle;
    private GUIStyle centeredLabelStyle;
    private GUIStyle centeredSmallLabelStyle;
    private GUIStyle linkStyle;
    private GUIStyle plusLabelStyle;
    private GUIStyle plusStyle;
    private GUIStyle scriptLabelStyle;
    private GUIStyle iconButtonStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle textAreaStyle;
    private GUIStyle zoomLabelStyle;


    private JObject editingLink;
    private JObject editingLinkSourceNode;
    private bool showLinkEditor;
    private Vector2 linkEditorScroll;
    private Page currentPage = Page.Login;
    private Vector2 dragOffset;
    private string editCharacterExtra;

    private string editCharacterLook;

    // edit character
    private string editCharacterName;
    private string editCharacterProfession;
    private string editCharacterTalkStyle;
    private string editCharacterTraits;
    private string editGameDescription;

    private int editGameGenre;

    // edit game
    private string editGameName;
    private int editGameTechLevel;
    private int editGameTonality;
    private JObject editingNode;
    private Vector2 editingNodeScroll;
    private JArray editSceneCharacters;

    private string editSceneDescription;

    // edit scene
    private string editSceneName;
    private string editScriptAdditional;
    private string editScriptDescription;
    private int editScriptMaxDepth;
    private int editScriptMaxMainChar;
    private int editScriptMinDepth;

    private int editScriptMinMainChar;

    private Vector2 lineEditInfoScroll;

    private Stack<JObject> undoStack = new Stack<JObject>();
    private Stack<JObject> redoStack = new Stack<JObject>();
    private bool isTakingSnapshot = false;

    // edit script
    private string editScriptName;

    // dropdowns
    private readonly string[] gameGenres =
        { "Приключения", "Фэнтези", "Детектив", "Драма", "Комедия", "Ужасы", "Стратегия" };

    private readonly string[] gameTechLevels =
        { "Каменный век", "Средневековье", "Индустриальный", "Современность", "Будущее", "Другое" };

    private readonly string[] gameTonalities =
        { "Нейтральная", "Героическая", "Трагическая", "Комическая", "Сказочная" };

    private Rect graphBounds;
    private Vector2 graphPanOffset = Vector2.zero;
    private Rect graphRect;
    private float graphZoom = 1.0f;

    private bool isHoveringLink;
    private bool isPanning;
    private bool windowInitialized;
    private Vector2 lastMousePosition;

    // cashed styles
    private Vector2 lastGraphMousePos;

    // fields
    // login
    private string loginEmail = "";
    private string loginPassword = "";

    private bool pageInitialized;
    private Vector2 panStart;
    private bool playerGetsInfo;
    private string playerGetsInfoCondition;
    private string playerGetsInfoName;
    private bool playerGetsItem;
    private string playerGetsItemCondition;
    private string playerGetsItemName;
    private string registerConfirm = "";

    // register
    private string registerEmail = "";
    private string registerPassword = "";
    private string registerName = "";
    private string registerSurname = "";

    private readonly Dictionary<long, bool> sceneExpandedStates = new();

    private readonly string[] scriptCharacterAttitude = { "Не знаком", "Хорошо", "Нейтрально", "Плохо" };
    private Vector2 scrollPosition;
    private JObject selectedCharacter;

    private string lineEditText;
    private string lineEditInfo;

    // selected objects
    private JObject selectedGame;
    private string selectedMainCharacterId;

    // graph data
    private JObject selectedNode;
    private string selectedNPCId;
    private JObject selectedScene;
    private JObject selectedScript;
    private bool showNodeEditor;
    private int toMainCharacterRelation;
    private int toNpcRelation;
    string nodeEditText;
    private int nodeEditItem = -1;

    private bool isCreatingLink = false;
    private JObject linkCreationSource;
    private Vector2 contextMenuPosition;
    private bool showContextMenu = false;
    private JObject contextMenuNode;
    private JObject contextMenuLink;
    private JObject contextMenuLinkSource;

    private void OnEnable()
    {
        SwitchPage(currentPage);
        CreateStyles();
    }

    private void OnGUI()
    {
        var backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.96f, 0.96f, 0.96f);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);
        
        GUILayout.BeginArea(new Rect(
            20,
            20,
            position.width - 40,
            position.height - 40
        ));

        GUILayout.BeginVertical();
        GUILayout.Space(20);

        switch (currentPage)
        {
            case Page.Login:
                DrawLoginPage();
                break;
            case Page.Register:
                DrawRegisterPage();
                break;
            case Page.Main:
                DrawMainPage();
                break;
            case Page.GameDetail:
                DrawGameDetailPage();
                break;
            case Page.EditGame:
                DrawEditGamePage();
                break;
            case Page.EditCharacters:
                DrawEditCharactersPage();
                break;
            case Page.EditCharacter:
                DrawEditCharacterPage();
                break;
            case Page.EditScene:
                DrawEditScenePage();
                break;
            case Page.EditScript:
                DrawEditScriptPage();
                break;
            case Page.GraphEditor:
                DrawGraphEditorPage();
                break;
        }

        GUILayout.Space(20);
        GUILayout.EndVertical();

        GUILayout.EndArea();

        EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height),
            isHoveringLink ? MouseCursor.Link : MouseCursor.Arrow);

        isHoveringLink = false;
    }

    [MenuItem("PlotTalkAI/PlotTalkAI")]
    public static void ShowWindow()
    {
        GetWindow<PlotTalkAI>("PlotTalkAI").minSize = new Vector2(450, 500);
    }

    private void CreateStyles()
{
    var textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
    var linkColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.1f, 0.3f, 0.8f);
    var backgroundColor = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.96f, 0.96f, 0.96f);
    var cardBackgroundColor = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(1f, 1f, 1f);

    linkStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = linkColor },
        hover = { textColor = Color.Lerp(linkColor, Color.white, 0.3f) },
        alignment = TextAnchor.MiddleCenter,
        fontSize = 12
    };

    centeredLabelStyle = new GUIStyle(EditorStyles.label)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 24,
        fontStyle = FontStyle.Bold,
        normal = { textColor = textColor },
        wordWrap = true
    };

    textAreaStyle = new GUIStyle(EditorStyles.textArea)
    {
        wordWrap = true
    };

    centeredSmallLabelStyle = new GUIStyle(EditorStyles.label)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 20,
        fontStyle = FontStyle.Bold,
        normal = { textColor = textColor },
        wordWrap = true
    };

    centeredItalicLabelStyle = new GUIStyle(EditorStyles.label)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = 14,
        fontStyle = FontStyle.Italic,
        normal = { textColor = textColor },
        wordWrap = true
    };

    fieldLabelStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = textColor },
        fontSize = 12,
        margin = new RectOffset(2, 0, 5, 2)
    };

    buttonStyle = new GUIStyle(EditorStyles.miniButton)
    {
        fontSize = 14,
        alignment = TextAnchor.MiddleCenter,
        fontStyle = FontStyle.Bold,
        fixedHeight = 40
    };

    lowButtonStyle = new GUIStyle(buttonStyle)
    {
        fixedHeight = 30
    };

    textFieldStyle = new GUIStyle(EditorStyles.textField)
    {
        fontSize = 14,
        fixedHeight = 35,
        padding = new RectOffset(10, 10, 8, 8),
        alignment = TextAnchor.MiddleLeft,
        normal = { textColor = textColor }
    };

    // Стиль для карточек игр
    cardStyle = new GUIStyle(EditorStyles.helpBox)
    {
        margin = new RectOffset(5, 5, 10, 10),
        padding = new RectOffset(15, 15, 15, 15),
        normal = { background = MakeTex(2, 2, cardBackgroundColor) }
    };

    addCardStyle = new GUIStyle(EditorStyles.helpBox)
    {
        margin = new RectOffset(5, 5, 10, 10),
        padding = new RectOffset(15, 15, 15, 15),
        normal = { background = MakeTex(2, 2, cardBackgroundColor) }
    };

    // Стиль для заголовка карточки
    cardTitleStyle = new GUIStyle(EditorStyles.label)
    {
        fontSize = 16,
        fontStyle = FontStyle.Bold,
        normal = { textColor = textColor },
        margin = new RectOffset(0, 0, 5, 10)
    };

    // Стиль для кнопок-иконок
    iconButtonStyle = new GUIStyle(EditorStyles.miniButton)
    {
        fixedWidth = 30,
        fixedHeight = 30,
        padding = new RectOffset(0, 0, 0, 0)
    };

    // Стиль для большого плюса
    plusStyle = new GUIStyle(EditorStyles.label)
    {
        fontSize = 64,
        alignment = TextAnchor.MiddleCenter,
        fontStyle = FontStyle.Bold,
        normal = { textColor = textColor }
    };

    // Стиль для подписи плюса
    plusLabelStyle = new GUIStyle(EditorStyles.label)
    {
        fontSize = 16,
        alignment = TextAnchor.MiddleCenter,
        fontStyle = FontStyle.Bold,
        normal = { textColor = textColor }
    };

    // Стиль для кнопки со стрелкой
    arrowButtonStyle = new GUIStyle(EditorStyles.label)
    {
        alignment = TextAnchor.UpperCenter,
        normal = { textColor = textColor },
        hover = { textColor = new Color(0.3f, 0.3f, 0.3f) },
        active = { textColor = new Color(0.3f, 0.3f, 0.3f) },
        padding = new RectOffset(0, 0, 0, 0),
        margin = new RectOffset(0, 0, 0, 0),
        fixedWidth = 20,
        fixedHeight = 20
    };

    scriptLabelStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = textColor },
        hover = { textColor = new Color(0.3f, 0.3f, 0.3f) },
        fontSize = 14
    };

    zoomLabelStyle = new GUIStyle(EditorStyles.label)
    {
        normal = { textColor = Color.gray },
        fontSize = 10,
        alignment = TextAnchor.LowerRight
    };
}


    private void DrawLoginPage()
    {
        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));
        GUILayout.Label("Вход", centeredLabelStyle);
        GUILayout.Space(30);

        GUILayout.Label("Email", fieldLabelStyle);
        loginEmail = EditorGUILayout.TextField(loginEmail, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Пароль", fieldLabelStyle);
        loginPassword = EditorGUILayout.PasswordField(loginPassword, textFieldStyle);

        GUILayout.Space(25);

        // Кнопка "Войти"
        var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(40));
        if (GUI.Button(buttonRect, "Войти", buttonStyle))
        {
            if (!string.IsNullOrEmpty(loginEmail) && !string.IsNullOrEmpty(loginPassword))
            {
                BackendApi.Login(loginEmail, loginPassword, (success, message, userData) =>
                {
                    if (success)
                    {
                        // Сохраняем данные пользователя через StorageApi
                        StorageApi.GetInstance().LogIn(
                            0, // userId будет получен от сервера
                            message, // token
                            userData.ToString() // user data as string
                        );
                        SwitchPage(Page.Main);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Ошибка", message, "OK");
                    }
                });
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Заполните все поля", "OK");
            }
        }

        GUILayout.Space(20);

        // Проверяем наведение на ссылку
        var linkRect = GUILayoutUtility.GetRect(new GUIContent("Регистрация"), linkStyle);
        if (GUI.Button(linkRect, "Регистрация", linkStyle)) SwitchPage(Page.Register);

        if (linkRect.Contains(Event.current.mousePosition)) isHoveringLink = true;

        GUILayout.EndArea();
    }

    private void DrawRegisterPage()
    {
        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));
        GUILayout.Label("Регистрация", centeredLabelStyle);
        GUILayout.Space(30);

        GUILayout.Label("Email", fieldLabelStyle);
        registerEmail = EditorGUILayout.TextField(registerEmail, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Имя", fieldLabelStyle);
        registerName = EditorGUILayout.TextField(registerName, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Фамилия", fieldLabelStyle);
        registerSurname = EditorGUILayout.TextField(registerSurname, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Пароль", fieldLabelStyle);
        registerPassword = EditorGUILayout.PasswordField(registerPassword, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Повторите пароль", fieldLabelStyle);
        registerConfirm = EditorGUILayout.PasswordField(registerConfirm, textFieldStyle);

        GUILayout.Space(25);

        // Кнопка "Зарегистрироваться"
        var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(40));
        if (GUI.Button(buttonRect, "Зарегистрироваться", buttonStyle))
        {
            if (string.IsNullOrEmpty(registerEmail) || string.IsNullOrEmpty(registerPassword) || 
                string.IsNullOrEmpty(registerName) || string.IsNullOrEmpty(registerSurname))
            {
                EditorUtility.DisplayDialog("Ошибка", "Заполните все поля", "OK");
            }
            else if (registerPassword != registerConfirm)
            {
                EditorUtility.DisplayDialog("Ошибка", "Пароли не совпадают", "OK");
            }
            else
            {
                BackendApi.Register(registerEmail, registerName, registerSurname, registerPassword, (success, message) =>
                {
                    if (success)
                    {
                        EditorUtility.DisplayDialog("Успех", message, "OK");
                        SwitchPage(Page.Login);
                        // Очищаем поля после успешной регистрации
                        registerEmail = "";
                        registerName = "";
                        registerSurname = "";
                        registerPassword = "";
                        registerConfirm = "";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Ошибка", message, "OK");
                    }
                });
            }
        }

        GUILayout.Space(20);

        // Проверяем наведение на ссылку
        var linkRect = GUILayoutUtility.GetRect(new GUIContent("Назад"), linkStyle);
        if (GUI.Button(linkRect, "Назад", linkStyle)) SwitchPage(Page.Login);

        if (linkRect.Contains(Event.current.mousePosition)) isHoveringLink = true;

        GUILayout.EndArea();
    }

    private void DrawMainPage()
    {
        var games = StorageApi.GetInstance().GetGamesArray(StorageApi.GetInstance().LoadFullJson());
        GUILayout.Label($"Добро пожаловать, {StorageApi.GetInstance().GetUser()?["data"]?["name"] ?? "Ошибка получения имени"} {StorageApi.GetInstance().GetUser()?["data"]?["surname"] ?? "Ошибка получения фамилии"}!", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную ширину с учетом полосы прокрутки
        var availableWidth = position.width - 40;
        var columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 300));
        var cardWidth = (availableWidth - (columns - 1) * 20 - 70) / columns; // Вычитаем ширину полосы прокрутки

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));

        var cardCount = games.Count + 1;

        // Создаем сетку карточек
        for (var i = 0; i < cardCount; i += columns)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(0); // Убираем любые отступы по умолчанию

            for (var j = 0; j < columns; j++)
            {
                var index = i + j;
                if (index >= cardCount) break;

                if (j > 0) GUILayout.Space(20);

                if (index == 0)
                    DrawCreateGameCard(cardWidth + 7, 120);
                else if (index - 1 < games.Count) DrawGameCard((JObject)games[index - 1], cardWidth, 120);
            }

            // Заполняем оставшееся пространство
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (i + columns < cardCount) GUILayout.Space(20);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(20);

        // Кнопка "Выйти"
        var buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(35));
        if (GUI.Button(buttonRect, "Выйти", buttonStyle))
        {
            StorageApi.GetInstance().LogOut();
            SwitchPage(Page.Login);
            loginEmail = "";
            loginPassword = "";
        }
    }

    private void DrawCreateGameCard(float width, float height)
    {
        GUILayout.BeginVertical(addCardStyle, GUILayout.Width(width), GUILayout.Height(height));

        // Центрируем содержимое по вертикали
        GUILayout.FlexibleSpace();

        // Большой плюс
        GUILayout.Label("+", plusStyle, GUILayout.Height(50));

        GUILayout.Space(10);

        // Подпись
        GUILayout.Label("Создать игру", plusLabelStyle);

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();

        // Обработка клика по карточке
        var cardRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && cardRect.Contains(Event.current.mousePosition))
        {
            CreateNewGame();
            Event.current.Use();
        }

        // Изменяем курсор при наведении
        if (cardRect.Contains(Event.current.mousePosition))
        {
            EditorGUIUtility.AddCursorRect(cardRect, MouseCursor.Link);
            isHoveringLink = true;
        }
    }

    private void DrawGameCard(JObject game, float width, float height)
    {
        GUILayout.BeginVertical(cardStyle, GUILayout.Width(width), GUILayout.Height(height));

        // Заголовок игры
        GUILayout.Label((string)game["name"], cardTitleStyle);

        // Описание игры с ограничением по высоте
        var descriptionHeight = height - 100; // Вычитаем высоту заголовка и кнопок
        var descriptionRect = GUILayoutUtility.GetRect(width - 30, descriptionHeight, EditorStyles.wordWrappedLabel);
        GUI.Label(descriptionRect, (string)game["description"], EditorStyles.wordWrappedLabel);

        GUILayout.FlexibleSpace();

        // Кнопки управления
        GUILayout.BeginHorizontal();

        // Кнопка перехода к игре
        if (GUILayout.Button("Открыть", GUILayout.Height(30)))
        {
            selectedGame = game;
            SwitchPage(Page.GameDetail);
        }


        if (GUILayout.Button($"Персонажи ({((JArray)game["characters"]).Count})", GUILayout.Height(30)))
        {
            selectedGame = game;
            SwitchPage(Page.EditCharacters);
        }

        // Кнопка настроек игры
        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle))
        {
            selectedGame = game;
            SwitchPage(Page.EditGame);
        }

        if (GUILayout.Button("X", iconButtonStyle, GUILayout.Height(30)))
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", игра будет безвозвратно удалена.",
                    "OK", "Отмена"))
                StorageApi.GetInstance().DeleteGame((string)game["id"]);

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void CreateNewGame()
    {
        SwitchPage(Page.EditGame);
        selectedGame = null;
    }

    private void DrawEditGamePage()
    {
        if (!pageInitialized)
        {
            if (selectedGame != null)
            {
                editGameName = (string)selectedGame["name"];
                editGameDescription = (string)selectedGame["description"];
                editGameGenre = Array.IndexOf(gameGenres, (string)selectedGame["genre"]);
                editGameTonality = Array.IndexOf(gameTonalities, (string)selectedGame["tonality"]);
                editGameTechLevel = Array.IndexOf(gameTechLevels, (string)selectedGame["techLevel"]);
            }
            else
            {
                ClearEditGamePageFields();
            }
        }

        pageInitialized = true;

        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));

        GUILayout.BeginVertical();
        GUILayout.Label("Игра", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную высоту для ScrollView
        var availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

        // Начинаем ScrollView с фиксированной высотой
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(availableHeight));

        GUILayout.Label("Название", fieldLabelStyle);
        editGameName = EditorGUILayout.TextField(editGameName, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Характеристики мира", fieldLabelStyle);
        editGameDescription = EditorGUILayout.TextArea(editGameDescription, textAreaStyle); // Фиксированная высота для текстового поля
        GUILayout.Space(5);
        editGameGenre = EditorGUILayout.Popup("Жанр", editGameGenre, gameGenres);
        GUILayout.Space(5);
        editGameTechLevel = EditorGUILayout.Popup("Исторический период", editGameTechLevel, gameTechLevels);
        GUILayout.Space(5);
        editGameTonality = EditorGUILayout.Popup("Тональность", editGameTonality, gameTonalities);

        GUILayout.EndScrollView();

        GUILayout.Space(15);

        GUILayout.Space(5);

        if (GUILayout.Button("Сохранить", buttonStyle, GUILayout.Height(40)))
        {
            if (!string.IsNullOrEmpty(editGameName) && !string.IsNullOrEmpty(editGameDescription))
            {
                if (selectedGame != null)
                {
                    var updGame = new JObject
                    {
                        ["name"] = editGameName,
                        ["genre"] = gameGenres[editGameGenre],
                        ["tonality"] = gameTonalities[editGameTonality],
                        ["techLevel"] = gameTechLevels[editGameTechLevel],
                        ["description"] = editGameDescription
                    };
                    StorageApi.GetInstance().UpdateGame((string)selectedGame["id"], updGame);
                    ClearEditGamePageFields();
                    SwitchPage(Page.Main);
                }
                else
                {
                    var newGame = new JObject
                    {
                        ["id"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                        ["name"] = editGameName,
                        ["genre"] = gameGenres[editGameGenre],
                        ["scenes"] = new JArray(),
                        ["characters"] = new JArray(),
                        ["tonality"] = gameTonalities[editGameTonality],
                        ["techLevel"] = gameTechLevels[editGameTechLevel],
                        ["description"] = editGameDescription
                    };
                    StorageApi.GetInstance().AddGame(newGame);
                    ClearEditGamePageFields();
                    selectedGame = newGame;
                    SwitchPage(Page.GameDetail);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Все поля обязательны для заполнения", "OK");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Отменить", buttonStyle, GUILayout.Height(40)))
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedGame = null;
                ClearEditGamePageFields();
                SwitchPage(Page.Main);
            }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawEditCharactersPage()
    {
        GUILayout.Label("Персонажи", centeredLabelStyle);
        GUILayout.Space(30);
        scrollPosition =
            GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        GUILayout.BeginVertical();
        foreach (var character in StorageApi.GetInstance().GetCharactersArray(selectedGame))
            DrawCharacterCard(character);

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        GUILayout.Space(15);

        if (GUILayout.Button("Создать персонажа", buttonStyle, GUILayout.Height(40)))
        {
            selectedCharacter = null;
            SwitchPage(Page.EditCharacter);
        }

        GUILayout.Space(5);
        if (GUILayout.Button("Готово", buttonStyle, GUILayout.Height(40)))
        {
            selectedCharacter = null;
            selectedGame = null;
            SwitchPage(Page.Main);
        }
    }

    private void DrawCharacterCard(JToken character)
    {
        GUILayout.BeginHorizontal(cardStyle);
        GUILayout.Label((string)character["name"], centeredLabelStyle);
        GUILayout.Space(10);

        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle))
        {
            selectedCharacter = (JObject)character;
            SwitchPage(Page.EditCharacter);
        }

        if (GUILayout.Button("X", iconButtonStyle, GUILayout.Height(30)))
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", персонаж будет безвозвратно удален.",
                    "OK", "Отмена"))
            {
                StorageApi.GetInstance().DeleteCharacter((string)selectedGame["id"], (string)character["id"]);
                selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
            }

        GUILayout.EndHorizontal();
    }

    private void DrawEditCharacterPage()
    {
        if (!pageInitialized)
        {
            if (selectedCharacter != null)
            {
                editCharacterName = (string)selectedCharacter["name"];
                editCharacterProfession = (string)selectedCharacter["profession"];
                editCharacterTraits = (string)selectedCharacter["traits"];
                editCharacterTalkStyle = (string)selectedCharacter["talk_style"];
                editCharacterLook = (string)selectedCharacter["look"];
                editCharacterExtra = (string)selectedCharacter["extra"];
            }
            else
            {
                ClearEditCharacterPageFields();
            }
        }

        pageInitialized = true;

        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));

        GUILayout.BeginVertical();
        GUILayout.Label("Персонаж", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную высоту для ScrollView
        var availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

        // Начинаем ScrollView с фиксированной высотой
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(availableHeight));

        GUILayout.Label("Имя", fieldLabelStyle);
        editCharacterName = EditorGUILayout.TextField(editCharacterName, textFieldStyle);
        GUILayout.Space(15);

        GUILayout.Label("Профессия", fieldLabelStyle);
        editCharacterProfession = EditorGUILayout.TextField(editCharacterProfession, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Характер", fieldLabelStyle);
        editCharacterTraits = EditorGUILayout.TextField(editCharacterTraits, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Стиль речи", fieldLabelStyle);
        editCharacterTalkStyle = EditorGUILayout.TextArea(editCharacterTalkStyle, textAreaStyle);

        GUILayout.Space(15);

        GUILayout.Label("Внешний вид", fieldLabelStyle);
        editCharacterLook = EditorGUILayout.TextArea(editCharacterLook, textAreaStyle);

        GUILayout.Space(15);

        GUILayout.Label("Характеристика", fieldLabelStyle);
        editCharacterExtra = EditorGUILayout.TextArea(editCharacterExtra, textAreaStyle); // Фиксированная высота для текстового поля

        GUILayout.Space(15);

        GUILayout.EndScrollView();

        // Кнопки вне ScrollView, чтобы они всегда были видны
        GUILayout.Space(15);

        if (GUILayout.Button("Сохранить", buttonStyle, GUILayout.Height(40)))
        {
            if (!string.IsNullOrEmpty(editCharacterLook) && !string.IsNullOrEmpty(editCharacterName) &&
                !string.IsNullOrEmpty(editCharacterProfession) && !string.IsNullOrEmpty(editCharacterTraits) &&
                !string.IsNullOrEmpty(editCharacterTalkStyle) && !string.IsNullOrEmpty(editCharacterLook) &&
                !string.IsNullOrEmpty(editCharacterExtra))
            {
                if (selectedCharacter != null)
                {
                    var updChar = new JObject
                    {
                        ["look"] = editCharacterLook,
                        ["name"] = editCharacterName,
                        ["extra"] = editCharacterExtra,
                        ["traits"] = editCharacterTraits,
                        ["profession"] = editCharacterProfession,
                        ["talk_style"] = editCharacterTalkStyle
                    };
                    StorageApi.GetInstance().UpdateCharacter((string)selectedGame["id"],
                        (string)selectedCharacter["id"], updChar);
                    ClearEditGamePageFields();
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    SwitchPage(Page.EditCharacters);
                }
                else
                {
                    var newChar = new JObject
                    {
                        ["id"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                        ["look"] = editCharacterLook,
                        ["name"] = editCharacterName,
                        ["extra"] = editCharacterExtra,
                        ["traits"] = editCharacterTraits,
                        ["profession"] = editCharacterProfession,
                        ["talk_style"] = editCharacterTalkStyle,
                        ["type"] = "NPC"
                    };
                    StorageApi.GetInstance().AddCharacter((string)selectedGame["id"], newChar);
                    ClearEditCharacterPageFields();
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    SwitchPage(Page.EditCharacters);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Все поля обязательны для заполнения", "OK");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Отменить", buttonStyle, GUILayout.Height(40)))
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedCharacter = null;
                SwitchPage(Page.EditCharacters);
            }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawGameDetailPage()
    {
        if (selectedGame == null)
        {
            SwitchPage(Page.Main);
            return;
        }

        GUILayout.BeginArea(new Rect(
            20,
            20,
            position.width - 50,
            position.height - 20
        ));

        // Заголовок
        GUILayout.Label($"Игра: {(string)selectedGame["name"]}", centeredLabelStyle);
        GUILayout.Space(20);

        // Кнопки управления
        if (GUILayout.Button("Назад", buttonStyle, GUILayout.Height(35))) SwitchPage(Page.Main);

        GUILayout.Space(10);

        if (GUILayout.Button("Создать сцену", buttonStyle, GUILayout.Height(35)))
        {
            selectedScene = null;
            SwitchPage(Page.EditScene);
        }

        GUILayout.Space(20);

        GUILayout.BeginHorizontal();

        // Список сцен
        scrollPosition =
            GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        GUILayout.BeginVertical();

        var scenes = selectedGame["scenes"] as JArray;
        if (scenes != null)
            for (var i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                var sceneId = (long)scene["id"];

                // Инициализируем состояние, если нужно
                if (!sceneExpandedStates.ContainsKey(sceneId)) sceneExpandedStates[sceneId] = false;

                GUILayout.BeginVertical();

                // Заголовок сцены
                GUILayout.BeginHorizontal(GUILayout.Height(50));

                // Создаем область для кнопки со стрелкой с вертикальным выравниванием
                GUILayout.BeginVertical(GUILayout.Width(20));
                GUILayout.Space(8.5f);
                // Кнопка раскрытия/скрытия с белой стрелкой
                var arrowContent = new GUIContent(sceneExpandedStates[sceneId] ? "▼" : "►");
                var arrowSize = GUI.skin.button.CalcSize(arrowContent);

                if (GUILayout.Button(arrowContent, arrowButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
                    sceneExpandedStates[sceneId] = !sceneExpandedStates[sceneId];

                GUILayout.EndVertical();

                if (GUILayout.Button((string)scene["name"], cardTitleStyle))
                    sceneExpandedStates[sceneId] = !sceneExpandedStates[sceneId];

                GUILayout.FlexibleSpace();

                // Кнопки управления сценой
                if (GUILayout.Button("+",
                        new GUIStyle(iconButtonStyle) { fontSize = 18, padding = new RectOffset(8, 5, 5, 8) }))
                {
                    selectedScript = null;
                    selectedScene = (JObject)scene;
                    SwitchPage(Page.EditScript);
                }

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle))
                {
                    selectedScene = (JObject)scene;
                    SwitchPage(Page.EditScene);
                }

                if (GUILayout.Button("X", iconButtonStyle))
                    if (EditorUtility.DisplayDialog("Вы уверены?",
                            "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", сцена будет безвозвратно удалена.",
                            "OK", "Отмена"))
                    {
                        StorageApi.GetInstance().DeleteScene((string)selectedGame["id"], sceneId);
                        selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    }

                if (GUILayout.Button(EditorGUIUtility.IconContent("Download-Available"), iconButtonStyle))
                {
                    ExportSceneDialogs((JObject)scene);
                }

                GUILayout.Space(5);

                GUILayout.EndHorizontal();

                // Раскрытая часть сцены
                if (sceneExpandedStates[sceneId])
                {
                    var scripts = scene["scripts"] as JArray;
                    if (scripts.Count == 0)
                    {
                        GUILayout.Label("В сцене еще нет диалогов...", centeredItalicLabelStyle);
                        GUILayout.Space(18);
                    }

                    foreach (var script in scripts)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20); // Отступ для вложенности
                        if (GUILayout.Button((string)script["name"], scriptLabelStyle))
                        {
                            selectedScript = (JObject)script;
                            selectedScene = (JObject)scene;
                            SwitchPage(Page.GraphEditor);
                        }

                        GUILayout.FlexibleSpace();

                        // Кнопки управления диалогом
                        if (GUILayout.Button(EditorGUIUtility.IconContent("d_Settings"), iconButtonStyle))
                        {
                            selectedScript = (JObject)script;
                            selectedScene = (JObject)scene;
                            SwitchPage(Page.EditScript);
                        }

                        if (GUILayout.Button("X", iconButtonStyle))
                            if (EditorUtility.DisplayDialog("Вы уверены?",
                                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", диалог будет безвозвратно удален.",
                                    "OK", "Отмена"))
                            {
                                StorageApi.GetInstance()
                                    .DeleteScript((string)selectedGame["id"], sceneId, (string)script["id"]);
                                selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                            }

                        if (GUILayout.Button(EditorGUIUtility.IconContent("Download-Available"), iconButtonStyle))
                        {
                            ExportDialogue((JObject)script);
                        }

                        GUILayout.Space(3.5f);

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(10);
            }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();
        GUILayout.Space(50);
        GUILayout.EndArea();
    }

    /// <summary>
    /// Экспортирует диалог в JSON файл
    /// </summary>
    private void ExportDialogue(JObject script)
    {
        try
        {
            // Получаем имена персонажей
            var npcName = GetCharacterNameById((string)script["npc"]);
            var heroName = GetCharacterNameById((string)script["main_character"]);

            // Создаем структуру для экспорта
            var exportData = new JArray();

            if (script["result"] != null && script["result"].HasValues)
            {
                var dialogueExport = new JObject
                {
                    ["id"] = script["id"],
                    ["npc_name"] = npcName,
                    ["hero_name"] = heroName,
                    ["data"] = script["result"]["data"]
                };
                exportData.Add(dialogueExport);
            }

            // Предлагаем выбрать место сохранения
            var defaultFileName = $"{script["id"]?.ToString() ?? "dialogue"}.json";
            var defaultFolder = Path.Combine(Application.dataPath, "Resources", "dialogues");

            // Создаем папку по умолчанию, если не существует
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
            }

            var path = EditorUtility.SaveFilePanel(
                "Экспортировать диалог",
                defaultFolder,
                defaultFileName,
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, exportData.ToString(Newtonsoft.Json.Formatting.Indented));
                EditorUtility.DisplayDialog("Успех", "Диалог успешно экспортирован", "OK");

                // Если файл сохранен в папке Assets, обновляем проект
                if (path.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось экспортировать диалог: {e.Message}", "OK");
        }
    }

    /// <summary>
    /// Экспортирует все диалоги сцены в JSON файл
    /// </summary>
    private void ExportSceneDialogs(JObject scene)
    {
        try
        {
            var sceneDialogs = new JArray();
            var scripts = scene["scripts"] as JArray;

            if (scripts == null || scripts.Count == 0)
            {
                EditorUtility.DisplayDialog("Информация", "В сцене нет диалогов для экспорта", "OK");
                return;
            }

            foreach (var script in scripts)
            {
                var scriptObj = (JObject)script;
                var npcName = GetCharacterNameById((string)scriptObj["npc"]);
                var heroName = GetCharacterNameById((string)scriptObj["main_character"]);

                if (scriptObj["result"] != null && scriptObj["result"].HasValues)
                {
                    var dialogueExport = new JObject
                    {
                        ["id"] = script["id"],
                        ["npc_name"] = npcName,
                        ["hero_name"] = heroName,
                        ["data"] = scriptObj["result"]["data"]
                    };
                    sceneDialogs.Add(dialogueExport);
                }
            }

            if (sceneDialogs.Count == 0)
            {
                EditorUtility.DisplayDialog("Информация", "В сцене нет сгенерированных диалогов", "OK");
                return;
            }

            // Предлагаем выбрать место сохранения
            var defaultFileName = $"{scene["id"]?.ToString() ?? "scene"}.json";
            var defaultFolder = Path.Combine(Application.dataPath, "Resources", "dialogues");

            // Создаем папку по умолчанию, если не существует
            if (!Directory.Exists(defaultFolder))
            {
                Directory.CreateDirectory(defaultFolder);
            }

            var path = EditorUtility.SaveFilePanel(
                "Экспортировать диалоги сцены",
                defaultFolder,
                defaultFileName,
                "json"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, sceneDialogs.ToString(Newtonsoft.Json.Formatting.Indented));
                EditorUtility.DisplayDialog("Успех", $"Экспортировано {sceneDialogs.Count} диалогов", "OK");

                // Если файл сохранен в папке Assets, обновляем проект
                if (path.StartsWith(Application.dataPath))
                {
                    AssetDatabase.Refresh();
                }
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не удалось экспортировать диалоги: {e.Message}", "OK");
        }
    }

    /// <summary>
    /// Получает имя персонажа по ID
    /// </summary>
    private string GetCharacterNameById(string characterId)
    {
        if (selectedGame == null || string.IsNullOrEmpty(characterId))
            return "Неизвестный персонаж";

        var characters = selectedGame["characters"] as JArray;
        if (characters == null) return "Неизвестный персонаж";

        var character = characters.FirstOrDefault(c => (string)c["id"] == characterId);
        return character?["name"]?.ToString() ?? "Неизвестный персонаж";
    }

    private void DrawEditScenePage()
    {
        if (!pageInitialized)
        {
            if (selectedScene != null)
            {
                editSceneName = (string)selectedScene["name"];
                editSceneDescription = (string)selectedScene["description"];
                editSceneCharacters = (JArray)selectedScene["characters"];
            }
            else
            {
                ClearEditScenePageFields();
            }
        }

        pageInitialized = true;

        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));

        GUILayout.BeginVertical();
        GUILayout.Label("Сцена", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную высоту для ScrollView
        var availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

        // Начинаем ScrollView с фиксированной высотой
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(availableHeight));

        GUILayout.Label("Название", fieldLabelStyle);
        editSceneName = EditorGUILayout.TextField(editSceneName, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Характеристики сцены", fieldLabelStyle);
        editSceneDescription = EditorGUILayout.TextArea(editSceneDescription, textAreaStyle); // Фиксированная высота для текстового поля

        GUILayout.Space(15);

        GUILayout.Label("Персонажи", fieldLabelStyle);
        var characters = StorageApi.GetInstance().GetCharactersArray(selectedGame);
        if (characters.Count == 0) GUILayout.Label("В игре еще нет персонажей...", centeredItalicLabelStyle);
        foreach (var character in characters)
        {
            GUILayout.BeginHorizontal();
            var charId = (string)character["id"];
            var isPresent = editSceneCharacters.Any(c => (string)c == charId);
            var newValue = EditorGUILayout.Toggle(isPresent, GUILayout.Width(20));

            if (newValue != isPresent)
            {
                if (newValue)
                {
                    editSceneCharacters.Add((string)character["id"]);
                }
                else
                {
                    var toRemove = editSceneCharacters.FirstOrDefault(c => (string)c == charId);
                    if (toRemove != null)
                        editSceneCharacters.Remove(toRemove);
                }
            }

            GUILayout.Label((string)character["name"]);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(15);

        GUILayout.EndScrollView();

        GUILayout.Space(15);

        GUILayout.Space(5);

        if (GUILayout.Button("Сохранить", buttonStyle, GUILayout.Height(40)))
        { 
            if (!string.IsNullOrEmpty(editSceneDescription) && !string.IsNullOrEmpty(editSceneName) &&
                editSceneCharacters.Count > 0)
            {
                if (selectedScene != null)
                {
                    var updScene = new JObject
                    {
                        ["name"] = editSceneName,
                        ["description"] = editSceneDescription,
                        ["characters"] = editSceneCharacters
                    };
                    StorageApi.GetInstance()
                        .UpdateScene((string)selectedGame["id"], (long)selectedScene["id"], updScene);
                    ClearEditScenePageFields();
                    selectedScene = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    SwitchPage(Page.GameDetail);
                }
                else
                {
                    var newScene = new JObject
                    {
                        ["id"] = DateTimeOffset.Now.ToUnixTimeSeconds(),
                        ["name"] = editSceneName,
                        ["scripts"] = new JArray(),
                        ["characters"] = editSceneCharacters,
                        ["description"] = editSceneDescription
                    };
                    StorageApi.GetInstance().AddScene((string)selectedGame["id"], newScene);
                    ClearEditScenePageFields();
                    selectedScene = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    SwitchPage(Page.GameDetail);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Все поля обязательны для заполнения", "OK");
            }
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Отменить", buttonStyle, GUILayout.Height(40)))
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedScene = null;
                ClearEditScenePageFields();
                SwitchPage(Page.GameDetail);
            }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawEditScriptPage()
    {
        if (!pageInitialized)
        {
            if (selectedScript != null)
            {
                editScriptName = (string)selectedScript["name"] ?? "";
                editScriptMinMainChar = (int)(selectedScript["answers_from_m"] ?? 1);
                editScriptMaxMainChar = (int)(selectedScript["answers_to_m"] ?? 3);
                editScriptMinDepth = (int)(selectedScript["answers_from_n"] ?? 1);
                editScriptMaxDepth = (int)(selectedScript["answers_to_n"] ?? 3);
                selectedMainCharacterId = (string)selectedScript["main_character"] ?? "";
                selectedNPCId = (string)selectedScript["npc"] ?? "";
                editScriptDescription = (string)selectedScript["description"] ?? "";
                
                // Безопасная инициализация infoData
                var infoData = selectedScript["infoData"] as JObject ?? new JObject();
                playerGetsInfo = (bool)(infoData["gets"] ?? false);
                playerGetsInfoName = (string)(infoData["name"] ?? "");
                playerGetsInfoCondition = (string)(infoData["condition"] ?? "");
                
                // Безопасная инициализация itemData
                var itemData = selectedScript["itemData"] as JObject ?? new JObject();
                playerGetsItem = (bool)(itemData["gets"] ?? false);
                playerGetsItemName = (string)(itemData["name"] ?? "");
                playerGetsItemCondition = (string)(itemData["condition"] ?? "");
                
                editScriptAdditional = (string)selectedScript["additional"] ?? "";
                
                // Безопасная инициализация отношений
                var toMainCharacterRelations = (string)selectedScript["to_main_character_relations"] ?? scriptCharacterAttitude[0];
                var toNpcRelations = (string)selectedScript["to_npc_relations"] ?? scriptCharacterAttitude[0];
                
                toMainCharacterRelation = Array.IndexOf(scriptCharacterAttitude, toMainCharacterRelations);
                if (toMainCharacterRelation == -1) toMainCharacterRelation = 0;
                
                toNpcRelation = Array.IndexOf(scriptCharacterAttitude, toNpcRelations);
                if (toNpcRelation == -1) toNpcRelation = 0;
            }
            else
            {
                ClearEditScriptPageFields();
            }
        }

        pageInitialized = true;

        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));

        GUILayout.BeginVertical();
        GUILayout.Label("Диалог", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную высоту для ScrollView
        var availableHeight = CalculateAvailableHeight();

        // Начинаем ScrollView с фиксированной высотой
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(availableHeight));

        GUILayout.Label("Название", fieldLabelStyle);
        editScriptName = EditorGUILayout.TextField(editScriptName, textFieldStyle);

        GUILayout.Space(30);

        GUILayout.Label("Ответы главного персонажа: от", fieldLabelStyle);
        editScriptMinMainChar = EditorGUILayout.IntField(editScriptMinMainChar, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("до", fieldLabelStyle);
        editScriptMaxMainChar = EditorGUILayout.IntField(editScriptMaxMainChar, textFieldStyle);

        GUILayout.Space(30);

        GUILayout.Label("Глубина дерева диалогов: от", fieldLabelStyle);
        editScriptMinDepth = EditorGUILayout.IntField(editScriptMinDepth, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("до", fieldLabelStyle);
        editScriptMaxDepth = EditorGUILayout.IntField(editScriptMaxDepth, textFieldStyle);

        GUILayout.Space(30);

        GUILayout.Label("Персонажи", centeredSmallLabelStyle);
        GUILayout.Space(5);
        GUILayout.Label(" Главный персонаж", cardTitleStyle);
        GUILayout.Space(5);
        
        var sceneCharacterIds = ((JArray)selectedScene["characters"] ?? new JArray()).ToObject<string[]>();
        var availableCharacters = ((JArray)selectedGame["characters"] ?? new JArray())
            .Where(c => sceneCharacterIds.Contains((string)c["id"]))
            .ToArray();

        // Создаем массивы для отображения и соответствия
        var characterNames = availableCharacters
            .Select(c => (string)c["name"])
            .ToArray();

        var characterIds = availableCharacters
            .Select(c => (string)c["id"])
            .ToArray();

        // Находим текущий индекс выбранного персонажа
        var selectedMainCharacterIndex = Array.IndexOf(characterIds, selectedMainCharacterId);
        if (selectedMainCharacterIndex == -1) selectedMainCharacterIndex = 0;

        var selectedNPCIndex = Array.IndexOf(characterIds, selectedNPCId);
        if (selectedNPCIndex == -1) selectedNPCIndex = 0;

        // Отрисовка dropdown
        selectedMainCharacterIndex = EditorGUILayout.Popup(selectedMainCharacterIndex, characterNames);
        selectedMainCharacterId = characterIds[selectedMainCharacterIndex];
        GUILayout.Space(5);
        toNpcRelation = EditorGUILayout.Popup("Отношение к NPC", toNpcRelation, scriptCharacterAttitude);
        GUILayout.Space(5);
        GUILayout.Label(" NPC", cardTitleStyle);
        GUILayout.Space(5);
        
        // Отрисовка dropdown
        selectedNPCIndex = EditorGUILayout.Popup(selectedNPCIndex, characterNames);
        selectedNPCId = characterIds[selectedNPCIndex];
        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Отношение к главному персонажу");
        toMainCharacterRelation = EditorGUILayout.Popup(toMainCharacterRelation, scriptCharacterAttitude);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(15);
        GUILayout.Label("Краткое содержание", centeredSmallLabelStyle);
        GUILayout.Space(15);
        editScriptDescription = EditorGUILayout.TextArea(editScriptDescription, textAreaStyle);
        GUILayout.Space(30);

        // Сохраняем предыдущие значения перед изменением
        bool previousPlayerGetsItem = playerGetsItem;
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Персонаж получит предмет");
        playerGetsItem = EditorGUILayout.Toggle(playerGetsItem, GUILayout.Width(20));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Очищаем поля только если значение изменилось с true на false
        if (previousPlayerGetsItem && !playerGetsItem)
        {
            playerGetsItemName = "";
            playerGetsItemCondition = "";
        }

        EditorGUI.BeginDisabledGroup(!playerGetsItem);
        GUILayout.Label("Предмет", fieldLabelStyle);
        playerGetsItemName = EditorGUILayout.TextField(playerGetsItemName, textFieldStyle);
        GUILayout.Space(15);
        GUILayout.Label("Условие достижения", fieldLabelStyle);
        playerGetsItemCondition = EditorGUILayout.TextField(playerGetsItemCondition, textFieldStyle);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(30);

        // Сохраняем предыдущие значения перед изменением
        bool previousPlayerGetsInfo = playerGetsInfo;
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Персонаж получит информацию");
        playerGetsInfo = EditorGUILayout.Toggle(playerGetsInfo, GUILayout.Width(20));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Очищаем поля только если значение изменилось с true на false
        if (previousPlayerGetsInfo && !playerGetsInfo)
        {
            playerGetsInfoName = "";
            playerGetsInfoCondition = "";
        }

        EditorGUI.BeginDisabledGroup(!playerGetsInfo);
        GUILayout.Label("Информация", fieldLabelStyle);
        playerGetsInfoName = EditorGUILayout.TextField(playerGetsInfoName, textFieldStyle);
        GUILayout.Space(15);
        GUILayout.Label("Условие достижения", fieldLabelStyle);
        playerGetsInfoCondition = EditorGUILayout.TextField(playerGetsInfoCondition, textFieldStyle);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(30);
        GUILayout.Label("Дополнительно", centeredSmallLabelStyle);
        GUILayout.Space(15);
        editScriptAdditional = EditorGUILayout.TextArea(editScriptAdditional, textAreaStyle);

        GUILayout.Space(15);

        GUILayout.EndScrollView();

        GUILayout.Space(15);

        // КНОПКИ - разделяем логику для нового и существующего скрипта
        if (selectedScript == null)
        {
            // Для нового скрипта - только одна кнопка "Сохранить и сгенерировать"
            if (GUILayout.Button("Сохранить и сгенерировать", buttonStyle, GUILayout.Height(40)))
            {
                if (ValidateScriptFields())
                {
                    SaveAndGenerateScript();
                }
            }
        }
        else
        {
            // Для существующего скрипта - две кнопки
            GUILayout.BeginHorizontal();
        
            if (GUILayout.Button("Сохранить", buttonStyle, GUILayout.Height(40)))
            {
                if (ValidateScriptFields())
                {
                    SaveScriptOnly();
                }
            }
        
            if (GUILayout.Button("Перегенерировать", buttonStyle, GUILayout.Height(40)))
            {
                if (ValidateScriptFields())
                {
                    SaveAndGenerateScript();
                }
            }
        
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Отменить", buttonStyle, GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedScript = null;
                ClearEditScriptPageFields();
                SwitchPage(Page.GameDetail);
            }
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    private bool ValidateScriptFields()
    {
        if (string.IsNullOrEmpty(editScriptName) || 
            editScriptMinMainChar <= 0 || 
            editScriptMaxMainChar <= 0 ||
            editScriptMinDepth <= 0 || 
            editScriptMaxDepth <= 0 ||
            string.IsNullOrEmpty(editScriptDescription))
        {
            EditorUtility.DisplayDialog("Ошибка", "Все поля обязательны для заполнения", "OK");
            return false;
        }
        
        if (editScriptMinMainChar > editScriptMaxMainChar)
        {
            EditorUtility.DisplayDialog("Ошибка", "Минимальное количество ответов не может быть больше максимального", "OK");
            return false;
        }
        
        if (editScriptMinDepth > editScriptMaxDepth)
        {
            EditorUtility.DisplayDialog("Ошибка", "Минимальная глубина не может быть больше максимальной", "OK");
            return false;
        }
        
        return true;
    }

    private void SaveScriptOnly()
    {
        var scriptData = CreateScriptData(selectedScript["result"] as JObject);
        
        if (selectedScript != null)
        {
            // Обновляем существующий скрипт
            StorageApi.GetInstance().UpdateScript((string)selectedGame["id"], (long)selectedScene["id"],
                (string)selectedScript["id"], scriptData);
        }
        else
        {
            // Создаем новый скрипт
            scriptData["id"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            scriptData["result"] = new JObject();
            StorageApi.GetInstance().AddScript((string)selectedGame["id"], (long)selectedScene["id"], scriptData);
            selectedScript = scriptData;
        }
        
        // Обновляем данные
        selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
        selectedScene = StorageApi.GetInstance().GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
        selectedScript = null;
        
        EditorUtility.DisplayDialog("Успех", "Диалог сохранен", "OK");
        SwitchPage(Page.GameDetail);
    }

    private void SaveAndGenerateScript()
    {
        var scriptData = selectedScript != null ? CreateScriptData(selectedScript["result"] as JObject) : CreateScriptData(null);
        
        // Сначала сохраняем скрипт
        if (selectedScript != null)
        {
            StorageApi.GetInstance().UpdateScript((string)selectedGame["id"], (long)selectedScene["id"],
                (string)selectedScript["id"], scriptData);
        }
        else
        {
            scriptData["id"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            scriptData["result"] = new JObject();
            StorageApi.GetInstance().AddScript((string)selectedGame["id"], (long)selectedScene["id"], scriptData);
            selectedScript = scriptData;
        }
        
        // Затем генерируем диалог
        GenerateDialogue();
    }

    private JObject CreateScriptData(JObject existingScript)
    {
        return new JObject
        {
            ["name"] = editScriptName,
            ["answers_from_m"] = editScriptMinMainChar,
            ["answers_to_m"] = editScriptMaxMainChar,
            ["answers_from_n"] = editScriptMinDepth,
            ["answers_to_n"] = editScriptMaxDepth,
            ["main_character"] = selectedMainCharacterId,
            ["npc"] = selectedNPCId,
            ["to_main_character_relations"] = scriptCharacterAttitude[toMainCharacterRelation],
            ["to_npc_relations"] = scriptCharacterAttitude[toNpcRelation],
            ["description"] = editScriptDescription,
            ["infoData"] = new JObject
            {
                ["gets"] = playerGetsInfo,
                ["name"] = playerGetsInfoName,
                ["condition"] = playerGetsInfoCondition
            },
            ["itemData"] = new JObject
            {
                ["gets"] = playerGetsItem,
                ["name"] = playerGetsItemName,
                ["condition"] = playerGetsItemCondition
            },
            ["additional"] = editScriptAdditional,
            ["result"] = existingScript ?? new JObject()
        };
    }

    private void GenerateDialogue()
    {
        // Получаем данные персонажей
        var npcCharacter = GetCharacterById(selectedNPCId);
        var heroCharacter = GetCharacterById(selectedMainCharacterId);
        
        if (npcCharacter == null || heroCharacter == null)
        {
            EditorUtility.DisplayDialog("Ошибка", "Не удалось найти данные персонажей", "OK");
            return;
        }

        // Формируем цели
        var goals = new JArray();
        
        if (playerGetsInfo)
        {
            goals.Add(new JObject
            {
                ["type"] = "info",
                ["object"] = playerGetsInfoName,
                ["condition"] = playerGetsInfoCondition
            });
        }
        
        if (playerGetsItem)
        {
            goals.Add(new JObject
            {
                ["type"] = "item", 
                ["object"] = playerGetsItemName,
                ["condition"] = playerGetsItemCondition
            });
        }

        // Формируем данные для запроса
        var requestData = new JObject
        {
            ["npc"] = new JObject
            {
                ["name"] = npcCharacter["name"]?.ToString() ?? "",
                ["profession"] = npcCharacter["profession"]?.ToString() ?? "",
                ["talk_style"] = npcCharacter["talk_style"]?.ToString() ?? "",
                ["traits"] = npcCharacter["traits"]?.ToString() ?? "",
                ["look"] = npcCharacter["look"]?.ToString() ?? "",
                ["extra"] = npcCharacter["extra"]?.ToString() ?? ""
            },
            ["hero"] = new JObject
            {
                ["name"] = heroCharacter["name"]?.ToString() ?? "",
                ["profession"] = heroCharacter["profession"]?.ToString() ?? "",
                ["talk_style"] = heroCharacter["talk_style"]?.ToString() ?? "",
                ["traits"] = heroCharacter["traits"]?.ToString() ?? "",
                ["look"] = heroCharacter["look"]?.ToString() ?? "",
                ["extra"] = heroCharacter["extra"]?.ToString() ?? ""
            },
            ["world_settings"] = selectedGame["description"]?.ToString() ?? "",
            ["NPC_to_hero_relation"] = scriptCharacterAttitude[toNpcRelation],
            ["hero_to_NPC_relation"] = scriptCharacterAttitude[toMainCharacterRelation],
            ["mx_answers_cnt"] = editScriptMaxMainChar,
            ["mn_answers_cnt"] = editScriptMinMainChar,
            ["mx_depth"] = editScriptMaxDepth,
            ["mn_depth"] = editScriptMinDepth,
            ["scene"] = selectedScene["description"]?.ToString() ?? "",
            ["genre"] = selectedGame["genre"]?.ToString() ?? "",
            ["epoch"] = selectedGame["techLevel"]?.ToString() ?? "",
            ["tonality"] = selectedGame["tonality"]?.ToString() ?? "",
            ["extra"] = editScriptAdditional ?? "",
            ["context"] = editScriptDescription ?? "",
            ["goals"] = goals,
            ["game_id"] = selectedGame["id"]?.ToString() ?? "",
            ["scene_id"] = selectedScene["id"]?.ToString() ?? "",
            ["script_id"] = selectedScript["id"]?.ToString() ?? ""
        };
        
        selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
        selectedScene = StorageApi.GetInstance().GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
        selectedScript = StorageApi.GetInstance().GetScriptById((string)selectedGame["id"], (long)selectedScene["id"], (string)selectedScript["id"]);
        SwitchPage(Page.GraphEditor);

        // Отправляем запрос на генерацию
        BackendApi.GenerateDialogue(requestData, (success, response) =>
        {
            if (success)
            {
                EditorUtility.DisplayDialog("Успех", "Диалог успешно сгенерирован. Чтобы увидеть результат, перезагрузите диалог", "OK");
            }
            else
            {
                string errorMessage = response?["message"]?.ToString() ?? "Неизвестная ошибка";
                EditorUtility.DisplayDialog("Ошибка генерации", $"Не удалось сгенерировать диалог: {errorMessage}", "OK");
            }
        });
    }

    private JObject GetCharacterById(string characterId)
    {
        if (string.IsNullOrEmpty(characterId) || selectedGame == null)
            return null;
            
        var characters = StorageApi.GetInstance().GetCharactersArray(selectedGame);
        return characters?.FirstOrDefault(c => (string)c["id"] == characterId) as JObject;
    }

    private void DrawGraphEditorPage()
    {
        if (!pageInitialized)
        {
            // Проверяем, есть ли уже расположенные узлы
            bool needsLayout = selectedScript != null &&
                               selectedScript["result"] != null &&
                               selectedScript["result"]["data"] != null &&
                               ((JArray)selectedScript["result"]["data"]).Count > 0 &&
                               !HasNodePositions((JArray)selectedScript["result"]["data"]);

            if (needsLayout)
            {
                AutoLayoutDAG();
            }

            pageInitialized = true;
        }

        // Сначала обрабатываем ввод
        HandleGraphInput();

        // Разделяем область на две части: управление и график
        float controlPanelHeight = 185f;

        // Панель управления
        GUILayout.BeginVertical(GUILayout.Height(controlPanelHeight));
        DrawGraphControlPanel();
        GUILayout.EndVertical();

        // Область графа
        graphRect = new Rect(
            0,
            controlPanelHeight,
            position.width,
            position.height - controlPanelHeight
        );

        // Отображаем график
        GUI.BeginClip(graphRect);
        DrawGraphContent();
        GUI.EndClip();

        // Окно редактирования узла (рисуется поверх всего)
        if (showNodeEditor && editingNode != null)
            DrawNodeEditorWindow();

        // Окно редактирования связи (рисуется поверх всего)
        if (showLinkEditor && editingLink != null)
            DrawLinkEditorWindow();
    }

    private bool HasNodePositions(JArray nodes)
    {
        foreach (JObject node in nodes)
        {
            if (node["meta"]?["x"] == null || node["meta"]?["y"] == null)
                return false;
        }

        return true;
    }

    // Метод для создания снимка состояния
    private void TakeSnapshot()
    {
        if (isTakingSnapshot || selectedScript == null) return;

        isTakingSnapshot = true;

        // Создаем глубокую копию текущего состояния
        var snapshot = DeepCopy(selectedScript["result"] as JObject);
        undoStack.Push(snapshot);

        // Очищаем стек redo при новом действии
        redoStack.Clear();

        isTakingSnapshot = false;
    }

// Метод для глубокого копирования JObject
    private JObject DeepCopy(JObject original)
    {
        if (original == null) return null;
        return JObject.Parse(original.ToString());
    }

// Метод отмены
    private void Undo()
    {
        if (undoStack.Count == 0) return;

        // Сохраняем текущее состояние в redo stack
        if (selectedScript != null && selectedScript["result"] != null)
        {
            redoStack.Push(DeepCopy(selectedScript["result"] as JObject));
        }

        // Восстанавливаем предыдущее состояние
        var previousState = undoStack.Pop();
        if (selectedScript != null)
        {
            selectedScript["result"] = previousState;
            Repaint();
        }
    }

// Метод возврата
    private void Redo()
    {
        if (redoStack.Count == 0) return;

        // Сохраняем текущее состояние в undo stack
        if (selectedScript != null && selectedScript["result"] != null)
        {
            undoStack.Push(DeepCopy(selectedScript["result"] as JObject));
        }

        // Восстанавливаем отмененное состояние
        var nextState = redoStack.Pop();
        if (selectedScript != null)
        {
            selectedScript["result"] = nextState;
            Repaint();
        }
    }

    private void DrawNodeEditorWindow()
    {
        if (editingNode == null) return;

        if (!windowInitialized)
        {
            nodeEditText = editingNode["line"]?.ToString() ?? "";
            nodeEditItem = (int)(editingNode["goal_achieved"]?["info"] ?? -1);
            windowInitialized = true;
        }

        // Создаем затемнение фона
        var backgroundColor = new Color(0, 0, 0, 0.5f);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        // Окно редактирования узла
        var windowRect = new Rect(position.width / 2 - 190, position.height / 2 - 190, 380, 380);

        // Рисуем фон окна
        var windowBackground = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.98f, 0.98f, 0.98f);
        EditorGUI.DrawRect(windowRect, windowBackground);

        GUILayout.BeginArea(windowRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        GUILayout.Space(10);

        GUILayout.Label("Редактирование реплики NPC", headerStyle);
        GUILayout.Space(10);

        var labelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        GUILayout.Label("Текст реплики:", labelStyle);
        GUILayout.Space(5);

        editingNodeScroll = GUILayout.BeginScrollView(editingNodeScroll, GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(100));
        nodeEditText = EditorGUILayout.TextArea(nodeEditText, textAreaStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("ID получаемого предмета:", labelStyle);
        nodeEditItem = EditorGUILayout.IntField(nodeEditItem);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Сохранить"))
        {
            TakeSnapshot();
            editingNode["line"] = nodeEditText;
            editingNode["goal_achieved"]["info"] = nodeEditItem;
            showNodeEditor = false;
            windowInitialized = false;
            Repaint();
        }

        if (GUILayout.Button("Отменить"))
        {
            showNodeEditor = false;
            windowInitialized = false;
            Repaint();
        }

        // Новая кнопка удаления
        if (GUILayout.Button("Удалить"))
        {
            if (EditorUtility.DisplayDialog("Удалить узел?",
                    "Вы уверены, что хотите удалить этот узел и все связанные с ним связи?", "Да", "Отмена"))
            {
                TakeSnapshot();
                DeleteNode(editingNode);
                showNodeEditor = false;
                windowInitialized = false;
                Repaint();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        // Обработка закрытия окна по клику вне области
        if (Event.current.type == EventType.MouseDown && !windowRect.Contains(Event.current.mousePosition))
        {
            showNodeEditor = false;
            windowInitialized = false;
            Event.current.Use();
            Repaint();
        }
    }

    private void DrawLinkEditorWindow()
    {
        if (editingLink == null) return;

        if (!windowInitialized)
        {
            lineEditText = editingLink["line"]?.ToString() ?? "";
            lineEditInfo = editingLink["info"]?.ToString() ?? "";
            windowInitialized = true;
        }

        // Создаем затемнение фона
        var backgroundColor = new Color(0, 0, 0, 0.5f);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        // Окно редактирования связи
        var windowRect = new Rect(position.width / 2 - 190, position.height / 2 - 220, 380, 440);

        // Рисуем фон окна
        var windowBackground = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.98f, 0.98f, 0.98f);
        EditorGUI.DrawRect(windowRect, windowBackground);

        GUILayout.BeginArea(windowRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);
        GUILayout.BeginVertical();

        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        GUILayout.Space(10);

        GUILayout.Label("Редактирование реплики\nглавного персонажа", headerStyle);
        GUILayout.Space(10);

        var labelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        GUILayout.Label("Текст реплики:", labelStyle);
        GUILayout.Space(5);

        // TextArea с запретом горизонтальной прокрутки
        linkEditorScroll = GUILayout.BeginScrollView(linkEditorScroll, GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(80));
        lineEditText = EditorGUILayout.TextArea(lineEditText, textAreaStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label("Сокращенный текст (отображается на кнопках):", labelStyle);
        GUILayout.Space(5);

        // Второй TextArea с запретом горизонтальной прокрутки
        lineEditInfoScroll = GUILayout.BeginScrollView(lineEditInfoScroll, GUIStyle.none, GUI.skin.verticalScrollbar,
            GUILayout.Height(80));
        lineEditInfo = EditorGUILayout.TextArea(lineEditInfo, textAreaStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(15);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Сохранить"))
        {
            TakeSnapshot();
            editingLink["line"] = lineEditText;
            editingLink["info"] = lineEditInfo;
            showLinkEditor = false;
            windowInitialized = false;
            Repaint();
        }

        if (GUILayout.Button("Отменить"))
        {
            showLinkEditor = false;
            windowInitialized = false;
            Repaint();
        }

        // Новая кнопка удаления
        if (GUILayout.Button("Удалить"))
        {
            if (EditorUtility.DisplayDialog("Удалить связь?",
                    "Вы уверены, что хотите удалить эту связь?", "Да", "Отмена"))
            {
                TakeSnapshot();
                DeleteLink(editingLinkSourceNode, editingLink);
                showLinkEditor = false;
                windowInitialized = false;
                Repaint();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(10);
        GUILayout.EndVertical();
        GUILayout.Space(10);
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        // Обработка закрытия окна по клику вне области
        if (Event.current.type == EventType.MouseDown && !windowRect.Contains(Event.current.mousePosition))
        {
            showLinkEditor = false;
            windowInitialized = false;
            Event.current.Use();
            Repaint();
        }
    }

    private void DrawGraphControlPanel()
    {
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.Space(5);
        GUILayout.BeginVertical();
        GUILayout.Space(5);

        // Кнопка "Назад"
        if (GUILayout.Button("Назад", buttonStyle, GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Вы уверены?", "Все несохраненные изменения будут сброшены!", "Да, выйти",
                    "Отмена"))
            {
                selectedScript = null;
                selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                selectedScene = StorageApi.GetInstance()
                    .GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
                SwitchPage(Page.GameDetail);
            }
        }

        GUILayout.Space(5);

        if (selectedScript != null)
        {
            GUILayout.Label(selectedScript["name"]?.ToString(), centeredLabelStyle);
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            // Кнопки undo/redo
            EditorGUI.BeginDisabledGroup(undoStack.Count == 0);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_back"), iconButtonStyle))
            {
                Undo();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(redoStack.Count == 0);
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_forward"), iconButtonStyle))
            {
                Redo();
            }

            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            // Кнопка создания новой ноды
            if (GUILayout.Button("Создать фразу", lowButtonStyle))
            {
                TakeSnapshot();
                // Создаем ноду в центре видимой области
                var center = new Vector2(graphRect.width / 2, graphRect.height / 2);
                var graphCenter = (center - graphPanOffset) / graphZoom;
                var newNode = CreateNewNode(graphCenter);
                selectedNode = newNode;
            }

            GUILayout.FlexibleSpace();

            // Кнопка "Сохранить"
            if (GUILayout.Button("Сохранить", lowButtonStyle, GUILayout.Height(30)))
            {
                StorageApi.GetInstance().UpdateScript(
                    (string)selectedGame["id"],
                    (long)selectedScene["id"],
                    (string)selectedScript["id"],
                    selectedScript
                );
                EditorUtility.DisplayDialog("Успех", "Изменения сохранены", "OK");
            }

            GUILayout.FlexibleSpace();

            // Кнопка авто-расположения
            if (GUILayout.Button("Авторасстановка", lowButtonStyle, GUILayout.Height(30)))
            {
                TakeSnapshot();
                AutoLayoutDAG();
            }

            GUILayout.FlexibleSpace();

            // В методе DrawGraphControlPanel() замените код кнопки обновления:
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), iconButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Обновить данные?", 
                        "Вы хотите загрузить последнюю версию данных с сервера? Все несохраненные локальные изменения будут потеряны.", 
                        "Да, обновить", "Отмена"))
                {
                    SyncDataFromServer();
                }
            }

            GUILayout.EndHorizontal();

            // Панель управления выделенной нодой
            if (selectedNode != null)
            {
                GUILayout.Space(5);

                GUILayout.BeginHorizontal();

                // Кнопка редактирования ноды
                if (GUILayout.Button("Редактировать", lowButtonStyle, GUILayout.Height(25)))
                {
                    windowInitialized = false;
                    editingNode = selectedNode;
                    showNodeEditor = true;
                    editingNodeScroll = Vector2.zero;
                }

                // Кнопка создания связи
                if (GUILayout.Button("Создать связь", lowButtonStyle, GUILayout.Height(25)))
                {
                    isCreatingLink = true;
                    linkCreationSource = selectedNode;
                }

                // Кнопка создания дочерней ноды
                if (GUILayout.Button("Создать дочернюю", lowButtonStyle, GUILayout.Height(25)))
                {
                    TakeSnapshot();
                    // Создаем новую ноду со смещением от выбранной
                    var nodePos = GetNodePosition(selectedNode) + new Vector2(200, 0);
                    var newNode = CreateNewNode(nodePos);

                    if (newNode != null)
                    {
                        // Автоматически создаем связь от выбранной ноды к новой
                        var newLink = new JObject
                        {
                            ["id"] = (int)newNode["id"],
                            ["line"] = "Новая реплика",
                            ["info"] = "Новая реплика"
                        };

                        if (selectedNode["to"] == null)
                            selectedNode["to"] = new JArray();

                        ((JArray)selectedNode["to"]).Add(newLink);

                        selectedNode = newNode;
                    }
                }

                // Кнопка удаления ноды
                if (GUILayout.Button("Удалить", lowButtonStyle, GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Удалить ноду?",
                            "Вы уверены, что хотите удалить эту ноду и все связанные с ней связи?", "Да", "Отмена"))
                    {
                        TakeSnapshot();
                        DeleteNode(selectedNode);
                        selectedNode = null;
                    }
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            else
            {
                GUILayout.Space(5);
                GUILayout.Label("Здесь появятся кнопки для управления выбранной фразой...", centeredItalicLabelStyle,
                    GUILayout.Height(25));
                GUILayout.Space(5);
            }
        }

        GUILayout.Space(5);
        GUILayout.EndVertical();
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }
    
    private void SyncDataFromServer()
    {
        if (!StorageApi.GetInstance().IsLoggedIn())
        {
            EditorUtility.DisplayDialog("Ошибка", "Пользователь не авторизован", "OK");
            return;
        }

        // Показываем индикатор загрузки
        EditorUtility.DisplayProgressBar("Синхронизация", "Загрузка данных с сервера...", 0.5f);

        try
        {
            // Получаем актуальные данные с сервера
            BackendApi.GetUserDataFromServer((success, serverData) =>
            {
                EditorUtility.ClearProgressBar();
                
                if (success)
                {
                    try
                    {
                        // Загружаем текущий локальный файл
                        var fullJson = StorageApi.GetInstance().LoadFullJson();
                        
                        // Сохраняем токен и ID пользователя, заменяем только данные
                        var currentUser = fullJson["user"];
                        currentUser["data"] = serverData["data"];
                        
                        // Сохраняем обновленный файл
                        StorageApi.GetInstance().SetDataString(StorageApi.Serialize(fullJson));
                        
                        // Перезагружаем текущую игру, сцену и скрипт из обновленных данных
                        selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                        selectedScene = StorageApi.GetInstance().GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
                        selectedScript = StorageApi.GetInstance().GetScriptById((string)selectedGame["id"], (long)selectedScene["id"], (string)selectedScript["id"]);

                        ResetGraphView();
                        
                        // Очищаем стеки при перезагрузке
                        undoStack.Clear();
                        redoStack.Clear();
                        selectedNode = null;
                        
                        EditorUtility.DisplayDialog("Успех", "Данные успешно загружены с сервера", "OK");
                        Repaint();
                    }
                    catch (Exception e)
                    {
                        EditorUtility.DisplayDialog("Ошибка", $"Ошибка при обновлении локальных данных: {e.Message}", "OK");
                    }
                }
                else
                {
                    string errorMessage = serverData?["message"]?.ToString() ?? "Неизвестная ошибка";
                    EditorUtility.DisplayDialog("Ошибка синхронизации", 
                        $"Не удалось загрузить данные с сервера: {errorMessage}", "OK");
                }
            });
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Ошибка", $"Ошибка при синхронизации: {e.Message}", "OK");
        }
    }

    private void HandleGraphInput()
    {
        if (showNodeEditor || showLinkEditor)
            return;

        var e = Event.current;
        var mousePos = e.mousePosition;

        // Проверяем, находится ли курсор в области графа
        if (!graphRect.Contains(mousePos))
            return;

        if (e.type == EventType.MouseMove)
        {
            if (isCreatingLink)
            {
                Repaint();
            }
        }

        // Преобразуем координаты мыши в координаты графа
        var graphMousePos = (mousePos - graphRect.position - graphPanOffset) / graphZoom;

        // Масштабирование колесом мыши
        if (e.type == EventType.ScrollWheel)
        {
            var zoomChange = -e.delta.y * 0.01f;
            var oldZoom = graphZoom;
            graphZoom = Mathf.Clamp(graphZoom + zoomChange, MIN_ZOOM, MAX_ZOOM);

            // Корректируем позицию для сохранения точки под курсором
            var localMousePos = (mousePos - graphRect.position - graphPanOffset) / oldZoom;
            graphPanOffset = mousePos - graphRect.position - localMousePos * graphZoom;

            e.Use();
            Repaint();
            return;
        }

        // Начало панорамирования
        if (e.type == EventType.MouseDown && (e.button == 1 || e.button == 2))
        {
            isPanning = true;
            panStart = mousePos;
            lastMousePosition = mousePos;
            e.Use();
            return;
        }

        // Панорамирование
        if (e.type == EventType.MouseDrag && isPanning)
        {
            graphPanOffset += e.delta;
            e.Use();
            Repaint();
            return;
        }

        // Завершение панорамирования
        if (e.type == EventType.MouseUp && (e.button == 1 || e.button == 2))
        {
            isPanning = false;
            e.Use();
            return;
        }

        // Обработка создания связей
        if (isCreatingLink && e.type == EventType.MouseDown && e.button == 0)
        {
            HandleLinkCreation(graphMousePos);
            e.Use();
            return;
        }

        // Обработка кликов по узлам
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Снимаем выделение при клике на пустое место
            var clickedNode = GetNodeAtPosition(graphMousePos);
            if (clickedNode == null)
            {
                selectedNode = null;
                e.Use();
                Repaint();
                return;
            }

            // Двойной клик - редактирование
            if (e.clickCount == 2)
            {
                HandleDoubleClick(graphMousePos);
                e.Use();
            }
            else // Одинарный клик - выделение и начало перетаскивания
            {
                TakeSnapshot();
                selectedNode = clickedNode;
                HandleNodeSelection(graphMousePos);
                e.Use();
            }

            return;
        }

        // Перетаскивание узла
        if (e.type == EventType.MouseDrag && e.button == 0 && selectedNode != null)
        {
            var currentGraphMousePos = (mousePos - graphRect.position - graphPanOffset) / graphZoom;
            var delta = currentGraphMousePos - lastGraphMousePos;

            if (selectedNode["meta"] == null)
                selectedNode["meta"] = new JObject();

            var currentX = (float)(selectedNode["meta"]["x"] ?? 0);
            var currentY = (float)(selectedNode["meta"]["y"] ?? 0);

            selectedNode["meta"]["x"] = currentX + delta.x;
            selectedNode["meta"]["y"] = currentY + delta.y;

            lastGraphMousePos = currentGraphMousePos;

            e.Use();
            Repaint();
            return;
        }

        // Обновляем последнюю позицию мыши для следующего кадра
        if (e.type == EventType.MouseMove)
        {
            lastGraphMousePos = (mousePos - graphRect.position - graphPanOffset) / graphZoom;
        }

        // Завершение перетаскивания узла
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            // Не снимаем выделение при отпускании, только сбрасываем состояние перетаскивания
            e.Use();
            return;
        }

        // Отмена создания связи по Escape
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            if (isCreatingLink)
            {
                isCreatingLink = false;
                linkCreationSource = null;
                e.Use();
                Repaint();
            }
        }
    }

    private void HandleNodeSelection(Vector2 graphMousePos)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return;

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return;

        // Сохраняем текущую позицию мыши
        lastGraphMousePos = graphMousePos;

        // Ищем узел под курсором в координатах графа
        foreach (JObject node in nodes)
        {
            var nodePos = GetNodePosition(node);
            var nodeRect = new Rect(nodePos.x, nodePos.y, 180, 50);

            // Проверяем попадание мыши в узел в координатах графа
            if (nodeRect.Contains(graphMousePos))
            {
                selectedNode = node;
                return;
            }
        }

        // Если кликнули на пустое место - снимаем выделение
        selectedNode = null;
    }

    private void HandleLinkCreation(Vector2 graphMousePos)
    {
        var targetNode = GetNodeAtPosition(graphMousePos);

        if (targetNode != null && targetNode != linkCreationSource)
        {
            // Проверяем, не существует ли уже такая связь
            var existingLinks = (JArray)linkCreationSource["to"];
            bool linkExists = false;

            if (existingLinks != null)
            {
                foreach (JObject link in existingLinks)
                {
                    if ((int)link["id"] == (int)targetNode["id"])
                    {
                        linkExists = true;
                        break;
                    }
                }
            }

            // Проверяем, нет ли обратной связи (двусторонней)
            bool reverseLinkExists = false;
            var targetLinks = (JArray)targetNode["to"];
            if (targetLinks != null)
            {
                foreach (JObject link in targetLinks)
                {
                    if ((int)link["id"] == (int)linkCreationSource["id"])
                    {
                        reverseLinkExists = true;
                        break;
                    }
                }
            }

            if (!linkExists && !reverseLinkExists)
            {
                TakeSnapshot();

                // Создаем новую связь
                var newLink = new JObject
                {
                    ["id"] = (int)targetNode["id"],
                    ["line"] = "Новая реплика",
                    ["info"] = "Новая реплика"
                };

                if (linkCreationSource["to"] == null)
                    linkCreationSource["to"] = new JArray();

                ((JArray)linkCreationSource["to"]).Add(newLink);
            }
            else if (linkExists)
            {
                EditorUtility.DisplayDialog("Внимание", "Связь между этими нодами уже существует", "OK");
            }
            else if (reverseLinkExists)
            {
                EditorUtility.DisplayDialog("Внимание", "Двусторонние связи запрещены. Обратная связь уже существует.",
                    "OK");
            }
        }
        else if (targetNode == linkCreationSource)
        {
            EditorUtility.DisplayDialog("Внимание", "Нельзя создать связь ноды с самой собой", "OK");
        }

        isCreatingLink = false;
        linkCreationSource = null;
        Repaint();
    }

    private void HandleDoubleClick(Vector2 graphMousePos)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return;

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return;

        // Проверяем двойной клик по узлу
        var clickedNode = GetNodeAtPosition(graphMousePos);
        if (clickedNode != null)
        {
            windowInitialized = false;
            editingNode = clickedNode;
            showNodeEditor = true;
            editingNodeScroll = Vector2.zero;
            Repaint();
            return;
        }

        // Двойной клик на пустом месте - создаем новый узел
        CreateNewNode(graphMousePos);
    }

    private JObject GetNodeAtPosition(Vector2 graphMousePos)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return null;

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return null;

        foreach (JObject node in nodes)
        {
            var nodePos = GetNodePosition(node);
            var nodeRect = new Rect(nodePos.x, nodePos.y, 180, 50);

            if (nodeRect.Contains(graphMousePos))
                return node;
        }

        return null;
    }

    private (JObject sourceNode, JObject link) GetLinkAtPosition(Vector2 graphMousePos)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return (null, null);

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return (null, null);

        foreach (JObject node in nodes)
        {
            var fromPos = GetNodePosition(node);

            foreach (JObject link in node["to"])
            {
                var toId = (int)link["id"];
                var toNode = nodes.FirstOrDefault(n => (int)n["id"] == toId) as JObject;
                if (toNode != null)
                {
                    var toPos = GetNodePosition(toNode);

                    // Проверяем близость к линии связи
                    if (IsPointNearLine(graphMousePos, fromPos, toPos, 10f))
                    {
                        return (node, link);
                    }
                }
            }
        }

        return (null, null);
    }

    private bool IsPointNearLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd, float maxDistance)
    {
        // Вычисляем расстояние от точки до линии
        var lineLength = Vector2.Distance(lineStart, lineEnd);
        var lineDir = (lineEnd - lineStart).normalized;

        var projection = Vector2.Dot(point - lineStart, lineDir);
        projection = Mathf.Clamp(projection, 0, lineLength);

        var closestPoint = lineStart + lineDir * projection;
        var distance = Vector2.Distance(point, closestPoint);

        return distance <= maxDistance;
    }

    private JObject CreateNewNode(Vector2 position)
    {
        if (selectedScript == null || selectedScript["result"] == null) return null;

        TakeSnapshot();

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return null;

        // Создаем новый узел
        var newNode = new JObject
        {
            ["id"] = GetNextNodeId(nodes),
            ["line"] = "Новая реплика NPC",
            ["goal_achieved"] = new JObject
            {
                ["info"] = -1
            },
            ["to"] = new JArray(),
            ["meta"] = new JObject
            {
                ["x"] = position.x,
                ["y"] = position.y
            }
        };

        nodes.Add(newNode);

        Repaint();
        return newNode;
    }

    private void DeleteNode(JObject node)
    {
        if (selectedScript == null || selectedScript["result"] == null) return;

        TakeSnapshot();

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return;

        var nodeId = (int)node["id"];

        // Удаляем узел
        nodes.Remove(node);

        // Удаляем все связи, ведущие к этому узлу
        foreach (JObject otherNode in nodes)
        {
            var toArray = (JArray)otherNode["to"];
            if (toArray != null)
            {
                for (int i = toArray.Count - 1; i >= 0; i--)
                {
                    var link = (JObject)toArray[i];
                    if ((int)link["id"] == nodeId)
                    {
                        toArray.RemoveAt(i);
                    }
                }
            }
        }

        Repaint();
    }

    private void DeleteLink(JObject sourceNode, JObject link)
    {
        TakeSnapshot();

        var toArray = (JArray)sourceNode["to"];
        if (toArray != null)
        {
            toArray.Remove(link);
        }

        Repaint();
    }

    private int GetNextNodeId(JArray nodes)
    {
        int maxId = 0;
        foreach (JObject node in nodes)
        {
            var id = (int)node["id"];
            if (id > maxId) maxId = id;
        }

        return maxId + 1;
    }

    private void StartCreatingLink(JObject sourceNode)
    {
        isCreatingLink = true;
        linkCreationSource = sourceNode;
        showContextMenu = false;
    }

    private Vector2 GetNodePosition(JObject node)
    {
        if (node["meta"] == null)
        {
            node["meta"] = new JObject();
            node["meta"]["x"] = Random.Range(50, 500);
            node["meta"]["y"] = Random.Range(50, 500);
        }

        return new Vector2(
            (float)(node["meta"]["x"] ?? 0),
            (float)(node["meta"]["y"] ?? 0)
        );
    }

    private void DrawGraphContent()
    {
        if (selectedScript == null) return;

        if (selectedScript["result"] == null || !selectedScript["result"].HasValues)
        {
            var messagePos = new Vector2(graphRect.width / 2 - 100, graphRect.height / 2 - 10);
            GUI.Label(new Rect(messagePos, new Vector2(200, 20)), "Диалог еще генерируется...",
                centeredItalicLabelStyle);
            return;
        }

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return;

        // 1. Сначала рисуем связи (под узлами)
        Handles.BeginGUI();
        foreach (JObject node in nodes)
        {
            var fromPos = GetNodePosition(node);
            var fromRect = new Rect(
                fromPos.x * graphZoom + graphPanOffset.x,
                fromPos.y * graphZoom + graphPanOffset.y,
                180 * graphZoom,
                50 * graphZoom
            );

            foreach (JObject link in node["to"])
            {
                var toId = (int)link["id"];
                var toNode = nodes.FirstOrDefault(n => (int)n["id"] == toId) as JObject;
                if (toNode != null)
                {
                    var toPos = GetNodePosition(toNode);
                    var toRect = new Rect(
                        toPos.x * graphZoom + graphPanOffset.x,
                        toPos.y * graphZoom + graphPanOffset.y,
                        180 * graphZoom,
                        50 * graphZoom
                    );

                    // Проверяем видимость линии
                    if (IsLineVisible(fromRect.center, toRect.center,
                            new Rect(0, 0, graphRect.width, graphRect.height)))
                    {
                        // Вычисляем точки выхода и входа для кривой с перпендикулярными касательными
                        Vector2 startPoint, endPoint;
                        Vector2 startTangent, endTangent;
                        CalculateCurveWithTangents(fromRect, toRect, out startPoint, out endPoint, out startTangent,
                            out endTangent);

                        // Рисуем кривую Безье с правильными касательными
                        Handles.color = Color.white;
                        Handles.DrawBezier(startPoint, endPoint, startTangent, endTangent, Color.white, null, 3f);

                        // Рисуем стрелку, ориентированную по направлению кривой
                        DrawArrowAlongCurve(endPoint, endTangent);

                        // Рисуем текст реплики на связи
                        DrawLinkText(link, startPoint, endPoint, startTangent, endTangent, node);
                    }
                }
            }
        }

        Handles.EndGUI();

        // 2. Затем рисуем узлы поверх связей
        foreach (JObject node in nodes)
    {
        var nodePos = GetNodePosition(node);
        var nodeRect = new Rect(
            nodePos.x * graphZoom + graphPanOffset.x,
            nodePos.y * graphZoom + graphPanOffset.y,
            180 * graphZoom,
            50 * graphZoom
        );

        if (IsNodeVisible(nodeRect, new Rect(0, 0, graphRect.width, graphRect.height)))
        {
            // Создаем стиль с масштабируемым шрифтом
            var nodeStyle = new GUIStyle(EditorStyles.helpBox);
            nodeStyle.wordWrap = true;
            nodeStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            // Подсветка выделенной ноды
            if (node == selectedNode)
            {
                nodeStyle.normal.background = MakeTex(2, 2, 
                    EditorGUIUtility.isProSkin ? 
                    new Color(0.2f, 0.4f, 0.8f, 0.8f) : // Синяя подсветка для темной темы
                    new Color(0.6f, 0.8f, 1f, 0.8f));   // Светло-синяя подсветка для светлой темы
            }
            else
            {
                nodeStyle.normal.background = MakeTex(2, 2, 
                    EditorGUIUtility.isProSkin ? 
                    new Color(0.3f, 0.3f, 0.3f) :       // Темный фон для темной темы
                    new Color(0.95f, 0.95f, 0.95f));    // Светлый фон для светлой темы
            }

            // Масштабируем шрифт в зависимости от зума
            nodeStyle.fontSize = Mathf.RoundToInt(12 * graphZoom);

            // Добавляем отступы, которые также масштабируются
            nodeStyle.padding = new RectOffset(
                Mathf.RoundToInt(8 * graphZoom),
                Mathf.RoundToInt(8 * graphZoom),
                Mathf.RoundToInt(4 * graphZoom),
                Mathf.RoundToInt(4 * graphZoom)
            );

            // Отрисовка узла с масштабируемым текстом
            var nodeText = TruncateText(node["line"]?.ToString() ?? "", 10);
            GUI.Box(nodeRect, nodeText, nodeStyle);
        }
    }

        // Отрисовка временной линии при создании связи
        if (isCreatingLink && linkCreationSource != null)
        {
            Handles.BeginGUI();

            var sourcePos = GetNodePosition(linkCreationSource);
            var sourceRect = new Rect(
                sourcePos.x * graphZoom + graphPanOffset.x,
                sourcePos.y * graphZoom + graphPanOffset.y,
                180 * graphZoom,
                50 * graphZoom
            );

            var currentMousePos = Event.current.mousePosition;

            // Рисуем временную линию
            Handles.color = Color.yellow;
            Handles.DrawLine(sourceRect.center, currentMousePos);

            // Рисуем стрелку
            DrawArrowAlongCurve(currentMousePos, sourceRect.center);

            Handles.EndGUI();

            // Изменяем курсор
            EditorGUIUtility.AddCursorRect(graphRect, MouseCursor.Link);
        }
    }

    private void DrawLinkText(JObject link, Vector2 startPoint, Vector2 endPoint, Vector2 startTangent,
        Vector2 endTangent, JObject fromNode)
    {
        // Получаем текст реплики (если есть)
        var linkText = link["line"]?.ToString();
        if (string.IsNullOrEmpty(linkText)) return;

        // Вычисляем точку на середине кривой Безье
        var midPoint = CalculateBezierPoint(0.5f, startPoint, startTangent, endTangent, endPoint);

        // Создаем стиль для текста связи
        var linkTextStyle = new GUIStyle(EditorStyles.label);
        linkTextStyle.normal.textColor = Color.white;
        linkTextStyle.fontSize = Mathf.RoundToInt(10 * graphZoom);
        linkTextStyle.alignment = TextAnchor.MiddleCenter;
        linkTextStyle.wordWrap = true;

        // Вычисляем размер текста
        var content = new GUIContent(TruncateText(linkText, 5));
        var textSize = linkTextStyle.CalcSize(content);

        // Создаем Rect для текста
        var textRect = new Rect(
            midPoint.x - textSize.x / 2,
            midPoint.y - textSize.y / 2,
            textSize.x,
            textSize.y
        );

        // Рисуем фон для текста для лучшей читаемости
        var backgroundColor =
            EditorGUIUtility.isProSkin ? new Color(0.1f, 0.1f, 0.1f, 0.8f) : new Color(1f, 1f, 1f, 0.8f);

        EditorGUI.DrawRect(new Rect(
            textRect.x - 2, textRect.y - 1,
            textRect.width + 4, textRect.height + 2
        ), backgroundColor);

        // Рисуем текст
        GUI.Label(textRect, content, linkTextStyle);
        if (showNodeEditor || showLinkEditor)
            return;
        // Обработка клика по тексту
        if (Event.current.type == EventType.Used && Event.current.button == 0 && Event.current.clickCount == 1)
        {
            if (textRect.Contains(Event.current.mousePosition))
            {
                windowInitialized = false;
                editingLink = link;
                showLinkEditor = true;
                editingLinkSourceNode = fromNode;
                linkEditorScroll = Vector2.zero;
                Event.current.Use();
            }
        }

        // Изменяем курсор при наведении
        if (textRect.Contains(Event.current.mousePosition))
        {
            EditorGUIUtility.AddCursorRect(textRect, MouseCursor.Link);
            isHoveringLink = true;
        }
    }

    // Вычисляет точку на кривой Безье для параметра t (0-1)
    private Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector2 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

// Находит узел по ID
    private JObject FindNodeById(int id)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return null;

        var nodes = (JArray)selectedScript["result"]["data"];
        return nodes?.FirstOrDefault(n => (int)n["id"] == id) as JObject;
    }

    private string TruncateText(string text, int maxWords = 10)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= maxWords) return text;

        return string.Join(" ", words.Take(maxWords)) + "...";
    }

// Вычисляет точки и касательные для кривой с перпендикулярными соединениями
    private void CalculateCurveWithTangents(Rect fromRect, Rect toRect, out Vector2 startPoint, out Vector2 endPoint,
        out Vector2 startTangent, out Vector2 endTangent)
    {
        Vector2 fromCenter = fromRect.center;
        Vector2 toCenter = toRect.center;

        // Определяем направление от исходного узла к целевому
        Vector2 direction = (toCenter - fromCenter).normalized;

        // Вычисляем точки на границах прямоугольников
        startPoint = GetBorderPoint(fromRect, direction);
        endPoint = GetBorderPoint(toRect, -direction);

        // Определяем нормали к границам в точках соединения (внешние)
        Vector2 startNormal = GetOutwardBorderNormal(fromRect, startPoint);
        Vector2 endNormal = GetOutwardBorderNormal(toRect, endPoint);

        // Расстояние между точками соединения
        float distance = Vector2.Distance(startPoint, endPoint);

        // Длина касательных (зависит от расстояния)
        float tangentLength = Mathf.Min(distance * 0.3f, 100f);

        // Касательные направлены вдоль нормалей (наружу)
        startTangent = startPoint + startNormal * tangentLength;
        endTangent = endPoint + endNormal * tangentLength;

        // Для длинных связей добавляем изгиб
        if (distance > 200f)
        {
            // Вычисляем среднюю точку и смещение для изгиба
            Vector2 midPoint = (startPoint + endPoint) * 0.5f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            // Определяем направление изгиба
            float bendDirection = (fromCenter.x < toCenter.x) ? 1f : -1f;
            if (Mathf.Abs(direction.x) < 0.3f) // Если связь в основном вертикальная
                bendDirection = (fromCenter.y < toCenter.y) ? -1f : 1f;

            Vector2 bendOffset = perpendicular * bendDirection * Mathf.Min(distance * 0.2f, 80f);

            // Смещаем касательные для создания изгиба
            startTangent = startPoint + startNormal * tangentLength * 0.7f + bendOffset * 0.3f;
            endTangent = endPoint + endNormal * tangentLength * 0.7f + bendOffset * 0.3f;
        }
    }

// Находит точку на границе прямоугольника в заданном направлении
    private Vector2 GetBorderPoint(Rect rect, Vector2 direction)
    {
        Vector2 center = rect.center;

        // Нормализуем направление
        direction = direction.normalized;

        // Вычисляем пересечение луча из центра с границей прямоугольника
        float tX = float.MaxValue;
        float tY = float.MaxValue;

        if (Mathf.Abs(direction.x) > 0.001f)
        {
            tX = direction.x > 0 ? (rect.xMax - center.x) / direction.x : (rect.xMin - center.x) / direction.x;
        }

        if (Mathf.Abs(direction.y) > 0.001f)
        {
            tY = direction.y > 0 ? (rect.yMax - center.y) / direction.y : (rect.yMin - center.y) / direction.y;
        }

        // Используем минимальное положительное t
        float t = Mathf.Min(tX, tY);

        return center + direction * t;
    }

// Определяет внешнюю нормаль к границе прямоугольника в заданной точке
    private Vector2 GetOutwardBorderNormal(Rect rect, Vector2 point)
    {
        // Определяем, на какой стороне находится точка
        float leftDist = Mathf.Abs(point.x - rect.xMin);
        float rightDist = Mathf.Abs(point.x - rect.xMax);
        float topDist = Mathf.Abs(point.y - rect.yMin);
        float bottomDist = Mathf.Abs(point.y - rect.yMax);

        float minDist = Mathf.Min(leftDist, rightDist, topDist, bottomDist);

        // Возвращаем внешнюю нормаль к ближайшей стороне
        if (minDist == leftDist)
            return Vector2.left; // Внешняя нормаль к левой стороне (направлена влево)
        else if (minDist == rightDist)
            return Vector2.right; // Внешняя нормаль к правой стороне (направлена вправо)
        else if (minDist == topDist)
            return Vector2.down; // Внешняя нормаль к верхней стороне (направлена вверх)
        else
            return Vector2.up; // Внешняя нормаль к нижней стороне (направлена вниз)
    }

// Рисует стрелку, ориентированную по направлению кривой
    private void DrawArrowAlongCurve(Vector2 arrowHead, Vector2 tangentPoint)
    {
        // Вычисляем направление кривой в конечной точке
        // Направление - от контрольной точки к конечной точке
        Vector2 curveDirection = (arrowHead - tangentPoint).normalized;

        // Если направление нулевое, используем направление по умолчанию
        if (curveDirection.sqrMagnitude < 0.001f)
            curveDirection = Vector2.right;

        // Размер стрелки с учетом масштаба
        float arrowLength = 7f * graphZoom;
        float arrowWidth = 4f * graphZoom;

        // Смещаем стрелку немного назад по кривой, чтобы она не перекрывала точку соединения
        Vector2 adjustedArrowHead = arrowHead;

        // Вершины стрелки
        Vector2 arrowBase = adjustedArrowHead - curveDirection * arrowLength;
        Vector2 arrowRight = arrowBase + new Vector2(-curveDirection.y, curveDirection.x) * arrowWidth;
        Vector2 arrowLeft = arrowBase - new Vector2(-curveDirection.y, curveDirection.x) * arrowWidth;

        // Рисуем треугольник стрелки
        Handles.color = Color.white;
        Handles.DrawAAConvexPolygon(adjustedArrowHead, arrowRight, arrowLeft);
    }

// Обновляем проверку видимости линии для работы с Vector2
    private bool IsLineVisible(Vector2 start, Vector2 end, Rect visibleArea)
    {
        return visibleArea.Contains(start) || visibleArea.Contains(end) ||
               LineIntersectsRect(start, end, visibleArea);
    }

    private bool IsNodeVisible(Rect nodeRect, Rect visibleArea)
    {
        return nodeRect.Overlaps(visibleArea);
    }

    private void ResetGraphView()
    {
        graphZoom = 1.0f;
        graphPanOffset = Vector2.zero;
        isPanning = false;
        selectedNode = null;
    
        // Пересчитываем границы графа
        CalculateGraphBounds();
    
        // Автоматическое расположение узлов если нужно
        if (selectedScript != null && selectedScript["result"] != null && selectedScript["result"].HasValues)
        {
            var nodes = (JArray)selectedScript["result"]["data"];
            if (nodes != null && nodes.Count > 0 && !HasNodePositions(nodes))
            {
                AutoLayoutDAG();
            }
        }
    
        Repaint();
    }

    private void AutoLayoutDAG()
    {
        if (selectedScript == null || selectedScript["result"] == null) return;

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null || nodes.Count == 0) return;

        // Вычисляем уровни для каждого узла
        var levels = CalculateNodeLevels(nodes);

        // Распределяем узлы по уровням
        var levelNodes = new Dictionary<int, List<JObject>>();
        foreach (var kvp in levels)
        {
            var level = kvp.Value;
            if (!levelNodes.ContainsKey(level))
                levelNodes[level] = new List<JObject>();

            levelNodes[level].Add(FindNodeById(nodes, kvp.Key));
        }

        // Располагаем узлы по уровням
        float startX = 100f;
        float startY = 100f;
        float levelHeight = 120f;
        float nodeWidth = 180f;

        foreach (var level in levelNodes.Keys.OrderBy(l => l))
        {
            var nodesInLevel = levelNodes[level];
            float levelWidth = nodesInLevel.Count * (nodeWidth + 20f);
            float currentX = startX + (position.width - levelWidth) / 2; // центрируем уровень

            for (int i = 0; i < nodesInLevel.Count; i++)
            {
                var node = nodesInLevel[i];
                if (node["meta"] == null)
                    node["meta"] = new JObject();

                node["meta"]["x"] = currentX + i * (nodeWidth + 20f);
                node["meta"]["y"] = startY + level * levelHeight;
            }
        }

        Repaint();
    }

    private Dictionary<int, int> CalculateNodeLevels(JArray nodes)
    {
        var levels = new Dictionary<int, int>();
        var visited = new HashSet<int>();

        // Находим корневые узлы (без входящих связей)
        var rootNodes = FindRootNodes(nodes);

        // BFS для определения уровней
        var queue = new Queue<JObject>();
        foreach (var root in rootNodes)
        {
            levels[(int)root["id"]] = 0;
            queue.Enqueue(root);
            visited.Add((int)root["id"]);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentLevel = levels[(int)current["id"]];

            foreach (JObject link in current["to"])
            {
                var childId = (int)link["id"];
                var childNode = FindNodeById(nodes, childId);

                if (childNode != null)
                {
                    // Уровень ребенка = максимальный(текущий уровень, уровень родителя + 1)
                    var newLevel = Mathf.Max(
                        levels.ContainsKey(childId) ? levels[childId] : 0,
                        currentLevel + 1
                    );

                    levels[childId] = newLevel;

                    if (!visited.Contains(childId))
                    {
                        visited.Add(childId);
                        queue.Enqueue(childNode);
                    }
                }
            }
        }

        return levels;
    }

    private List<JObject> FindRootNodes(JArray nodes)
    {
        var nodesWithIncoming = new HashSet<int>();

        // Находим все узлы, у которых есть входящие связи
        foreach (JObject node in nodes)
        {
            foreach (JObject link in node["to"])
            {
                nodesWithIncoming.Add((int)link["id"]);
            }
        }

        // Корневые узлы - те, у которых нет входящих связей
        var roots = new List<JObject>();
        foreach (JObject node in nodes)
        {
            if (!nodesWithIncoming.Contains((int)node["id"]))
                roots.Add(node);
        }

        // Если все узлы имеют входящие связи, берем узел с минимальным id
        if (roots.Count == 0 && nodes.Count > 0)
            roots.Add((JObject)nodes[0]);

        return roots;
    }

    private JObject FindNodeById(JArray nodes, int id)
    {
        return nodes.FirstOrDefault(n => (int)n["id"] == id) as JObject;
    }

    private bool LineIntersectsRect(Vector2 start, Vector2 end, Rect rect)
    {
        // Проверяем пересечение линии с прямоугольником
        return LineIntersectsLine(start, end, new Vector2(rect.x, rect.y), new Vector2(rect.x + rect.width, rect.y)) ||
               LineIntersectsLine(start, end, new Vector2(rect.x + rect.width, rect.y),
                   new Vector2(rect.x + rect.width, rect.y + rect.height)) ||
               LineIntersectsLine(start, end, new Vector2(rect.x + rect.width, rect.y + rect.height),
                   new Vector2(rect.x, rect.y + rect.height)) ||
               LineIntersectsLine(start, end, new Vector2(rect.x, rect.y + rect.height), new Vector2(rect.x, rect.y));
    }

    private bool LineIntersectsLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        // Проверяем пересечение двух линий
        var b = a2 - a1;
        var d = b2 - b1;
        var bDotDPerp = b.x * d.y - b.y * d.x;

        if (bDotDPerp == 0)
            return false;

        var c = b1 - a1;
        var t = (c.x * d.y - c.y * d.x) / bDotDPerp;
        if (t < 0 || t > 1)
            return false;

        var u = (c.x * b.y - c.y * b.x) / bDotDPerp;
        if (u < 0 || u > 1)
            return false;

        return true;
    }

    private void CalculateGraphBounds()
    {
        if (selectedScript == null) return;
        if (selectedScript["result"] == null || !selectedScript["result"].HasValues)
        {
            graphBounds = new Rect(0, 0, position.width, position.height);
            return;
        }

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null || nodes.Count == 0)
        {
            graphBounds = new Rect(0, 0, position.width, position.height);
            return;
        }

        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        foreach (JObject node in nodes)
        {
            var nodePos = GetNodePosition(node);
            minX = Mathf.Min(minX, nodePos.x);
            minY = Mathf.Min(minY, nodePos.y);
            maxX = Mathf.Max(maxX, nodePos.x + 180 * graphZoom);
            maxY = Mathf.Max(maxY, nodePos.y + 50 * graphZoom);
        }

        // Добавляем паддинг
        graphBounds = new Rect(
            minX - GRAPH_PADDING,
            minY - GRAPH_PADDING,
            maxX - minX + GRAPH_PADDING * 2,
            maxY - minY + GRAPH_PADDING * 2
        );
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = new Color[width * height];
        for (var i = 0; i < pix.Length; i++)
            pix[i] = col;
        var result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void ClearEditScriptPageFields()
    {
        editScriptName = "";
        editScriptMinMainChar = 1;
        editScriptMaxMainChar = 3;
        editScriptMinDepth = 1;
        editScriptMaxDepth = 3;
        selectedMainCharacterId = null;
        selectedNPCId = null;
        editScriptDescription = "";
        playerGetsInfo = false;
        playerGetsItem = false;
        playerGetsInfoName = "";
        playerGetsItemName = "";
        playerGetsInfoCondition = "";
        playerGetsItemCondition = "";
        editScriptAdditional = "";
        toMainCharacterRelation = 0;
        toNpcRelation = 0;
    }

    private void ClearEditScenePageFields()
    {
        editSceneName = null;
        editSceneDescription = null;
        editSceneCharacters = new JArray();
    }

    private void ClearEditGamePageFields()
    {
        editGameName = null;
        editGameDescription = null;
        editGameGenre = 0;
        editGameTonality = 0;
        editGameTechLevel = 0;
    }

    private void ClearEditCharacterPageFields()
    {
        editCharacterName = null;
        editCharacterProfession = null;
        editCharacterTraits = null;
        editCharacterTalkStyle = null;
        editCharacterLook = null;
        editCharacterExtra = null;
    }

    private float CalculateAvailableHeight()
    {
        // Базовые отступы
        var basePadding = 40f;

        // Высота заголовка и отступов
        var headerHeight = 50f + 30f;

        // Высота кнопок (3 кнопки по 40 + отступы)
        var buttonsHeight = 3 * 40f + 2 * 5f + 20f;

        return position.height - basePadding - headerHeight - buttonsHeight;
    }

    public void SwitchPage(Page page)
    {
        graphZoom = 1.0f;
        graphPanOffset = Vector2.zero;
        isPanning = false;
        selectedNode = null;
        scrollPosition = Vector2.zero;
        var loggedIn = StorageApi.GetInstance().IsLoggedIn();
        if (page is Page.Register or Page.Login)
        {
            if (loggedIn)
            {
                currentPage = Page.Main;
                return;
            }
        }
        else
        {
            if (!loggedIn)
            {
                currentPage = Page.Login;
                return;
            }
        }

        if (page != Page.GraphEditor)
        {
            undoStack.Clear();
            redoStack.Clear();
        }

        pageInitialized = false;

        currentPage = page;
    }
}