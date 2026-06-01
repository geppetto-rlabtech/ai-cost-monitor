using System.Net.Http.Headers;
using System.Text.Json;
using AiCostMonitor.Core.Entities;
using AiCostMonitor.Core.Interfaces;

namespace AiCostMonitor.Api.Adapters;

public class AnthropicAdapter(HttpClient http, ILogger<AnthropicAdapter> logger) : IProviderAdapter
{
    public string ProviderName => "anthropic";

    private const string BaseUrl = "https://api.anthropic.com/v1";
    private const string ApiVersion = "2023-06-01";

    public async Task<bool> TestKeyAsync(string apiKey, CancellationToken ct = default)
    {
        try
        {
            var req = BuildRequest(HttpMethod.Get,
                $"{BaseUrl}/organizations/usage?start_time={DateTimeOffset.UtcNow.AddDays(-1):O}&limit=1",
                apiKey);
            var resp = await http.SendAsync(req, ct);
            return resp.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Anthropic key test failed");
            return false;
        }
    }

    public async Task<IEnumerable<UsageRecord>> FetchUsageAsync(
        string apiKey, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var records = new List<UsageRecord>();
        string? cursor = null;

        do
        {
            var url = $"{BaseUrl}/organizations/usage" +
                      $"?start_time={from:yyyy-MM-ddTHH:mm:ssZ}" +
                      $"&end_time={to:yyyy-MM-ddTHH:mm:ssZ}" +
                      $"&time_granularity=day&limit=100" +
                      (cursor is not null ? $"&page={cursor}" : "");

            var req = BuildRequest(HttpMethod.Get, url, apiKey);
            var resp = await http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            foreach (var item in root.GetProperty("data").EnumerateArray())
            {
                records.Add(new UsageRecord
                {
                    Id = Guid.NewGuid(),
                    Provider = ProviderName,
                    Model = item.GetProperty("model").GetString() ?? "unknown",
                    PeriodStart = DateTimeOffset.Parse(item.GetProperty("period_start").GetString()!),
                    PeriodEnd = DateTimeOffset.Parse(item.GetProperty("period_end").GetString()!),
                    InputTokens = item.TryGetProperty("input_tokens", out var it) ? it.GetInt64() : 0,
                    OutputTokens = item.TryGetProperty("output_tokens", out var ot) ? ot.GetInt64() : 0,
                    CacheReadTokens = item.TryGetProperty("cache_read_input_tokens", out var cr) ? cr.GetInt64() : 0,
                    CacheCreationTokens = item.TryGetProperty("cache_creation_input_tokens", out var cc) ? cc.GetInt64() : 0,
                    CostUsd = item.TryGetProperty("cost_usd", out var cost) ? (decimal)cost.GetDouble() : 0m,
                    RawJson = item.GetRawText(),
                    SyncedAt = DateTimeOffset.UtcNow
                });
            }

            var hasMore = root.TryGetProperty("has_more", out var hm) && hm.GetBoolean();
            cursor = hasMore && root.TryGetProperty("last_id", out var li) && li.ValueKind != JsonValueKind.Null
                ? li.GetString()
                : null;

        } while (cursor is not null);

        return records;
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string apiKey)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", ApiVersion);
        return req;
    }
}