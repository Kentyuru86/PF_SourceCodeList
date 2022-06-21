using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// PlayerMove.csのパラメータをUnityエディタのInspector内で編集しやすくするための機能をまとめたプログラム
/// </summary>
[CustomEditor(typeof(PlayerMoveCtrl))]
public class CustomPlayerMoveInspector : Editor
{
    #region ### Parameters ###
    
    public static class Styles
    {
        private static GUIContent[] tabToggles = null;
        public static GUIContent[] TabToggles
        {
            get
            {
                if (tabToggles == null)
                {
                    tabToggles = System.Enum.GetNames(typeof(Tab)).Select(x => new GUIContent(x)).ToArray();
                }

                return tabToggles;
            }
        }

        public static readonly GUIStyle TabButtonStyle = "LargeButton";

        public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
    }
    /// <summary>
    /// Inspectorの表示タイプ
    /// </summary>
    public enum Tab
    {
        /// <summary>
        /// 馬力、トルク
        /// </summary>
        Power,
        /// <summary>
        /// ギア比
        /// </summary>
        Gear,
        /// <summary>
        /// タイヤのグリップ
        /// </summary>
        Grip,
        /// <summary>
        /// ハンドリング
        /// </summary>
        Handling,
        /// <summary>
        /// アシスト
        /// </summary>
        Assist,
        /// <summary>
        /// 標準のInspector
        /// </summary>
        Default
    }
    /// <summary>
    /// Inspector内のPlayerMoveCtrl欄に表示するもの
    /// </summary>
    Tab menuTab = Tab.Default;

    [Header("Data")]
    List<float> torqueList = new List<float>();
    List<float> hpList = new List<float>();

    [Header("Graph / Line")]
    Color graphDefaultColor = new Color(1, 1, 1, 0.25f);
    Color handlesDefaultColor;

    [Header("Grip")]
    static bool isOpenForward;
    static bool isOpenSideways;

    #endregion ### Parameters ###



    public override void OnInspectorGUI()
    {
        PlayerMoveCtrl playerCtrl = (PlayerMoveCtrl)target;
        
        // メニュータブの表示
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();

            menuTab = (Tab)GUILayout.Toolbar((int)menuTab, Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize);

            GUILayout.FlexibleSpace();
        }
        

        // 仕切り
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        GUILayout.BeginVertical();
        
        switch (menuTab)
        {
            case Tab.Default:
                // 標準のInspectorの描画を行う
                base.OnInspectorGUI();
                break;
            case Tab.Power:
                // 馬力とトルクカーブの表示
                ShowPower(playerCtrl);
                break;
            case Tab.Gear:
                ShowGear(playerCtrl);
                break;
            case Tab.Grip:
                ShowGrip(playerCtrl);
                break;
            default:
                break;
        }
        
        GUILayout.EndVertical();
        
    }
    
    #region ### Power ###

    /// <summary>
    /// 馬力とトルクカーブを表示する
    /// </summary>
    /// <param name="playerData">プレイヤーの挙動データ</param>
    void ShowPower(PlayerMoveCtrl playerData)
    {

        CollectPlayerData(playerData);

        int paramHeight = 250;

        // グラフ
        GUILayout.BeginVertical("box", GUILayout.Height(paramHeight + 76));
        {
            DrawPowerGraph(playerData, (int)EditorGUILayout.GetControlRect().width, paramHeight);
        }
        GUILayout.EndVertical();

        // 最大パワーの表示
        GUILayout.BeginVertical("box");
        {
            GUILayout.Label($"最大トルク\t{torqueList.Max():0.0} [N]");
            GUILayout.Label($"最大馬力\t{hpList.Max():0.0} [PS]");
        }
        GUILayout.EndVertical();

        // パラメータ表
        GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        {
            GUILayout.Label($"RPM\t|\t\tTorque [N]");
            for (int i = 0; i < playerData.Torque.Length; i++)
            {
                GUILayout.BeginHorizontal();
                
                GUILayout.Label($"{i * 1000,6:#,0}", GUILayout.Width(60));
                playerData.Torque[i] = GUILayout.HorizontalSlider(playerData.Torque[i], 0, 1000, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                playerData.Torque[i] = EditorGUILayout.FloatField("", playerData.Torque[i], GUILayout.Width(60));
                
                GUILayout.EndHorizontal();
            }

        }
        GUILayout.EndVertical();

    }

    /// <summary>
    /// トルクカーブのグラフを表示する
    /// </summary>
    /// <param name="playerData">プレイヤーの挙動データ</param>
    /// <param name="width">グラフの横幅</param>
    /// <param name="height">グラフの高さ</param>
    void DrawPowerGraph(PlayerMoveCtrl playerData, int width, int height)
    {
        Color graphHpColor = Color.cyan;
        Color graphTorqueColor = Color.gray;
        Color graphCvtColor = Color.yellow;
        Color graphRpmColor = Color.red;

        // グラフのスペース（上）
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        // グラフのスペース（左）
        GUILayout.Space(40);

        Rect graphRect = GUILayoutUtility.GetRect(width, height);

        List<float> torqueList = new List<float>();
        List<float> hpList = new List<float>();

        for (int i = 0; i < playerData.Torque.Length; i++)
        {
            torqueList.Add(playerData.Torque[i]);
            hpList.Add(playerData.Torque[i] * i * 1000 * 0.0014f / 9.8f);
        }

        // グラフの範囲設定
        float torqueyMax = Mathf.Ceil(torqueList.Max() * 1.1f);
        float hpyMax = Mathf.Ceil(hpList.Max() * 1.1f);

        // 軸
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] {
                new Vector2(graphRect.x,    graphRect.y),
                new Vector2(graphRect.xMax, graphRect.y),
                new Vector2(graphRect.xMax, graphRect.yMax),
                new Vector2(graphRect.x,    graphRect.yMax)
            },
            new Color(0, 0, 0, 0), Color.white);

        // グリッドとラベル
        Handles.color = graphDefaultColor;
        const int div = 10;
        // 横ラベル
        Handles.Label(new Vector2((graphRect.xMax + graphRect.xMin) / 2 - 40, graphRect.yMax + 25), $"Rpm[x1,000]");
        // 縦ラベル（トルク）
        Handles.color = graphTorqueColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(graphRect.xMin - 30, graphRect.yMin - 22.5f, 60, 15), graphTorqueColor, Color.clear);
        Handles.Label(new Vector2(graphRect.xMin - 30, graphRect.yMin - 20), $"Torque[N]");
        // 縦ラベル（馬力）
        Handles.color = graphHpColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(graphRect.xMax + 10, graphRect.yMin - 22.5f, 20, 15), graphHpColor, Color.clear);
        Handles.Label(new Vector2(graphRect.xMax + 10, graphRect.yMin - 20), $"PS");

        // グリッドの描画
        for (int i = 0; i <= div; i++)
        {
            float y = graphRect.height / div * i;
            float x = graphRect.width / div * i;

            Handles.color = graphDefaultColor;
            // 横
            Handles.DrawLine(
                new Vector2(graphRect.x, graphRect.y + y),
                new Vector2(graphRect.xMax, graphRect.y + y));

            // 縦
            Handles.DrawLine(
                new Vector2(graphRect.x + x, graphRect.y),
                new Vector2(graphRect.x + x, graphRect.yMax));

            // 横ラベル（エンジン回転数）
            Handles.Label(new Vector2(graphRect.x + x, graphRect.yMax + 10), $"{i:0}");

            // 縦ラベル（トルク）
            Handles.color = graphTorqueColor;
            Handles.Label(new Vector2(graphRect.xMin - 30, graphRect.yMax - y), $"{(i / (float)div) * torqueyMax:0}");

            // 縦ラベル（馬力）
            Handles.color = graphHpColor;
            Handles.Label(new Vector2(graphRect.xMax + 10, graphRect.yMax - y), $"{(i / (float)div) * hpyMax:0}");
        }

        // CVTRpm
        // Start
        float cvtRpmRate = playerData.MinCvtRpm / playerData.MaxRpm;
        Handles.color = graphCvtColor;
        Handles.DrawLine(
            new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.y),
            new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.yMax));
        // End
        cvtRpmRate = playerData.MaxCvtRpm / playerData.MaxRpm;
        Handles.color = graphCvtColor;
        Handles.DrawLine(
            new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.y),
            new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.yMax));

        // 現在のRpm（実行中のみ）
        if (EditorApplication.isPlaying)
        {
            cvtRpmRate = playerData.Rpm / playerData.MaxRpm;
            Handles.color = graphRpmColor;
            Handles.DrawLine(
                new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.y),
                new Vector2(graphRect.x * (1 - cvtRpmRate) + graphRect.xMax * cvtRpmRate, graphRect.yMax));

        }

        // データ（トルク）
        Handles.color = graphTorqueColor;
        if (torqueList.Count > 0)
        {
            var points = new List<Vector3>();
            var dx = graphRect.width / (torqueList.Count - 1);
            var dy = graphRect.height / torqueyMax;
            for (int i = 0; i < torqueList.Count; i++)
            {
                var x = graphRect.x + dx * i;
                var y = graphRect.yMax - dy * torqueList[i];
                points.Add(new Vector2(x, y));
            }
            Handles.DrawAAPolyLine(5f, points.ToArray());
        }

        // データ（馬力）
        Handles.color = graphHpColor;
        if (hpList.Count > 0)
        {
            var points = new List<Vector3>();
            var dx = graphRect.width / (hpList.Count - 1);
            var dy = graphRect.height / hpyMax;
            for (int i = 0; i < hpList.Count; i++)
            {
                var x = graphRect.x + dx * i;
                var y = graphRect.yMax - dy * hpList[i];
                points.Add(new Vector2(x, y));
            }
            Handles.DrawAAPolyLine(5f, points.ToArray());
        }

        // グラフのスペース（右）
        GUILayout.Space(40);

        GUILayout.EndHorizontal();

        // グラフのスペース（下）
        GUILayout.Space(40);

        Handles.color = handlesDefaultColor;
    }


    #endregion ### Power ###

    #region ### Gear ###

    /// <summary>
    /// ギア比の調整画面を表示する
    /// </summary>
    /// <param name="playerData">プレイヤーの挙動データ</param>
    void ShowGear(PlayerMoveCtrl playerData)
    {
        CollectPlayerData(playerData);

        // 現在のギア比（実行中のみ）
        if (EditorApplication.isPlaying)
        {
            GUILayout.BeginVertical("box");
            {
                // ヘッダー
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Start", GUILayout.Width(50));

                    GUILayout.Label($"| Now: {playerData.TotalGearRatio:0.000}", GUILayout.ExpandWidth(true));

                    GUILayout.Label($"| End", GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();

                // 値
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"{playerData.CVTStartGearRatio * playerData.FinalGearRatio:0.000}", GUILayout.Width(50));

                    GUILayout.HorizontalSlider(playerData.TotalGearRatio, playerData.CVTStartGearRatio * playerData.FinalGearRatio, playerData.CVTEndGearRatio * playerData.FinalGearRatio, GUILayout.ExpandWidth(true));

                    GUILayout.Label($"{playerData.CVTEndGearRatio * playerData.FinalGearRatio:0.000}", GUILayout.Width(50));
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }
        
        

        GUILayout.BeginHorizontal();
        {
            int paramHeight = 250;

            // パラメータ表
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"ギア", GUILayout.Width(50));
                    GUILayout.Label($"| 変速比", GUILayout.ExpandWidth(true));
                    GUILayout.Label($"| 速度", GUILayout.Width(50));
                    GUILayout.Label($"| 加速度", GUILayout.Width(60));
                }
                GUILayout.EndHorizontal();

                // Start
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Start\t", GUILayout.Width(50));
                    playerData.CVTStartGearRatio = GUILayout.HorizontalSlider(playerData.CVTStartGearRatio, 0.1f, 40, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    playerData.CVTStartGearRatio = EditorGUILayout.FloatField("", playerData.CVTStartGearRatio, GUILayout.Width(50));

                    float maxSpeed = (Mathf.PI * playerData.LengthLeg * playerData.MaxRpm * 60) / (1000 * playerData.CVTStartGearRatio * playerData.FinalGearRatio);
                    float maxAccel = (torqueList.Max() * playerData.CVTStartGearRatio * playerData.FinalGearRatio / (playerData.LengthLeg / 2)) / playerData.Weight;

                    GUILayout.Label($"| {maxSpeed,3:0.0}", GUILayout.Width(50));
                    GUILayout.Label($"| {maxAccel,3:0.0}", GUILayout.Width(60));

                }
                GUILayout.EndHorizontal();

                // End
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"End\t", GUILayout.Width(50));
                    playerData.CVTEndGearRatio = GUILayout.HorizontalSlider(playerData.CVTEndGearRatio, 0.1f, 40, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    playerData.CVTEndGearRatio = EditorGUILayout.FloatField("", playerData.CVTEndGearRatio, GUILayout.Width(50));

                    float maxSpeed = (Mathf.PI * playerData.LengthLeg * playerData.MaxRpm * 60) / (1000 * playerData.CVTEndGearRatio * playerData.FinalGearRatio);
                    float maxAccel = (torqueList.Max() * playerData.CVTEndGearRatio * playerData.FinalGearRatio / (playerData.LengthLeg / 2)) / playerData.Weight;

                    GUILayout.Label($"| {maxSpeed,3:0.0}", GUILayout.Width(50));
                    GUILayout.Label($"| {maxAccel,3:0.0}", GUILayout.Width(60));

                }
                GUILayout.EndHorizontal();

                // Final
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Final\t", GUILayout.Width(50));
                    playerData.FinalGearRatio = GUILayout.HorizontalSlider(playerData.FinalGearRatio, 0.1f, 10, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    playerData.FinalGearRatio = EditorGUILayout.FloatField("", playerData.FinalGearRatio, GUILayout.Width(50));

                    GUILayout.Label($"", GUILayout.Width(115));
                }
                GUILayout.EndHorizontal();

            }
            GUILayout.EndVertical();

        }
        GUILayout.EndHorizontal();
    }

    #endregion ### Gear ###

    #region ### Grip ###

    void ShowGrip(PlayerMoveCtrl playerData)
    {
        int paramHeight = 250;

        // グラフ
        GUILayout.BeginVertical("box", GUILayout.Height(paramHeight + 76));
        {
            DrawGripGraph(playerData, (int)EditorGUILayout.GetControlRect().width, paramHeight);
        }
        GUILayout.EndVertical();

        // パラメータ表
        GUILayout.BeginVertical();
        {

            // 縦
            isOpenForward = EditorGUILayout.Foldout(isOpenForward, "Forward");
            if (isOpenForward)
            {
                GUILayout.Label($"滑り率\t|\t\tGripPower");
                for (int i = 0; i < playerData.Torque.Length; i++)
                {
                    GUILayout.BeginHorizontal("box");

                    GUILayout.Label($"{i * 10,6:#,0}", GUILayout.Width(60));
                    playerData.GripForward[i] = GUILayout.HorizontalSlider(playerData.GripForward[i], 0, 1, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    playerData.GripForward[i] = EditorGUILayout.FloatField("", playerData.GripForward[i], GUILayout.Width(60));

                    GUILayout.EndHorizontal();
                }
            }

            // 横
            isOpenSideways = EditorGUILayout.Foldout(isOpenSideways, "Sideways");
            if (isOpenSideways)
            {
                GUILayout.Label($"滑り率\t|\t\tGripPower");
                for (int i = 0; i < playerData.Torque.Length; i++)
                {
                    GUILayout.BeginHorizontal("box");

                    GUILayout.Label($"{i * 10,6:#,0}", GUILayout.Width(60));
                    playerData.GripSideways[i] = GUILayout.HorizontalSlider(playerData.GripSideways[i], 0, 1, GUILayout.ExpandWidth(true));
                    GUILayout.Space(10);
                    playerData.GripSideways[i] = EditorGUILayout.FloatField("", playerData.GripSideways[i], GUILayout.Width(60));

                    GUILayout.EndHorizontal();
                }
            }

            // 摩擦係数
            GUILayout.Label($"摩擦係数");

            GUILayout.BeginHorizontal("box");
            {
                GUILayout.Label($"Tire Cof", GUILayout.Width(80));
                playerData.TireCof = GUILayout.HorizontalSlider(playerData.TireCof, 0, 10, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                playerData.TireCof = EditorGUILayout.FloatField("", playerData.TireCof, GUILayout.Width(60));

            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal("box");
            {
                GUILayout.Label($"Ground Cof", GUILayout.Width(80));
                playerData.GroundCof = GUILayout.HorizontalSlider(playerData.GroundCof, 0, 10, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                playerData.GroundCof = EditorGUILayout.FloatField("", playerData.GroundCof, GUILayout.Width(60));

            }
            GUILayout.EndHorizontal();

            // グリップ力の割合
            GUILayout.Label($"GripPowerRate");
            // 前後
            GUILayout.BeginHorizontal("box");
            {
                GUILayout.Label($"Forward", GUILayout.Width(80));
                playerData.GripPowerRateForward = GUILayout.HorizontalSlider(playerData.GripPowerRateForward, 0, 10, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                playerData.GripPowerRateForward = EditorGUILayout.FloatField("", playerData.GripPowerRateForward, GUILayout.Width(60));

            }
            GUILayout.EndHorizontal();
            // 左右
            GUILayout.BeginHorizontal("box");
            {
                GUILayout.Label($"Sideways", GUILayout.Width(80));
                playerData.GripPowerRateSideways = GUILayout.HorizontalSlider(playerData.GripPowerRateSideways, 0, 10, GUILayout.ExpandWidth(true));
                GUILayout.Space(10);
                playerData.GripPowerRateSideways = EditorGUILayout.FloatField("", playerData.GripPowerRateSideways, GUILayout.Width(60));

            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

    }

    void DrawGripGraph(PlayerMoveCtrl playerData, int width, int height)
    {
        Color forwardColor = new Color(0, 0.5f, 0.75f, 1);
        Color sidewaysColor = new Color(1, 0.25f, 0, 1);
        Color graphGripRateColor = Color.red;

        // グラフのスペース（上）
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        // グラフのスペース（左）
        GUILayout.Space(40);

        Rect graphRect = GUILayoutUtility.GetRect(width, height);

        List<float> gripFList = new List<float>();
        List<float> gripSList = new List<float>();

        for (int i = 0; i < playerData.GripForward.Length; i++)
        {
            gripFList.Add(playerData.GripForward[i]);
            gripSList.Add(playerData.GripSideways[i]);
        }

        // グラフの範囲設定
        float gripFyMax = gripFList.Max() * 1.1f;
        float gripSyMax = gripSList.Max() * 1.1f;

        // 軸
        Handles.DrawSolidRectangleWithOutline(
            new Vector3[] {
                new Vector2(graphRect.x,    graphRect.y),
                new Vector2(graphRect.xMax, graphRect.y),
                new Vector2(graphRect.xMax, graphRect.yMax),
                new Vector2(graphRect.x,    graphRect.yMax)
            },
            new Color(0, 0, 0, 0), Color.white);

        // グリッドとラベル
        Handles.color = graphDefaultColor;
        const int div = 10;
        // 横ラベル
        Handles.Label(new Vector2((graphRect.xMax + graphRect.xMin) / 2 - 40, graphRect.yMax + 25), $"滑り率[%]");
        // 縦ラベル（縦グリップ）
        Handles.color = forwardColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(graphRect.xMin - 30, graphRect.yMin - 22.5f, 90, 15), forwardColor, Color.clear);
        Handles.Label(new Vector2(graphRect.xMin - 30, graphRect.yMin - 20), $"グリップ力（縦）");
        // 縦ラベル（横グリップ）
        Handles.color = sidewaysColor;
        Handles.DrawSolidRectangleWithOutline(new Rect(graphRect.xMax - 50, graphRect.yMin - 22.5f, 90, 15), sidewaysColor, Color.clear);
        Handles.Label(new Vector2(graphRect.xMax - 50, graphRect.yMin - 20), $"グリップ力（横）");

        // グリッドの描画
        for (int i = 0; i <= div; i++)
        {
            float y = graphRect.height / div * i;
            float x = graphRect.width / div * i;

            Handles.color = graphDefaultColor;
            // 横
            Handles.DrawLine(
                new Vector2(graphRect.x, graphRect.y + y),
                new Vector2(graphRect.xMax, graphRect.y + y));

            // 縦
            Handles.DrawLine(
                new Vector2(graphRect.x + x, graphRect.y),
                new Vector2(graphRect.x + x, graphRect.yMax));

            // 横ラベル（滑り率）
            Handles.Label(new Vector2(graphRect.x + x - 10, graphRect.yMax + 10), $"{i * 10:0}");

            // 縦ラベル（縦グリップ）
            Handles.color = forwardColor;
            Handles.Label(new Vector2(graphRect.xMin - 30, graphRect.yMax - y), $"{(i / (float)div) * gripFyMax:0.0}");

            // 縦ラベル（横グリップ）
            Handles.color = sidewaysColor;
            Handles.Label(new Vector2(graphRect.xMax + 10, graphRect.yMax - y), $"{(i / (float)div) * gripSyMax:0.0}");
        }

        // 現在のグリップ率（実行中のみ）
        if (EditorApplication.isPlaying)
        {
            Handles.color = graphGripRateColor;
            Handles.DrawLine(
                new Vector2(graphRect.x * (1 - playerData.GripRate) + graphRect.xMax * playerData.GripRate, graphRect.y),
                new Vector2(graphRect.x * (1 - playerData.GripRate) + graphRect.xMax * playerData.GripRate, graphRect.yMax));

        }

        // データ（縦グリップ）
        Handles.color = forwardColor;
        if (gripFList.Count > 0)
        {
            var points = new List<Vector3>();
            var dx = graphRect.width / (gripFList.Count - 1);
            var dy = graphRect.height / gripFyMax;
            for (int i = 0; i < gripFList.Count; i++)
            {
                var x = graphRect.x + dx * i;
                var y = graphRect.yMax - dy * gripFList[i];
                points.Add(new Vector2(x, y));
            }
            Handles.DrawAAPolyLine(5f, points.ToArray());
        }

        // データ（横グリップ）
        Handles.color = sidewaysColor;
        if (gripSList.Count > 0)
        {
            var points = new List<Vector3>();
            var dx = graphRect.width / (gripSList.Count - 1);
            var dy = graphRect.height / gripSyMax;
            for (int i = 0; i < gripSList.Count; i++)
            {
                var x = graphRect.x + dx * i;
                var y = graphRect.yMax - dy * gripSList[i];
                points.Add(new Vector2(x, y));
            }
            Handles.DrawAAPolyLine(5f, points.ToArray());
        }

        // グラフのスペース（右）
        GUILayout.Space(40);

        GUILayout.EndHorizontal();

        // グラフのスペース（下）
        GUILayout.Space(40);

        Handles.color = handlesDefaultColor;
    }


    #endregion ### Grip ###

    
    #region ### CollectData ###

    /// <summary>
    /// プレイヤーの挙動データを取得する
    /// </summary>
    /// <param name="playerData">プレイヤーの挙動データ</param>
    void CollectPlayerData(PlayerMoveCtrl playerData)
    {
        torqueList = new List<float>();
        hpList = new List<float>();

        for (int i = 0; i < playerData.Torque.Length; i++)
        {
            torqueList.Add(playerData.Torque[i]);
            hpList.Add(playerData.Torque[i] * i * 1000 * 0.0014f / 9.8f);
        }
    }

    #endregion ### CollectData ###
}
