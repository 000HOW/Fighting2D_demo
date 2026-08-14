using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SkillSender))]
public class PlayerPanelManager : MonoBehaviour
{
    PanelManager panelManager;
    CharacterControler character;
    SkillSender skillSender;
    Action tick;
    public GameObject SkillslotButton;
    public GameObject BufferSlotPrefab;
    void OnEnable()
    {
        skillSender = GetComponent<SkillSender>();
        character = GetComponent<CharacterControler>();

        panelManager = new PanelManager();
        panelManager.Push(new PlayerPanel(SkillslotButton,skillSender,character,ref tick,BufferSlotPrefab));
    }
    void Update()
    {
        tick?.Invoke();
    }
    void OnDisable()
    {
        // 清空 tick 订阅，防止面板/UI 销毁后残留的旧订阅仍被 Update 调用
        // （旧 SkillSlotUI/BuffSlotManager 会访问已销毁的 Image 组件，抛出 MissingReferenceException）
        tick = null;
        panelManager.Clear();
    }


    void AddSkill(PlayerSkillData skill)
    {

    }
}

public struct NewSkillEvent
{
    
}