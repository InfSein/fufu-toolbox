using System.Collections.Generic;
using System.Collections.ObjectModel;
using fufu_toolbox.Models;

namespace fufu_toolbox.ViewModels;

public sealed class HomeViewModel
{
    public IReadOnlyList<string> Categories { get; } =
    [
        "文本处理",
        "文件与路径",
        "网络与接口",
        "开发辅助",
        "系统效率"
    ];

    public ObservableCollection<ToolCardItem> PlannedTools { get; } =
    [
        new ToolCardItem
        {
            Name = "合并TXT",
            Description = "扫描目录、调整顺序并合并多个 txt 文件。",
            Status = "已接入",
            Category = "文本处理"
        },
        new ToolCardItem
        {
            Name = "合并PDF",
            Description = "检索目录内的 PDF 和图片，自选并排序后合并输出。",
            Status = "已接入",
            Category = "文件与路径"
        },
        new ToolCardItem
        {
            Name = "FFXIV物品翻译",
            Description = "批量翻译最终幻想XIV物品名，支持保留行前缀。",
            Status = "已接入",
            Category = "网络与接口"
        },
        new ToolCardItem
        {
            Name = "端口管理",
            Description = "查看端口占用，按端口或程序筛选，并可中止进程释放端口。",
            Status = "已接入",
            Category = "系统效率"
        },
        new ToolCardItem
        {
            Name = "批量重命名",
            Description = "按规则一次性改很多文件名。",
            Status = "待实现",
            Category = "文件与路径"
        },
        new ToolCardItem
        {
            Name = "JSON 格式化",
            Description = "粘贴内容后快速排版和校验。",
            Status = "待实现",
            Category = "文本处理"
        },
        new ToolCardItem
        {
            Name = "接口连通检查",
            Description = "输入地址后快速判断能否访问。",
            Status = "待实现",
            Category = "网络与接口"
        }
    ];
}
