# ARCHITECTURE

## 模块职责
- `App.xaml.cs`：应用启动入口，负责依赖注入容器和主窗口创建。
- `MainWindow.xaml`：主框架，包含导航菜单（工具主页、合并TXT、合并PDF、FFXIV物品翻译、设置）。
- `MainWindow.xaml.cs`：处理导航与主题应用。
- `Services/NavigationService.cs`：统一管理页面键与页面跳转。
- `Services/ThemeService.cs`：主题状态管理与本地持久化。
- `Services/TxtMergeService.cs`：扫描 txt 文件并按规则拼接文本。
- `Services/PdfMergeService.cs`：扫描 pdf/图片文件并按顺序合并输出 PDF。
- `Services/XivItemTranslationService.cs`：调用 XIVAPI 与 Waking Sands 接口翻译 FFXIV 物品名。
- `Models/ToolCardItem.cs`：首页工具卡片模型。
- `Models/ThemeOptionItem.cs`：主题选项模型。
- `Models/MergeTxtFileItem.cs`：合并TXT文件项模型（选择、别名、顺序）。
- `Models/MergePdfFileItem.cs`：合并PDF文件项模型（选择、文件类型、顺序）。
- `Models/XivTranslateLineResult.cs`：单行物品翻译结果模型（成功/失败/原因）。
- `Models/XivTranslateBatchResult.cs`：批量翻译结果模型（输出文本、成功失败数量、失败摘要）。
- `ViewModels/MainViewModel.cs`：主窗口状态。
- `ViewModels/HomeViewModel.cs`：首页工具列表数据。
- `ViewModels/SettingsViewModel.cs`：设置页主题选择状态。
- `ViewModels/MergeTxtViewModel.cs`：合并TXT页面状态、排序与合并流程。
- `ViewModels/MergePdfViewModel.cs`：合并PDF页面状态、排序与合并流程。
- `ViewModels/XivItemTranslatorViewModel.cs`：FFXIV物品翻译页面状态、进度与失败汇总。
- `Pages/HomePage.xaml`：工具总览页。
- `Pages/SettingsPage.xaml`：可折叠分组设置页。
- `Pages/MergeTxtPage.xaml`：合并TXT工具页面。
- `Pages/MergePdfPage.xaml`：合并PDF工具页面。
- `Pages/XivItemTranslatorPage.xaml`：FFXIV物品翻译工具页面。

## 调用关系
- `App` 注册并创建 `MainWindow`。
- `MainWindow` 通过 `INavigationService` 导航到各页面。
- `SettingsPage` 通过 `SettingsViewModel` 调用 `ThemeService`。
- `MergeTxtPage` 通过 `MergeTxtViewModel` 调用 `TxtMergeService`。
- `MergePdfPage` 通过 `MergePdfViewModel` 调用 `PdfMergeService`。
- `XivItemTranslatorPage` 通过 `XivItemTranslatorViewModel` 调用 `XivItemTranslationService`。
- `MergeTxtViewModel` 维护文件顺序、选择和别名，生成文本合并输入。
- `MergePdfViewModel` 维护文件顺序和选择状态，生成 PDF 合并输入。
- `PdfMergeService` 负责将 PDF 直接追加、将图片转为页面并输出到同一个 PDF。
- `XivItemTranslationService` 负责先按名称检索物品ID，再读取目标语言名称返回给界面。

## 关键设计决定
- 删除“关于”页面，聚焦工具入口和实际功能。
- 合并工具统一采用“页面负责交互、ViewModel负责状态、Service负责读写逻辑”的分层。
- 合并输出使用系统保存对话框，让用户自由选位置和文件名。
- 合并PDF支持“PDF + 图片混合输入”，并以列表从上到下的顺序输出，保持用户可控。
- FFXIV物品翻译沿用旧工具“按行翻译 + 失败保留原文”的行为，减少迁移后的使用差异。
- 主项目显式排除 `fufu-tools` 目录源码，避免 WinForms 子项目参与 WinUI 主项目编译。
