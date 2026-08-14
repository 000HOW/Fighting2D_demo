using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 环境和状态监测
/// 注意：依赖注入，需要驱动
/// </summary>
public class EnvironmentSensor
{
    Blackboard bb;
    Rigidbody2D rigid;
    AttackBox curAttackBox;
    bool attackInitialize =false;
    public EnvironmentSensor(Blackboard data,Rigidbody2D rigid,GameObject _self)
    {
        bb = data;
        this.rigid = rigid;
        bb.characterRunTimeData.self = _self;
        bb.eventBus.Subscribe<StateEntryStart>(AttackInitialize);
    }
    /// <summary>
    /// 驱动函数
    /// </summary>
    public void Onupdate()
    {

        GroundCheck();
        AttackCheck();

        

        bb.characterRunTimeData.horizontalVelocity = rigid.velocity.x;
        bb.characterRunTimeData.verticalVelocity = rigid.velocity.y;
    }

    /// <summary>
    /// 地面检测：
    /// 根据地面检测盒子给isground赋值
    /// </summary>
    void GroundCheck()
    {
        if (bb.CharacterSO.groundCheckBox==null)
        {
            Debug.LogError("no groundCheckBox !!!");
        }

        Vector2 orig = rigid.position + bb.CharacterSO.groundCheckBox.boxOffset;
        Vector2 size = new Vector2(bb.CharacterSO.groundCheckBox.boxsizeX,bb.CharacterSO.groundCheckBox.boxsizeY);
        float casdiatance = bb.CharacterSO.groundCheckBox.castDistance;
        LayerMask layer = bb.CharacterSO.groundCheckBox.layerMask;
        RaycastHit2D hit = Physics2D.BoxCast(orig,size,0,Vector2.down,casdiatance,layer);

        if (hit.collider==null)
        {
            bb.characterRunTimeData.isground = false;
            return;
        }
        if (hit.distance<=bb.CharacterSO.groundCheckBox.feetwidth)
        {
            bb.characterRunTimeData.isground = true;
        }
        else bb.characterRunTimeData.isground = false;   
    }

    /// <summary>
    /// 根据传入的攻击盒子进行检测
    /// </summary>
    /// <param name="attackbox">要检测的攻击盒子</param>
    void AttackScan(AttackBox attackbox)
    {
            Vector2 size = new Vector2(attackbox.boxsizeX,attackbox.boxsizeY);
            Vector2 center = new Vector2(rigid.position.x + attackbox.boxOffset.x*bb.characterRunTimeData.facingDir,rigid.position.y + attackbox.boxOffset.y);
            Collider2D []colliders = Physics2D.OverlapBoxAll(center,size,attackbox.angle,attackbox.layerMask);
            bool landed = false;
            if (colliders.Length!=0)
            {
                foreach(Collider2D collider in colliders)
                {
                    if (collider.gameObject==bb.characterRunTimeData.self) continue;
                    if (bb.readytoApply.Enemies.Contains(collider.gameObject)) continue;
                    // Debug.Log($"attackScan: {colliders.Length}");
                    if (collider.TryGetComponent(out IDamageable damageable))
                    {
                        bb.readytoApply.Enemies.Add(collider.gameObject);

                        // 攻击类型与数值来自当前攻击段配置（与范围盒子解耦）
                        AttackData cfg = bb.characterRunTimeData.curAttack;
                        DamageData damage = new DamageData
                        {
                            damageType = cfg.damageType,
                            baseValue = cfg.baseValue * bb.modificationManager.FinalModificationMultiplier(ModifyValueType.AttackPower),
                            Attacker = bb.characterRunTimeData.self,
                            hitDirection = collider.transform.position - bb.characterRunTimeData.self.transform.position ,
                        };

                        // 只有目标真正吃到伤害才算命中：
                        // 已死亡的目标 TakeDamage 返回 false → 不触发连击、不播放命中特效
                        if (damageable.TakeDamage(damage))
                        {
                            landed = true;
                            if (bb.CharacterSO.hitImpactFX!=null)
                            FXManager.Instance?.PlayFX(bb.CharacterSO.hitImpactFX, bb.CharacterSO.hitImpactFX.GetWorldPos(rigid.position,bb.characterRunTimeData.facingDir), bb.characterRunTimeData.facingDir);
                        }
                    }
                }
            }
            // 一次攻击只要命中≥1个敌人就计入一次连击（按“攻击段”计数，不按目标数）
            if (landed) bb.comboManager?.RegisterHit();

            bb.characterRunTimeData.attackboxActive = false;
    }
    /// <summary>
    /// 攻击检测：
    /// 检测当前攻击类型的攻击盒子范围内的敌人引用
    /// </summary>
    void AttackCheck()
    {
        // 放宽门：Boss 冲刺等自定义状态（实现 IAttackBoxScanEnabled 且 attackScanEnabled）也可扫描攻击盒
        bool customScan = bb.characterRunTimeData.currentstate is Customstate cs
            && cs.customStateData is IAttackBoxScanEnabled s && s.attackScanEnabled;
        if (bb.characterRunTimeData.currentstateType != StateType.attack && !customScan)
        { 
            attackInitialize = false;
            return;
        }
        // 自定义态可能在状态中途才开启扫描（如下落攻击仅 diving 阶段）：
        // attackScanEnabled 从 false→true 时重新武装 attackInitialize，
        // 否则 Entry 阶段被清 false 后永远扫不到攻击盒。
        if (customScan) attackInitialize = true;
        if (!attackInitialize) return;
        
        // 只在主动画段(mainClip)做窗口判断，起手/收招阶段一律不扫。
        // Custom 态（customScan）跳过该限制：开盒时机完全由自定义状态的 attackScanEnabled 控制，
        // 动画剪辑留空（AnimNormalizedTime 无法推进）时也能正常扫描攻击盒。
        if (bb.characterRunTimeData.stateProgress != StateProgress.Main && !customScan) return;

        // 攻击盒每帧从运行时读取：冲刺 Custom 态在 OnEntryStart 才写入 curAttackBox（晚于
        // StateEntryStart 触发的 AttackInitialize 缓存），必须刷新否则缓存陈旧/空引用。
        curAttackBox = bb.characterRunTimeData.curAttackBox;
        if (curAttackBox == null) return;

        if (!customScan)
        {
            float NormalizedTime = bb.characterRunTimeData.AnimNormalizedTime;
            if (NormalizedTime > this.curAttackBox.startNormalizedTime && NormalizedTime <  this.curAttackBox.endNormalizedTime)
            {
                // Debug.Log($"attackBoxActive: {bb.playerRunTimeData.attackboxActive}");
                bb.characterRunTimeData.attackboxActive = true;
            }
            else 
            {
                // Debug.Log($"NormalizedTime: {NormalizedTime}");
                bb.characterRunTimeData.attackboxActive = false;
            }

            if (bb.characterRunTimeData.attackboxActive==false) return;
        }
        else
        {
            // Custom 态（IAttackBoxScanEnabled）：不依赖动画归一化窗口，直接按攻击盒配置扫描；
            // 开关由自定义状态自己的 attackScanEnabled 控制（下落攻击仅下落阶段 / 旋风踢全程）
            bb.characterRunTimeData.attackboxActive = true;
        }
        AttackScan(curAttackBox);
    }
    void AttackInitialize(StateEntryStart entryStart)
    {
        curAttackBox = bb.characterRunTimeData.curAttackBox;
        bb.readytoApply.Enemies.Clear();
        attackInitialize = true;
    }
}
