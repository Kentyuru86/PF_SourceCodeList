using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public class DebugRaceData : EditorWindow
{
    #region ### Parameter ###
    /// <summary>
    /// デバッグ画面に表示するタブ
    /// </summary>
    public enum Tab
    {
        /// <summary>
        /// ルール設定
        /// </summary>
        Rule,
        /// <summary>
        /// キャラクター設定
        /// </summary>
        Charactor,
        /// <summary>
        /// ステージ設定
        /// </summary>
        Stage,
        /// <summary>
        /// コントローラの設定
        /// </summary>
        Input,
        /// <summary>
        /// シーンの設定
        /// </summary>
        Scene,
        /// <summary>
        /// レース中のデータ表示
        /// </summary>
        Race,
        /// <summary>
        /// ショートカットリスト
        /// </summary>
        ShortCut
    }
    
    [Header("Shared Parameter")]
    Tab menuTab = Tab.Rule;
    /// <summary>
    /// エディタウインドウのタブスタイル
    /// </summary>
    public static class Styles
    {
        private static GUIContent[] tabToggles = null;
        public static GUIContent[] TabToggles
        {
            get
            {
                if(tabToggles == null)
                {
                    tabToggles = System.Enum.GetNames(typeof(Tab)).Select(x => new GUIContent(x)).ToArray();
                }

                return tabToggles;
            }
        }

        public static readonly GUIStyle TabButtonStyle = "LargeButton";

        public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
    }

    public static bool Foldout(string title, bool display)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;

        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint)
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            display = !display;
            e.Use();
        }

        return display;
    }

    #endregion ### Parameter ###



    [MenuItem("Tools/Race Data/Open Debug Data")]
    public static void Open()
    {
        GetWindow<DebugRaceData>("DebugRaceData");
    }

    #region ### Draw GUI ###

    void OnGUI()
    {
        // メニュータブの表示
        using(new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            menuTab = (Tab)GUILayout.Toolbar((int)menuTab, Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize);

            GUILayout.FlexibleSpace();
        }

        // 仕切り
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
        
        // 内容の表示
        GUILayout.BeginVertical();
        {
            switch (menuTab)
            {
                case Tab.Rule:
                    ShowRule();
                    break;
                case Tab.Charactor:
                    ShowSelectedCharactor();
                    break;
                case Tab.Stage:
                    ShowSelectedStage();
                    break;
                case Tab.Input:
                    ShowInputStatus();
                    break;
                case Tab.Scene:
                    ShowSceneDataStatus();
                    break;
                case Tab.Race:
                    ShowGameStatus();
                    break;
                case Tab.ShortCut:
                    ShowShortCut();
                    break;
            }
            
        }
        GUILayout.EndVertical();
    }

    #region ### Rule ###
    public enum RuleTab
    {
        Time,
        Score,
        Free
    }
    [Header("Rule")]
    RuleTab ruleTab = RuleTab.Time;
    Vector2 scrollPositionRule;
    public static bool isFoldOutShared = true;
    public static bool isFoldOutTime = true;
    public static bool isFoldOutScore = true;
    public static bool isFoldOutOption = true;
    float sliderVolumeTimer = 30;

    /// <summary>
    /// ルール設定画面を表示する
    /// </summary>
    void ShowRule()
    {

        // 現在のルール設定の表示
        string strTotalRule = "";
        switch (RuleData.gameSetMode)
        {
            case RuleData.GameSetMode.Time:
                strTotalRule += "タイム制";
                strTotalRule += $" ({Mathf.FloorToInt(RuleData.gameTime / 60)}分{RuleData.gameTime % 60}秒以内)";
                break;
            case RuleData.GameSetMode.Score:
                strTotalRule += "スコア制";
                strTotalRule += $" ({RuleData.setcount}セット{RuleData.setpoint}ポイントマッチ)";
                break;
            case RuleData.GameSetMode.FreeRun:
                strTotalRule += "フリーラン";
                strTotalRule += "（勝利条件なし。ポーズで終了）";
                break;
            default:
                strTotalRule += "未設定　";
                strTotalRule += "※DebugRaceData.csに内容を足してください";
                break;
        }
        GUILayout.Box(strTotalRule, GUILayout.ExpandWidth(true));

        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));

        // ここより下の内容をスクロールさせる
        scrollPositionRule = EditorGUILayout.BeginScrollView(scrollPositionRule);

        GUILayout.BeginVertical();

        isFoldOutShared = Foldout("共通", isFoldOutShared);
        if (isFoldOutShared)
        {
            // ゲームルール
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("勝利条件", GUILayout.Width(100));
                GUILayout.Box("", GUILayout.Width(1));
                
                int cntRule = GUILayout.Toolbar((int)RuleData.gameSetMode, new string[] { "タイム制", "スコア制", "フリーラン" });
                RuleData.gameSetMode = (RuleData.GameSetMode)Enum.ToObject(typeof(RuleData.GameSetMode), cntRule);

            }
            GUILayout.EndHorizontal();

            // ギミック
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("ギミック", GUILayout.Width(100));

                GUILayout.Box("", GUILayout.Width(1));

                RuleData.gimmick = GUILayout.Toggle(RuleData.gimmick, RuleData.gimmick ? "〇　あり" : "なし", "button");


            }
            GUILayout.EndHorizontal();

            // アイテム
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("アイテム", GUILayout.Width(100));

                GUILayout.Box("", GUILayout.Width(1));

                RuleData.item = GUILayout.Toggle(RuleData.item, RuleData.item ? "〇　あり" : "なし", "button");


            }
            GUILayout.EndHorizontal();
        }
        
        isFoldOutTime = Foldout("タイム制", isFoldOutTime);
        if (isFoldOutTime)
        {
            // 制限時間
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("制限時間", GUILayout.Width(100));

                GUILayout.Box("", GUILayout.Width(1));
                
                RuleData.gameTime = (int)(GUILayout.HorizontalSlider(RuleData.gameTime / sliderVolumeTimer, 60 / sliderVolumeTimer, 600 / sliderVolumeTimer, GUILayout.ExpandWidth(true))) * sliderVolumeTimer;
                GUILayout.Space(10);
                RuleData.gameTime = EditorGUILayout.FloatField("", RuleData.gameTime, GUILayout.Width(40));
                GUILayout.Label("秒", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();
        }

        isFoldOutScore = Foldout("スコア制", isFoldOutScore);
        if (isFoldOutScore)
        {
            // ポイント数
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("1セット取得に\n必要なポイント数", GUILayout.Width(100));

                GUILayout.Box("", GUILayout.Width(1));

                RuleData.setpoint = (int)GUILayout.HorizontalSlider((float)RuleData.setpoint, 1, 20, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                RuleData.setpoint = EditorGUILayout.IntField("", RuleData.setpoint, GUILayout.Width(40));
                GUILayout.Label("pt", GUILayout.Width(40));

            }
            GUILayout.EndHorizontal();

            // セット数
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("勝利に必要な\nセット数", GUILayout.Width(100));

                GUILayout.Box("", GUILayout.Width(1));

                RuleData.setcount = (int)GUILayout.HorizontalSlider((float)RuleData.setcount, 1, 10, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                RuleData.setcount = EditorGUILayout.IntField("", RuleData.setcount, GUILayout.Width(40));
                GUILayout.Label("セット", GUILayout.Width(40));

            }
            GUILayout.EndHorizontal();
        }

        isFoldOutOption = Foldout("オプション", isFoldOutOption);
        if (isFoldOutOption)
        {
            // ブースト
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("ブースト", GUILayout.Width(100));
                GUILayout.Box("", GUILayout.Width(1));

                int cntBoost = GUILayout.Toolbar((int)RuleData.boostMode, new string[] { "なし", "弱め", "強め" });
                RuleData.boostMode = (RuleData.BoostMode)Enum.ToObject(typeof(RuleData.BoostMode), cntBoost);

            }
            GUILayout.EndHorizontal();

            
        }

        GUILayout.EndVertical();

        EditorGUILayout.EndScrollView();

    }

    #endregion ### Rule ###

    #region ### Charactor ###

    [Header("Charactor")]
    Vector2 scrollPositionChara;

    /// <summary>
    /// キャラクター設定画面を表示する
    /// </summary>
    void ShowSelectedCharactor()
    {
        scrollPositionChara = EditorGUILayout.BeginScrollView(scrollPositionChara);

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        // 各プレイヤーの登録状況を表示
        for (int i = 0; i < SelectedPlayerData.charaDatas.Length; i++)
        {
            GUILayout.BeginVertical("box", GUILayout.Width(position.size.x-110));
            {
                GUILayout.BeginHorizontal();
                {
                    
                    // プレイヤー番号
                    GUILayout.Label($"{i + 1} P", GUILayout.Width(25));

                    // 操作タイプ
                    SelectedPlayerData.operatorTypes[i] = (SelectedPlayerData.OperatorType)EditorGUILayout.Popup((int)SelectedPlayerData.operatorTypes[i], new string[] { "LOCAL", "CPU", "NETWORK" }, GUILayout.Width(75));

                    // データ編集用
                    SelectedPlayerData.charaDatas[i] = (CharaData)EditorGUILayout.ObjectField(SelectedPlayerData.charaDatas[i], typeof(CharaData), GUILayout.ExpandWidth(true), GUILayout.Width(position.size.x - 230));

                }
                GUILayout.EndHorizontal();

                // 詳細データの表示
                ShowCharaDatailData(i);
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndVertical();

        GUILayout.BeginVertical(GUILayout.Width(80));
        {
            ShowCharaDataPreset();
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 選択したキャラクターの詳細情報を表示する
    /// </summary>
    /// <param name="num"></param>
    void ShowCharaDatailData(int num)
    {
        Texture charaTexture;
        string strDetail = "";
        
        if (SelectedPlayerData.charaDatas[num] == null)
        {
            // 未選択時
            charaTexture = null;
            strDetail += "選択されていません\n";

        }
        else
        {
            charaTexture = SelectedPlayerData.charaDatas[num].charaSprite.texture;
            strDetail += "Name:";
            strDetail += $" {SelectedPlayerData.charaDatas[num].charaNameEng}";
            strDetail += $" / {SelectedPlayerData.charaDatas[num].charaNameJp}";
            
            strDetail += "\n";

            strDetail += "Spec:";
            strDetail += $" S:{SelectedPlayerData.charaDatas[num].paramMaxSpeed, 3}";
            strDetail += $" A:{SelectedPlayerData.charaDatas[num].paramAccel,3}";
            strDetail += $" H:{SelectedPlayerData.charaDatas[num].paramHandling,3}";
        }

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        {
            GUILayout.Space(40);

            // キャラのアイコン
            GUILayout.Label(charaTexture, GUIStyle.none, GUILayout.Width(50), GUILayout.Height(50));

            GUILayout.Space(10);

            // テキスト
            GUILayout.Label(strDetail, GUILayout.ExpandWidth(true));
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

    }

    /// <summary>
    /// キャラクター設定でよく使うセットを表示する
    /// </summary>
    void ShowCharaDataPreset()
    {
        Foldout("Preset", true);

        // 1Pのみ、キャラクターはランダムで決定、自分で操作する
        if (GUILayout.Button("1P_Rand_L"))
        {
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");
            string path = AssetDatabase.GUIDToAssetPath(charaGuids[UnityEngine.Random.Range(0, charaGuids.Length - 1)]);
            CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

            SelectedPlayerData.charaDatas[0] = charaData;
            SelectedPlayerData.operatorTypes[0] = SelectedPlayerData.OperatorType.LOCAL;

            for(int i = 1; i < 4; i++)
            {
                SelectedPlayerData.charaDatas[i] = null;
            }
        }

        // 1Pのみ、キャラクターはランダムで決定、コンピュータが自動で操作する
        if (GUILayout.Button("1P_Rand_C"))
        {
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");
            string path = AssetDatabase.GUIDToAssetPath(charaGuids[UnityEngine.Random.Range(0, charaGuids.Length - 1)]);
            CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

            SelectedPlayerData.charaDatas[0] = charaData;
            SelectedPlayerData.operatorTypes[0] = SelectedPlayerData.OperatorType.CPU;

            for (int i = 1; i < 4; i++)
            {
                SelectedPlayerData.charaDatas[i] = null;
            }
        }

        // 4人、キャラクターはランダムで決定、すべて自分で操作する
        if (GUILayout.Button("4P_Rand_L"))
        {
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");

            for(int i = 0; i < 4; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(charaGuids[UnityEngine.Random.Range(0, charaGuids.Length - 1)]);
                CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

                SelectedPlayerData.charaDatas[i] = charaData;
                SelectedPlayerData.operatorTypes[i] = SelectedPlayerData.OperatorType.LOCAL;
            }

        }

        // 4人、キャラクターはランダムで決定、すべてコンピュータが自動で操作する
        if (GUILayout.Button("4P_Rand_C"))
        {
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");

            for (int i = 0; i < 4; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(charaGuids[UnityEngine.Random.Range(0, charaGuids.Length - 1)]);
                CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

                SelectedPlayerData.charaDatas[i] = charaData;
                SelectedPlayerData.operatorTypes[i] = SelectedPlayerData.OperatorType.CPU;
            }

        }
        
        
    }

    #endregion ### Charactor ###

    #region ### Stage ###

    /// <summary>
    /// ステージ設定画面を表示する
    /// </summary>
    void ShowSelectedStage()
    {
        // ステージのデータ
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Stage Data", GUILayout.Width(150));

            GUILayout.Box("", GUILayout.Width(1));

            // データ編集用
            SelectedStageData.stageData = (StageData)EditorGUILayout.ObjectField(SelectedStageData.stageData, typeof(StageData), GUILayout.ExpandWidth(true), GUILayout.Width(position.size.x - 180));
        }
        GUILayout.EndHorizontal();
        
        // ステージの名称
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Stage Name", GUILayout.Width(150));

            GUILayout.Box("", GUILayout.Width(1));

            if (SelectedStageData.IsStageSelected)
            {
                GUILayout.Label(SelectedStageData.stageData.stageName);
            }
            else
            {
                GUILayout.Label("---");
            }
        }
        GUILayout.EndHorizontal();
        
        // ステージのシーン名
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Stage Scene Name", GUILayout.Width(150));

            GUILayout.Box("", GUILayout.Width(1));

            if (SelectedStageData.IsStageSelected)
            {
                GUILayout.Label(SelectedStageData.stageData.stageSceneName);
            }
            else
            {
                GUILayout.Label("---");
            }
        }
        GUILayout.EndHorizontal();

        // ステージのイメージ画像
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label("Stage Image　", GUILayout.Width(150), GUILayout.Height(100));

            GUILayout.Box("", GUILayout.Width(1), GUILayout.Height(100));

            if (SelectedStageData.IsStageSelected)
            {
                GUILayout.Button(SelectedStageData.stageData.stageSprite.texture, GUIStyle.none, GUILayout.Width(100), GUILayout.Height(100));
            }
            else
            {
                GUILayout.Label("No Image", GUILayout.Height(100));
            }
        }
        GUILayout.EndHorizontal();
        
    }

    #endregion ### Stage ###

    #region ### Input ###

    [Header("Input")]
    static bool isChangeInput = false;
    static int changeInputNum = 0;
    Vector2 scrollPosition;

    /// <summary>
    /// コントローラの設定画面を表示する
    /// </summary>
    void ShowInputStatus()
    {
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.BeginHorizontal();
        {
            // Unityが識別しているコントローラ名と順番
            GUILayout.BeginVertical("box");
            Foldout("Unity", true);
            for (int i = 0; i < GamePadManager.padNumber.Length; i++)
            {
                GUILayout.BeginHorizontal();
                
                // 番号
                GUILayout.Label($"{i,3}", GUILayout.Width(25));

                // 名前
                GUILayout.Label($"{GamePadManager.GetDebugDeviceName(i).ToString()}", GUILayout.ExpandWidth(true));

                EditorGUI.BeginDisabledGroup(!isChangeInput);
                
                // 変更用ボタン
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    
                    //すでに登録されているコントローラの入力は受け付けない
                    for (int j = 0; j < GamePadManager.padNumber.Length; j++)
                    {
                        if (i == GamePadManager.padNumber[j])
                        {
                            //下のreturnをつけないと番号の重複オッケーになる
                            return;
                        }
                    }
                    //Aボタンが押されたコントローラの接続番号を登録
                    GamePadManager.padNumber[changeInputNum] = i;

                    Debug.Log($"Unity：【{i}】→GamePadManager：【{changeInputNum}】に変更");
                    isChangeInput = false;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();
                
                GUILayout.Space(5);
            }
            GUILayout.EndVertical();


            // GamePadManagerが登録しているコントローラ名と順番
            GUILayout.BeginVertical("box");
            Foldout("GamepadManager", true);
            for (int i = 0; i < GamePadManager.padNumber.Length; i++)
            {
                GUILayout.BeginHorizontal();
                
                // 登録先の番号
                GUILayout.Label($"{GamePadManager.padNumber[i],3}", GUILayout.Width(25));
                
                // 名前
                GUILayout.Label($"{GamePadManager.GetRegisteredDeviceName(i)}", GUILayout.ExpandWidth(true));

                EditorGUI.BeginDisabledGroup(isChangeInput);
                if (GUILayout.Button("Change", GUILayout.Width(60)))
                {
                    Debug.Log("コントローラの接続先を変更します");
                    isChangeInput = true;
                    Debug.Log($"change:{isChangeInput}");
                    changeInputNum = i;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
            }
            GUILayout.EndVertical();

        }
        GUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        // 補足用の説明バー
        GUILayout.BeginVertical("box");
        if (isChangeInput)
        {
            GUILayout.Label($"【{changeInputNum}】に登録するコントローラの横にある「Select」を押してください", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("キャンセル"))
            {
                isChangeInput = false;
            }
        }
        else
        {
            GUILayout.Label("コントローラの接続先を変更する時は「Change」を押してください", GUILayout.ExpandWidth(true));
        }
        GUILayout.EndVertical();

    }

    #endregion ### Input ###

    #region ### Scene ###

    /// <summary>
    /// シーンの管理画面を表示する
    /// </summary>
    void ShowSceneDataStatus()
    {
        bool buttonReload = false;
        bool buttonRaceStart = false;

        Foldout("Status", true);

        ShowParamLabel("現在のシーン", SceneManager.GetActiveScene().name);
        ShowParamLabel("読み込むシーン", SceneDataManager.GetSceneName());
        ShowParamLabel("戻るシーン", SceneDataManager.GetPreSceneName());


        Foldout("Debug", true);

        ShowParamButton("現シーン再読込", ref buttonReload, "Reload", "Reload");
        if (buttonReload)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            buttonReload = false;
        }

        ShowParamButton("レースシーン読込", ref buttonRaceStart, "Load", "Load");
        if (buttonRaceStart)
        {
            RegistRaceSceneData();
            SceneManager.LoadScene("Loading");
        }
    }

    /// <summary>
    /// シーンの登録を行う
    /// </summary>
    void RegistRaceSceneData()
    {
        // ステージのシーン名を登録
        SceneDataManager.SetSceneName(SelectedStageData.stageData.stageSceneName);

        // 戻ってくるシーンをメニュー画面に設定する
        SceneDataManager.SetPreSceneName("SettingMenu02");
    }

    #endregion ### Scene ###

    #region ### Race ###

    [Header("Race")]
    Vector2 scrollPositionRace;
    static bool isFoldOutRaceTime = true;
    static bool isFoldOutTimeScale = true;
    static bool stopTimer = false;

    /// <summary>
    /// レース中に使うパラメータを表示する
    /// </summary>
    void ShowGameStatus()
    {
        // ここより下の内容をスクロールさせる
        scrollPositionRace = EditorGUILayout.BeginScrollView(scrollPositionRace);

        isFoldOutRaceTime = Foldout("RaceTime", isFoldOutRaceTime);
        if (isFoldOutRaceTime)
        {
            
            ShowParamFloat("残り時間", ref GameStatusManager.remainTimer, 0, 6000, "秒");
            ShowParamButton("タイマーストップ", ref stopTimer, "○　ストップ", "Off");
            
            if (SelectedStageData.stageData != null && SceneManager.GetActiveScene().name == SelectedStageData.stageData.stageSceneName)
            {
                RaceGameManager.Instance.stopTimer = stopTimer;
            }
            
            ShowParamLabel("ポーズ中かどうか", GameStatusManager.isPaused ? "True" : "False");
            ShowParamLabel("ポーズできるか", GameStatusManager.isPaused ? "可能" : "不可"); 
            
        }

        isFoldOutTimeScale = Foldout("TimeScale", isFoldOutTimeScale);
        if (isFoldOutTimeScale)
        {
            GUILayout.Label("◆GameStatus ---");
            ShowParamFloat("全体", GameStatusManager.raceTimeScale, 0, 1, "");
            ShowParamFloat("ポーズ", GameStatusManager.pauseTimeScale, 0, 1, "");

            GUILayout.Label("◆Time ---");
            ShowParamFloat("TimeScale", Time.timeScale, 0, 1, "");


            // DeltaTime
            ShowParamLabel("DeltaTime", $"{Time.deltaTime * 1000:0.0}ms   ({1 / Time.deltaTime:0.0} fps)");
            ShowParamLabel("Fixed DeltaTime", $"{Time.fixedDeltaTime * 1000:0.0}ms   ({1 / Time.fixedDeltaTime:0.0} fps)");
        }


            

            EditorGUILayout.EndScrollView();

    }

    #endregion ### Race ###

    #region ### ShortCut ###

    /// <summary>
    /// よく使う作業リストを表示
    /// </summary>
    void ShowShortCut()
    {
        Foldout("Test Play", true);

        bool isButtonDown = false;

        ShowParamButton("CPU移動テスト\n・フリーラン\n・ユニティ(1Pのみ)\n・Temple", ref isButtonDown, "Load", "Load");
        if (isButtonDown)
        {
            // 再生していない場合はゲームを再生させる
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }

            // ルールの設定
            RuleData.gameSetMode = RuleData.GameSetMode.FreeRun;

            // キャラクターの設定
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");
            string path = AssetDatabase.GUIDToAssetPath(charaGuids[0]);
            CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

            SelectedPlayerData.charaDatas[0] = charaData;
            SelectedPlayerData.operatorTypes[0] = SelectedPlayerData.OperatorType.CPU;

            for (int i = 1; i < 4; i++)
            {
                SelectedPlayerData.charaDatas[i] = null;
            }

            // ステージ設定
            string[] stageGuids = AssetDatabase.FindAssets("t:StageData");
            path = AssetDatabase.GUIDToAssetPath(stageGuids[0]);
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(path);
            SelectedStageData.stageData = stageData;

            // シーンを移動する
            RegistRaceSceneData();
            SceneManager.LoadScene("Loading");

            return;
        }

        ShowParamButton("CPU4人テスト\n・フリーラン\n・キャラランダム\n・Temple", ref isButtonDown, "Load", "Load");
        if (isButtonDown)
        {
            // 再生していない場合はゲームを再生させる
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.ExecuteMenuItem("Edit/Play");
            }

            // ルールの設定
            RuleData.gameSetMode = RuleData.GameSetMode.FreeRun;

            // キャラクターの設定
            string[] charaGuids = AssetDatabase.FindAssets("t:CharaData");

            string path;
            for (int i = 0; i < 4; i++)
            {
                path = AssetDatabase.GUIDToAssetPath(charaGuids[UnityEngine.Random.Range(0, charaGuids.Length - 1)]);
                CharaData charaData = AssetDatabase.LoadAssetAtPath<CharaData>(path);

                SelectedPlayerData.charaDatas[i] = charaData;
                SelectedPlayerData.operatorTypes[i] = SelectedPlayerData.OperatorType.CPU;
            }

            for (int i = 1; i < 4; i++)
            {
                SelectedPlayerData.charaDatas[i] = null;
            }

            // ステージ設定
            string[] stageGuids = AssetDatabase.FindAssets("t:StageData");
            path = AssetDatabase.GUIDToAssetPath(stageGuids[0]);
            StageData stageData = AssetDatabase.LoadAssetAtPath<StageData>(path);
            SelectedStageData.stageData = stageData;

            // シーンを移動する
            RegistRaceSceneData();
            SceneManager.LoadScene("Loading");

            return;
        }
    }

    #endregion ### ShortCut ###

    /// <summary>
    /// スライダー付きのパラメータGUIを表示する
    /// </summary>
    /// <param name="title"></param>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="unit"></param>
    void ShowParamFloat(string title, float value, float min, float max, string unit)
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title, GUILayout.Width(100));

            GUILayout.Box("", GUILayout.Width(1));

            GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            EditorGUILayout.FloatField("", value, GUILayout.Width(40));

            GUILayout.Label(unit, GUILayout.Width(40));
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 編集が可能なスライダー付きのパラメータGUIを表示する
    /// </summary>
    /// <param name="title"></param>
    /// <param name="value"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="unit"></param>
    void ShowParamFloat(string title, ref float value, float min, float max, string unit)
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title, GUILayout.Width(100));

            GUILayout.Box("", GUILayout.Width(1));

            value = GUILayout.HorizontalSlider(value, min, max, GUILayout.ExpandWidth(true));
            GUILayout.Space(10);
            value = EditorGUILayout.FloatField("", value, GUILayout.Width(40));

            GUILayout.Label(unit, GUILayout.Width(40));
        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// ラベルGUIを表示
    /// </summary>
    /// <param name="title"></param>
    /// <param name="content"></param>
    void ShowParamLabel(string title, string content)
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title, GUILayout.Width(100));

            GUILayout.Box("", GUILayout.Width(1));

            GUILayout.Label(content);

        }
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// 編集可能なボタンGUIを表示
    /// </summary>
    /// <param name="title"></param>
    /// <param name="button"></param>
    /// <param name="strOn"></param>
    /// <param name="strOff"></param>
    void ShowParamButton(string title, ref bool button, string strOn, string strOff)
    {
        GUILayout.BeginHorizontal();
        {
            GUILayout.Label(title, GUILayout.Width(100));

            GUILayout.Box("", GUILayout.Width(1));

            button = GUILayout.Toggle(button, button ? strOn : strOff, "button");

        }
        GUILayout.EndHorizontal();
    }

    #endregion ### Draw GUI ###

}
