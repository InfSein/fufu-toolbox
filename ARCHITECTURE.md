# ARCHITECTURE

## 模块职责
- `App.xaml.cs`：应用启动入口，负责依赖注入容器和主窗口创建。
- `MainWindow.xaml`：主框架，包含导航菜单（工具主页、合并TXT、合并PDF、FFXIV物品翻译、端口管理、设置）。
- `MainWindow.xaml.cs`：处理导航与主题应用。
- `Services/NavigationService.cs`：统一管理页面键与页面跳转。
- `Services/ThemeService.cs`：主题状态管理与本地持久化。
- `Services/TxtMergeService.cs`：扫描 txt 文件并按规则拼接文本。
- `Services/PdfMergeService.cs`：扫描 pdf/图片文件并按顺序合并输出 PDF。
- `Services/XivItemTranslationService.cs`：调用 XIVAPI 与 Waking Sands 接口翻译 FFXIV 物品名。
- `Services/PortManagementService.cs`：读取本机端口占用、进程信息并执行中止流程。
- `Models/ToolCardItem.cs`：首页工具卡片模型。
- `Models/ThemeOptionItem.cs`：主题选项模型。
- `Models/MergeTxtFileItem.cs`：合并TXT文件项模型（选择、别名、顺序）。
- `Models/MergePdfFileItem.cs`：合并PDF文件项模型（选择、文件类型、顺序）。
- `Models/XivTranslateLineResult.cs`：单行物品翻译结果模型（成功/失败/原因）。
- `Models/XivTranslateBatchResult.cs`：批量翻译结果模型（输出文本、成功失败数量、失败摘要）。
- `Models/PortOccupancyItem.cs`：端口占用行模型（协议、端口、PID、程序名、图标与路径）。
- `ViewModels/MainViewModel.cs`：主窗口状态。
- `ViewModels/HomeViewModel.cs`：首页工具列表数据。
- `ViewModels/SettingsViewModel.cs`：设置页主题选择状态。
- `ViewModels/MergeTxtViewModel.cs`：合并TXT页面状态、排序与合并流程。
- `ViewModels/MergePdfViewModel.cs`：合并PDF页面状态、排序与合并流程。
- `ViewModels/XivItemTranslatorViewModel.cs`：FFXIV物品翻译页面状态、进度与失败汇总。
- `ViewModels/PortManagementViewModel.cs`：端口管理页面状态、筛选词与刷新/中止流程。
- `Pages/HomePage.xaml`：工具总览页。
- `Pages/SettingsPage.xaml`：可折叠分组设置页。
- `Pages/MergeTxtPage.xaml`：合并TXT工具页面。
- `Pages/MergePdfPage.xaml`：合并PDF工具页面。
- `Pages/XivItemTranslatorPage.xaml`：FFXIV物品翻译工具页面。
- `Pages/PortManagementPage.xaml`：端口管理工具页面。

## 调用关系
- `App` 注册并创建 `MainWindow`。
- `MainWindow` 通过 `INavigationService` 导航到各页面。
- `SettingsPage` 通过 `SettingsViewModel` 调用 `ThemeService`。
- `MergeTxtPage` 通过 `MergeTxtViewModel` 调用 `TxtMergeService`。
- `MergePdfPage` 通过 `MergePdfViewModel` 调用 `PdfMergeService`。
- `XivItemTranslatorPage` 通过 `XivItemTranslatorViewModel` 调用 `XivItemTranslationService`。
- `PortManagementPage` 通过 `PortManagementViewModel` 调用 `PortManagementService`。
- `MergeTxtViewModel` 维护文件顺序、选择和别名，生成文本合并输入。
- `MergePdfViewModel` 维护文件顺序和选择状态，生成 PDF 合并输入。
- `PortManagementService` 调用 `netstat` 读取端口占用，再映射系统进程信息与图标。
- `PortManagementViewModel` 负责把筛选词应用到端口数据，并触发中止进程与刷新。

## 关键设计决定
- 删除“关于”页面，聚焦工具入口和实际功能。
- 合并工具统一采用“页面负责交互、ViewModel负责状态、Service负责读写逻辑”的分层。
- 合并输出使用系统保存对话框，让用户自由选位置和文件名。
- 合并PDF支持“PDF + 图片混合输入”，并以列表从上到下的顺序输出，保持用户可控。
- FFXIV物品翻译沿用旧工具“按行翻译 + 失败保留原文”的行为，减少迁移后的使用差异。
- 主项目显式排除 `fufu-tools` 目录源码，避免 WinForms 子项目参与 WinUI 主项目编译。
- 端口管理中止进程前必须确认；权限不足时触发管理员提权执行中止，降低误操作和权限失败影响。
