using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Export, Import, and Prompt pack (design S-10, F-06).</summary>
public sealed class ExportImportTests : BunitContext
{
    private async Task<(IRenderedComponent<ExportImport> Cut, AppData Data, FakeBrowserCapabilities Browser)> RenderAsync(
        string uri = "http://localhost/backup",
        Plan? plan = null,
        Workout[]? workouts = null,
        Change[]? extra = null,
        FakeBrowserCapabilities? browser = null,
        IDataStore? store = null)
    {
        var data = await OpenSeededAsync(store, plan ?? Plan(), workouts ?? []);
        if (extra is { Length: > 0 })
        {
            var commit = await data.CommitAsync(extra);
            Assert.IsType<CommitResult.Committed>(commit);
        }

        browser ??= new FakeBrowserCapabilities();
        Wire(this, data, browser: browser);
        Services.GetRequiredService<NavigationManager>().NavigateTo(uri);
        var cut = Render<ExportImport>();
        return (cut, data, browser);
    }

    [Fact]
    public async Task Renders_three_tabs_and_defaults_to_Export()
    {
        var (cut, _, _) = await RenderAsync();

        var tabs = cut.FindAll(".fa-backup__tabs button");
        Assert.Equal(["Export", "Import", "Prompts"], tabs.Select(t => t.TextContent.Trim()).ToArray());
        Assert.Equal("true", tabs[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", tabs[1].GetAttribute("aria-pressed"));
        Assert.Equal("false", tabs[2].GetAttribute("aria-pressed"));
    }

    [Theory]
    [InlineData("http://localhost/backup?tab=import", "Import")]
    [InlineData("http://localhost/backup?tab=prompts", "Prompts")]
    [InlineData("http://localhost/backup?tab=EXPORT", "Export")]
    public async Task Honours_tab_query_parameter(string uri, string expectedTab)
    {
        var (cut, _, _) = await RenderAsync(uri);

        var pressed = cut.Find(".fa-backup__tabs button[aria-pressed='true']");
        Assert.Equal(expectedTab, pressed.TextContent.Trim());
    }

    [Fact]
    public async Task Export_defaults_to_last_12_weeks_with_health_data_and_guides_off_and_live_summary()
    {
        var pain = new PainCheck("p1", PainStatus.Reported, [BodyArea.Knee], PainSeverity.Mild, null, null);
        var w1 = Finished("w1", Today, Set("s1", 60, 5)) with { PainCheck = pain };
        var w2 = Finished("w2", Today.AddDays(-10), Set("s2", 55, 5));
        var weighIn = new WeighIn("wi1", Today, 80.5);
        var guide = new Guide(Squat, "Legs", "Core lift", null, "Squat down", [], [], null, null, [], null, null);

        var (cut, _, _) = await RenderAsync(
            workouts: [w1, w2],
            extra: [Change.Put(weighIn), Change.Put(guide)]);

        Assert.Contains("Last 12 weeks", cut.Find(".fa-rangefield__text").TextContent);

        Assert.True(cut.Find("input[aria-label='Workouts']").HasAttribute("checked"));
        Assert.True(cut.Find("input[aria-label='Plan']").HasAttribute("checked"));
        Assert.True(cut.Find("input[aria-label='Exercises']").HasAttribute("checked"));
        Assert.False(cut.Find("input[aria-label='Guides']").HasAttribute("checked"));
        Assert.False(cut.Find("input[aria-label='Health data (Weigh-ins and Pain checks)']").HasAttribute("checked"));

        var summary = cut.Find("[data-testid='export-summary']").TextContent.Trim();
        Assert.Equal("2 Workouts, 1 Plan, 0 Pain checks, 5 Exercises · 25 Aug to 16 Nov 2026", summary);
    }

    [Fact]
    public async Task Custom_date_inputs_validate_start_not_after_end_and_apply_valid_range()
    {
        var w1 = Finished("w1", Today, Set("s1", 60, 5));
        var w2 = Finished("w2", Today.AddDays(-10), Set("s2", 55, 5));
        var (cut, _, _) = await RenderAsync(workouts: [w1, w2]);

        cut.Find("button.fa-rangefield").Click();
        cut.FindAll(".fa-rangechoices button").First(b => b.TextContent.Trim() == "Custom").Click();

        cut.Find("input[aria-label='Start date']").Change("2026-11-15");
        cut.Find("input[aria-label='End date']").Change("2026-11-10");
        cut.FindAll(".fa-rangeactions button").First(b => b.TextContent.Trim() == "Apply").Click();

        Assert.Equal("The start date is after the end date.", cut.Find(".fa-rangeerror").TextContent.Trim());

        cut.Find("input[aria-label='Start date']").Change("2026-11-10");
        cut.Find("input[aria-label='End date']").Change("2026-11-16");
        cut.FindAll(".fa-rangeactions button").First(b => b.TextContent.Trim() == "Apply").Click();

        var summary = cut.Find("[data-testid='export-summary']").TextContent.Trim();
        Assert.Equal("1 Workout, 1 Plan, 0 Pain checks, 5 Exercises · 10 Nov to 16 Nov 2026", summary);
    }

    [Fact]
    public async Task Toggling_health_data_and_format_updates_summary_and_delivery_payloads()
    {
        var pain = new PainCheck("p1", PainStatus.Reported, [BodyArea.Knee], PainSeverity.Mild, null, null);
        var w1 = Finished("w1", Today, Set("s1", 60, 5)) with { PainCheck = pain };
        var weighIn = new WeighIn("wi1", Today, 80.5);

        var (cut, _, browser) = await RenderAsync(
            workouts: [w1],
            extra: [Change.Put(weighIn)]);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Copy").Click();
        Assert.Single(browser.CopiedTexts);
        Assert.StartsWith("# Training export", browser.CopiedTexts[0]);
        Assert.Equal("Copied. Health data left out.", cut.Find("[data-testid='export-status']").TextContent.Trim());

        cut.Find("input[aria-label='Health data (Weigh-ins and Pain checks)']").Change(true);
        var summary = cut.Find("[data-testid='export-summary']").TextContent.Trim();
        Assert.Equal("1 Workout, 1 Plan, 1 Weigh-in, 1 Pain check, 5 Exercises · 25 Aug to 16 Nov 2026", summary);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "JSON").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Download file").Click();

        Assert.Single(browser.Downloads);
        var downloaded = browser.Downloads[0];
        Assert.Equal("fitness-export-2026-08-25-to-2026-11-16.json", downloaded.FileName);
        Assert.Equal("application/json", downloaded.ContentType);
        Assert.Contains("\"weighIns\"", downloaded.Content);
        Assert.Contains("\"painCheck\"", downloaded.Content);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Copy").Click();
        Assert.Equal("Copied.", cut.Find("[data-testid='export-status']").TextContent.Trim());
    }

    [Fact]
    public async Task Share_button_is_hidden_when_browser_does_not_support_share()
    {
        var unsupported = new FakeBrowserCapabilities { ShareSupported = false };
        var (cut, _, _) = await RenderAsync(browser: unsupported);
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Trim() == "Share");
    }

    [Fact]
    public async Task Share_button_appears_when_supported_and_invokes_browser_share()
    {
        var supported = new FakeBrowserCapabilities { ShareSupported = true };
        var (cut, _, _) = await RenderAsync(browser: supported);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Share").Click();
        Assert.Single(supported.Shares);
        Assert.Equal("fitness-export-2026-08-25-to-2026-11-16.md", supported.Shares[0].FileName);
    }

    [Fact]
    public async Task Export_everything_backup_downloads_full_json_and_records_timestamp()
    {
        var weighIn = new WeighIn("wi1", Today, 80.5);
        var (cut, data, browser) = await RenderAsync(extra: [Change.Put(weighIn)]);

        Assert.Equal("Never", cut.Find("[data-testid='backup-last-date']").TextContent.Trim());
        Assert.Null(data.LastBackupAt);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Export everything (Backup)").Click();

        Assert.Single(browser.Downloads);
        var downloaded = browser.Downloads[0];
        Assert.Equal("fitness-backup-2026-11-16.json", downloaded.FileName);
        Assert.Equal("application/json", downloaded.ContentType);
        Assert.Contains("\"weighIns\"", downloaded.Content);
        Assert.Contains("\"settings\"", downloaded.Content);
        Assert.Equal(Now, data.LastBackupAt);
        Assert.Equal("Mon 16 Nov", cut.Find("[data-testid='backup-last-date']").TextContent.Trim());
    }

    [Fact]
    public async Task Backup_download_failure_does_not_record_timestamp_and_shows_error()
    {
        var browser = new FakeBrowserCapabilities { DownloadResult = false };
        var (cut, data, _) = await RenderAsync(browser: browser);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Export everything (Backup)").Click();

        Assert.Null(data.LastBackupAt);
        Assert.Contains("Could not start", cut.Find("[data-testid='backup-error']").TextContent);
    }

    [Fact]
    public async Task Backup_is_disabled_when_read_only()
    {
        var env = new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere };
        var data = await AppData.OpenAsync(new InMemoryDataStore(), env);
        Wire(this, data);
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/backup");
        var cut = Render<ExportImport>();

        Assert.NotNull(cut.Find("[role='status']"));
        var backupButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Export everything (Backup)");
        Assert.True(backupButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task Llm_preset_prefills_past_7_days_markdown_workouts_and_plan_on_and_health_data_off()
    {
        var recent = Finished("w1", Today, Set("s1", 60, 5));
        var older = Finished("w2", Today.AddDays(-10), Set("s2", 55, 5));
        var (cut, _, _) = await RenderAsync(
            uri: "http://localhost/backup?preset=llm",
            workouts: [recent, older]);

        var markdownButton = cut.FindAll("button").First(b => b.TextContent.Trim() == "Markdown");
        Assert.Equal("true", markdownButton.GetAttribute("aria-pressed"));
        Assert.True(cut.Find("input[aria-label='Workouts']").HasAttribute("checked"));
        Assert.True(cut.Find("input[aria-label='Plan']").HasAttribute("checked"));
        Assert.False(cut.Find("input[aria-label='Guides']").HasAttribute("checked"));
        Assert.False(cut.Find("input[aria-label='Health data (Weigh-ins and Pain checks)']").HasAttribute("checked"));

        var summary = cut.Find("[data-testid='export-summary']").TextContent.Trim();
        Assert.Equal("1 Workout, 1 Plan, 0 Pain checks, 5 Exercises · 10 Nov to 16 Nov 2026", summary);
    }
}
