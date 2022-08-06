using UnityEngine;

public static class ExtensionMethods
{
    private static int s_alphaKeyCodeNameDifference = (int)KeyCode.A - 'A';
    

    public static string GetKeyName(this KeyCode keyCode)
    {
        if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
        {
            return keyCode.ToString();
        }
        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
        {
            return ((char)keyCode).ToString();
        }
        if (keyCode >= KeyCode.Mouse0 && keyCode <= KeyCode.Mouse6)
        {
            return keyCode.ToString();
        }
        switch (keyCode)
        {
            case KeyCode.BackQuote:
                return "`";
            case KeyCode.Minus:
                return "-";
            case KeyCode.Equals:
                return "=";
            case KeyCode.LeftBracket:
                return "[";
            case KeyCode.RightBracket:
                return "]";
            case KeyCode.Backslash:
                return "\\";
            case KeyCode.Semicolon:
                return ";";
            case KeyCode.Quote:
                return "'";
            case KeyCode.Comma:
                return ",";
            case KeyCode.Period:
                return ".";
            case KeyCode.Slash:
                return "/";
            case KeyCode.Space:
                return "Space";
            case KeyCode.Backspace:
                return "Backspace";
            case KeyCode.Tab:
                return "Tab";
            case KeyCode.CapsLock:
                return "CapsLock";
            case KeyCode.Return:
                return "Enter";
            case KeyCode.LeftShift:
                return "LeftShift";
            case KeyCode.RightShift:
                return "RightShift";
            case KeyCode.LeftControl:
                return "LeftCtrl";
            case KeyCode.RightControl:
                return "RightCtrl";
            case KeyCode.LeftAlt:
                return "LeftAlt";
            case KeyCode.RightAlt:
                return "RightAlt";
            default:
                return string.Empty;
        }
    }

    public static KeyCode GetKeyCode(this string keyName)
    {
        if (string.IsNullOrEmpty(keyName))
        {
            return KeyCode.None;
        }
        if (keyName.Length == 1)
        {
            if (keyName[0] >= 'A' && keyName[0] <= 'Z')
            {
                return (KeyCode)(keyName[0] + s_alphaKeyCodeNameDifference);
            }
            if (keyName[0] >= '0' && keyName[0] <= '9')
            {
                return (KeyCode)keyName[0];
            }
        }
        if (keyName == "`")
            return KeyCode.BackQuote;
        if (keyName == "-")
            return KeyCode.Minus;
        if (keyName == "=")
            return KeyCode.Equals;
        if (keyName == "[")
            return KeyCode.LeftBracket;
        if (keyName == "]")
            return KeyCode.RightBracket;
        if (keyName == "\\")
            return KeyCode.Backslash;
        if (keyName == ";")
            return KeyCode.Semicolon;
        if (keyName == "'")
            return KeyCode.Quote;
        if (keyName == ",")
            return KeyCode.Comma;
        if (keyName == ".")
            return KeyCode.Period;
        if (keyName == "/")
            return KeyCode.Slash;
        if (keyName == "Space")
            return KeyCode.Space;
        if (keyName == "Backspace")
            return KeyCode.Backspace;
        if (keyName == "Tab")
            return KeyCode.Tab;
        if (keyName == "CapsLock")
            return KeyCode.CapsLock;
        if (keyName == "Enter")
            return KeyCode.Return;
        if (keyName == "LeftShift")
            return KeyCode.LeftShift;
        if (keyName == "RightShift")
            return KeyCode.RightShift;
        if (keyName == "LeftCtrl")
            return KeyCode.LeftControl;
        if (keyName == "RightCtrl")
            return KeyCode.RightControl;
        if (keyName == "LeftAlt")
            return KeyCode.LeftAlt;
        if (keyName == "RightAlt")
            return KeyCode.RightAlt;
        if (keyName == "Mouse0")
            return KeyCode.Mouse0;
        if (keyName == "Mouse1")
            return KeyCode.Mouse1;
        if (keyName == "Mouse2")
            return KeyCode.Mouse2;
        if (keyName == "Mouse3")
            return KeyCode.Mouse3;
        if (keyName == "Mouse4")
            return KeyCode.Mouse4;
        if (keyName == "Mouse5")
            return KeyCode.Mouse5;
        if (keyName == "Mouse6")
            return KeyCode.Mouse6;
        return KeyCode.None;
    }
}