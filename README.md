# Fufu Toolbox

## 项目简介
Fufu Toolbox 是一个可持续扩展的桌面小工具集合。现在已接入两个工具：合并TXT、合并PDF。

## 技术架构
- 桌面框架：WinUI 3（Windows App SDK）
- 结构模式：MVVM（Model + ViewModel + View）
- 依赖管理：.NET 依赖注入（Host + ServiceCollection）
- 导航方式：`NavigationView` + 统一导航服务
- 主题管理：`ThemeService` 负责主题切换与本地持久化
- 文本合并：`TxtMergeService` 负责扫描与合并文本
- PDF合并：`PdfMergeService` 负责扫描 pdf/图片并合并输出

## 本地运行
1. 进入项目目录：`cd fufu-toolbox`
2. 还原依赖：`dotnet restore`
3. 编译：`dotnet build -p:Platform=x64`
4. 运行：用 Visual Studio 打开 `fufu-toolbox.csproj` 调试运行

## 持续集成与发布
- 项目配置了 GitHub Action (`.github/workflows/build-release.yml`)。
- 每次向 `master` 分支推送代码，都会自动触发构建并生成、更新对应的 GitHub Release。
- 发布的安装包格式为 `.msix` 以及包含所有依赖的 `.zip` 压缩包。
- 如果推送时 `Package.appxmanifest` 中的版本号未变，Action 会自动覆盖更新当前的 Release 附件。

## 测试方法和常用命令
- 本地构建校验：`dotnet build -p:Platform=x64`
- 模拟发布构建：`dotnet publish -c Release -p:Platform=x64`

## 搜索记录
- 时间：2026-04-04
- 来源：skills.sh（关键词：WinUI / MVVM）
  - 结论：未检索到可直接复用的 WinUI 3 工具箱模板，主要是通用技能索引。
- 来源：GitHub（关键词：WinUI 3 NavigationView MVVM sample）
  - 结论：社区有可参考示例（如 `manusoft/WinUI-Samples`），可用于页面组织和导航结构对照。

## 已完成功能
- 初始化 WinUI 3 项目：`fufu-toolbox`
- 建立主布局：标题栏 + 左侧导航 + 页面承载区
- 建立 MVVM 基础结构（Models/ViewModels/Services）
- 设置页支持主题切换：跟随系统、浅色、深色（重启后保持）
- 接入“合并TXT”：
  - 选择目录并扫描全部 txt 文件
  - 勾选多个文件参与合并
  - 自由调整合并顺序
  - 为每个文件设置别名
  - 合并后弹窗选择输出位置和文件名
- 接入“合并PDF”：
  - 选择目录并扫描全部 pdf 和图片文件
  - 勾选多个文件参与合并
  - 自由调整合并顺序（按列表从上到下输出）
  - 合并后弹窗选择输出位置和文件名
- 移除“关于”页面
- 提供工具接入规范文档 `TOOL_BASE.md`

## 待办事项
- [x] 接入第一个工具“合并TXT”
- [x] 接入第二个工具“合并PDF”
- [x] 配置 GitHub Action 自动构建与 Release 发布
- [ ] 按 `TOOL_BASE.md` 逐步接入更多工具模块
- [ ] 为关键工具补充自动化测试
