using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

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
    private bool isHoveringButton = false;
    private Vector2 scrollPosition;

    // Данные для демонстрации
    private List<GameData> games = new List<GameData>();
    private GameData selectedGame;

    [System.Serializable]
    private class GameData
    {
        public string id;
        public string title;
        public string description;
        public List<CharacterData> characters;
    }

    [System.Serializable]
    private class CharacterData
    {
        public string name;
        public string role;
    }

    [MenuItem("Window/PlotTalkAI")]
    public static void ShowWindow()
    {
        GetWindow<PlotTalkAI>("PlotTalkAI").minSize = new Vector2(450, 500);
    }

    private void OnEnable()
    {
        SwitchPage(currentPage);
        CreateStyles();

        // Заглушка с демо-данными
        games.Add(new GameData
        {
            id = "1",
            title = "Ролевая игра",
            description = "Приключенческая RPG в фэнтезийном мире",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Элрик", role = "Воин" },
                new CharacterData { name = "Лиана", role = "Маг" }
            }
        });

        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
        games.Add(new GameData
        {
            id = "2",
            title = "Космическая одиссея",
            description = "Исследуйте галактику в поисках новых миров",
            characters = new List<CharacterData>
            {
                new CharacterData { name = "Кэптен Нова", role = "Капитан" },
                new CharacterData { name = "R2-X2", role = "Механик" }
            }
        });
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
        isHoveringButton = false;
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

        // Проверяем наведение на кнопку
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            isHoveringButton = true;
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

        // Проверяем наведение на кнопку
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            isHoveringButton = true;
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
                    DrawCreateGameCard(cardWidth+7, 120);
                }
                else if (index - 1 < games.Count)
                {
                    DrawGameCard(games[index - 1], cardWidth, 120);
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
    
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            isHoveringButton = true;
        }
    }

private float EstimateCardHeight(GameData game, float width)
{
    // Оцениваем высоту карточки на основе содержимого
    float height = 60; // Минимальная высота (заголовок и отступы)
    
    // Добавляем высоту для описания (примерно 2 строки)
    height += 40;
    
    // Добавляем высоту для кнопок
    height += 40;
    
    return height;
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

private void DrawGameCard(GameData game, float width, float height)
{
    GUILayout.BeginVertical(cardStyle, GUILayout.Width(width), GUILayout.Height(height));
    
    // Заголовок игры
    GUILayout.Label(game.title, cardTitleStyle);
    
    // Описание игры с ограничением по высоте
    float descriptionHeight = height - 100; // Вычитаем высоту заголовка и кнопок
    Rect descriptionRect = GUILayoutUtility.GetRect(width - 30, descriptionHeight, EditorStyles.wordWrappedLabel);
    GUI.Label(descriptionRect, game.description, EditorStyles.wordWrappedLabel);
    
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
        Debug.Log($"Настройки игры: {game.title}");
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

        GUILayout.Label(selectedGame.title, centeredLabelStyle);
        GUILayout.Space(20);

        GUILayout.Label("Описание:", EditorStyles.boldLabel);
        GUILayout.Label(selectedGame.description, EditorStyles.wordWrappedLabel);

        GUILayout.Space(20);

        GUILayout.Label("Персонажи:", EditorStyles.boldLabel);

        foreach (var character in selectedGame.characters)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"• {character.name} ({character.role})");
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
        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width - 40)) / 2,
            20,
            Mathf.Min(400, position.width * 0.8f),
            position.height - 40
        ));
        GUILayout.BeginVertical();
        GUILayout.Label("Игра", centeredLabelStyle);
        
        
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

        // Проверяем наведение на кнопку
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            isHoveringButton = true;
        }

        GUILayout.Space(20);
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    public void SwitchPage(Page page)
    {
        previousPage = currentPage;
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

        currentPage = page;
    }
}