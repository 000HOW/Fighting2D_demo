using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 攻击发动系统
/// 注意：需要初始化，需要驱动
/// </summary>
public class AttackManager
{
    List<SpecialAttack> specialAttacks;
    InputData inputData;
    CharacterRunTimeData playerRunTimeData;
    StateTransitionArbiter arbiter;
    ComboManager comboManager;
    public AttackManager(Blackboard blackboard,StateTransitionArbiter _arbiter,ComboManager _comboManager)
    {
        inputData = blackboard.inputData;
        playerRunTimeData = blackboard.characterRunTimeData;
        arbiter = _arbiter;
        comboManager = _comboManager;
        specialAttacks = blackboard.CharacterSO.specialAttacks;
        if (specialAttacks==null||specialAttacks.Count==0)
        {
            Debug.LogWarning("no specialAttacks in attaackManager !!!");
        }
    }

    public void OnUpdate()
    {
        if (playerRunTimeData.isDead) return;   // 死亡后不再识别/发起攻击
        if (!playerRunTimeData.useAttackManager) return;

        // 当前状态未到可取消时间时不识别攻击指令（避免误消费输入缓冲）
        if (!arbiter.CanCancelCurrentState()) return;

        SpecialAttack specialAttack = CheckInputBuffer();
        if (specialAttack!=null)
        {
            // 派生连招段选择：切招打断旧招 / 连击段 / 默认段
            ComboStage stage = comboManager.SelectStage(specialAttack);

            StateData data = stage?.attackData ?? specialAttack.attackData;
            AttackBox box = stage?.attackBox ?? specialAttack.attackBox;
            SwitchStateType switchType = stage?.switchType ?? specialAttack.switchType;
            AttackData attack = stage?.attack ?? specialAttack.attack;

            playerRunTimeData.curAttackBox = box;
            playerRunTimeData.curAttack = attack;
            // ignoreCancelTime 用命名参数（原代码误传进 checkSame 位）
            arbiter.Request(StateType.attack,data,switchType,ignoreCancelTime:data.ignoreCancelTime);
            // playerRunTimeData.BlockConditionManager = true;
        }
    }

    SpecialAttack CheckInputBuffer()
    {
        if (specialAttacks==null) return null;
        foreach (SpecialAttack attack in specialAttacks)
        {
            if (attack.commandList==null) continue;
            int count = attack.commandList.Count;
            if (count == 0)
            {
                Debug.LogWarning("no command in SpecialAttack !!!");
                continue;
            }
            

            if (attack.commandList[0] != inputData.PeekBufferComand().eCommand) continue;

            float startTime = inputData.PeekBufferComand().pressedTime;

            for (int i = 0; i < count; i++)
            {
                ECommand command = default;
                if (!inputData.ReadBufferComand(i,out command)) break;

                if (attack.commandList[i] != command.eCommand) break;

                if (command.pressedTime - startTime > attack.checkWindowTime) break;

                if (i == count - 1)
                {
                    if (!inputData.UseBufferCommand(i+1))break;
                    // Debug.Log($"识别成功{attack}");
                    return attack;
                }
            }
        }
        return null;
    }

}


// public enum AttackType
// {
//     None,
//     LightAttack,        // 轻击（快，硬直小，伤害低）
//     MediumAttack,       // 中击（平衡）
//     HeavyAttack,        // 重击（慢，硬直大，伤害高/削韧强）
// }