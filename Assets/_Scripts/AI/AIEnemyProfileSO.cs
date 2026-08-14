using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIEnemyProfile", menuName = "Enemy/AIEnemyProfile")]
public class AIEnemyProfileSO : ScriptableObject
{
    [Header("感知")]
    [Tooltip("玩家所在层")] public LayerMask targetLayer;
    [Tooltip("索敌半径：玩家进入该范围才被自动发现")] public float detectRadius = 8f;

    [Header("攻击")]
    [Tooltip("进入攻击状态的门槛距离：玩家进入该距离后敌人开始朝目标走位、准备出手（大于 attackStopDistance）")]
    public float attackRange = 1.2f;
    [Tooltip("两次出手的最小间隔（>= 攻击动画时长）")] public float attackInterval = 1.2f;
    [Tooltip("贴身出手距离：敌人走到该距离内便站定出招（应≈攻击盒实际命中距离；同时防止在玩家身边左右晃动）")]
    public float attackStopDistance = 0.5f;

    [Header("领地")]
    [Tooltip("离出生点超过该距离即放弃追击回家")] public float fieldLength = 5f;
    [Tooltip("回家完成判定距离：开始回家后须回到离基地该距离内才算回家完成（防领地边界来回切换，取值建议远小于 fieldLength）")]
    public float returnArriveDistance = 0.5f;

    [Header("技能表（可选）")]
    [Tooltip("普通敌人留空；需要技能的敌人在这里挂 AISkillSlot 资产（每个技能自带触发判定 CanUse 与自定义状态 customStateData；技能拥有最高优先级：任意 AI 状态下满足 CanUse 即施放，不限于战斗状态）")]
    public List<AISkillSlot> skills = new();
}
