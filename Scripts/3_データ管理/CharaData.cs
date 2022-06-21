using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Create Data/Charactor Data")]
public class CharaData : ScriptableObject
{
    [Header("Data")]
    public int charaId;
    public string charaNameEng;
    public string charaNameJp;
    public GameObject prefab;
    public Sprite charaSprite;

    [Header("Spec")]
    [Multiline(2)] public string charaOverview;
    [Range(0, 100)] public int paramMaxSpeed = 60;
    [Range(0, 100)] public int paramAccel = 60;
    [Range(0, 100)] public int paramHandling = 60;

    [Header("Spec / Detail")]
    [SerializeField] [Range(0, 10)] float scoreFastStability;
    [SerializeField] [Range(0, 10)] float scoreSlowStability;
    [SerializeField] [Range(0, 10)] float scoreHandlingDynamicRange;

    [System.Serializable]
    public struct PhotoData
    {
        [Tooltip("画像を適切に表示するためのオフセット")]
        public Vector2 pivotOffset;
        public Vector2 scale;
    }

    [Header("Photo")]
    [Tooltip("顔のアップ用")]
    public PhotoData headshot;
    [Tooltip("レース設定画面用")]
    public PhotoData raceSetting;
    [Tooltip("レース設定画面のスタート演出用")]
    public PhotoData vsStart;

    [Header("Color")]
    public Color themeColor = new Color(1, 1, 1, 1);


#if UNITY_EDITOR

    void OnEnable()
    {
        // ランダムのみ処理しない
        if (name.Equals("Random"))
        {
            return;
        }

        float preMaxSpeed = paramMaxSpeed;
        float preAccel = paramAccel;
        float preHandling = paramHandling;
        
        paramMaxSpeed = (int)(Mathf.Clamp(GetMaxSpeed(), 0, 400) / 400f * 100);
        
        paramAccel = (int)(Mathf.Clamp(GetMaxAccel(), 0, 1000) / 1000f * 100);

        paramHandling = (int)(Mathf.Clamp(GetHandling(), 0, 30) / 30f * 100);

        Debug.Log($"キャラクターデータ[{charaNameJp}]の更新完了\nMaxSpeed:{preMaxSpeed}→{paramMaxSpeed}, Accel:{preAccel}→{paramAccel}, Handling:{preHandling}→{paramHandling}");
    }


    /// <summary>
    /// 最高速度を返す
    /// </summary>
    /// <returns></returns>
    public float GetMaxSpeed()
    {
        try
        {
            PlayerMoveCtrl player = prefab.transform.GetChild(0).GetComponent<PlayerMoveCtrl>();
            if (player == null)
            {
                return 0;
            }
            else
            {
                return player.GetMaxSpeed();
            }
        }
        catch(UnassignedReferenceException e)
        {
            Debug.LogError($"{this.name}のデータ取得でエラー\n{e}");
            return 0;
        }
        catch (MissingReferenceException e)
        {
            Debug.LogError($"{this.name}のデータ取得でエラー\nCharaDataが設定されていません\n{e}");
            return 0;
        }
    }

    /// <summary>
    /// 最高加速度を返す
    /// </summary>
    /// <returns></returns>
    public float GetMaxAccel()
    {
        PlayerMoveCtrl player = prefab.transform.GetChild(0).GetComponent<PlayerMoveCtrl>();

        if (player == null)
        {
            return 0;
        }
        else
        {
            return player.GetMaxAccel();
        }
    }

    public float GetHandling()
    {
        PlayerMoveCtrl player = prefab.transform.GetChild(0).GetComponent<PlayerMoveCtrl>();

        if (player == null)
        {
            return 0;
        }
        else
        {
            Vector2 vecHandling = player.GetHandling();
            // 高速時の向きの変えやすさ
            // 低いと高速時の向き変えがゆっくりになり、ふらつきにくい
            float slowHandling = vecHandling.x;
            // 低速時の向きの変えやすさ
            // 高いと低速時の向き変えを素早く行える
            float fastHandling = vecHandling.y;

            int[] slowHandlingPoint = new int[11] { 6, 8, 9, 10, 9, 7, 5, 3, 2, 1, 0 };
            int[] fastHandlingPoint = new int[11] { 1, 3, 5, 7, 9, 10, 9, 8, 7, 6, 5 };
            int[] dynamicRangePoint = new int[11] { 8, 9, 10, 9, 8, 6, 4, 3, 2, 1, 0 };

            int num;
            float rate;

            // 低速時の操作のしやすさ
            num = Mathf.FloorToInt(fastHandling);
            rate = fastHandling - num;
            scoreFastStability = (num>=fastHandlingPoint.Length-1)? fastHandlingPoint[num] : fastHandlingPoint[num] * (1 - rate) + fastHandlingPoint[num + 1] * rate;

            // 高速時の操作のしやすさ
            num = Mathf.FloorToInt(slowHandling);
            rate = slowHandling - num;
            scoreSlowStability = (num >= slowHandlingPoint.Length - 1) ? slowHandlingPoint[num] : slowHandlingPoint[num] * (1 - rate) + slowHandlingPoint[num + 1] * rate;

            // 低速時と高速時のハンドリングの変化量
            num = Mathf.FloorToInt(Mathf.Abs(fastHandling - slowHandling));
            rate = Mathf.Abs(fastHandling - slowHandling) - num;
            scoreHandlingDynamicRange = (num >= slowHandlingPoint.Length - 1) ? dynamicRangePoint[num] : dynamicRangePoint[num] * (1 - rate) + dynamicRangePoint[num + 1] * rate;

            // 低速なほど早く、
            // 高速なほど遅く向きを変えると高得点
            // 向きの変えやすさの変動の幅が小さいほど高得点
            return scoreFastStability * 1.25f + scoreSlowStability * 1.25f + scoreHandlingDynamicRange * 0.5f;
        }
    }


#endif

}
