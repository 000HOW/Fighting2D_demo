using System;
using UnityEngine;

/// <summary>
/// 特效播放器：挂在特效预制体根节点，负责朝向、排序与播完自动回池
/// </summary>
public class FXPlayer : MonoBehaviour
{
    Animator anim;
    SpriteRenderer sr;
    bool playing;
    Action<FXPlayer> onFinished;

    void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 播放一次特效（池化复用：从第 0 帧重播），应用完整形态（位置/旋转/缩放/朝向/排序/速度）
    /// </summary>
    public void Play(FXPlayParams p, Action<FXPlayer> finished)
    {
        // 先重置动画再应用形态：避免 Rebind 重置 flipX/排序等渲染状态
        anim.Rebind();
        anim.Play(0, 0, 0f);                  // 从头重播（池化复用必备）
        anim.speed = p.playSpeed > 0f ? p.playSpeed : 1f;   // 动画播放速度

        transform.position = p.position;
        transform.rotation = Quaternion.Euler(0f, 0f, p.rotationZ);

        transform.localScale = new Vector3(p.scale.x, p.scale.y, 1f);
        if (sr != null)
        {
            sr.sortingOrder = p.sortingOrder;
        }
        playing = true;
        onFinished = finished;
    }

    void Update()
    {
        if (!playing) return;
        var st = anim.GetCurrentAnimatorStateInfo(0);
        // 当前动画播完且不在过渡中 → 回调回池
        bool done = st.normalizedTime >= 1f && !anim.IsInTransition(0);
        if (done)
        {
            playing = false;
            onFinished?.Invoke(this);
        }
    }
}
