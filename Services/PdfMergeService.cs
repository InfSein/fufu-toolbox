using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using fufu_toolbox.Models;

namespace fufu_toolbox.Services;

public interface IPdfMergeService
{
    Task<IReadOnlyList<string>> ScanSupportedFilesAsync(string folderPath);

    Task MergeFilesAsync(IReadOnlyList<MergePdfFileItem> filesInOrder, string outputPath);
}

public sealed class PdfMergeService : IPdfMergeService
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff"
    };

    // 扫描目录及子目录中支持的文件并做自然排序。
    public Task<IReadOnlyList<string>> ScanSupportedFilesAsync(string folderPath)
    {
        NaturalStringComparer comparer = NaturalStringComparer.Instance;

        IReadOnlyList<string> files = Directory
            .EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .OrderBy(path => Path.GetFileName(path), comparer)
            .ThenBy(path => path, comparer)
            .ToList();

        return Task.FromResult(files);
    }

    // 按当前顺序把 PDF 和图片合并为一个新的 PDF。
    public Task MergeFilesAsync(IReadOnlyList<MergePdfFileItem> filesInOrder, string outputPath)
    {
        using PdfDocument outputDocument = new();

        foreach (MergePdfFileItem item in filesInOrder)
        {
            string extension = Path.GetExtension(item.FilePath);

            if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                AppendPdf(outputDocument, item.FilePath);
                continue;
            }

            AppendImage(outputDocument, item.FilePath);
        }

        outputDocument.Save(outputPath);
        return Task.CompletedTask;
    }

    // 追加一个 PDF 文件的全部页面。
    private static void AppendPdf(PdfDocument outputDocument, string filePath)
    {
        using PdfDocument inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
        for (int i = 0; i < inputDocument.PageCount; i++)
        {
            outputDocument.AddPage(inputDocument.Pages[i]);
        }
    }

    // 追加一张图片并按原尺寸放入新页面。
    private static void AppendImage(PdfDocument outputDocument, string filePath)
    {
        using XImage image = XImage.FromFile(filePath);

        PdfPage page = outputDocument.AddPage();
        page.Width = PdfSharp.Drawing.XUnit.FromPoint(image.PointWidth);
        page.Height = PdfSharp.Drawing.XUnit.FromPoint(image.PointHeight);

        using XGraphics graphics = XGraphics.FromPdfPage(page);
        graphics.DrawImage(image, 0, 0, page.Width.Point, page.Height.Point);
    }
}
