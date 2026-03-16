# 20min2s 设计方案

## 目标

做一个 Windows / Android 程序：

- 用户有效使用设备累计 `20 分钟` 后，进入 `20 秒` 休息提醒。
- 如果用户正处于全屏游戏、全屏视频、演示等不适合打断的状态，提醒应 `延迟`，而不是立刻打断。
- “使用”不能等同于“设备开着”，无输入、无交互时不应继续累计。

## 对 `ProjectEye` 的参考结论

### 1. 它如何定义“离开电脑”

`ProjectEye` 在 [`MainService.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Service/MainService.cs#L566) 的 `IsUserLeave()` 中，用的是：

- 鼠标位置没有变化
- 系统当前没有播放声音

对应实现：

- 鼠标位置比较：[`MainService.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Service/MainService.cs#L549)
- 声音检测：[`AudioHelper.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/AudioHelper.cs#L18)

这意味着它并没有真正检测“键盘/鼠标输入”，也没有使用 `GetLastInputInfo` 这类系统级空闲时间接口。

### 2. 它的全屏免打扰其实是“跳过本次提醒”，不是“延迟提醒”

在 [`MainService.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Service/MainService.cs#L591) 的 `IsBreakReset()` 中：

- 如果开启“全屏时不要打扰我”
- 且前台窗口被判断为全屏
- 就直接返回 `true`

而调用方 [`ShowTipWindow()`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Service/MainService.cs#L473) 对 `true` 的处理是：

- 记一次跳过
- 重启工作计时

所以 `ProjectEye` 的语义是：

- 到点时如果你正在全屏，它会 `直接跳过这次休息`
- 然后重新开始下一轮计时

这不符合你要的“延迟提醒，不影响全屏体验，但之后还要补提醒”。

### 3. 它的全屏判断也比较粗

全屏判断在 [`Win32APIHelper.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Win32APIHelper.cs#L103)：

- 取前台窗口
- 看窗口尺寸是否接近主屏幕
- 对 Chrome 做了特殊处理

问题：

- 只看主屏尺寸，对多显示器和非主屏全屏不稳。
- 更像“窗口铺满屏幕”判断，不是严格意义上的“沉浸/独占全屏”判断。

### 4. 它的进程白名单判断也偏粗

在 [`MainService.cs`](/C:/Users/gg/project/20min2s/referrence/ProjectEye/src/Local/ProjectEye/Core/Service/MainService.cs#L609) 它遍历的是“所有正在运行的进程”，不是“当前前台进程”。

结果是：

- 只要某个白名单程序在后台挂着，也可能导致本次提醒被跳过。

## 建议的“使用”定义

### 核心定义

只有在以下条件同时成立时，才累计“使用时长”：

1. 设备处于可交互状态
2. 用户在最近一小段时间内发生过有效输入
3. 当前前台场景没有被用户手动排除

### 推荐判定规则

`使用中 = 设备亮屏/解锁 + 最近 45 秒内存在有效输入 + 当前前台应用可计时`

这里的 `45 秒` 是建议默认值，可配置为 `30-90 秒`。

这样定义有几个直接好处：

- 电脑只是开着，不会计时。
- 用户短暂停下来看内容，不会立刻被判定离开。
- 不需要记录用户具体按了什么键，只要知道“发生过输入”。

### 什么叫“有效输入”

#### Windows

推荐把以下都视为有效输入：

- 键盘按键
- 鼠标移动
- 鼠标点击
- 鼠标滚轮

实现上不需要自己全局 hook 全部输入，优先直接用系统空闲时间：

- `GetLastInputInfo`

这个接口的语义更接近你要的“用户最近有没有实际操作过设备”，比 `ProjectEye` 只比鼠标坐标靠谱很多。

#### Android

Android 上如果你也要求“无输入不计时”，严格实现通常需要：

- `UsageStatsManager` 判断前台应用
- `AccessibilityService` 判断最近交互
- 前台服务 + 本地通知做提醒

如果没有 `AccessibilityService`，Android 很难精确判断“用户最近是否真的在操作”，这时只能退化成：

- 亮屏
- 已解锁
- 前台应用持续活跃

所以 Android 版建议把“严格输入判定”做成一个带权限说明的增强模式。

### 是否把“纯看视频”算使用

按你的表述，默认应当 `不算`。

也就是：

- 视频全屏播放
- 用户 20 分钟都没有输入

这种情况不累计使用时长。

但产品上最好保留一个设置项：

- `被动观看也计入使用时长`

因为有些人会希望看视频、上网课、看文档也被计入眼睛使用。

## 正确的计时语义

不要把逻辑写成“每 20 分钟触发一次检查”。

应该写成“累计 20 分钟有效使用时长后，进入待休息状态”。

### 推荐状态机

- `Idle`
  - 当前无有效输入，不累计时长
- `Active`
  - 正在累计有效使用时长
- `BreakDueDeferred`
  - 已经达到 20 分钟，但当前不宜打断，提醒被延迟
- `Resting`
  - 正在休息 20 秒

### 状态转换

- `Idle -> Active`
  - 检测到有效输入
- `Active -> Idle`
  - 超过空闲阈值未输入
- `Active -> Resting`
  - 达到 20 分钟，且当前允许提醒
- `Active -> BreakDueDeferred`
  - 达到 20 分钟，但当前是全屏/演示/白名单场景
- `BreakDueDeferred -> Resting`
  - 全屏结束或不再处于延迟场景
- `Resting -> Idle`
  - 休息完成，等待下一次输入重新开始累计

### 关键点

进入 `BreakDueDeferred` 之后：

- `不要重置 20 分钟计时`
- `也不要继续无限累加`

应该把累计时间钉在阈值上，等合适时机再补提醒。

这就是你要的“延迟提醒”，不是“跳过提醒”。

## Windows 设计

### 推荐架构

- `ActivityMonitor`
  - 负责用户输入、锁屏、亮屏、前台窗口、全屏状态
- `TimerEngine`
  - 负责状态机、累计时间、延迟提醒
- `ReminderOrchestrator`
  - 负责提醒窗口、托盘、声音、稍后提醒
- `SettingsStore`
  - 负责阈值、白名单、用户偏好

### Windows 上如何定义“正在使用”

每 1 秒轮询一次：

1. 读取 `GetLastInputInfo`
2. 计算 `idleSeconds`
3. 判断当前会话是否锁屏、睡眠、屏幕关闭
4. 获取当前前台窗口和前台进程
5. 决定是否累计

推荐规则：

- `idleSeconds <= 45` 才算活跃
- 锁屏 / 睡眠 / 屏幕关闭时强制暂停
- 前台进程在“忽略列表”时不累计

### Windows 上如何判断全屏延迟

优先判断“当前前台窗口是否覆盖所在显示器工作区域/监视器区域”，并结合以下信息：

- 前台窗口句柄
- 窗口矩形
- 所在显示器矩形
- 前台进程名
- 是否为系统壳窗口、桌面、任务栏

建议规则：

- 前台窗口接近覆盖整个监视器时，判为全屏候选
- 若进程是浏览器、播放器、游戏，提升可信度
- 若窗口属于桌面、资源管理器、普通最大化办公窗口，不自动延迟，避免误伤

### Windows 上的提醒行为

#### 正常场景

- 达到 20 分钟
- 弹出休息窗口或托盘通知
- 用户开始休息 20 秒

#### 全屏场景

- 达到 20 分钟
- 不立刻打断
- 进入 `BreakDueDeferred`
- 当用户退出全屏后，等待 `3-5 秒` 再补提醒

这个 `3-5 秒` 很重要：

- 刚退出游戏全屏时，立刻弹窗体验仍然很差
- 给一个很短的缓冲更自然

### Windows MVP 建议使用的系统能力

- `GetLastInputInfo`
- `GetForegroundWindow`
- `GetWindowRect`
- `MonitorFromWindow` / `GetMonitorInfo`
- 会话锁定/解锁通知
- 电源状态变化通知

## Android 设计

### Android 上建议不要做“强打断全屏覆盖”

Android 上跨应用强覆盖、全局拦截、全屏识别都更敏感，体验和权限成本都比 Windows 高。

更稳的方案是：

- 用前台服务维持计时
- 用本地通知提醒休息
- 在不适合打断时挂起提醒，等用户回到普通交互场景再补

### Android 上如何定义“正在使用”

建议分两档：

#### 默认模式

- 屏幕亮
- 设备已解锁
- 有前台应用

这是低权限版本，但精度一般。

#### 精确模式

需要用户额外授权：

- `UsageStatsManager`
- `AccessibilityService`

这样可以更接近：

- 最近发生过界面交互
- 当前确实在使用手机

### Android 上的延迟提醒

Android 版建议把“延迟条件”做成更保守的规则：

- 当前应用在延迟名单中
- 屏幕关闭
- 锁屏
- 正在全屏视频/游戏场景中

一旦进入延迟状态：

- 标记 `pendingBreak = true`
- 不重置 20 分钟
- 等到用户回到普通场景后发通知

推荐补提醒时机：

- 解锁后
- 切回桌面后
- 前台应用从延迟名单切出后

## 推荐的伪代码

```text
every 1 second:
  context = readSystemContext()

  if state == Resting:
    continue

  if !context.deviceInteractive:
    state = Idle
    return

  if state == BreakDueDeferred:
    if context.shouldDeferReminder:
      return
    showRestReminder()
    state = Resting
    return

  if context.hasRecentInput && context.isCountableApp:
    state = Active
    activeElapsed += 1s
    if activeElapsed >= 20min:
      activeElapsed = 20min
      if context.shouldDeferReminder:
        state = BreakDueDeferred
      else:
        showRestReminder()
        state = Resting
  else:
    state = Idle
```

## 产品建议

### 必做设置项

- 工作时长，默认 `20 分钟`
- 休息时长，默认 `20 秒`
- 空闲阈值，默认 `45 秒`
- 全屏时延迟提醒
- 延迟名单
- 是否将被动观看计入使用

### 不建议照搬 `ProjectEye` 的地方

- 不要用“鼠标没动 + 没声音”定义离开
- 不要把全屏场景直接当成“跳过本次提醒”
- 不要用“任意白名单进程在运行”来决定跳过

## 实施顺序

### 第一阶段

先做 Windows MVP：

- `GetLastInputInfo` 活跃判定
- 前台窗口/全屏判定
- `BreakDueDeferred` 状态
- 托盘通知 + 简单休息窗口

### 第二阶段

再做 Android：

- 前台服务
- 通知提醒
- `UsageStatsManager`
- 可选 `AccessibilityService`

这是因为 Windows 对“有效输入”和“全屏窗口”的系统信号更完整，更容易先把语义做对。

## 结论

如果按你的要求，“使用”最合理的定义不是“设备开着”，也不是 `ProjectEye` 那种“鼠标动过/系统有声音”。

更合适的定义是：

- `最近一段时间内有真实输入`
- `设备处于可交互状态`
- `当前场景允许计时`

并且在提醒到点时：

- 全屏场景应 `延迟提醒`
- 不应 `跳过并重置`

这会比 `ProjectEye` 更符合你的产品目标。
