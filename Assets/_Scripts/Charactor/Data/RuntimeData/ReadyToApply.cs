using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时待执行的数据
/// 执行器读取执行
/// </summary>
public class ReadyToApply
{
    public float exp_horizontalVelocity;
    public float exp_VerticalVelocity;
    /// <summary>
    /// 持有的敌人的游戏物体缓存,
    /// hashset去重
    /// </summary>
    public HashSet<GameObject> Enemies = new();
}