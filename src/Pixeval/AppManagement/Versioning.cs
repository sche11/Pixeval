using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Pixeval.AppManagement;

public class Versioning
{
    private AppVersion? _newestVersion;

    public AppVersion CurrentVersion { get; } = new(3, 0, 0, IterationStage.Preview);

    public AppVersion? NewestVersion
    {
        get => _newestVersion;
        private set
        {
            _newestVersion = value;
            UpdateState = CurrentVersion.CompareUpdateState(value);
            UpdateAvailable = UpdateState is not UpdateState.UpToDate and not UpdateState.Insider and not UpdateState.Unknown;
        }
    }

    public UpdateState UpdateState { get; private set; }

    public bool UpdateAvailable { get; private set; }

    public async Task<AppReleaseModel?> CheckForUpdateAsync(HttpClient client)
    {
        try
        {
            if (!client.DefaultRequestHeaders.Contains("User-Agent"))
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36 Edg/121.0.0.0");
            var stringAsync = await client.GetStringAsync("https://api.github.com/repos/Pixeval/Pixeval/releases");
            var gitHubReleases = JsonSerializer.Deserialize<GitHubRelease[]>(stringAsync);
            if (gitHubReleases is not [var gitHubRelease, ..])
                return null;
            if (AppVersion.TryParse(gitHubRelease.TagName, out var appVersion))
            {
                NewestVersion = appVersion;
                App.AppViewModel.AppSetting.LastCheckedUpdate = DateTimeOffset.Now;
                return new AppReleaseModel(appVersion, gitHubRelease.Notes, gitHubRelease.Assets[0].BrowserDownloadUrl);
            }
        }
        catch (Exception)
        {
            // ignored
        }
        NewestVersion = null;
        return null;
    }
}

public record AppReleaseModel(
    AppVersion Version,
    string ReleaseNote,
    string ReleaseUri);

file class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public required string TagName { get; set; }

    [JsonPropertyName("assets")]
    public required Assets[] Assets { get; set; }

    [JsonPropertyName("body")]
    public required string Notes { get; set; }
}

file class Assets
{
    [JsonPropertyName("browser_download_url")]
    public required string BrowserDownloadUrl { get; set; }
}
