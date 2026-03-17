# Rest UI Task: Balanced Default

Branch: `rest-ui-balanced-default`
Worktree: `C:\Users\gg\project\20min2s-rest-ui-balanced`

## Goal
产出一个适合作为项目默认休息页的新 UI。它应该比现在的纯白全屏更柔和、更现代，但仍然保持明确的“该休息了”信号，不要过于隐身。

## Recommended Direction
- 使用柔和的半透明遮罩或渐变背景，替代当前的纯白底。
- 保留全屏休息页的存在感，但把信息层级收束到中央卡片区域。
- 页面应该兼顾白天与夜间观感，不刺眼，但也不能弱到用户忽略。
- 这是三套方案里最适合作为默认值的版本，需兼顾审美、可读性、实现成本和稳定性。

## Constraints
- 必须兼容现有提醒流转、休息倒计时、跳过按钮、预提醒、超时处理和全屏延迟提醒。
- 不要依赖过重或高风险的系统特效；如果你尝试模糊/亚克力效果，需要准备一个稳定的退化方案。
- 设计上不要走“AI 风格大渐变海报”，要更像一款长期常驻的桌面工具。
- 尽量复用现有 UI 生成机制，不要无必要地重写整个提示窗口基础设施。

## Primary Files To Inspect
- `windows/20min20s/ViewModels/TipViewModel.cs`
- `windows/20min20s/Core/Service/ThemeService.cs`
- `windows/20min20s/Views/TipWindow.xaml`
- `windows/20min20s/Views/TipViewDesignWindow.xaml`
- `windows/20min20s/Core/Models/Options/StyleModel.cs`
- `windows/20min20s/Resources/Language/zh.xaml`
- `windows/20min20s/Resources/Language/en.xaml`

## Likely Implementation Paths
- 从 `ThemeService.GetCreateDefaultTipWindowUI(...)` 入手，重新定义默认遮罩、卡片、文案区、按钮区和倒计时区的视觉结构。
- 在 `TipViewModel.CreateUI()` 里确认容器背景透明度、元素顺序和数据绑定是否足够支撑新布局。
- 如需增加新的默认插图或视觉素材，放到现有主题资源目录中，并保证 Dark/Blue 等主题下都能退化得体。
- 若发现当前 UI JSON 机制限制太大，可以做局部增强，但应避免破坏用户已有自定义布局能力。

## Deliverables
- 完成一版可作为默认方案的平衡型休息页 UI。
- 保证新方案在没有自定义 UI 文件时能正确生成。
- 如需新增配置或文案，补齐中英文资源。
- 在本分支提交清晰的实现说明到 commit message。

## Acceptance Criteria
- 视觉上明显优于当前纯白全屏。
- 提醒感依然明确，不会因为过度低调而失去存在感。
- 倒计时、主文案、按钮层级清晰，可快速理解。
- 深浅主题下都不突兀。
- 不引入明显卡顿、闪屏或多屏布局错位。

## Suggested Validation
- 运行 `Release` 版本检查普通提醒场景。
- 测试不同主题下首次生成默认休息页。
- 测试点击“休息”和“暂时不”的流转。
- 测试长时间驻留时的超时处理。
- 测试全屏延迟提醒恢复后的显示表现。
