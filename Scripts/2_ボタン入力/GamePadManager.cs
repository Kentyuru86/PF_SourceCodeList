using UnityEngine;

/// <summary>
/// コントローラの名前のリスト
/// </summary>
public enum GamePadName
{
    // PlayStation
    DUALSHOCK4,
    DUALSHOCK3,

    // Nintendo
    NSProController,

    // XBox
    XBOX360,
    XBOXOne,

    // 該当なし
    Unknown,

    // 未接続
    NoDevice
}

/// <summary>
/// コントローラのボタンのリスト
/// </summary>
public enum GamePadKeyCode
{
    // ボタン
    A = 0,
    B = 1,
    X = 2,
    Y = 3,
    L = 4,
    R = 5,
    LT = 6,
    RT = 7,
    Start = 8,
    Select = 9,
    Home = 10,

    // スティック
    LStickH = 11,
    LStickV = 12,
    RStickH = 13,
    RStickV = 14,
    LStickButton = 15,
    RStickButton = 16,

    // 十字キー
    CrossKeyH = 17,
    CrossKeyV = 18
}

/// <summary>
/// ゲームパッドの情報（デバイス情報、ボタン入力）を取り扱うクラス
/// </summary>
public static class GamePadManager
{
    #region ### Parameters ###

    /// <summary>
    /// コントローラに割り当てられた番号
    /// (-1=接続されていない,-1以外=割り当て先のインプットマネージャーの番号)
    /// </summary>
    public static int[] padNumber =
    {
        0, -1, -1, -1,
        -1, -1, -1, -1,
        -1, -1, -1, -1,
        -1, -1, -1, -1
    };

    /// <summary>
    /// 使用するコントローラの数
    /// (使用するために登録したものの数のみ計算)
    /// </summary>
    public static int padNum;

    private static float pressed = 0;

    /// <summary>
    /// 押されたかどうか(0=押されてない,-1or1=押されている)
    /// </summary>
    static float[] isDownLStick = { 0, 0 };
    /// <summary>
    /// 押されたかどうか(0=押されてない,-1or1=押されている)
    /// </summary>
    static float[] isDownRStick = { 0, 0 };

    /// <summary>
    /// 押されたかどうか(0=押されてない,-1or1=押されている)
    /// </summary>
    static float[] isDownCrossKey = { 0, 0 };

    /// <summary>
    /// Lスティックが倒されたかどうか
    /// [プレイヤー番号, (0=左右, 1=上下)]
    /// </summary>
    static bool[,] isDownLSticks = { { false, false }, { false, false }, { false, false }, { false, false } };

    /// <summary>
    /// 十字キーが倒されたかどうか
    /// [プレイヤー番号, (0=左右, 1=上下)]
    /// </summary>
    static bool[,] isDownCrossKeys = { { false, false }, { false, false }, { false, false }, { false, false } };

    /// <summary>
    /// 登録されているコントローラーの数を取得
    /// </summary>
    public static int NumOfRegisteredGamePad
    {
        get
        {
            padNum = 0;
            for (int i = 0; i < padNumber.Length; i++)
            {
                if (padNumber[i] != -1)
                {
                    padNum++;
                }

            }

            return padNum;
        }
    }

    [Header("String")]
    const string StrNoConnection = "No Connection";
    const string StrWirelessController = "Wireless Controller";
    const string StrPs3Controller = "PLAYSTAION(R)3Controller";
    const string StrWirelessGamepad = "Wireless Gamepad";
    const string StrXbox360T = "Controller (XBOX 360 For Windows)";
    const string StrXboxOneT = "Controller (Xbox One For Windows)";
    const string StrEmpty = "";
    
    const string StrDualshock4 = "DUAL SHOCK 4";
    const string StrDualshock3 = "DUAL SHOCK 3";
    const string StrNSProcontroller = "Pro Controller";
    const string StrXbox360 = "XBOX360";
    const string StrXboxOne = "XBOXOne";

    // InputManagerの先頭の省略名
    const string StrAbbDualshock4 = "DS4";
    const string StrAbbDualshock3 = "DS3";
    const string StrAbbNSProcontroller = "NSP";
    const string StrAbbXbox360 = "XBOX360";
    const string StrAbbXboxOne = "XBOXOne";
    const string StrNoKeycode = "No KeyCode";

    #endregion ### Parameters ###

    #region ### Methods / GamePad Status ###

    /// <summary>
    /// ≪ゲーム起動時専用≫コントローラに割り当てられた番号を初期化
    /// </summary>
    /// <param name="initializeZero">0番目も初期化するかどうか</param>
    public static void InitializePadNumber(bool initializeZero)
    {
        if (initializeZero)
        {
            padNumber[0] = -1;
        }
        else
        {
            padNumber[0] = 0;
        }
        for (int i = 1; i < padNumber.Length; i++)
        {
            padNumber[i] = -1;
        }
    }

    /// <summary>
    /// コントローラに割り当てられた番号を初期化
    /// </summary>
    public static void ResetPadNumber()
    {
        for (int i = 0; i < padNumber.Length; i++)
        {
            padNumber[i] = -1;
        }
    }

    /// <summary>
    /// 使用するコントローラの数を計算
    /// </summary>
    public static void CalcPadNum()
    {
        padNum = 0;
        for (int i = 0; i < padNumber.Length; i++)
        {
            if(padNumber[i] != -1)
            {
                padNum++;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// ゲームパッド名(UnityDeviceでの名前)を返す
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static string GetJoyStickName(int num)
    {
        // 指定した番号にコントローラが接続されていなければ
        // 「未接続」と返す
        if (num > Input.GetJoystickNames().Length - 1 || num == -1)
        {
            return StrNoConnection;
        }

        return Input.GetJoystickNames()[num];
    }

    /// <summary>
    /// 登録されたJoyStickの名前(OS上での名前)を返す
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static string GetRegisteredJoyStickName(int num)
    {
        return GetJoyStickName(padNumber[num]);
    }

    /// <summary>
    /// 登録されたJoyStickの名前(Unity上での名前)を返す
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static string GetRegisteredDeviceName(int num)
    {
        return GetDeviceName(padNumber[num]).ToString();
    }

    
    /// <summary>
    /// ゲームパッド名(製品名)を返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <returns></returns>
    public static string CheckDeviceName(int num)
    {
        // 指定した番号にコントローラが接続されていなければ
        // 「未接続」と返す
        if (num > Input.GetJoystickNames().Length - 1 || num == -1)
        {
            return "No Connection";
        }

        // PCでのコントローラ名を正式な製品名に変換する
        switch (Input.GetJoystickNames()[num])
        {
            case StrWirelessController: //PS4コントローラ
                return "DUAL SHOCK 4";
            case StrPs3Controller: //PS3コントローラ
                return "DUAL SHOCK 3";
            case StrWirelessGamepad:    //NintendoSwitchPROコントローラ
                return "Pro Controller";
            case StrXbox360: //XBOX360
                return "XBOX360";
            case StrXboxOne: //XBOXOne
                return "XBOXOne";
            case StrEmpty:   // つながっていないときはエラーとする
                return "No Connection";
            default:  // PCで判別できないコントローラ
                return "Unknown Controller";
        }
    }
    
    /// <summary>
    /// ゲームパッド名(製品名)を返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <returns></returns>
    public static GamePadName GetDeviceName(int num)
    {
        // 指定した番号にコントローラが接続されていなければ
        // 「未接続」と返す
        if (num > Input.GetJoystickNames().Length - 1 || num < 0)
        {
            return GamePadName.NoDevice;
        }

        // PCでのコントローラ名を正式な製品名に変換する
        switch (Input.GetJoystickNames()[num])
        {
            case StrWirelessController: // PS4コントローラ
                return GamePadName.DUALSHOCK4;
            case StrPs3Controller: // PS3コントローラ
                return GamePadName.DUALSHOCK3;
            case StrWirelessGamepad:    // NintendoSwitchPROコントローラ
                return GamePadName.NSProController;
            case StrXbox360T: //XBOX360
                return GamePadName.XBOX360;
            case StrXboxOneT: // XBOXOne
                return GamePadName.XBOXOne;
            case StrEmpty:   // つながっていないときはエラーとする
                return GamePadName.NoDevice;
            default:  // PCで判別できないコントローラ
                return GamePadName.Unknown;
        }
    }

    /// <summary>
    /// ゲームパッド名(製品名)を返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <returns></returns>
    public static string DebugCheckDeviceName(int num)
    {
        // 指定した番号にコントローラが接続されていなければ
        // 「未接続」と返す
        if (num > Input.GetJoystickNames().Length - 1 || num < 0)
        {
            return "No Connection";
        }

        // PCでのコントローラ名を正式な製品名に変換する
        switch (Input.GetJoystickNames()[num])
        {
            case "Wireless Controller": //PS4コントローラ
                return "DUAL SHOCK 4";
            case "PLAYSTAION(R)3Controller": //PS3コントローラ
                return "DUAL SHOCK 3";
            case "Wireless Gamepad":    //NintendoSwitchPROコントローラ
                return "Pro Controller";
            case "Controller (XBOX 360 For Windows)": //XBOX360コントローラ
                return "XBOX360";
            case "Controller (Xbox One For Windows)":
                return "XBOXOne";
            case "":   //つながっていないときはエラーとする
                return "No Connection";
            default:  //PCで判別できないコントローラ
                return "Unknown Controller";
        }
    }

    /// <summary>
    /// ゲームパッド名(製品名)を返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <returns></returns>
    public static GamePadName GetDebugDeviceName(int num)
    {
        // 指定した番号にコントローラが接続されていなければ
        // 「未接続」と返す
        if (num > Input.GetJoystickNames().Length - 1 || num < 0)
        {
            return GamePadName.NoDevice;
        }

        try
        {
            // PCでのコントローラ名を正式な製品名に変換する
            switch (Input.GetJoystickNames()[num])
            {
                case StrWirelessController: //PS4コントローラ
                    return GamePadName.DUALSHOCK4;
                case StrPs3Controller: //PS3コントローラ
                    return GamePadName.DUALSHOCK3;
                case StrWirelessGamepad:    //NintendoSwitchPROコントローラ
                    return GamePadName.NSProController;
                case StrXbox360: //XBOX360
                    return GamePadName.XBOX360;
                case StrXboxOne: //XBOXOne
                    return GamePadName.XBOXOne;
                case StrEmpty:   // つながっていないときはエラーとする
                    return GamePadName.NoDevice;
                default:  // PCで判別できないコントローラ
                    return GamePadName.Unknown;
            }
        }
        catch(System.IndexOutOfRangeException e)
        {
            Debug.LogError($"{num}番のコントローラは以前に接続したが、現在接続が切れているか、認識エラーです。\n{e}");
            return GamePadName.NoDevice;
        }
    }

    #region ### Button Name ###

    /// <summary>
    /// コントローラ毎のポーズボタンの名称を返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <returns></returns>
    public static string GetPauseButtonName(int num)
    {
        switch (GamePadManager.GetDeviceName(num))
        {
            case GamePadName.DUALSHOCK4:
                return "OPTIONSボタン";
            case GamePadName.DUALSHOCK3:
                return "SELECTボタン";
            case GamePadName.NSProController:
                return "+ボタン";
            case GamePadName.XBOX360:
                return "Startボタン";
            case GamePadName.XBOXOne:
                return "メニューボタン";
            default:
                return "Escapeキー";
        }
    }

    #endregion ### Button Name ###

    #endregion ### Methods / GamePad Status ###

    #region ### Methods / Input Status ###

    /// <summary>
    /// ゲームパッドの種類と接続番号に応じたキーを返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <param name="keyCodeName">入力キーの名称</param>
    /// <returns></returns>
    public static string GetInputManagerKeyName(int num, string keyCodeName)
    {
        switch (CheckDeviceName(num))
        {
            case StrDualshock4:    // PS4コントローラ
                return "DS4" + num.ToString() + keyCodeName;
            case StrDualshock3:    // PS3コントローラ
                return "DS3" + num.ToString() + keyCodeName;
            case StrNSProcontroller:  // ニンテンドースイッチPROコントローラ
                return "NSP" + num.ToString() + keyCodeName;
            case StrXbox360:            // XBOX360コントローラ
                return "XBOX360" + num.ToString() + keyCodeName;
            case StrXboxOne:            // XBOXOneコントローラ
                return "XBOXOne" + num.ToString() + keyCodeName;
            default:
                return "No KeyCode";
        }
    }

    /// <summary>
    /// ゲームパッドの種類と接続番号に応じたキーを返す
    /// </summary>
    /// <param name="num">コントローラの接続番号</param>
    /// <param name="keyCodeName">入力キーの名称</param>
    /// <returns></returns>
    public static string GetInputManagerKeyName(int num, GamePadKeyCode key)
    {
        switch (GetDeviceName(num))
        {
            case GamePadName.DUALSHOCK4:    // PS4コントローラ
                return StrAbbDualshock4 + num.ToString() + key.ToString();
            case GamePadName.DUALSHOCK3:    // PS3コントローラ
                return StrAbbDualshock3 + num.ToString() + key.ToString();
            case GamePadName.NSProController:  // ニンテンドースイッチPROコントローラ
                return StrAbbNSProcontroller + num.ToString() + key.ToString();
            case GamePadName.XBOX360:            // XBOX360コントローラ
                return StrAbbXbox360 + num.ToString() + key.ToString();
            case GamePadName.XBOXOne:            // XBOXOneコントローラ
                return StrAbbXboxOne + num.ToString() + key.ToString();
            default:
                return StrNoKeycode;
        }
    }


    #region ### Button ###

    /// <summary>
    /// ジョイスティックのボタンが押されたかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetJoyStickButtonDown(int playerNum, string keyCodeName)
    {
        //接続されていない場合はfalseを返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;
        return Input.GetButtonDown(GetInputManagerKeyName(padNumber[playerNum], keyCodeName));
    }

    /// <summary>
    /// ジョイスティックのボタンが押されたかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetButtonDown(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDeviceName(padNumber[playerNum]) == GamePadName.NoDevice) return false;
        return Input.GetButtonDown(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    /// <summary>
    /// ジョイスティックのボタンが押されたかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetDebugButtonDown(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDebugDeviceName(playerNum) == GamePadName.NoDevice)
        {
            return false;
        }

        return Input.GetButtonDown(GetInputManagerKeyName(playerNum, key));
    }

    /// <summary>
    /// ジョイスティックのボタンが押されているかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetJoyStickButton(int playerNum, string keyCodeName)
    {
        
        //接続されていない場合はfalseを返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;
        return Input.GetButton(GetInputManagerKeyName(padNumber[playerNum], keyCodeName));
    }

    /// <summary>
    /// ジョイスティックのボタンが押されているかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="key">キー</param>
    /// <returns></returns>
    public static bool GetButton(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDeviceName(padNumber[playerNum]) == GamePadName.NoDevice)
        {
            return false;
        }


        return Input.GetButton(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    /// <summary>
    /// ジョイスティックのボタンが押されたかどうかを判定
    /// </summary>
    /// <param name="controllerNum">コントローラの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetDebugJoyStickButton(int controllerNum, string keyCodeName)
    {
        //接続されていない場合はfalseを返す
        if (DebugCheckDeviceName(controllerNum).Equals("No Connection")) return false;
        return Input.GetButton(GetInputManagerKeyName(controllerNum, keyCodeName));
    }

    public static bool GetDebugButton(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDebugDeviceName(padNumber[playerNum]) == GamePadName.NoDevice)
        {
            return false;
        }


        return Input.GetButton(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    /// <summary>
    /// ジョイスティックのボタンが離されたかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static bool GetJoyStickButtonUp(int playerNum, string keyCodeName)
    {
        //接続されていない場合はfalseを返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;
        return Input.GetButtonUp(GetInputManagerKeyName(padNumber[playerNum], keyCodeName));
    }

    /// <summary>
    /// ジョイスティックのボタンが離されたかどうかを判定
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="key">キー</param>
    /// <returns></returns>
    public static bool GetButtonUp(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDeviceName(padNumber[playerNum]) == GamePadName.NoDevice)
        {
            return false;
        }


        return Input.GetButtonUp(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    #endregion ### Button ###

    #region ### Stick / Trigger ###

    /// <summary>
    /// ジョイスティックのスティックの入力量を返す(補間あり)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static float GetJoyStickAxis(int playerNum, string keyCodeName)
    {
        //接続されていない場合は0を返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return 0;
        return Input.GetAxis(GetInputManagerKeyName(padNumber[playerNum], keyCodeName));
    }

    /// <summary>
    /// ジョイスティックのスティックの入力量を返す(補間あり)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="key">キー</param>
    /// <returns></returns>
    public static float GetAxis(int playerNum, GamePadKeyCode key)
    {
        //接続されていない場合はfalseを返す
        if (GetDeviceName(padNumber[playerNum]) == GamePadName.NoDevice)
        {
            return 0;
        }


        return Input.GetAxis(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    /// <summary>
    /// ジョイスティックのスティックの入力量を返す(補間なし)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName">キー名</param>
    /// <returns></returns>
    public static float GetJoyStickAxisRaw(int playerNum, string keyCodeName)
    {
        //接続されていない場合は0を返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return 0;
        return Input.GetAxisRaw(GetInputManagerKeyName(padNumber[playerNum], keyCodeName));
    }

    /// <summary>
    /// ジョイスティックのスティックの入力量を返す(補間なし)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="key">キー</param>
    /// <returns></returns>
    public static float GetAxisRaw(int playerNum, GamePadKeyCode key)
    {
        // 接続されていない場合はfalseを返す
        if (GetDeviceName(padNumber[playerNum]) == GamePadName.NoDevice)
        {
            return 0;
        }


        return Input.GetAxisRaw(GetInputManagerKeyName(padNumber[playerNum], key));
    }

    /// <summary>
    /// ジョイスティックが押されたかどうかを判定(0=押されてない,-1=左/下におされた,1=右/上に押された)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName"></param>
    /// <param name="dir">キーの方向(1=右/上,-1=左/下)</param>
    /// <returns></returns>
    public static bool GetJoyStickAxisDown(int playerNum, string keyCodeName, int dir)
    {
        //接続されていない場合は0を返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;

        switch (keyCodeName)
        {
            case "LStickH":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownLStick[0];
                
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) {
                    isDownLStick[0] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                    return true;
                }
                
                break;

            case "LStickV":
                pressed = GetJoyStickAxisRaw(playerNum, keyCodeName) - isDownLStick[1];
                isDownLStick[1] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) {
                    isDownLStick[1] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                    return true;
                }
                
                break;

            case "RStickH":
                pressed = GetJoyStickAxisRaw(playerNum, keyCodeName) - isDownRStick[0];
                isDownRStick[0] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) return true;
                break;

            case "RStickV":
                pressed = GetJoyStickAxisRaw(playerNum, keyCodeName) - isDownRStick[1];
                isDownRStick[1] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) return true;
                break;

            case "CrossKeyH":
                pressed = GetJoyStickAxisRaw(playerNum, keyCodeName) - isDownCrossKey[0];
                isDownCrossKey[0] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) return true;
                break;

            case "CrossKeyV":
                pressed = GetJoyStickAxisRaw(playerNum, keyCodeName) - isDownCrossKey[1];
                isDownCrossKey[1] = GetJoyStickAxisRaw(playerNum, keyCodeName);
                //該当キーが押されていたらtrueを返す
                if (pressed == dir) return true;
                break;
        }
        return false;
    }

    /// <summary>
    /// ジョイスティックが離されたかどうかを判定(false=離されてない,ture=離された)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName"></param>
    /// <returns></returns>
    public static bool GetJoyStickAxisUp(int playerNum, string keyCodeName)
    {
        //一台も接続されていない場合はfalseを返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;

        switch (keyCodeName)
        {
            case "LStickH":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownLStick[0];
                isDownLStick[0] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
            case "LStickV":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownLStick[1];
                isDownLStick[1] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
            case "RStickH":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownRStick[0];
                isDownRStick[0] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
            case "RStickV":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownRStick[1];
                isDownRStick[1] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
            case "CrossKeyH":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownCrossKey[0];
                isDownCrossKey[0] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
            case "CrossKeyV":
                pressed = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName) - isDownCrossKey[1];
                isDownCrossKey[1] = GetJoyStickAxisRaw(padNumber[playerNum], keyCodeName);
                if (pressed != 0) return true;
                else return false;
        }
        return false;
    }

    /// <summary>
    /// ジョイスティックが離されたかどうかを判定(false=押されてない/押されたまま,true=押されたばかり)
    /// </summary>
    /// <param name="playerNum">プレイヤーの番号</param>
    /// <param name="keyCodeName"></param>
    /// <returns></returns>
    public static bool GetStickDown(int playerNum, string keyCodeName)
    {
        //接続されていない場合は0を返す
        if (CheckDeviceName(padNumber[playerNum]).Equals("No Connection")) return false;

        switch (keyCodeName)
        {
            case "LStickH":
                if (isDownLSticks[playerNum, 0])
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) == 0)
                    {
                        isDownLSticks[playerNum, 0] = false;
                    }
                    return false;
                }
                else
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) != 0)
                    {
                        isDownLSticks[playerNum, 0] = true;
                        return true;
                    }
                    else
                    {
                        isDownLSticks[playerNum, 0] = false;
                        return false;
                    }
                }
            case "LStickV":
                if (isDownLSticks[playerNum, 1])
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) == 0)
                    {
                        isDownLSticks[playerNum, 1] = false;
                    }
                    return false;
                }
                else
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) != 0)
                    {
                        isDownLSticks[playerNum, 1] = true;
                        return true;
                    }
                    else
                    {
                        isDownLSticks[playerNum, 1] = false;
                        return false;
                    }
                }
            case "CrossKeyH":
                if (isDownCrossKeys[playerNum, 0])
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) == 0)
                    {
                        isDownCrossKeys[playerNum, 0] = false;
                    }
                    return false;
                }
                else
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) != 0)
                    {
                        isDownCrossKeys[playerNum, 0] = true;
                        return true;
                    }
                    else
                    {
                        isDownCrossKeys[playerNum, 0] = false;
                        return false;
                    }
                }
            case "CrossKeyV":
                if (isDownCrossKeys[playerNum, 1])
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) == 0)
                    {
                        isDownCrossKeys[playerNum, 1] = false;
                    }
                    return false;
                }
                else
                {
                    if (GetJoyStickAxisRaw(playerNum, keyCodeName) != 0)
                    {
                        isDownCrossKeys[playerNum, 1] = true;
                        return true;
                    }
                    else
                    {
                        isDownCrossKeys[playerNum, 1] = false;
                        return false;
                    }
                }
            default:
                return false;
        }
    }

    #endregion ### Stick / Trigger ###

    #endregion ### Methods / Input Status ###

}