using UnityEngine;
using UnityEngine.InputSystem;

namespace DuckovVP.Views;

public class ConversionUtils
{
    public static string KeyCodeToKey(KeyCode keyCode,
    string unknownKey = "")
{
    switch (keyCode)
    {
        case KeyCode.None:              return "<Keyboard>/none";
        case KeyCode.Backspace:         return "<Keyboard>/backspace";
        case KeyCode.Delete:            return "<Keyboard>/delete";
        case KeyCode.Tab:               return "<Keyboard>/tab";
        case KeyCode.Clear:             return unknownKey; // Conversion unknown.
        case KeyCode.Return:            return "<Keyboard>/enter";
        case KeyCode.Pause:             return "<Keyboard>/pause";
        case KeyCode.Escape:            return "<Keyboard>/escape";
        case KeyCode.Space:             return "<Keyboard>/space";
        case KeyCode.Keypad0:           return "<Keyboard>/numpad0";
        case KeyCode.Keypad1:           return "<Keyboard>/numpad1";
        case KeyCode.Keypad2:           return "<Keyboard>/numpad2";
        case KeyCode.Keypad3:           return "<Keyboard>/numpad3";
        case KeyCode.Keypad4:           return "<Keyboard>/numpad4";
        case KeyCode.Keypad5:           return "<Keyboard>/numpad5";
        case KeyCode.Keypad6:           return "<Keyboard>/numpad6";
        case KeyCode.Keypad7:           return "<Keyboard>/numpad7";
        case KeyCode.Keypad8:           return "<Keyboard>/numpad8";
        case KeyCode.Keypad9:           return "<Keyboard>/numpad9";
        case KeyCode.KeypadPeriod:      return "<Keyboard>/numpadperiod";
        case KeyCode.KeypadDivide:      return "<Keyboard>/numpaddivide";
        case KeyCode.KeypadMultiply:    return "<Keyboard>/numpadmultiply";
        case KeyCode.KeypadMinus:       return "<Keyboard>/numpadminus";
        case KeyCode.KeypadPlus:        return "<Keyboard>/numpadplus";
        case KeyCode.KeypadEnter:       return "<Keyboard>/numpadenter";
        case KeyCode.KeypadEquals:      return "<Keyboard>/numpadequals";
        case KeyCode.UpArrow:           return "<Keyboard>/uparrow";
        case KeyCode.DownArrow:         return "<Keyboard>/downarrow";
        case KeyCode.RightArrow:        return "<Keyboard>/rightarrow";
        case KeyCode.LeftArrow:         return "<Keyboard>/leftarrow";
        case KeyCode.Insert:            return "<Keyboard>/insert";
        case KeyCode.Home:              return "<Keyboard>/home";
        case KeyCode.End:               return "<Keyboard>/end";
        case KeyCode.PageUp:            return "<Keyboard>/pageup";
        case KeyCode.PageDown:          return "<Keyboard>/pagedown";
        case KeyCode.F1:                return "<Keyboard>/f1";
        case KeyCode.F2:                return "<Keyboard>/f2";
        case KeyCode.F3:                return "<Keyboard>/f3";
        case KeyCode.F4:                return "<Keyboard>/f4";
        case KeyCode.F5:                return "<Keyboard>/f5";
        case KeyCode.F6:                return "<Keyboard>/f6";
        case KeyCode.F7:                return "<Keyboard>/f7";
        case KeyCode.F8:                return "<Keyboard>/f8";
        case KeyCode.F9:                return "<Keyboard>/f9";
        case KeyCode.F10:               return "<Keyboard>/f10";
        case KeyCode.F11:               return "<Keyboard>/f11";
        case KeyCode.F12:               return "<Keyboard>/f12";
        case KeyCode.F13:               return unknownKey; // Conversion unknown.
        case KeyCode.F14:               return unknownKey; // Conversion unknown.
        case KeyCode.F15:               return unknownKey; // Conversion unknown.
        case KeyCode.Alpha0:            return "<Keyboard>/digit0";
        case KeyCode.Alpha1:            return "<Keyboard>/digit1";
        case KeyCode.Alpha2:            return "<Keyboard>/digit2";
        case KeyCode.Alpha3:            return "<Keyboard>/digit3";
        case KeyCode.Alpha4:            return "<Keyboard>/digit4";
        case KeyCode.Alpha5:            return "<Keyboard>/digit5";
        case KeyCode.Alpha6:            return "<Keyboard>/digit6";
        case KeyCode.Alpha7:            return "<Keyboard>/digit7";
        case KeyCode.Alpha8:            return "<Keyboard>/digit8";
        case KeyCode.Alpha9:            return "<Keyboard>/digit9";
        case KeyCode.Exclaim:           return unknownKey; // Conversion unknown.
        case KeyCode.DoubleQuote:       return unknownKey; // Conversion unknown.
        case KeyCode.Hash:              return unknownKey; // Conversion unknown.
        case KeyCode.Dollar:            return unknownKey; // Conversion unknown.
        case KeyCode.Percent:           return unknownKey; // Conversion unknown.
        case KeyCode.Ampersand:         return unknownKey; // Conversion unknown.
        case KeyCode.Quote:             return "<Keyboard>/quote";
        case KeyCode.LeftParen:         return unknownKey; // Conversion unknown.
        case KeyCode.RightParen:        return unknownKey; // Conversion unknown.
        case KeyCode.Asterisk:          return unknownKey; // Conversion unknown.
        case KeyCode.Plus:              return "<Keyboard>/none"; // TODO
        case KeyCode.Comma:             return "<Keyboard>/comma";
        case KeyCode.Minus:             return "<Keyboard>/minus";
        case KeyCode.Period:            return "<Keyboard>/period";
        case KeyCode.Slash:             return "<Keyboard>/slash";
        case KeyCode.Colon:             return unknownKey; // Conversion unknown.
        case KeyCode.Semicolon:         return "<Keyboard>/semicolon";
        case KeyCode.Less:              return "<Keyboard>/none";
        case KeyCode.Equals:            return "<Keyboard>/equals";
        case KeyCode.Greater:           return unknownKey; // Conversion unknown.
        case KeyCode.Question:          return unknownKey; // Conversion unknown.
        case KeyCode.At:                return unknownKey; // Conversion unknown.
        case KeyCode.LeftBracket:       return "<Keyboard>/leftbracket";
        case KeyCode.Backslash:         return "<Keyboard>/backslash";
        case KeyCode.RightBracket:      return "<Keyboard>/rightbracket";
        case KeyCode.Caret:             return "<Keyboard>/none"; // TODO
        case KeyCode.Underscore:        return unknownKey; // Conversion unknown.
        case KeyCode.BackQuote:         return "<Keyboard>/backquote";
        case KeyCode.A:                 return "<Keyboard>/a";
        case KeyCode.B:                 return "<Keyboard>/b";
        case KeyCode.C:                 return "<Keyboard>/c";
        case KeyCode.D:                 return "<Keyboard>/d";
        case KeyCode.E:                 return "<Keyboard>/e";
        case KeyCode.F:                 return "<Keyboard>/f";
        case KeyCode.G:                 return "<Keyboard>/g";
        case KeyCode.H:                 return "<Keyboard>/h";
        case KeyCode.I:                 return "<Keyboard>/i";
        case KeyCode.J:                 return "<Keyboard>/j";
        case KeyCode.K:                 return "<Keyboard>/k";
        case KeyCode.L:                 return "<Keyboard>/l";
        case KeyCode.M:                 return "<Keyboard>/m";
        case KeyCode.N:                 return "<Keyboard>/n";
        case KeyCode.O:                 return "<Keyboard>/o";
        case KeyCode.P:                 return "<Keyboard>/p";
        case KeyCode.Q:                 return "<Keyboard>/q";
        case KeyCode.R:                 return "<Keyboard>/r";
        case KeyCode.S:                 return "<Keyboard>/s";
        case KeyCode.T:                 return "<Keyboard>/t";
        case KeyCode.U:                 return "<Keyboard>/u";
        case KeyCode.V:                 return "<Keyboard>/v";
        case KeyCode.W:                 return "<Keyboard>/w";
        case KeyCode.X:                 return "<Keyboard>/x";             
        case KeyCode.Y:                 return "<Keyboard>/y";
        case KeyCode.Z:                 return "<Keyboard>/z";
        case KeyCode.LeftCurlyBracket:  return unknownKey; // Conversion unknown.
        case KeyCode.Pipe:              return unknownKey; // Conversion unknown.
        case KeyCode.RightCurlyBracket: return unknownKey; // Conversion unknown.
        case KeyCode.Tilde:             return unknownKey; // Conversion unknown.
        case KeyCode.Numlock:           return "<Keyboard>/numlock";
        case KeyCode.CapsLock:          return "<Keyboard>/capslock";
        case KeyCode.ScrollLock:        return "<Keyboard>/scrolllock";
        case KeyCode.RightShift:        return "<Keyboard>/rightshift";
        case KeyCode.LeftShift:         return "<Keyboard>/leftshift";
        case KeyCode.RightControl:      return "<Keyboard>/rightctrl";
        case KeyCode.LeftControl:       return "<Keyboard>/leftctrl";
        case KeyCode.RightAlt:          return "<Keyboard>/rightalt";
        case KeyCode.LeftAlt:           return "<Keyboard>/leftalt";
        case KeyCode.LeftCommand:       return "<Keyboard>/leftcommand";
          // case KeyCode.LeftApple: (same as LeftCommand)
        case KeyCode.LeftWindows:       return "<Keyboard>/leftwindows";
        case KeyCode.RightCommand:      return "<Keyboard>/rightcommand";
          // case KeyCode.RightApple: (same as RightCommand)
        case KeyCode.RightWindows:      return "<Keyboard>/rightwindows";
        case KeyCode.AltGr:             return "<Keyboard>/altgr";
        case KeyCode.Help:              return unknownKey; // Conversion unknown.
        case KeyCode.Print:             return "<Keyboard>/printscreen";
        case KeyCode.SysReq:            return unknownKey; // Conversion unknown.
        case KeyCode.Break:             return unknownKey; // Conversion unknown.
        case KeyCode.Menu:              return "<Keyboard>/contextmenu";
        case KeyCode.Mouse0: return "<Mouse>/leftButton";
        case KeyCode.Mouse1: return "<Mouse>/rightButton";
        case KeyCode.Mouse2: return "<Mouse>/middleButton";
        case KeyCode.Mouse3: return "<Mouse>/backButton";
        case KeyCode.Mouse4: return "<Mouse>/forwardButton";
        case KeyCode.Mouse5: return "<Mouse>/plugin0";
        case KeyCode.Mouse6: return "<Mouse>/plugin1";

        // All other keys are joystick keys which do not
        // exist anymore in the new input system.
        default:
            return "";
    }
}
}