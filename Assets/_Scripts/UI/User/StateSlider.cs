using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class StateSlider
{
    public Slider oriSlider;
    public Slider decreaseSlider;
    public Slider increaseSlider;

    private float currentValue;
    private float duration = 0.5f; // 缓冲动画时长

    public StateSlider(Slider ori, Slider dec, Slider inc)
    {
        oriSlider = ori;
        decreaseSlider = dec;
        increaseSlider = inc;
        // 初始按满血，避免读取预制体里硬编码的残留值(如 0.276)导致启动补条
        float init = 1f;
        currentValue = init;
        decreaseSlider.value = init;
        increaseSlider.value = init;
    }

    public void SetValue(float newValue)
    {
        newValue = Mathf.Clamp01(newValue);
        oriSlider.value = newValue; // 真实值立即变化

        // 终止可能正在进行的旧动画（避免冲突）
        decreaseSlider.DOKill();
        increaseSlider.DOKill();

        if (newValue < currentValue) // 减少
        {
            // 减少缓冲滑块从当前值平滑降到目标值
            decreaseSlider.value = currentValue;
            decreaseSlider.DOValue(newValue, duration);
            // 增加缓冲滑块立即跳到目标值
            increaseSlider.value = newValue;
        }
        else if (newValue > currentValue) // 增加
        {
            increaseSlider.value = currentValue;
            increaseSlider.DOValue(newValue, duration);
            decreaseSlider.value = newValue;
        }
        else
        {
            // 无变化，全部同步
            decreaseSlider.value = newValue;
            increaseSlider.value = newValue;
        }

        currentValue = newValue;
    }
}