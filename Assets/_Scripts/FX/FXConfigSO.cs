using UnityEngine;

/// <summary>
/// 特效配置资产：定义播放哪种特效及其形态（位置/旋转/缩放/朝向），可跨状态复用
/// </summary>
[CreateAssetMenu(fileName = "FXConfig", menuName = "PlayerControler/FXConfig")]
public class FXConfigSO : ScriptableObject
{
    [Header("特效与时机")]
    public FXType fxType = FXType.None;
    public StateFXTiming timing = StateFXTiming.MainLoop;
    [Tooltip("MainLoop 特效的生成间隔（秒），其他时机忽略")]
    public float spawnInterval = 0.06f;
    [Tooltip("动画播放速度倍率，1 为原始速度")]
    public float playSpeed = 1f;

    [Header("形态（相对角色本地坐标）")]
    [Tooltip("相对角色本地位置，followFacing 开启时 x 随朝向镜像")]
    public Vector2 localOffset;
    [Tooltip("绕 Z 轴旋转角度（度）")]
    public float localRotationZ;
    [Tooltip("特效本地缩放")]
    public Vector2 localScale = Vector2.one;
    [Tooltip("位置偏移与 Sprite 是否跟随角色朝向翻转")]
    public bool followFacing = true;

    /// <summary>计算相对角色的挂点位置（世界坐标）</summary>
    public Vector3 GetWorldPos(Vector3 selfPos, int facingDir)
    {
        float ox = followFacing ? localOffset.x * facingDir : localOffset.x;
        return selfPos + new Vector3(ox, localOffset.y, 0f);
    }
}

public enum StateFXTiming
{
    OnEntryOnce,   // 进入状态时播一次（如起跳喷发）
    MainLoop,      // 主循环阶段按间隔持续生成（如跑步尾气）
    OnExitOnce,    // 退出状态时播一次
}
