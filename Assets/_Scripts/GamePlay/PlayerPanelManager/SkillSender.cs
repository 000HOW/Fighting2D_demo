using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Event;
using UnityEngine;

[RequireComponent(typeof(CharacterControler))]
public class SkillSender : MonoBehaviour
{
    [HideInInspector]
    public List<PlayerSkillData> skillBag{get;private set;}
    public static readonly int SLOTCOUNT = 2;
    public PlayerSkillData[] equipmentSlot = new PlayerSkillData[SLOTCOUNT];
    public PlayerSkillData[] EquipmentSlot
    {
        get
        {
            return equipmentSlot;
        }
    }
    public int Bagcount{get;private set;}
    // 每个装备槽的剩余冷却时间（0 = 就绪）
    float[] cooldownRemaining = new float[SLOTCOUNT];
    ISkill CharacterSkill;

    /// <summary>
    /// 初始化放在 Awake（而非 Start）：
    /// PlayerSessionBridge.Start 会先 RestoreFromSession 恢复技能数据，
    /// 若本组件 Start 在其后执行会把恢复的数据重新清空（跨场景技能丢失 bug）。
    /// Awake 保证先于任何 Start 执行，恢复数据不会被覆盖。
    /// </summary>
    void Awake()
    {
        CharacterSkill = GetComponent<ISkill>();
        equipmentSlot = new PlayerSkillData[SLOTCOUNT];
        cooldownRemaining = new float[SLOTCOUNT];
        skillBag = new();
    }

    public void SkillAddInBag(PlayerSkillData skill)
    {
        if (skillBag==null) skillBag = new();
        skillBag.Add(skill);
        Bagcount++;
    }

    public bool EquipSkill(PlayerSkillData skill)
    {
        if (!skillBag.Contains(skill)) return false;

        for(int i=0;i<SLOTCOUNT;i++)
        {
            if (equipmentSlot[i]==null)
            {
                equipmentSlot[i] = skill;
                cooldownRemaining[i] = 0; // 新装备的技能立即可用
                skillBag.Remove(skill);
                EventBus.Global.Fire(new SkillEquipmentChangedEvent(i));
                return true;
            }
        }
        return false;
    }

    public bool UnloadSkill(PlayerSkillData skill)
    {
        int index = -1;
        for (int i=0;i<SLOTCOUNT;i++)
        {
            if (equipmentSlot[i]==skill)
            {
                index = i;
                break;
            }
        }
        if (index==-1) return false;

        // 冷却中的技能不可卸下
        if (IsOnCooldown(index)) return false;

        skillBag.Add(skill);
        equipmentSlot[index] = null;
        cooldownRemaining[index] = 0;
        EventBus.Global.Fire(new SkillEquipmentChangedEvent(index));
        return true;
    }

    /// <summary>
    /// 查询：指定槽位是否处于冷却中
    /// </summary>
    public bool IsOnCooldown(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SLOTCOUNT) return false;
        return cooldownRemaining[slotIndex] > 0;
    }

    /// <summary>
    /// 查询：指定槽位剩余冷却时间（秒）
    /// </summary>
    public float GetCooldownRemaining(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SLOTCOUNT) return 0;
        return cooldownRemaining[slotIndex];
    }

    /// <summary>
    /// 查询：指定槽位技能的总冷却时长（取 SkillData.ColdownTime，空槽返回 0）
    /// </summary>
    public float GetCooldownDuration(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SLOTCOUNT) return 0;
        return equipmentSlot[slotIndex] != null ? equipmentSlot[slotIndex].ColdownTime : 0;
    }

    /// <summary>
    /// 查询：指定槽位的冷却填充值（0=刚释放/空 → 1=就绪/满）
    /// 用于 UI 的 Image.fillAmount，方向与 Buff 相反（从 0 填满到 1）
    /// </summary>
    public float GetCooldownFill(int slotIndex)
    {
        float duration = GetCooldownDuration(slotIndex);
        if (duration <= 0) return 1f; // 空槽或零冷却：全亮
        return Mathf.Clamp01(1f - cooldownRemaining[slotIndex] / duration);
    }

    /// <summary>
    /// 尝试释放指定槽位的技能：越界/空槽/冷却中/取消时间锁中均返回 false
    /// 成功释放后启动该槽冷却并广播开始事件
    /// </summary>
    public bool TryUseSkill(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SLOTCOUNT) return false;

        PlayerSkillData skill = equipmentSlot[slotIndex];
        if (skill == null || IsOnCooldown(slotIndex)) return false;

        // 技能核心是 BaseCustomStateData（SkillState）：配置缺失则无法释放
        if (skill.SkillState == null)
        {
            Debug.LogError($"[SkillSender] 技能 {skill.name} 缺少 SkillState 配置！");
            return false;
        }

        // 取消判断改为技能感知：技能勾了 ignoreCancelTime 可强制打断当前动作
        // （唯一判断权仍在角色控制器，与状态机完全一致）
        if (CharacterSkill != null && !CharacterSkill.CanUseSkill(skill.SkillState)) return false;

        CharacterSkill.UseSkill(skill.SkillState);

        cooldownRemaining[slotIndex] = skill.ColdownTime;
        EventBus.Global.Fire(new SkillCooldownStartEvent(slotIndex, skill, skill.ColdownTime));
        return true;
    }

    /// <summary>
    /// 每帧递减各槽位冷却时间，跨 0 时归零并广播结束事件
    /// </summary>
    void TickCooldowns()
    {
        for (int i = 0; i < SLOTCOUNT; i++)
        {
            if (cooldownRemaining[i] <= 0) continue;
            cooldownRemaining[i] -= Time.deltaTime;
            if (cooldownRemaining[i] <= 0)
            {
                cooldownRemaining[i] = 0;
                EventBus.Global.Fire(new SkillCooldownEndEvent(i));
            }
        }
    }

    /// <summary>
    /// 快照当前各槽位冷却时间（跨场景保护用）。
    /// </summary>
    public float[] CooldownSnapshot()
    {
        return cooldownRemaining != null
            ? (float[])cooldownRemaining.Clone()
            : new float[SLOTCOUNT];
    }

    /// <summary>
    /// 从跨场景会话恢复背包/装备槽/冷却（PlayerSessionBridge 调用）。
    /// </summary>
    public void RestoreFromSession(List<PlayerSkillData> bag, PlayerSkillData[] slots, float[] cd)
    {
        skillBag = bag ?? new List<PlayerSkillData>();
        Bagcount = skillBag.Count;

        if (slots != null)
        {
            for (int i = 0; i < SLOTCOUNT && i < slots.Length; i++)
                equipmentSlot[i] = slots[i];
        }
        if (cd != null)
        {
            for (int i = 0; i < SLOTCOUNT && i < cd.Length; i++)
                cooldownRemaining[i] = Mathf.Max(0f, cd[i]);
        }

        // 全量恢复完成：广播装备变更事件，通知技能槽 UI 重扫
        // （PlayerPanelManager.OnEnable 的 UI 扫描早于本恢复，此处补一次刷新修复跨场景 UI 不显示）
        EventBus.Global.Fire(new SkillEquipmentChangedEvent(-1));
    }

    void Update()
    {
        TickCooldowns();

        if (Input.GetKeyDown(KeyCode.U))
        {
            TryUseSkill(0);
        }
        else if (Input.GetKey(KeyCode.I))
        {
            TryUseSkill(1);
        }
        else if (Input.GetKey(KeyCode.O))
        {
            TryUseSkill(2); // 越界会被 TryUseSkill 边界检查拦截，安全
        }
    }
    
}
