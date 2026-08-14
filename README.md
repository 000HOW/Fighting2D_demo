# Unity 角色控制框架与玩法系统演示

大家好，我是**问号**，这是我使用 Unity 开发的 demo，项目完全从 0 开始开发了一个月。  
包含以下系统：

- 基于角色通用控制器框架
- 队列式的伤害结算
- Buff 乘区系统
- 连击系统
- 技能系统
- AI 敌人与 Boss 系统
- 对话与奖励系统
- 事件总线
- 对象池特效
- 面板堆栈 UI
- Addressables 异步场景加载 + Timeline 过场动画

---

## 一、角色控制器框架（核心）

### 设计思想

采用 **“输入与检测 → 运行时数据（Blackboard）→ 状态决策 → 仲裁器仲裁 → 执行器输出”** 的逻辑链路，配合 **事件总线** 做模块间及外部系统的消息传递，实现解耦。


### 可扩展性与维护性

- 玩家与 AI 共用同一套输入接口，输入决策层与角色框架完全解耦
- 修改状态表现只需修改对应状态注入数据
- 新增技能或自定义状态：新建继承 `BaseCustomStateData` 的行为资产（含配置），**框架层零改动**


---

## 逻辑链路拆解

### （1）输入检测部分

#### 输入管线
- `IInput` 接口是唯一输入源抽象，返回 `InputCommand` 指令数据（`struct` 值类型）
- `InputSensor` 每个驱动帧采样写入黑板类的 `InputData` 按键检测数据类
- 黑板中的 `InputData` 使用**固定大小数组实现环形缓冲**，存 `ECommand` 原子指令（`struct`），避免 GC
- 超过检测窗口时间的旧指令自动剔除
- 缓存输入指令可实现输入手感调整，或实现序列指令搓招


#### 环境检测
- `EnvironmentSensor` 每个驱动帧检测角色的运行时状态和环境数据，写入黑板类的 `CharacterRunTimeData` 数据容器


---

### （2）运行时数据（Blackboard）

`Blackboard` 类作为**运行时数据中枢**，持有以下容器：

- `CharacterRunTimeData`（实例运行状态）
- `InputData`
- `ReadyToApply`（每帧速度意图 + 本帧命中去重集合）
- `CharacterSO`（静态配置）

**特点**：
- 引用传递、构造注入，系统间不复制数据、单向依赖
- 数据类型分类封装到对应数据类容器中，避免所有数据直接塞入黑板导致混乱

---

### （3）状态机决策 + 仲裁器仲裁

#### 状态设计

**生命周期细分**：
- `OnEntryStart` → `OnEntry` → `OnUpdate`（主循环）→ `OnExitStart` → `OnExit`
- 由 `StateMachine` 按 `StateProgress` 标志（Entry → Main → Exit）三阶段驱动
- 支持 `pending` 目标状态的延迟切换
- 相比传统生命周期，可实现多帧起始/结束动作的逻辑衔接

**状态分类**：
- `GenericState`：通过基础状态枚举（idle, walk, run 等）区分，运行时注入 `StateData`（可序列化）达到不同表现
- `CustomState`：针对高度自定义的状态，通过重写 `BaseCharacterState` 抽象类实现多态，注入的行为资产 `BaseCustomStateData` 继承自 `ScriptableObject`，**新增动作 = 新建资产，框架零改动**

#### 状态转换调度

- 状态逻辑与切换逻辑分离，由外部**状态调度系统**统一调度
- `ConditionsManager` 根据配置的**状态转移表**调度
  - 每个状态类别有一张转移表
  - 条件原语（地面检测、移动意图、跳跃意图等）颗粒度合理抽象封装为转移条件
  - 转移表持有条件并指向目标状态，`ConditionsManager` 轮循各表进行调度

#### 仲裁器

**解决“一帧内多个系统同时申请切状态，谁说了算”**：

- **帧内收集，帧末统一仲裁**：
  - 任意系统在帧内随时 `Request(...)`
  - `Execute()` 在帧末按优先级排序，只执行最高优先级请求，清空队列 —— 避免一帧内多次切换竞态

- **两级打断控制**：
  - `cancelTime`（可取消时间）：当前状态进入主循环后需等待 `cancelTime` 秒才允许被打断 —— 防止“无限连取消”（攻击前摇不可取消）
  - `MinInterruptPriority`（霸体）：当前状态声明最小打断优先级，只有更高优先级请求才能打断。`Death = int.MaxValue` 是终态，谁也不能顶

- **强制打断**：受击、死亡走 `ignoreCancelTime:true`；技能是否强制打断由资产配置（勾选即等同受击强制打断）

- `Request` 返回 `bool`：申请成功与否立即可知，调用方据此决定是否消费输入缓冲 / 保留技能请求 —— 杜绝“吞输入”“吞技能”

**当前优先级表**：
```
Idle/Walk/Run 0 < Custom(技能) 3 < Up/Fall 5 < Dash 10 < Attack 50 < Hit 100 < Death ∞
```

---

### 执行器输出

- `MotionActuator` 和 `AnimationActuator` 读取黑板待执行数据，执行对应表现，实现单一职责
- **动画执行**：不使用传统 `Animator Controller` 状态机
  - 原因：Animator 擅长表现切换，但难以表达“多来源状态请求 + 优先级仲裁 + 霸体 + 可取消时机”，且必须预先定义所有动画状态，强绑定
  - 方案：代码直接控制动画，逻辑状态机用代码写，Animator 只作为动画执行器
  - 对 `Playable API` 简单封装，实现类似 Animancer 的基础功能（满足当前需求；自造轮子为理解原理，且项目动画复杂度不高）

---

## 其他拓展系统

### 1. 伤害系统（队列式帧末结算）
- `DamageReceiver.TakeDamage(DamageData)` 只入队，不立即结算，返回 `bool` 表示“是否真正接收”（死亡角色返回 false，攻击方据此判定未命中）
- 帧末 `CalculateDamage()` 统一出队结算，触发扣血或死亡事件

### 2. Buff 修改器
- 新 Buff 先进入 `pendingAdds` 缓冲，`OnUpdate` 开头合并（本帧立即参与计算，遍历中写主列表不冲突）
- `UsableModifier` 带持续时间，`Tick()` 到期自动移除，倒序 `RemoveAt` 零 GC

### 3. 连击系统
- 击中目标后计算连招，连续攻击同一敌人触发当前攻击的派生连招
- UI 连击数与攻击连段是**两套独立状态**：
  - 连击数跨攻击累计，只有受击或超时清零
  - 连段在切招时重置（UI 不清零）

### 4. 技能系统
**调用链**：
```
玩家按键 → SkillSender → CharacterController.UseSkill(BaseCustomStateData)
→ SkillManager.UseSkill（异步入队，标记 SkillPending）
→ 下一次驱动帧尝试 arbiter.Request(Custom, …, Refresh, customData)
→ 帧末仲裁执行 → CustomState 驱动动画 + 资产行为回调
```

---

## 模块统一管理

所有模块通过 `CharacterController` 类（唯一继承 `MonoBehaviour`）进行：
- 集中实例化
- 注入相关依赖
- 明确统一的 `Tick` 驱动调用

便于管理和调试。

---

## 二、玩法系统

### 1. AI 敌人系统

**架构**：
- `AI_EnemyBrain`（决策，每实例）→ `EnemyInput`（实现 `IInput`）→ 复用角色框架

**行为状态机**：
```
Idle → Patrol → Chase → Attack → ReturnHome
```

**决策优先级**：
```
回家 > 领地内近距攻击 > 领地内追击 > 巡逻 > 待机
```

**扩展性**：Boss 等复杂多阶段敌人继承 `AI_EnemyBrain` 重写部分功能进行拓展。

---

### 2. 对话系统

- **图结构**：`DialogueGraph.nodes[].choices[].jumpTo`
- 通过 `ScriptableObject` 配置对话内容、选择跳转结点等
- **事件驱动** `DialoguePanel`
- 奖励结点“到达只发一次”
- 打字中按 `E` 跳过打字

---

## 三、事件系统

- **EventBus**：完全类型安全的泛型事件系统，无字符串事件
- `Dictionary<Type, object>` 统一装箱存储，触发时强转 `List<Action<T>>` 零装箱
- **父子级联冒泡**：
  - 每个角色一条私有总线（`new EventBus(EventBus.Global)`）
  - 本地触发自动冒泡到全局根总线
  - 外部系统（UI/奖励/特效）在 Global 订阅、按引用过滤
- **生命周期**：`IDisposable`，`CharacterController.OnDestroy` / `EventBusBinder` 释放，防订阅泄漏

---

## 四、UI 框架

- 参考 B 站 Up 主分享的框架：[【Unity编程】这大概是最好理解的UI框架了吧](https://www.bilibili.com/video/BV1Bz4y1D7rL/?share_source=copy_web&vd_source=3f82c1b801f9c1f638c8c4a5f9c1a125)
- 搭配事件系统与外界通信

---

## 五、场景加载

- 使用 **Addressables** 实现异步加载场景
- 搭配 **Timeline** 实现场景过渡

---

## 演示与配置

### 角色控制框架的使用和配置

- 角色物体挂载基本组件
- 挂载控制器脚本，添加角色配置文件（已提前配置）
- 状态转移表：每个转移表对应状态的 `StateData` 配置
- 拖拽键盘按键输入源组件，即可实现基本控制

### StateData 配置示例

- 删除跑步的结束动画
- 调整跑步运动的快慢节奏感

> （现场演示）

---

## 项目演示（穿插系统实现）

### 场景加载
- Addressables 异步加载 + Timeline 过度

### 事件系统
- EventBus 父子级联冒泡

### UI 框架
- 使用上述 Up 主框架，搭配事件通信

### 对话系统
- 图结构 SO 配置，事件驱动 DialoguePanel
- 奖励结点一次性触发
- 打字跳过（E 键）
- 对话结点可设置奖励（如获取技能，在背包可见）

### 技能系统
- 背包技能可穿戴至技能槽
- 点击图标安装/拆卸
- 按对应按键释放，技能槽显示冷却

### Buff 系统
- 击败敌人获得限时移速 Buff

### AI 敌人系统
- 架构与行为状态机如前所述
- Boss 等继承扩展

### 战斗表现
- 技能动作可被受击打断
- 屏幕中间显示攻击命中连招
- 命中敌人可触发派生动作
