/// <summary>
/// 特效类型枚举：与 FXManager 的对象池一一对应
/// </summary>
public enum FXType
{
    None,
    HitExplosion,       // 普通攻击命中爆炸（one-shot）：EnvironmentSensor.AttackScan 命中经 CharacterSO.hitImpactFX 触发
    RunDust,            // 跑步尾气（loop）
    JumpDust,           // 跳起尾气
    FireballExplosion,  // 火球命中爆炸（one-shot）：FireballProjectile 命中经 FireballStateData.hitFX 触发（独立于普通攻击命中特效）
}
