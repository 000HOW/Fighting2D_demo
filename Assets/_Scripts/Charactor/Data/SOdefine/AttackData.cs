/// <summary>
/// 攻击配置数据：每段攻击的类型与数值
/// 纯配置结构，不含任何运行时字段（攻击者、命中方向等运行时数据归 DamageData 管理）
/// </summary>
[System.Serializable]
public struct AttackData
{
    /// <summary>攻击类型（决定受击反应，查 HitInfo 映射）</summary>
    public DamageType damageType;
    /// <summary>基础伤害数值</summary>
    public float baseValue;
}
