using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家跨场景数据桥（单例，挂 GameRoot 下，DontDestroyOnLoad）。
/// 场景切换前由 SceneSystem 捕获玩家运行时数据，切换后由 PlayerSessionBridge 恢复。
/// 仅内存保存，不落盘。
/// </summary>
public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance { get; private set; }

    /// <summary>是否已捕获一份待恢复的快照（消费后置 false，避免下次误恢复）。</summary>
    public bool HasSession { get; set; }

    public float CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public int FacingDir { get; private set; } = 1;
    public List<PlayerSkillData> SkillBag { get; private set; }
    public PlayerSkillData[] EquipmentSlot { get; private set; }
    public float[] CooldownRemaining { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>抓取当前玩家快照（切换前调用）。</summary>
    public void Capture(CharacterControler ctrl, SkillSender sender)
    {
        if (ctrl == null || sender == null) return;

        var rt = ctrl.blackboard.characterRunTimeData;
        CurrentHealth = rt.currentHealth;
        IsDead = rt.isDead;
        FacingDir = rt.facingDir;

        SkillBag = sender.skillBag != null
            ? new List<PlayerSkillData>(sender.skillBag)
            : new List<PlayerSkillData>();
        EquipmentSlot = sender.EquipmentSlot != null
            ? (PlayerSkillData[])sender.EquipmentSlot.Clone()
            : new PlayerSkillData[SkillSender.SLOTCOUNT];
        CooldownRemaining = sender.CooldownSnapshot();

        HasSession = true;
    }
}
