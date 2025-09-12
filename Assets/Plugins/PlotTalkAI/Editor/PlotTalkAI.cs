using System;
using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

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
        EditCharacter
    }

    private Page currentPage = Page.Login;
    private Page previousPage = Page.Login;
    
    // fields
    private string loginEmail = "";
    private string loginPassword = "";
    private string registerEmail = "";
    private string registerPassword = "";
    private string registerConfirm = "";
    private string editGameName;
    private string editGameDescription;
    private int editGameGenre;
    private int editGameTechLevel;
    private int editGameTonality;
    private string editCharacterName;
    private string editCharacterProfession;
    private string editCharacterTraits;
    private string editCharacterTalkStyle;
    private string editCharacterLook;
    private string editCharacterExtra;

    // cashed styles
    private GUIStyle linkStyle;
    private GUIStyle centeredLabelStyle;
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

    // selected objects
    private JObject selectedGame;
    private JObject selectedCharacter;

    private bool pageInitialized;
    
    private Dictionary<long, bool> sceneExpandedStates = new Dictionary<long, bool>();

    [MenuItem("Window/PlotTalkAI")]
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
            hover = {textColor = new Color(0.75f, 0.75f, 0.75f)},
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
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 220));
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


        if (GUILayout.Button("Персонажи", GUILayout.Height(30)))
        {
            selectedGame = game;
            SwitchPage(Page.EditCharacters);
        }

        // Кнопка настроек игры
        if (GUILayout.Button(EditorGUIUtility.IconContent("Settings"), iconButtonStyle))
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

        GUILayout.Label("Жанр", fieldLabelStyle);
        editGameGenre = EditorGUILayout.Popup(editGameGenre, gameGenres, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Исторический период", fieldLabelStyle);
        editGameTechLevel = EditorGUILayout.Popup(editGameTechLevel, gameTechLevels, textFieldStyle);

        GUILayout.Space(15);

        GUILayout.Label("Тональность", fieldLabelStyle);
        editGameTonality = EditorGUILayout.Popup(editGameTonality, gameTonalities, textFieldStyle);

        GUILayout.Space(15);

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
                        ["characters"] = new JArray(),
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

        if (GUILayout.Button(EditorGUIUtility.IconContent("Settings"), iconButtonStyle))
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
        position.width - 40,
        position.height - 40
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
        // Логика создания сцены
    }

    GUILayout.Space(20);

    // Список сцен
    scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
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
            if (GUILayout.Button("+", new GUIStyle(iconButtonStyle){fontSize = 18, padding = new RectOffset(8, 5, 5, 8)}))
            {
                // Логика добавления диалога
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("Settings"), iconButtonStyle))
            {
                // Логика изменения сцены
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
            
            GUILayout.Space(20);
            
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
                    if (GUILayout.Button(EditorGUIUtility.IconContent("Settings"), iconButtonStyle))
                    {
                        // Логика изменения диалога
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

                    GUILayout.Space(18);

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }
    }

    GUILayout.EndVertical();
    GUILayout.EndScrollView();
    GUILayout.EndArea();
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
        previousPage = currentPage;
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