using System.Collections;
using UnityEngine;

/// <summary>
/// 通用 UI 渐变助手（挂在需要播放补间的面板根物体上）。
/// 用协程实现，不依赖 DOTween，保证逻辑层可独立运行。
/// </summary>
public class FadeHelper : MonoBehaviour
{
    public void Run(IEnumerator routine)
    {
        if (routine != null) StartCoroutine(routine);
    }

    /// <summary>
    /// 线性渐变 CanvasGroup.alpha，使用 unscaledDeltaTime（不受 Time.timeScale 影响，加载期间可靠）。
    /// </summary>
    public IEnumerator TweenAlpha(CanvasGroup group, float from, float to, float duration)
    {
        return TweenAlpha(group, from, to, duration, null);
    }

    /// <summary>
    /// 渐变 CanvasGroup.alpha，可指定缓动曲线（curve 为 null / 空曲线时退化为线性），使用 unscaledDeltaTime。
    /// </summary>
    public IEnumerator TweenAlpha(CanvasGroup group, float from, float to, float duration, AnimationCurve curve)
    {
        if (group == null) yield break;

        float safeDuration = Mathf.Max(duration, 0.001f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / safeDuration;
            float k = Mathf.Clamp01(t);
            float eased = (curve != null && curve.length > 0) ? curve.Evaluate(k) : k;
            group.alpha = Mathf.Lerp(from, to, eased);
            yield return null;
        }
        group.alpha = to;
    }
}
