using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayableTest : MonoBehaviour,IDisposable
{
     //播放图：整个树状结构的管理器，负责创建、连接和销毁所有节点
    PlayableGraph graph;
    //树的“根”，负责将最终处理好的数据（如动画）应用到游戏对象上
    AnimationPlayableOutput output;
    //树上的每个节点。它是数据的来源或处理器
    AnimationClipPlayable clipPlayable;
    public Animator animator;
    public AnimationClip clip;

    // Start is called before the first frame update
    void Start()
    {
        animator.runtimeAnimatorController = null;
        // 创建 PlayableGraph
        graph = PlayableGraph.Create("CharAnimSystem");

        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // 创建输出，连接到 Animator
        output = AnimationPlayableOutput.Create(graph, "AnimOutput", animator);


        // 开始播放图
        graph.Play();

        clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetTime(0);

        // 4. 【关键】将剪辑连接到输出
        output.SetSourcePlayable(clipPlayable);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
