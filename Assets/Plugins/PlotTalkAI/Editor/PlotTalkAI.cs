using System;
using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public class PlotTalkAI : EditorWindow
{
    public enum Page
    {
        Login,
        Register,
        Main,
        GameDetail,
        EditGame
    }

    private Page currentPage = Page.Login;
    private Page previousPage = Page.Login;
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

    // Кэшированные стили
    private GUIStyle linkStyle;
    private GUIStyle centeredLabelStyle;
    private GUIStyle fieldLabelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle textFieldStyle;
    private GUIStyle cardStyle;
    private GUIStyle addCardStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle iconButtonStyle;
    private GUIStyle plusStyle;
    private GUIStyle plusLabelStyle;

    private bool isHoveringLink = false;
    private Vector2 scrollPosition;
    
    private string[] gameGenres = {"Приключения", "Фэнтези", "Детектив", "Драма", "Комедия", "Ужасы", "Стратегия"};
    private string[] gameTechLevels = {"Каменный век", "Средневековье", "Индустриальный", "Современность", "Будущее", "Другое"};
    private string[] gameTonalities = {"Нейтральная", "Героическая", "Трагическая", "Комическая", "Сказочная"};
    
    private JObject selectedGame;
    
    private bool pageInitialized;

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

        GUILayout.Space(5);

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
                Debug.Log($"Удаление игры");
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

    private void DrawGameDetailPage()
    {
        if (selectedGame == null)
        {
            SwitchPage(Page.Main);
            return;
        }

        GUILayout.Label((string)selectedGame["title"], centeredLabelStyle);
        GUILayout.Space(20);

        GUILayout.Label("Описание:", EditorStyles.boldLabel);
        GUILayout.Label((string)selectedGame["description"], EditorStyles.wordWrappedLabel);

        GUILayout.Space(20);

        GUILayout.Label("Персонажи:", EditorStyles.boldLabel);

        foreach (var character in selectedGame["characters"])
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("• " + (string)character["name"] + " ("+ (string)character["role"] + ")");
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(30);

        // Кнопка возврата
        if (GUILayout.Button("Назад", buttonStyle))
        {
            SwitchPage(Page.Main);
        }
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
            editGameName = null;
            editGameDescription = null;
            editGameGenre = 0;
            editGameTonality = 0;
            editGameTechLevel = 0;
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
    
    // Кнопки вне ScrollView, чтобы они всегда были видны
    GUILayout.Space(15);
    
    if (GUILayout.Button("Изменить персонажей", buttonStyle, GUILayout.Height(40)))
    {
        // Ваш код для перехода к редактированию персонажей
    }
    
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
            SwitchPage(Page.Main);
        }
    }

    GUILayout.EndVertical();
    GUILayout.EndArea();
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