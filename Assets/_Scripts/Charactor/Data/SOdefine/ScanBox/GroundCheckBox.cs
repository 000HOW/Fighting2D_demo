using UnityEngine;

/// <summary>
/// 地面检测盒子：
/// 配置地面检测盒子的参数
/// </summary>
[CreateAssetMenu(fileName ="GroundCheckBox",menuName ="PlayerControler/GroundCheckBox")]
public class GroundCheckBox : BaseScanBox
{
    [Min(0)]
    public float  castDistance = 0.3f;
    [Min(0)]
    public float feetwidth;
}
