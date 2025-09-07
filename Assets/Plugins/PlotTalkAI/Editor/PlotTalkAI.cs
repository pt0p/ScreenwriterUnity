using Plugins.PlotTalkAI.Utils;
using UnityEditor;
using UnityEngine;

public class PlotTalkAI : EditorWindow
{
    public enum Page
    {
        Login,
        Register,
        Main
    }

    private Page currentPage = Page.Login;
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
    
    private bool isHoveringLink = false;
    private bool isHoveringButton = false;

    [MenuItem("Window/PlotTalkAI")]
    public static void ShowWindow()
    {
        GetWindow<PlotTalkAI>("PlotTalkAI").minSize = new Vector2(400, 500);
    }

    private void OnEnable()
    {
        SwitchPage(currentPage);
        // Создаем стили один раз при инициализации
        CreateStyles();
    }

    private void OnGUI()
    {
        // Используем стандартный фон Unity
        Color backgroundColor = EditorGUIUtility.isProSkin ? 
            new Color(0.22f, 0.22f, 0.22f) : 
            new Color(0.76f, 0.76f, 0.76f);
        
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), backgroundColor);

        // Центрированный контент
        GUILayout.BeginArea(new Rect(
            (position.width - Mathf.Min(400, position.width * 0.8f)) / 2, 
            20, 
            Mathf.Min(400, position.width * 0.8f), 
            position.height - 40
        ));
        
        // Вертикальный отступ
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
        }
        
        GUILayout.Space(20);
        GUILayout.EndVertical();
        
        GUILayout.EndArea();

        // Изменяем курсор при наведении на ссылку
        EditorGUIUtility.AddCursorRect(new Rect(0, 0, position.width, position.height), 
            isHoveringLink ? MouseCursor.Link : MouseCursor.Arrow);
        
        isHoveringLink = false; // Сбрасываем флаг каждый кадр
        isHoveringButton = false; // Сбрасываем флаг каждый кадр
    }

    private void CreateStyles()
    {
        // Определяем цвета в зависимости от темы
        Color textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        Color linkColor = EditorGUIUtility.isProSkin ? 
            new Color(0.85f, 0.85f, 0.85f) : // Синий для темной темы
            new Color(0.1f, 0.3f, 0.8f);   // Синий для светлой темы
        
        // Создаем стили
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
    }

    private void DrawLoginPage()
    {
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
    }

    private void DrawRegisterPage()
    {
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
    }

    private void DrawMainPage()
    {
        GUILayout.Label($"Добро пожаловать,\n{loginEmail}!", centeredLabelStyle);
        GUILayout.Space(30);
        
        // Кнопка "Выйти"
        Rect buttonRect = GUILayoutUtility.GetRect(GUIContent.none, buttonStyle, GUILayout.Height(35));
        if (GUI.Button(buttonRect, "Выйти", buttonStyle))
        {
            StorageApi.GetInstance().LogOut();
            SwitchPage(Page.Login);
            loginEmail = "";
            loginPassword = "";
        }
        
        // Проверяем наведение на кнопку
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            isHoveringButton = true;
        }
    }

    public void SwitchPage(Page page)
    {
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
            // User-included pages
            if (!loggedIn)
            {
                currentPage = Page.Login;
                return;
            }
        }
        currentPage = page;
    }
}