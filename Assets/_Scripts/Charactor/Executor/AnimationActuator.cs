using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 动画播放器 ：根据当前是状态数据里的动画数据播放动画
/// 注意：销毁类时一定调用Dispose（）销毁非托管资源！！！
/// 需要初始化，驱动
/// </summary>
public class AnimationActuator : IDisposable
{
    //播放图：整个树状结构的管理器，负责创建、连接和销毁所有节点
    PlayableGraph graph;
    //树的“根”，负责将最终处理好的数据（如动画）应用到游戏对象上
    AnimationPlayableOutput output;
    //树上的每个节点。它是数据的来源或处理器
    public AnimationClipPlayable curClipPlayable{get;private set;}
    Blackboard bb;
    CharacterRunTimeData playerRunTimeData;
    StateData cur_stateData;
    StateProgress progress;
    StateProgress lastProgress;
    Dictionary<AnimationClip,AnimationClipPlayable> nodeMap = new();
    // 是否已初始化
    bool initialized;
    bool firstEntryPlay;
    bool firstExitPlay;
    bool firstMainPlay;
    float animNormalizedTime = 0;

    public void Initialize(Animator animator, Blackboard bb)
    {
        if (initialized) return;

        this.bb = bb;
        playerRunTimeData = this.bb.characterRunTimeData;
        animator.runtimeAnimatorController = null;

        // 创建 PlayableGraph
        graph = PlayableGraph.Create("CharAnimSystem");

        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // 创建输出，连接到 Animator
        output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);

        // 创建一个空剪辑的 Playable 占位
        curClipPlayable = AnimationClipPlayable.Create(graph, null);
        output.SetSourcePlayable(curClipPlayable);

        firstEntryPlay = true;
        firstExitPlay = true;
        firstMainPlay = true;

        // 开始播放图
        graph.Play();

        // 订阅状态进入事件：真正进入新状态时强制重置首播标志，保证动画重新播放并归零时间
        // （解决连招/刷新状态时 progress 不变导致动画不重播、归一化时间残留的隐患）
        bb.eventBus.Subscribe<StateEntryStart>(OnStateEntryStart);

        initialized = true;
    }

    /// <summary>
    /// 状态真正进入时触发：重置各阶段首播标志（配合 OnUpdate 里 progress 变化兜底，解决连招刷新不重播动画）
    /// </summary>
    void OnStateEntryStart(StateEntryStart entryStart)
    {
        firstEntryPlay = true;
        firstExitPlay = true;
        firstMainPlay = true;
    }

    public void OnUpdate()
    {
        progress = bb.characterRunTimeData.stateProgress;
        if (progress != lastProgress)
        {
            // Debug.Log("newstateSgine");
            firstEntryPlay = true;
            firstExitPlay = true;
            firstMainPlay = true;
        }
        cur_stateData = bb.characterRunTimeData.curStateData;
        

        if (cur_stateData==null)
        {
            Debug.LogWarning("No clip to play,because no stateData");
            return;
        }

        switch(progress)
        {
            case StateProgress.Entry:
                if (!firstEntryPlay) break;
                if (cur_stateData.startClip==null) return;
                // Debug.Log($"first play startClip: {cur_stateData.startClip.name}");
                PlayClip(cur_stateData.startClip,cur_stateData.startClipSpeed);
                firstEntryPlay = false;
            break;

            case StateProgress.Exit:
                if (!firstExitPlay) break;
                if (cur_stateData.endClip==null) return;
                // Debug.Log($"first play endClip: {cur_stateData.endClip.name}");
                PlayClip(cur_stateData.endClip,cur_stateData.endClipSpeed);
                firstExitPlay = false;
            break;

            case StateProgress.Main:
                if (firstMainPlay)
                {
                    if (cur_stateData.mainClip==null)
                    {
                        Debug.LogError($"no mainclip in {bb.characterRunTimeData.currentstateType} !!!");
                        return;
                    }

                    curClipPlayable = GetclipPlayble(cur_stateData.mainClip);
                    firstMainPlay = false;
                    PlayClipPlayable(curClipPlayable,cur_stateData.mainClipSpeed);
                    // Debug.Log($"first play mainclip: {cur_stateData.mainClip.name}");
                }
                // Debug.Log(animNormalizedTime);
                if(animNormalizedTime>0.99f&&cur_stateData.loop)
                {
                    PlayClipPlayable(curClipPlayable,cur_stateData.mainClipSpeed);
                }
            break;
        }

        if (!curClipPlayable.IsValid())
        {
            Debug.LogWarning("curClipPlayable is not Valid!!!");
            return;
        }

        double currentTime = curClipPlayable.GetTime();

        //GetDuration()：返回 Playable 的“计划播放时长”，默认无限长（double.MaxValue）
        
        double totalDuration = curClipPlayable.GetAnimationClip().length;

        animNormalizedTime = (float)(currentTime/totalDuration);
        // Debug.Log($"GetTime: {currentTime}, GetDuration: {totalDuration},\n Normalized: {animNormalizedTime}");

        playerRunTimeData.AnimNormalizedTime = animNormalizedTime;

        lastProgress = progress;
    }

    public void SetCurrentClipSpeed(float speed)=>curClipPlayable.SetSpeed(speed);
    public void PlayClip(AnimationClip clip,float speed)
    {
        if (clip == null) return;
        //------------  获取动画结点  --------------------
        AnimationClipPlayable clipPlayable = GetclipPlayble(clip);
        //-------------  播放动画  --------------------
        PlayClipPlayable(clipPlayable,speed);
    }

    public void PlayClipPlayable(AnimationClipPlayable clipPlayable,float speed)
    {

        speed *= bb.modificationManager.FinalModificationMultiplier(ModifyValueType.MoveSpeed);
        clipPlayable.SetSpeed(speed);

        clipPlayable.SetTime(0);

        output.SetSourcePlayable(clipPlayable);

        curClipPlayable = clipPlayable;
    }

    public AnimationClipPlayable GetclipPlayble(AnimationClip clip)
    {
        if (!nodeMap.TryGetValue(clip, out AnimationClipPlayable clipPlayable))
        {
            clipPlayable = AnimationClipPlayable.Create(graph, clip);
            nodeMap[clip] = clipPlayable;
        }

        return clipPlayable;
    }

    public void Dispose()
    {
        if (bb != null && bb.eventBus != null)
        {
            bb.eventBus.Unsubscribe<StateEntryStart>(OnStateEntryStart);
        }
        if (graph.IsValid())
        {
            graph.Destroy();
        }
        initialized = false;
    }
}
