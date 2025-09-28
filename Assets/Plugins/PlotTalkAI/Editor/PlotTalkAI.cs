using System;
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

    // edit script
    private string editScriptName;
    private GUIStyle fieldLabelStyle;

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
    private GUIStyle iconButtonStyle;

    private bool isHoveringLink;
    private bool isPanning;
    private Vector2 lastMousePosition;

    // cashed styles
    private GUIStyle linkStyle;
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
    private GUIStyle plusLabelStyle;
    private GUIStyle plusStyle;

    private string registerConfirm = "";

    // register
    private string registerEmail = "";
    private string registerPassword = "";

    private readonly Dictionary<long, bool> sceneExpandedStates = new();

    private readonly string[] scriptCharacterAttitude = { "Не знаком", "Хорошо", "Нейтрально", "Плохо" };
    private GUIStyle scriptLabelStyle;
    private Vector2 scrollPosition;
    private JObject selectedCharacter;

    // selected objects
    private JObject selectedGame;
    private string selectedMainCharacterId;

    // graph data
    private JObject selectedNode;
    private string selectedNPCId;
    private JObject selectedScene;
    private JObject selectedScript;
    private bool showNodeEditor;
    private GUIStyle textFieldStyle;
    private int toMainCharacterRelation;
    private int toNpcRelation;
    private GUIStyle zoomLabelStyle;

    private void OnEnable()
    {
        SwitchPage(currentPage);
        CreateStyles();
    }

    private void OnGUI()
    {
        var backgroundColor =
            EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.76f, 0.76f, 0.76f);

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
            padding = new RectOffset(15, 15, 15, 15)
        };

        addCardStyle = new GUIStyle(EditorStyles.helpBox)
        {
            margin = new RectOffset(5, 5, 10, 10),
            padding = new RectOffset(15, 15, 15, 15)
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
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        // Стиль для подписи плюса
        plusLabelStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        // Стиль для кнопки со стрелкой
        arrowButtonStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white },
            hover = { textColor = new Color(0.75f, 0.75f, 0.75f) },
            active = { textColor = new Color(0.75f, 0.75f, 0.75f) },
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedWidth = 20,
            fixedHeight = 20
        };

        scriptLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = textColor },
            hover = { textColor = new Color(0.75f, 0.75f, 0.75f) },
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
            if (!string.IsNullOrEmpty(loginEmail) && loginPassword == "1234")
            {
                StorageApi.GetInstance().LogIn(0, "token_will_be_here", "{\"games\":[]}");
                SwitchPage(Page.Main);
            }
            else
            {
                EditorUtility.DisplayDialog("Ошибка", "Неверные данные", "OK");
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
            if (string.IsNullOrEmpty(registerEmail) || string.IsNullOrEmpty(registerPassword))
            {
                EditorUtility.DisplayDialog("Ошибка", "Заполните все поля", "OK");
            }
            else if (registerPassword != registerConfirm)
            {
                EditorUtility.DisplayDialog("Ошибка", "Пароли не совпадают", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Успех", "Регистрация прошла успешно", "OK");
                SwitchPage(Page.Login);
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
        GUILayout.Label("Добро пожаловать, ТутБудетИмя!", centeredLabelStyle);
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
        editGameDescription = EditorGUILayout.TextArea(editGameDescription,
            GUILayout.Height(60)); // Фиксированная высота для текстового поля
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
        editCharacterTalkStyle = EditorGUILayout.TextArea(editCharacterTalkStyle,
            GUILayout.Height(60));

        GUILayout.Space(15);

        GUILayout.Label("Внешний вид", fieldLabelStyle);
        editCharacterLook = EditorGUILayout.TextArea(editCharacterLook,
            GUILayout.Height(60));

        GUILayout.Space(15);

        GUILayout.Label("Характеристика", fieldLabelStyle);
        editCharacterExtra = EditorGUILayout.TextArea(editCharacterExtra,
            GUILayout.Height(60)); // Фиксированная высота для текстового поля

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
                    // Логика сохранения сцены
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
        editSceneDescription = EditorGUILayout.TextArea(editSceneDescription,
            GUILayout.Height(60)); // Фиксированная высота для текстового поля

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
                editScriptName = (string)selectedScript["name"];
                editScriptMinMainChar = (int)selectedScript["answers_from_m"];
                editScriptMaxMainChar = (int)selectedScript["answers_to_m"];
                editScriptMinDepth = (int)selectedScript["answers_from_n"];
                editScriptMaxDepth = (int)selectedScript["answers_to_n"];
                selectedMainCharacterId = (string)selectedScript["main_character"];
                selectedNPCId = (string)selectedScript["npc"];
                editScriptDescription = (string)selectedScript["description"];
                playerGetsInfo = (bool)selectedScript["infoData"]["gets"];
                playerGetsItem = (bool)selectedScript["itemData"]["gets"];
                playerGetsInfoName = (string)selectedScript["infoData"]["name"];
                playerGetsItemName = (string)selectedScript["itemData"]["name"];
                playerGetsInfoCondition = (string)selectedScript["infoData"]["condition"];
                playerGetsItemCondition = (string)selectedScript["itemData"]["condition"];
                editScriptAdditional = (string)selectedScript["additional"];
                toMainCharacterRelation = Array.IndexOf(scriptCharacterAttitude,
                    (string)selectedScript["to_main_character_relations"]);
                toNpcRelation = Array.IndexOf(scriptCharacterAttitude, (string)selectedScript["to_npc_relations"]);
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
        var availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

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
        var sceneCharacterIds = ((JArray)selectedScene["characters"]).ToObject<string[]>();
        var availableCharacters = ((JArray)selectedGame["characters"])
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
        editScriptDescription = EditorGUILayout.TextArea(editScriptDescription,
            GUILayout.Height(60));
        GUILayout.Space(30);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Персонаж получит предмет");
        playerGetsItem = EditorGUILayout.Toggle(playerGetsItem, GUILayout.Width(20));
        if (!playerGetsItem)
        {
            playerGetsItemName = "";
            playerGetsItemCondition = "";
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginDisabledGroup(!playerGetsItem);

        GUILayout.Label("Предмет", fieldLabelStyle);
        playerGetsItemName = EditorGUILayout.TextField(playerGetsItemName, textFieldStyle);
        GUILayout.Space(15);
        GUILayout.Label("Условие достижения", fieldLabelStyle);
        playerGetsItemCondition = EditorGUILayout.TextField(playerGetsItemCondition, textFieldStyle);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(30);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Персонаж получит информацию");
        playerGetsInfo = EditorGUILayout.Toggle(playerGetsInfo, GUILayout.Width(20));
        if (!playerGetsInfo)
        {
            playerGetsInfoName = "";
            playerGetsInfoCondition = "";
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

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
        editScriptAdditional = EditorGUILayout.TextArea(editScriptAdditional,
            GUILayout.Height(60));

        GUILayout.Space(15);

        GUILayout.EndScrollView();

        GUILayout.Space(15);

        if (GUILayout.Button("Сохранить", buttonStyle, GUILayout.Height(40)))
        {
            if (!string.IsNullOrEmpty(editScriptName) && editScriptMinMainChar > 0 && editScriptMaxMainChar > 0 &&
                editScriptMinDepth > 0 && editScriptMaxDepth > 0 && !string.IsNullOrEmpty(editScriptDescription))
            {
                if (selectedScript != null)
                {
                    var updScript = new JObject
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
                        ["additional"] = editScriptAdditional
                    };
                    StorageApi.GetInstance().UpdateScript((string)selectedGame["id"], (long)selectedScene["id"],
                        (string)selectedScript["id"], updScript);
                    ClearEditScriptPageFields();
                    selectedScript = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    selectedScene = StorageApi.GetInstance()
                        .GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
                    SwitchPage(Page.GameDetail);
                }
                else
                {
                    var newScript = new JObject
                    {
                        ["id"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString(),
                        ["result"] = new JObject(),
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
                        ["additional"] = editScriptAdditional
                    };
                    StorageApi.GetInstance()
                        .AddScript((string)selectedGame["id"], (long)selectedScene["id"], newScript);
                    ClearEditScriptPageFields();
                    selectedScript = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    selectedScene = StorageApi.GetInstance()
                        .GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
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
                selectedScript = null;
                ClearEditScriptPageFields();
                SwitchPage(Page.GameDetail);
            }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawGraphEditorPage()
    {
        // Сначала обрабатываем ввод
        HandleGraphInput();

        // Разделяем область на две части: управление и график
        float controlPanelHeight = 150f;

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

            // Кнопки навигации
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_back"), iconButtonStyle))
            {
                // Логика для кнопки "Назад"
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_forward"), iconButtonStyle))
            {
                // Логика для кнопки "Вперед"
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

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh"), iconButtonStyle))
            {
                if (EditorUtility.DisplayDialog("Вы уверены?", "Все несохраненные изменения будут сброшены!",
                        "Да, перезагрузить", "Отмена"))
                {
                    selectedScript = StorageApi.GetInstance().GetScriptById((string)selectedGame["id"],
                        (long)selectedScene["id"], (string)selectedScript["id"]);
                    ResetGraphView();
                }
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(5);
        GUILayout.EndVertical();
        GUILayout.Space(5);
        GUILayout.EndHorizontal();
    }

    private void HandleGraphInput()
    {
        var e = Event.current;
        var mousePos = e.mousePosition;

        // Проверяем, находится ли курсор в области графа
        if (!graphRect.Contains(mousePos))
            return;

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

        // Обработка кликов по узлам
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            // Двойной клик - редактирование
            if (e.clickCount == 2)
            {
                HandleDoubleClick(graphMousePos);
                e.Use();
            }
            else // Одинарный клик - начало перетаскивания
            {
                HandleNodeSelection(graphMousePos);
                e.Use();
            }

            return;
        }

        // Перетаскивание узла - ИСПРАВЛЕННАЯ ВЕРСИЯ
        if (e.type == EventType.MouseDrag && e.button == 0 && selectedNode != null)
        {
            // Получаем текущую позицию мыши в координатах графа
            var currentGraphMousePos = (mousePos - graphRect.position - graphPanOffset) / graphZoom;

            // Вычисляем дельту перемещения в координатах графа
            var delta = currentGraphMousePos - lastGraphMousePos;

            if (selectedNode["meta"] == null)
                selectedNode["meta"] = new JObject();

            // Получаем текущую позицию и добавляем дельту
            var currentX = (float)(selectedNode["meta"]["x"] ?? 0);
            var currentY = (float)(selectedNode["meta"]["y"] ?? 0);

            selectedNode["meta"]["x"] = currentX + delta.x;
            selectedNode["meta"]["y"] = currentY + delta.y;

            // Обновляем последнюю позицию мыши
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
            selectedNode = null;
            e.Use();
            return;
        }
    }


    private void HandleDoubleClick(Vector2 graphMousePos)
    {
        if (selectedScript == null || selectedScript["result"] == null)
            return;

        var nodes = (JArray)selectedScript["result"]["data"];
        if (nodes == null) return;

        // Ищем узел под курсором в координатах графа
        foreach (JObject node in nodes)
        {
            var nodePos = GetNodePosition(node);

            // Создаем Rect узла в координатах графа (без учета масштаба и панорамирования)
            var nodeRect = new Rect(nodePos.x, nodePos.y, 180, 50);

            // Проверяем попадание мыши в узел в координатах графа
            if (nodeRect.Contains(graphMousePos))
            {
                editingNode = node;
                showNodeEditor = true;
                editingNodeScroll = Vector2.zero;
                Repaint();
                return;
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

            // Создаем Rect узла в координатах графа (без учета масштаба и панорамирования)
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
                nodeStyle.normal.textColor = Color.white;
                nodeStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.3f));

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
        CalculateGraphBounds();

        if (graphBounds.width == 0 || graphBounds.height == 0)
        {
            graphZoom = 1.0f;
            graphPanOffset = Vector2.zero;
            return;
        }

        // Вычисляем zoom, который поместит весь граф в view
        var zoomX = graphRect.width / graphBounds.width;
        var zoomY = graphRect.height / graphBounds.height;
        graphZoom = Mathf.Min(zoomX, zoomY, 1.0f);

        // Центрируем граф
        graphPanOffset = new Vector2(
            (graphRect.width - graphBounds.width * graphZoom) / 2,
            (graphRect.height - graphBounds.height * graphZoom) / 2
        );

        Repaint();
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

    private void SetNodePosition(JObject node, Vector2 position)
    {
        if (node["meta"] == null)
            node["meta"] = new JObject();

        node["meta"]["x"] = position.x;
        node["meta"]["y"] = position.y;
    }

    private void DrawNodeEditorWindow()
    {
        if (editingNode == null) return;

        // Создаем затемнение фона
        var backgroundColor = new Color(0, 0, 0, 0.5f);
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        // Окно редактирования
        var windowRect = new Rect(position.width / 2 - 200, position.height / 2 - 150, 400, 300);
        GUI.Window(0, windowRect, DrawNodeEditorWindowContent, "Редактирование узла");
    }

    private void DrawNodeEditorWindowContent(int id)
    {
        editingNodeScroll = GUILayout.BeginScrollView(editingNodeScroll);

        // Поля для редактирования
        GUILayout.Label("Текст:");
        var line = GUILayout.TextArea(editingNode["line"]?.ToString() ?? "", GUILayout.Height(60));
        editingNode["line"] = line;

        GUILayout.Label("Тип:");
        string[] types = { "M", "C", "P" };
        var typeIndex = Array.IndexOf(types, editingNode["type"]?.ToString() ?? "M");
        typeIndex = EditorGUILayout.Popup(typeIndex, types);
        editingNode["type"] = types[typeIndex];

        GUILayout.Label("Настроение:");
        var mood = GUILayout.TextField(editingNode["mood"]?.ToString() ?? "");
        editingNode["mood"] = mood;

        GUILayout.Label("Достижение цели:");
        var goalAchieve = editingNode["goal_achieve"]?.ToObject<int>() ?? 0;
        goalAchieve = EditorGUILayout.IntSlider(goalAchieve, 0, 1);
        editingNode["goal_achieve"] = goalAchieve;

        GUILayout.EndScrollView();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Сохранить"))
        {
            showNodeEditor = false;
            Repaint();
        }

        if (GUILayout.Button("Отменить"))
        {
            showNodeEditor = false;
            Repaint();
        }

        GUILayout.EndHorizontal();
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

    private void ShowNodeContextMenu(JObject node)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("Удалить узел"), false, () =>
        {
            if (EditorUtility.DisplayDialog("Подтверждение", "Удалить этот узел?", "Да", "Нет"))
            {
                // Логика удаления узла
            }
        });

        menu.AddItem(new GUIContent("Редактировать текст"), false, () =>
        {
            // Логика редактирования текста узла
        });

        menu.AddItem(new GUIContent("Добавить переход"), false, () =>
        {
            // Логика добавления перехода
        });

        menu.ShowAsContext();
    }

    private void DrawNodeCurve(Vector2 start, Vector2 end, Color color)
    {
        // Преобразуем координаты с учетом zoom и pan
        var startPos = new Vector3(
            start.x * graphZoom + graphPanOffset.x + graphRect.x,
            start.y * graphZoom + graphPanOffset.y + graphRect.y,
            0
        );

        var endPos = new Vector3(
            end.x * graphZoom + graphPanOffset.x + graphRect.x,
            end.y * graphZoom + graphPanOffset.y + graphRect.y,
            0
        );

        var startTan = startPos + Vector3.right * 50;
        var endTan = endPos + Vector3.left * 50;

        Handles.DrawBezier(startPos, endPos, startTan, endTan, color, null, 3f);

        // Рисуем стрелку
        var arrowDir = (endPos - startPos).normalized;
        var arrowHead = endPos - arrowDir * 10;

        // Преобразуем все в Vector3 для совместимости
        Handles.DrawAAConvexPolygon(
            arrowHead,
            arrowHead + Quaternion.Euler(0, 0, 30) * -arrowDir * 10,
            arrowHead + Quaternion.Euler(0, 0, -30) * -arrowDir * 10
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
        editScriptName = null;
        editScriptMinMainChar = 0;
        editScriptMaxMainChar = 0;
        editScriptMinDepth = 0;
        editScriptMaxDepth = 0;
        selectedMainCharacterId = null;
        selectedNPCId = null;
        editScriptDescription = null;
        playerGetsInfo = false;
        playerGetsItem = false;
        playerGetsInfoName = "";
        playerGetsItemName = "";
        playerGetsInfoCondition = "";
        playerGetsItemCondition = "";
        editScriptAdditional = null;
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

        pageInitialized = false;

        currentPage = page;
    }
}