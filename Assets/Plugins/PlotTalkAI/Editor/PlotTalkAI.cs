using UnityEditor;
using UnityEngine;

public class PlotTalkAI : EditorWindow
{
    private enum Page
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

    [MenuItem("Window/PlotTalkAI")]
    public static void ShowWindow()
    {
        GetWindow<PlotTalkAI>("PlotTalkAI");
    }

    private void OnGUI()
    {
        Rect screenRect = new Rect(0, 0, position.width, position.height);
        GUILayout.BeginArea(screenRect);
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical("box", GUILayout.Width(350));
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
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    private void DrawLoginPage()
    {
        CenteredLabel("Вход", 18, true);
        GUILayout.Space(10);
        loginEmail = TextFieldStyled("Email", loginEmail);
        loginPassword = PasswordFieldStyled("Пароль", loginPassword);
        GUILayout.Space(15);
        if (ButtonGradient("Войти", 30))
        {
            if (!string.IsNullOrEmpty(loginEmail) && loginPassword == "1234") currentPage = Page.Main;
            else EditorUtility.DisplayDialog("Ошибка", "Неверные данные", "OK");
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Регистрация", LinkStyle())) currentPage = Page.Register;
    }

    private void DrawRegisterPage()
    {
        CenteredLabel("Регистрация", 18, true);
        GUILayout.Space(10);
        registerEmail = TextFieldStyled("Email", registerEmail);
        registerPassword = PasswordFieldStyled("Пароль", registerPassword);
        registerConfirm = PasswordFieldStyled("Повторите пароль", registerConfirm);
        GUILayout.Space(15);
        if (ButtonGradient("Зарегистрироваться", 30))
        {
            if (string.IsNullOrEmpty(registerEmail) || string.IsNullOrEmpty(registerPassword)) EditorUtility.DisplayDialog("Ошибка", "Заполните все поля", "OK");
            else if (registerPassword != registerConfirm) EditorUtility.DisplayDialog("Ошибка", "Пароли не совпадают", "OK");
            else
            {
                EditorUtility.DisplayDialog("Успех", "Регистрация прошла успешно", "OK");
                currentPage = Page.Login;
            }
        }
        GUILayout.Space(10);
        if (GUILayout.Button("Назад", LinkStyle())) currentPage = Page.Login;
    }

    private void DrawMainPage()
    {
        CenteredLabel("Добро пожаловать!", 20, true);
        GUILayout.Space(10);
        if (ButtonGradient("Выйти", 25))
        {
            currentPage = Page.Login;
            loginEmail = "";
            loginPassword = "";
        }
    }
    private void CenteredLabel(string text, int size, bool bold = false)
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal
        };
        GUILayout.Label(text, style);
    }

    private string TextFieldStyled(string placeholder, string value)
    {
        var style = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };
        return EditorGUILayout.TextField(placeholder, value, style, GUILayout.Height(25));
    }

    private string PasswordFieldStyled(string placeholder, string value)
    {
        var style = new GUIStyle(EditorStyles.textField)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };
        return EditorGUILayout.PasswordField(placeholder, value, style, GUILayout.Height(25));
    }

    private bool ButtonGradient(string text, int height)
    {
        Rect rect = GUILayoutUtility.GetRect(200, height, GUILayout.ExpandWidth(true));
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUI.DrawRect(rect, new Color(0.7f, 0.5f, 1f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(0.7f, 0.5f, 1f, 0.5f));
        return GUI.Button(rect, text, style);
    }

    private GUIStyle LinkStyle()
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = Color.blue },
            alignment = TextAnchor.MiddleCenter
        };
        return style;
    }
}