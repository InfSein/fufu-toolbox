# TOOL_BASE

## 目标
这份文档定义“以后怎么往 Fufu Toolbox 里加新工具”，让新增工具保持统一、易维护。

## 新增一个工具的标准步骤
1. 在 `Models/` 里定义工具需要的数据结构（如果已有可复用就不新建）。
2. 在 `ViewModels/` 里新增对应 `XXXViewModel`，只放页面状态和业务动作。
3. 在 `Pages/` 里新增 `XXXPage.xaml` 和 `XXXPage.xaml.cs`，页面只负责展示和交互绑定。
4. 在 `Services/NavigationService.cs` 的页面映射中加入新的导航键与页面类型。
5. 在 `MainWindow.xaml` 的左侧导航里加入入口（菜单或底部菜单）。
6. 在首页预留区（`HomeViewModel` 的 `PlannedTools`）添加卡片说明。
7. 在 `App.xaml.cs` 的依赖注入里注册新 ViewModel（必要时注册服务）。

## 命名规范
- 页面：`XxxPage`
- 视图模型：`XxxViewModel`
- 模型：`XxxModel` 或明确业务名
- 导航键：放在 `NavigationKeys` 里统一管理

## 代码边界
- View（页面）：不写业务逻辑，只做展示和事件转发。
- ViewModel：处理页面状态、命令和流程。
- Model：只描述数据。
- Service：放可复用能力（导航、文件、网络等）。

## 最低质量要求
- 能正常编译通过。
- 页面有清晰的空状态或默认提示。
- 出错时给出明确提示，不允许静默失败。
- 新增功能后同步更新 `CONTEXT.md`；涉及模块变更时更新 `README.md` 和 `ARCHITECTURE.md`。
