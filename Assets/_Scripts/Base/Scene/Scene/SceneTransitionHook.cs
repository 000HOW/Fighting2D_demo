using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 场景过场钩子：挂到每个需要参与过场的场景根节点上。
/// - outroDirector：切出该场景时播放的退场 Timeline（如 Start 场景）。
/// - introDirector：进入该场景时播放的开场 Timeline（如 Main 场景）。
/// - loopAnimator：Start 场景默认循环播放的背景动画，outro 播放前停掉，避免与 Timeline 冲突。
/// </summary>
public class SceneTransitionHook : MonoBehaviour
{
    [Header("过场 Timeline")]
    [Tooltip("切出本场景时播放的退场 Timeline（例如 Start 场景的开场演出/标题退场）。")]
    public PlayableDirector outroDirector;

    [Tooltip("进入本场景时播放的开场 Timeline（例如 Main 场景的开场演出）。")]
    public PlayableDirector introDirector;

    [Header("背景循环动画（可选）")]
    [Tooltip("本场景默认循环播放的背景动画。outro 开始前会被停掉，避免与 Timeline 叠加冲突。")]
    public Animator loopAnimator;

    /// <summary>暂停背景循环动画（outro 播放前由 SceneSystem 调用）。</summary>
    public void StopLoopAnimation()
    {
        if (loopAnimator != null) loopAnimator.enabled = false;
    }
}
