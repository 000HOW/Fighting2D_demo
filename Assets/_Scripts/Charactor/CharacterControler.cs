using GameFramework.Event;
using UnityEditor.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterControler : MonoBehaviour , IDamageable , ISkill , IModifier
{
    //==============  输入源  ==========================
    public IInput InputSource;

    //==============  角色配置文件  =====================
    [SerializeField]
    CharacterSO characterSO;

    //===============  运行时数据  =====================
    //理想：  “输入 → 运行时状态 → 输出指令”的管线思维
    CharacterRunTimeData playerRunTimeData;
    ReadyToApply readytoApply;
    InputData inputData;
    public Blackboard blackboard{get;private set;}

    //==============  角色状态和环境检测  ==================
    EnvironmentSensor environmentSensor;
    InputSensor inputSensor;

    //==============  参数执行器  =========================
    MotionActuator motionActuator;
    AnimationActuator animationActuator;

    //==============  状态相关工具   =======================
    public EventBus eventBus{get; private set;}
    StateRepository stateRepository;
    Statemachine statemachine;
    SpriteRenderer spriteRenderer;
    Rigidbody2D rigidbody2d;
    Animator animator;
    ConditionsManager conditionsManager;

    AttackManager attackManager;
    SkillManager skillManager;
    ComboManager comboManager;
    public StateTransitionArbiter arbiter{get;private set;}

    ModificationManager modificationManager;
    
    DamageReceiver damageable;
    void Awake()
    {
        eventBus = new EventBus(EventBus.Global);

        rigidbody2d = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        if (characterSO==null)
        Debug.LogError("no CharacterSo in controler!!!");

        playerRunTimeData = new CharacterRunTimeData();
        readytoApply = new ReadyToApply();
        inputData = new InputData();
        modificationManager = new ModificationManager();
        blackboard = new Blackboard(playerRunTimeData,inputData,readytoApply,characterSO,modificationManager,eventBus);
        // 回填：修改器事件所需的所属角色引用直接从 blackboard.playerRunTimeData.self 取，无需构造注入
        modificationManager.blackboard = blackboard;
        comboManager = new ComboManager(blackboard);
        blackboard.comboManager = comboManager;

        stateRepository = new StateRepository();
        statemachine = new Statemachine(stateRepository,blackboard);
        arbiter = new StateTransitionArbiter(statemachine,blackboard);
        conditionsManager = new ConditionsManager();
        attackManager = new AttackManager(blackboard,arbiter,comboManager);
        skillManager = new SkillManager(blackboard,arbiter);
        

        environmentSensor = new EnvironmentSensor(blackboard,rigidbody2d,gameObject);
        inputSensor = new InputSensor(blackboard,()=>InputSource);
        motionActuator = new MotionActuator(spriteRenderer,rigidbody2d,blackboard);
        animationActuator = new AnimationActuator();
        damageable = new DamageReceiver(blackboard,arbiter,modificationManager);
    }

    void Start()
    {
        inputData.Initialize(characterSO);
        statemachine.Initialize();

        conditionsManager.Initialize(blackboard,arbiter);

        damageable.Initialize();
        
        animationActuator.Initialize(animator,blackboard);
    }
    void FixedUpdate()
    {
        inputSensor.OnUpdate();
        environmentSensor.Onupdate();

        modificationManager.OnUpdate();

        statemachine.OnUpdate();
        statemachine.UpdatePhysics();


        damageable.OnUpdate();
        skillManager.OnUpdate();
        comboManager.OnUpdate();
        attackManager.OnUpdate();
        conditionsManager.OnUpdate();

        arbiter.Execute();
        
        
        motionActuator.Onupdate();
        animationActuator.OnUpdate();
    }
    private void OnDestroy()
    {
        animationActuator.Dispose();   
        eventBus?.Dispose(); 
    }

    public bool TakeDamage(DamageData damage)
    {
        return damageable.TakeDamage(damage);
    }

    public void UseSkill(BaseCustomStateData skillState)
    {
        if (playerRunTimeData.isDead) return;   // 死亡后不可释放技能
        skillManager.UseSkill(skillState);
    }

    public bool CanUseSkill(BaseCustomStateData skillState)
    {
        if (playerRunTimeData.isDead) return false;   // 死亡后不可释放技能
        return skillManager.CanUseSkill(skillState);
    }

    /// <summary>
    /// 是否有待释放技能（UseSkill 入队后、真正切 Custom 态前为 true；申请成功自动 false）。
    /// AI 决策层用它判定"技能在途"，防止异步入队期间重复施放。
    /// </summary>
    public bool SkillPending => skillManager.HasPendingSkill;

    public void AddModifier(ModifierData modifier)
    {
        modificationManager.AddModifier(modifier);
    }

    /// <summary>
    /// 跨场景恢复运行时数据（由 PlayerSessionBridge 在进入新场景后调用）。
    /// 只写纯数据字段，不影响 Awake/Start 初始化流程。
    /// </summary>
    public void RestoreRuntime(float hp, bool isDead, int facingDir)
    {
        playerRunTimeData.currentHealth = hp;
        playerRunTimeData.isDead = isDead;
        playerRunTimeData.facingDir = facingDir;
        damageable.RestoreHealth(hp);
    }
}
