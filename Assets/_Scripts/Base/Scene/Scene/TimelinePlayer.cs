using System.Threading.Tasks;
using UnityEngine.Playables;

/// <summary>
/// Timeline 播放工具：播放 PlayableDirector 并等待其播放完成。
///
/// 实现说明：采用轮询 director.time 而非 director.stopped 事件 ——
/// PlayableDirector 首次 Play() 不会触发 stopped；默认 Hold 模式播完会停在 duration，
/// 轮询 time 最可靠。若 Timeline 是无限循环或时长为无穷，则直接返回（由信号机制或上层控制结束）。
/// </summary>
public static class TimelinePlayer
{
    /// <summary>播放 Timeline 并等待播放完成；director 为 null 时立即返回。</summary>
    public static async Task PlayAndWait(PlayableDirector director)
    {
        if (director == null)
        {
            await Task.Yield();
            return;
        }

        director.time = 0d;
        director.Play();

        // 无限循环 / 无效时长无法按 duration 判定结束，直接返回
        if (double.IsInfinity(director.duration) || director.duration <= 0d)
        {
            await Task.Yield();
            return;
        }

        // Hold 模式：播完 time 停在 duration，轮询比 stopped 事件可靠
        while (director.state == PlayState.Playing && director.time < director.duration - 0.01d)
        {
            await Task.Yield();
        }

        director.Pause();
    }
}
