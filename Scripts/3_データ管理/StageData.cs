using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Create Data/Stage Data")]
public class StageData : ScriptableObject
{
    public enum MusicStartTiming
    {
        [Tooltip("ステージのシーンに切り替わった直後に音楽を流す")]
        OnAwake,
        [Tooltip("レース開始時に音楽を流す")]
        RaceStart
    }

    [Header("Stage Status")]
    [Tooltip("ステージ判別用のID")]
    public int stageId;
    [Tooltip("ステージの名前")]
    public string stageName;
    [Tooltip("ステージがあるシーンの名前")]
    public string stageSceneName;
    [Tooltip("ステージのイメージ画像")]
    public Sprite stageSprite;
    [Tooltip("ステージの全体マップの画像")]
    public Sprite mapSprite;

    [Header("Player Setting")]
    [Tooltip("プレイヤーの初期位置")]
    public Vector3[] playerStartPos = new Vector3[4];
    [Tooltip("プレイヤーの初期角度")]
    public Vector3[] playerStartRot = new Vector3[4];
    
    [Header("Bgm")]
    [Tooltip("音楽を流すタイミング")]
    public MusicStartTiming musicStartTiming = MusicStartTiming.OnAwake;
    public AudioClip introAudioClip;
    public AudioClip loopAudioClip;

    [Header("Video")]
    [Tooltip("紹介用の動画")]
    public VideoClip promotionVideo;

    [System.Serializable]
    public enum StageSizeType
    {
        [Tooltip("小さめ：半径50m以下")]
        Small = 0,
        [Tooltip("普通　：半径50～100m")]
        Medium = 1,
        [Tooltip("大きめ：半径100m以上")]
        Large = 2,
        [Tooltip("未設定：---")]
        NoData = 999
    }
    [Header("Parameter")]
    [Tooltip("ステージの大きさ")]
    public StageSizeType stageSize = StageSizeType.Medium;
    [Tooltip("ステージの難しさ\n1:かんたん\n2:ふつう\n3むずかしい\n4:とてもむずかしい\n5:最高難易度\n0：---（未設定）")]
    [Range(0, 5)] public int difficulty = 2;

    [Tooltip("ステージ選択画面に表示させるかどうか")]
    public bool isSelectable = true;
}
