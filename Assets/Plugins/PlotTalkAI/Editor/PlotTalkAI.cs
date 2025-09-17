using System;
using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Linq;

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
        EditScript
    }

    private Page currentPage = Page.Login;

    // fields
    // login
    private string loginEmail = "";
    private string loginPassword = "";
    // register
    private string registerEmail = "";
    private string registerPassword = "";
    private string registerConfirm = "";
    // edit game
    private string editGameName;
    private string editGameDescription;
    private int editGameGenre;
    private int editGameTechLevel;
    private int editGameTonality;
    // edit character
    private string editCharacterName;
    private string editCharacterProfession;
    private string editCharacterTraits;
    private string editCharacterTalkStyle;
    private string editCharacterLook;
    private string editCharacterExtra;
    // edit scene
    private string editSceneName;
    private string editSceneDescription;
    private JArray editSceneCharacters;
    // edit script
    private string editScriptName;
    private int editScriptMinMainChar;
    private int editScriptMaxMainChar;
    private int editScriptMinDepth;
    private int editScriptMaxDepth;
    private string selectedMainCharacterId;
    private string selectedNPCId;
    private int toMainCharacterRelation;
    private int toNpcRelation;
    private string editScriptDescription;
    private bool playerGetsInfo;
    private bool playerGetsItem;
    private string playerGetsInfoName;
    private string playerGetsItemName;
    private string playerGetsInfoCondition;
    private string playerGetsItemCondition;
    private string editScriptAdditional;

    // cashed styles
    private GUIStyle linkStyle;
    private GUIStyle centeredLabelStyle;
    private GUIStyle centeredSmallLabelStyle;
    private GUIStyle centeredItalicLabelStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle cardStyle;
    private GUIStyle addCardStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle iconButtonStyle;
    private GUIStyle plusStyle;
    private GUIStyle plusLabelStyle;
    private GUIStyle arrowButtonStyle;
    private GUIStyle scriptLabelStyle;

    private bool isHoveringLink = false;
    private Vector2 scrollPosition;

    // dropdowns
    private string[] gameGenres = { "Приключения", "Фэнтези", "Детектив", "Драма", "Комедия", "Ужасы", "Стратегия" };

    private string[] gameTechLevels =
        { "Каменный век", "Средневековье", "Индустриальный", "Современность", "Будущее", "Другое" };

    private string[] gameTonalities = { "Нейтральная", "Героическая", "Трагическая", "Комическая", "Сказочная" };

    private string[] scriptCharacterAttitude = { "Не знаком", "Хорошо", "Нейтрально", "Плохо" };
    
    // selected objects
    private JObject selectedGame;
    private JObject selectedCharacter;
    private JObject selectedScene;
    private JObject selectedScript;

    private bool pageInitialized;

    private Dictionary<long, bool> sceneExpandedStates = new Dictionary<long, bool>();

    [MenuItem("PlotTalkAI/PlotTalkAI")]
    public static void ShowWindow()
    {
        GetWindow<PlotTalkAI>("PlotTalkAI").minSize = new Vector2(450, 500);
    }

    private void OnEnable()
    {
        SwitchPage(currentPage);
        CreateStyles();
    }

    private void OnGUI()
    {
        Color backgroundColor =
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
        }

        GUILayout.Space(20);
        GUILayout.EndVertical();

        GUILayout.EndArea();

        EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height),
            isHoveringLink ? MouseCursor.Link : MouseCursor.Arrow);

        isHoveringLink = false;
    }

    private void CreateStyles()
    {
        Color textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        Color linkColor = EditorGUIUtility.isProSkin ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.1f, 0.3f, 0.8f);

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
            fontSize = 14,
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
        Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(40));
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
        Rect linkRect = GUILayoutUtility.GetRect(new GUIContent("Регистрация"), linkStyle);
        if (GUI.Button(linkRect, "Регистрация", linkStyle))
        {
            SwitchPage(Page.Register);
        }

        if (linkRect.Contains(Event.current.mousePosition))
        {
            isHoveringLink = true;
        }

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
        Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(40));
        if (GUI.Button(buttonRect, "Зарегистрироваться", buttonStyle))
        {
            if (string.IsNullOrEmpty(registerEmail) || string.IsNullOrEmpty(registerPassword))
                EditorUtility.DisplayDialog("Ошибка", "Заполните все поля", "OK");
            else if (registerPassword != registerConfirm)
                EditorUtility.DisplayDialog("Ошибка", "Пароли не совпадают", "OK");
            else
            {
                EditorUtility.DisplayDialog("Успех", "Регистрация прошла успешно", "OK");
                SwitchPage(Page.Login);
            }
        }

        GUILayout.Space(20);

        // Проверяем наведение на ссылку
        Rect linkRect = GUILayoutUtility.GetRect(new GUIContent("Назад"), linkStyle);
        if (GUI.Button(linkRect, "Назад", linkStyle))
        {
            SwitchPage(Page.Login);
        }

        if (linkRect.Contains(Event.current.mousePosition))
        {
            isHoveringLink = true;
        }

        GUILayout.EndArea();
    }

    private void DrawMainPage()
    {
        var games = StorageApi.GetInstance().GetGamesArray(StorageApi.GetInstance().LoadFullJson());
        GUILayout.Label($"Добро пожаловать, ТутБудетИмя!", centeredLabelStyle);
        GUILayout.Space(30);

        // Рассчитываем доступную ширину с учетом полосы прокрутки
        float availableWidth = position.width - 40;
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 300));
        float cardWidth = (availableWidth - (columns - 1) * 20 - 70) / columns; // Вычитаем ширину полосы прокрутки

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));

        int cardCount = games.Count + 1;

        // Создаем сетку карточек
        for (int i = 0; i < cardCount; i += columns)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(0); // Убираем любые отступы по умолчанию

            for (int j = 0; j < columns; j++)
            {
                int index = i + j;
                if (index >= cardCount) break;

                if (j > 0) GUILayout.Space(20);

                if (index == 0)
                {
                    DrawCreateGameCard(cardWidth + 7, 120);
                }
                else if (index - 1 < games.Count)
                {
                    DrawGameCard((JObject)games[index - 1], cardWidth, 120);
                }
            }

            // Заполняем оставшееся пространство
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (i + columns < cardCount)
            {
                GUILayout.Space(20);
            }
        }

        GUILayout.EndScrollView();

        GUILayout.Space(20);

        // Кнопка "Выйти"
        Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(35));
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
        Rect cardRect = GUILayoutUtility.GetLastRect();
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
        float descriptionHeight = height - 100; // Вычитаем высоту заголовка и кнопок
        Rect descriptionRect = GUILayoutUtility.GetRect(width - 30, descriptionHeight, EditorStyles.wordWrappedLabel);
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
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", игра будет безвозвратно удалена.",
                    "OK", "Отмена"))
            {
                StorageApi.GetInstance().DeleteGame((string)game["id"]);
            }
        }

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
        float availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

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
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedGame = null;
                ClearEditGamePageFields();
                SwitchPage(Page.Main);
            }
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
        {
            DrawCharacterCard(character);
        }

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
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", персонаж будет безвозвратно удален.",
                    "OK", "Отмена"))
            {
                StorageApi.GetInstance().DeleteCharacter((string)selectedGame["id"], (string)character["id"]);
                selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
            }
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
        float availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

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
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedCharacter = null;
                SwitchPage(Page.EditCharacters);
            }
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
        if (GUILayout.Button("Назад", buttonStyle, GUILayout.Height(35)))
        {
            SwitchPage(Page.Main);
        }

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
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                var scene = scenes[i];
                long sceneId = (long)scene["id"];

                // Инициализируем состояние, если нужно
                if (!sceneExpandedStates.ContainsKey(sceneId))
                {
                    sceneExpandedStates[sceneId] = false;
                }

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
                {
                    sceneExpandedStates[sceneId] = !sceneExpandedStates[sceneId];
                }

                GUILayout.EndVertical();

                if (GUILayout.Button((string)scene["name"], cardTitleStyle))
                {
                    sceneExpandedStates[sceneId] = !sceneExpandedStates[sceneId];
                }

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
                {
                    if (EditorUtility.DisplayDialog("Вы уверены?",
                            "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", сцена будет безвозвратно удалена.",
                            "OK", "Отмена"))
                    {
                        StorageApi.GetInstance().DeleteScene((string)selectedGame["id"], sceneId);
                        selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    }
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
                            SwitchPage(Page.Main);
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
                        {
                            if (EditorUtility.DisplayDialog("Вы уверены?",
                                    "Это действие необратимо. После того, как вы нажмете на кнопку \"OK\", диалог будет безвозвратно удален.",
                                    "OK", "Отмена"))
                            {
                                StorageApi.GetInstance()
                                    .DeleteScript((string)selectedGame["id"], sceneId, (string)script["id"]);
                                selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                            }
                        }
                        GUILayout.Space(3.5f);

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(10);
            }
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
        float availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

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
        if (characters.Count == 0)
        {
            GUILayout.Label("В игре еще нет персонажей...", centeredItalicLabelStyle);
        }
        foreach (var character in characters)
        {
            GUILayout.BeginHorizontal();
            string charId = (string)character["id"];
            bool isPresent = editSceneCharacters.Any(c => (string)c == charId);
            bool newValue = EditorGUILayout.Toggle(isPresent, GUILayout.Width(20));
    
            if (newValue != isPresent)
            {
                if (newValue)
                    editSceneCharacters.Add((string)character["id"]);
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
            if (!string.IsNullOrEmpty(editSceneDescription) && !string.IsNullOrEmpty(editSceneName) && editSceneCharacters.Count > 0)
            {
                if (selectedScene != null)
                {
                    var updScene = new JObject
                    {
                        ["name"] = editSceneName,
                        ["description"] = editSceneDescription,
                        ["characters"] = editSceneCharacters
                    };
                    StorageApi.GetInstance().UpdateScene((string)selectedGame["id"], (long)selectedScene["id"], updScene);
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
        {
            if (EditorUtility.DisplayDialog("Вы уверены?",
                    "После того, как вы нажмете на кнопку \"Да\", все внесенные изменения сбросятся.",
                    "Да", "Отмена"))
            {
                selectedScene = null;
                ClearEditScenePageFields();
                SwitchPage(Page.GameDetail);
            }
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
                toMainCharacterRelation = Array.IndexOf(scriptCharacterAttitude, (string)selectedScript["to_main_character_relations"]);
                toNpcRelation = Array.IndexOf(scriptCharacterAttitude, ((string)selectedScript["to_npc_relations"]));
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
        float availableHeight = CalculateAvailableHeight(); // 120 - примерная высота заголовка и кнопок

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
        string[] characterNames = availableCharacters
            .Select(c => (string)c["name"])
            .ToArray();

        string[] characterIds = availableCharacters
            .Select(c => (string)c["id"])
            .ToArray();

        // Находим текущий индекс выбранного персонажа
        int selectedMainCharacterIndex = Array.IndexOf(characterIds, selectedMainCharacterId);
        if (selectedMainCharacterIndex == -1) selectedMainCharacterIndex = 0;
        
        int selectedNPCIndex = Array.IndexOf(characterIds, selectedNPCId);
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
            if (!string.IsNullOrEmpty(editScriptName) && editScriptMinMainChar > 0 && editScriptMaxMainChar > 0 && editScriptMinDepth > 0 && editScriptMaxDepth > 0 && !string.IsNullOrEmpty(editScriptDescription))
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
                            ["condition"] = playerGetsInfoCondition,
                        },
                        ["itemData"] = new JObject
                        {
                            ["gets"] = playerGetsItem,
                            ["name"] = playerGetsItemName,
                            ["condition"] = playerGetsItemCondition,
                        },
                        ["additional"] = editScriptAdditional
                    };
                    StorageApi.GetInstance().UpdateScript((string)selectedGame["id"], (long)selectedScene["id"], (string)selectedScript["id"], updScript);
                    ClearEditScriptPageFields();
                    selectedScript = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    selectedScene = StorageApi.GetInstance().GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
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
                            ["condition"] = playerGetsInfoCondition,
                        },
                        ["itemData"] = new JObject
                        {
                            ["gets"] = playerGetsItem,
                            ["name"] = playerGetsItemName,
                            ["condition"] = playerGetsItemCondition,
                        },
                        ["additional"] = editScriptAdditional
                    };
                    StorageApi.GetInstance().AddScript((string)selectedGame["id"], (long)selectedScene["id"], newScript);
                    ClearEditScriptPageFields();
                    selectedScript = null;
                    selectedGame = StorageApi.GetInstance().GetGameById((string)selectedGame["id"]);
                    selectedScene = StorageApi.GetInstance().GetSceneById((string)selectedGame["id"], (long)selectedScene["id"]);
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
        float basePadding = 40f;

        // Высота заголовка и отступов
        float headerHeight = 50f + 30f;

        // Высота кнопок (3 кнопки по 40 + отступы)
        float buttonsHeight = 3 * 40f + 2 * 5f + 20f;

        return position.height - basePadding - headerHeight - buttonsHeight;
    }

    public void SwitchPage(Page page)
    {
        scrollPosition = Vector2.zero;
        bool loggedIn = StorageApi.GetInstance().IsLoggedIn();
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