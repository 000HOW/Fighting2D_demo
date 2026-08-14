using GameFramework.Event;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家面板 - 生命条管理
/// 负责 HP 滑块（真实值 / 减少缓冲 / 增加缓冲）的初始化与实时更新
/// </summary>
public class HPSliderUI
{
    StateSlider HPslider;
    CharacterControler character;
    UItool uItool;

    public HPSliderUI(CharacterControler _character)
    {
        character = _character;
    }

    public void Initialize(UItool tool)
    {
        uItool = tool;
        HPslliderInitialize();
        // 先注销再订阅：防止面板重复初始化导致 ChangeSliderValue 被多次订阅
        EventBus.Global.Unsubscribe<OnHpChange>(ChangeSliderValue);
        EventBus.Global.Subscribe<OnHpChange>(ChangeSliderValue);
    }

    public void Dispose()
    {
        EventBus.Global.Unsubscribe<OnHpChange>(ChangeSliderValue);
    }

    void HPslliderInitialize()
    {
        GameObject HP = uItool.FindChildGameobj("HP");
        Slider oriSlider = uItool.GetOrAddComponentInChildren<Slider>(HP,"oriSlider");
        Slider decreaseSlider = uItool.GetOrAddComponentInChildren<Slider>(HP,"decreaseSlider");
        Slider increaseSlider = uItool.GetOrAddComponentInChildren<Slider>(HP,"increaseSlider");
        HPslider = new StateSlider(oriSlider,decreaseSlider,increaseSlider);
        HPslider.SetValue(1);
    }

    void ChangeSliderValue(OnHpChange hpChange)
    {
        var self = character?.blackboard?.characterRunTimeData?.self;
        if (self == null) return;
        if (hpChange.Character==self.name)
            HPslider.SetValue(hpChange.NormalizedValue);
    }
}

/// <summary>
/// 生命值变化事件（由 DamageReceiver 发出）
/// </summary>
public struct OnHpChange
{
    public readonly float NormalizedValue;
    public string Character;
    public OnHpChange(float cur,string name)
    {
        NormalizedValue = cur;
        Character = name;
    }
}
