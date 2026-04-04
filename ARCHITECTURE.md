# ARCHITECTURE

## 模块职责
- `App.xaml.cs`：应用启动入口，负责依赖注入容器和主窗口创建。
- `MainWindow.xaml`：主框架，包含导航菜单（工具主页、合并TXT、设置）。
- `MainWindow.xaml.cs`：处理导航与主题应用。
- `Services/NavigationService.cs`：统一管理页面键与页面跳转。
- `Services/ThemeService.cs`：主题状态管理与本地持久化。
- `Services/TxtMergeService.cs`：扫描 txt 文件并按规则拼接文本。
- `Models/ToolCardItem.cs`：首页工具卡片模型。
- `Models/ThemeOptionItem.cs`：主题选项模型。
- `Models/MergeTxtFileItem.cs`：合并TXT文件项模型（选择、别名、顺序）。
- `ViewModels/MainViewModel.cs`：主窗口状态。
- `ViewModels/HomeViewModel.cs`：首页工具列表数据。
- `ViewModels/SettingsViewModel.cs`：设置页主题选择状态。
- `ViewModels/MergeTxtViewModel.cs`：合并TXT页面状态、排序与合并流程。
- `Pages/HomePage.xaml`：工具总览页。
- `Pages/SettingsPage.xaml`：可折叠分组设置页。
- `Pages/MergeTxtPage.xaml`：合并TXT工具页面。

## 调用关系
- `App` 注册并创建 `MainWindow`。
- `MainWindow` 通过 `INavigationService` 导航到各页面。
- `SettingsPage` 通过 `SettingsViewModel` 调用 `ThemeService`。
- `MergeTxtPage` 通过 `MergeTxtViewModel` 调用 `TxtMergeService`。
- `MergeTxtViewModel` 维护文件顺序、选择和别名，生成合并输入。
- `TxtMergeService` 按“标题 + 空行 + 内容”的格式拼接，并在节间插入 3 个换行。

## 关键设计决定
- 删除“关于”页面，聚焦工具入口和实际功能。
- 合并TXT采用“页面负责交互、ViewModel负责状态、Service负责读写逻辑”的分层。
- 合并输出使用系统保存对话框，让用户自由选位置和文件名。
