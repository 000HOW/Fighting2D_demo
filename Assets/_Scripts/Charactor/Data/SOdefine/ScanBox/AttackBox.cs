using UnityEngine;

/// <summary>
/// 攻击盒子：
/// 配置攻击检测盒子的参数
/// </summary>
[CreateAssetMenu(fileName ="Hitbox",menuName ="PlayerControler/Hitbox")]
public class AttackBox : BaseScanBox
{
    public float angle;
    [Range(0,1f)]
    public float endNormalizedTime;
    //检测窗口开始的动画归一化时间
    [Range(0,1f)]
    public float startNormalizedTime;
}
