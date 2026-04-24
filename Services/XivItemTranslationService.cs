using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using fufu_toolbox.Models;

namespace fufu_toolbox.Services;

public interface IXivItemTranslationService
{
    Task<XivTranslateLineResult> TranslateItemNameAsync(string itemName, string inputLanguageCode, string outputLanguageCode, CancellationToken cancellationToken = default);
}

public sealed class XivItemTranslationService : IXivItemTranslationService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    // 按指定输入语言查询物品，再返回目标语言名称。
    public async Task<XivTranslateLineResult> TranslateItemNameAsync(string itemName, string inputLanguageCode, string outputLanguageCode, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return new XivTranslateLineResult
                {
                    Success = false,
                    ErrorMessage = "物品名不能为空"
                };
            }

            int? itemId = await GetItemIdByNameAsync(itemName, inputLanguageCode, cancellationToken);
            if (!itemId.HasValue)
            {
                return new XivTranslateLineResult
                {
                    Success = false,
                    ErrorMessage = "未获取到物品信息"
                };
            }

            return await GetItemNameByIdAsync(itemId.Value, outputLanguageCode, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return new XivTranslateLineResult
            {
                Success = false,
                ErrorMessage = "请求超时"
            };
        }
        catch (Exception ex)
        {
            return new XivTranslateLineResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    // 根据物品名在 XIVAPI 查询对应 ID。
    private async Task<int?> GetItemIdByNameAsync(string itemName, string languageCode, CancellationToken cancellationToken)
    {
        if (!languageCode.Equals("ja", StringComparison.OrdinalIgnoreCase) &&
            !languageCode.Equals("en", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string searchQuery = Uri.EscapeDataString($"Name@{languageCode}=\"{itemName}\"");
        string searchUrl = $"https://v2.xivapi.com/api/search?sheets=Item&fields=Name&query={searchQuery}";

        using HttpResponseMessage response = await _httpClient.GetAsync(searchUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        SearchItemResponse? result = JsonSerializer.Deserialize<SearchItemResponse>(json);
        if (result?.results is null || result.results.Length == 0)
        {
            return null;
        }

        return result.results[0].row_id;
    }

    // 根据物品 ID 从 Waking Sands 读取多语言名称。
    private async Task<XivTranslateLineResult> GetItemNameByIdAsync(int itemId, string outputLanguageCode, CancellationToken cancellationToken)
    {
        string itemUrl = $"https://cafemaker.wakingsands.com/item/{itemId}?columns=ID,Name,Name_en,Name_ja";
        using HttpResponseMessage response = await _httpClient.GetAsync(itemUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new XivTranslateLineResult
            {
                Success = false,
                ErrorMessage = $"请求失败({(int)response.StatusCode})"
            };
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        ItemDataResponse? item = JsonSerializer.Deserialize<ItemDataResponse>(json);
        if (item is null)
        {
            return new XivTranslateLineResult
            {
                Success = false,
                ErrorMessage = "数据解析失败"
            };
        }

        string translatedName = outputLanguageCode switch
        {
            "zh" => item.Name ?? string.Empty,
            "ja" => item.Name_ja ?? string.Empty,
            "en" => item.Name_en ?? string.Empty,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(translatedName))
        {
            return new XivTranslateLineResult
            {
                Success = false,
                ErrorMessage = "暂不支持此语言"
            };
        }

        return new XivTranslateLineResult
        {
            Success = true,
            TranslatedName = translatedName
        };
    }

    private sealed class SearchItemResponse
    {
        public SearchResultItem[]? results { get; init; }
    }

    private sealed class SearchResultItem
    {
        public int row_id { get; init; }
    }

    private sealed class ItemDataResponse
    {
        public string? Name { get; init; }

        public string? Name_en { get; init; }

        public string? Name_ja { get; init; }
    }
}
