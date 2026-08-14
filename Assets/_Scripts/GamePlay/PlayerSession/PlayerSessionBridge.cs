using UnityEngine;

/// <summary>
/// 玩家跨场景保护桥接（挂在场景中的 Player 物体上）。
/// 进入新场景后，从 PlayerSession 恢复 HP / 朝向 / 技能背包 / 装备槽 / 冷却；
/// 销毁前兜底把当前数据写回 PlayerSession。
/// </summary>
[RequireComponent(typeof(CharacterControler))]
[RequireComponent(typeof(SkillSender))]
public class PlayerSessionBridge : MonoBehaviour
{
    CharacterControler ctrl;
    SkillSender sender;

    void Awake()
    {
        ctrl = GetComponent<CharacterControler>();
        sender = GetComponent<SkillSender>();
    }

    void Start()
    {
        // Unity 保证本帧所有 Awake/Start 先于首个 FixedUpdate，
        // 此时 CharacterControler / SkillSender 已完成自身初始化，恢复安全。
        var session = PlayerSession.Instance;
        if (session == null || !session.HasSession) return;

        ctrl.RestoreRuntime(session.CurrentHealth, session.IsDead, session.FacingDir);
        sender.RestoreFromSession(session.SkillBag, session.EquipmentSlot, session.CooldownRemaining);

        session.HasSession = false;   // 一次性消费，避免下次场景误恢复
    }

    void OnDestroy()
    {
        // 兜底：未走 SceneSystem 捕获流程时也在销毁前保存
        if (PlayerSession.Instance != null && ctrl != null && sender != null)
        {
            PlayerSession.Instance.Capture(ctrl, sender);
        }
    }
}
