using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName ="HitInfo",menuName ="PlayerControler/HitInfo")]
public class HitInfo : ScriptableObject
{
    /// <summary>
    /// 受击反应的状态数据
    /// </summary>
    public StateData stateData;
    /// <summary>
    /// 受击对应的攻击类型
    /// </summary>
    public DamageType damageType;
}