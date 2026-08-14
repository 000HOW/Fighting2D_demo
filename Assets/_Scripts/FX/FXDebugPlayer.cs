using UnityEngine;

/// <summary>
/// 特效调试预览：挂角色身上，运行时把挂载的 FXConfigSO 循环在角色身上播放，
/// 边改资产参数边看真实效果。想换特效，直接手动拖入另一个 FXConfigSO 即可。
/// 纯运行时组件，仅开发期使用，正式场景不挂则零开销。
/// </summary>
public class FXDebugPlayer : MonoBehaviour
{
    [Tooltip("要循环预览的特效配置，换特效直接拖入另一个 FXConfigSO")]
    public FXConfigSO fxConfig;

    [Tooltip("循环播放间隔（秒），留 0 则用配置的 spawnInterval")]
    public float interval = 1f;

    CharacterControler controler;
    float timer;

    void Start()
    {
        controler = GetComponent<CharacterControler>();
    }

    void Update()
    {
        if (FXManager.Instance == null || fxConfig == null) return;

        float step = interval > 0f ? interval : fxConfig.spawnInterval;
        timer += Time.deltaTime;
        if (timer < step) return;
        timer = 0f;

        int dir = controler != null && controler.blackboard != null
            ? controler.blackboard.characterRunTimeData.facingDir : 1;
  
        Vector3 pos = fxConfig.GetWorldPos(transform.position, dir);
        FXManager.Instance.PlayFX(fxConfig, pos, controler.blackboard.characterRunTimeData.facingDir);

    }
}
