using Bunit;
using Core.Exchange;
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

    [Fact]
    public async Task Import_tab_shows_paste_box_with_Check_first_file_picker_second_backup_note_and_honours_kind_query()
    {
        var (cut, _, _) = await RenderAsync("http://localhost/backup?tab=import&kind=plan");

        var textarea = cut.Find("textarea[aria-label='Paste JSON']");
        var fileInput = cut.Find("input[type='file']");
        Assert.True(cut.Markup.IndexOf("Paste JSON", StringComparison.Ordinal) < cut.Markup.IndexOf("type=\"file\"", StringComparison.Ordinal));
        Assert.Contains(cut.FindAll("button"), b => b.TextContent.Trim() == "Check");
        Assert.Contains("automatic Backup", cut.Markup, StringComparison.OrdinalIgnoreCase);

        var planButton = cut.FindAll("[role='group'][aria-label='Import kind'] button").First(b => b.TextContent.Trim() == "Plan");
        Assert.Equal("true", planButton.GetAttribute("aria-pressed"));
        Assert.NotNull(textarea);
        Assert.NotNull(fileInput);
    }

    [Fact]
    public async Task Import_strips_markdown_fences_shows_notice_and_renders_preview_summary_and_diff_rows()
    {
        var newPlan = new Plan(
            "autumn",
            "Autumn block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Upper", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise("new-row", 3, 8, null, null)])]);
        var newEx = new Exercise("new-row", "Cable row", LogKind.Load, null, false);
        var docJson = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [newEx],
                null,
                Plans: [newPlan]));

        var wrapped = $"Here is your plan:\n```json\n{docJson}\n```\nEnjoy!";
        var (cut, _, _) = await RenderAsync("http://localhost/backup?tab=import");

        cut.Find("textarea[aria-label='Paste JSON']").Input(wrapped);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        Assert.Contains("Ignored text around the JSON.", cut.Find("[data-testid='import-notices']").TextContent);
        var preview = cut.Find("[data-testid='import-preview']");
        Assert.Contains("Autumn block", preview.TextContent);
        Assert.NotEmpty(preview.QuerySelectorAll(".fa-import-preview__row--add"));
    }

    [Fact]
    public async Task Import_invalid_or_newer_version_shows_field_errors_with_nothing_applied()
    {
        var (cut, data, _) = await RenderAsync("http://localhost/backup?tab=import");
        var initialPlans = data.Snapshot.Plans.Count;

        cut.Find("textarea[aria-label='Paste JSON']").Input("""{"formatVersion": 99}""");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        var errorBanner = cut.Find("[data-testid='import-errors'][role='alert']");
        Assert.Contains("formatVersion", errorBanner.TextContent);
        Assert.Empty(cut.FindAll("[data-testid='import-preview']"));

        cut.Find("textarea[aria-label='Paste JSON']").Input("""{"formatVersion": 2, "exercises": [{"id": "", "name": "Bad"}]}""");
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        errorBanner = cut.Find("[data-testid='import-errors'][role='alert']");
        Assert.Contains("exercises[0]", errorBanner.TextContent);
        Assert.Equal(initialPlans, data.Snapshot.Plans.Count);
    }

    [Fact]
    public async Task Import_file_picker_reads_uploaded_json_and_renders_preview()
    {
        var newPlan = new Plan(
            "spring",
            "Spring block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Upper", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)])]);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [PageFixture.Exercises[0]],
                null,
                Plans: [newPlan]));

        var (cut, _, _) = await RenderAsync("http://localhost/backup?tab=import");

        var inputFile = cut.FindComponent<Microsoft.AspNetCore.Components.Forms.InputFile>();
        inputFile.UploadFiles(Bunit.InputFileContent.CreateFromText(json, "plan.json", contentType: "application/json"));

        cut.WaitForAssertion(() => Assert.Contains("Spring block", cut.Find("[data-testid='import-preview']").TextContent));
    }

    [Fact]
    public async Task Import_kind_switching_reruns_preview_and_shows_warnings_and_ignored_sections()
    {
        var existingWorkout = Finished("w-existing", Today, Set("s1", 60, 5));
        var incomingWorkout = Finished("w-incoming", Today, Set("s2", 65, 5));
        var extraWeighIn = new WeighIn("wi-local", Today, 80.0);

        var backupDoc = new ExchangeDocument(
            Core.Serialisation.ExchangeFormat.CurrentVersion,
            Now.AddDays(-1),
            "test",
            PageFixture.Exercises,
            Settings.Default,
            WeighIns: [],
            Breaks: [],
            Badges: [],
            Guides: [],
            Plans: [Plan()],
            Workouts: [incomingWorkout],
            WeekSchedules: []);
        var json = Core.Serialisation.ExchangeFormat.Write(backupDoc);

        var (cut, _, _) = await RenderAsync(
            "http://localhost/backup?tab=import",
            workouts: [existingWorkout],
            extra: [Change.Put(extraWeighIn)]);

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        // Detected as Backup (Restore) -> shows data-loss warning
        var preview = cut.Find("[data-testid='import-preview']");
        Assert.Contains("Replaces all your data with this Backup.", preview.TextContent);
        Assert.Contains("Loses", preview.TextContent);
        Assert.NotEmpty(preview.QuerySelectorAll(".fa-import-preview__row--warning"));

        // Switch to Merge -> shows duplicate-date same-session Workout warning
        cut.FindAll("[role='group'][aria-label='Import kind'] button").First(b => b.TextContent.Trim() == "Merge").Click();
        preview = cut.Find("[data-testid='import-preview']");
        Assert.Contains("Both will be kept.", preview.TextContent);

        // Switch to Plan -> shows ignored sections for Workouts
        cut.FindAll("[role='group'][aria-label='Import kind'] button").First(b => b.TextContent.Trim() == "Plan").Click();
        preview = cut.Find("[data-testid='import-preview']");
        Assert.Contains("Ignored:", preview.TextContent);
        Assert.NotEmpty(preview.QuerySelectorAll(".fa-import-preview__row--ignore"));

        // Switch to Guides -> runs cleanly
        cut.FindAll("[role='group'][aria-label='Import kind'] button").First(b => b.TextContent.Trim() == "Guides").Click();
        Assert.NotNull(cut.Find("[data-testid='import-preview']"));
    }

    [Fact]
    public async Task Import_near_match_exercise_blocks_Apply_until_decided_and_updates_preview_live()
    {
        var nearEx = new Exercise("goblet-squats", "Goblet squats", LogKind.Load, null, false);
        var newPlan = new Plan(
            "summer",
            "Summer block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Legs", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise("goblet-squats", 3, 8, null, null)])]);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [nearEx],
                null,
                Plans: [newPlan]));

        var (cut, data, _) = await RenderAsync("http://localhost/backup?tab=import");
        var initialExerciseCount = data.Snapshot.Exercises.Count;

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        var matchRow = cut.Find("[data-testid='exercise-match-row']");
        Assert.Contains("fa-import-preview__row--decision", matchRow.ClassName);
        Assert.Contains("Goblet squat", matchRow.TextContent);
        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").HasAttribute("disabled"));

        matchRow.QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Yes").Click();

        Assert.False(cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").HasAttribute("disabled"));
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("Summer block", data.Snapshot.Plans.Select(p => p.Name)));
        Assert.Equal(initialExerciseCount, data.Snapshot.Exercises.Count);
    }

    [Fact]
    public async Task Import_guides_offers_Keep_mine_or_Replace_per_existing_Guide_and_keeps_personal_notes()
    {
        var existingGuide = new Guide(Squat, "Old works", null, null, null, [], [], null, null, [], null, "My knee note");
        var incomingSquat = new Guide(Squat, "New works", "Why", null, null, [], [], null, null, [], null, "File note");
        var incomingLunge = new Guide(Lunge, "Lunge works", null, null, null, [], [], null, null, [], null, null);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [PageFixture.Exercises[0], PageFixture.Exercises[1]],
                null,
                Guides: [incomingSquat, incomingLunge]));

        var (cut, data, _) = await RenderAsync(
            "http://localhost/backup?tab=import",
            extra: [Change.Put(existingGuide)]);

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        var preview = cut.Find("[data-testid='import-preview']");
        var keepMine = preview.QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Keep mine");
        var replace = preview.QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Replace");
        Assert.Equal("true", keepMine.GetAttribute("aria-pressed"));
        Assert.Equal("false", replace.GetAttribute("aria-pressed"));

        replace.Click();
        preview = cut.Find("[data-testid='import-preview']");
        Assert.Contains("Replaces your Guide for \"Goblet squat\". Your personal notes are kept.", preview.TextContent);

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid='import-applied']")));

        var savedSquat = data.Snapshot.Guides.Single(g => g.ExerciseId == Squat);
        Assert.Equal("New works", savedSquat.Works);
        Assert.Equal("My knee note", savedSquat.PersonalNotes);
        Assert.Contains(data.Snapshot.Guides, g => g.ExerciseId == Lunge);
    }

    [Fact]
    public async Task Import_apply_downloads_automatic_Backup_and_aborts_if_download_fails()
    {
        var newPlan = new Plan(
            "winter",
            "Winter block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Legs", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)])]);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [PageFixture.Exercises[0]],
                null,
                Plans: [newPlan]));

        // Download fails -> aborts with nothing changed
        var browser = new FakeBrowserCapabilities { DownloadResult = false };
        var (cut, data, _) = await RenderAsync("http://localhost/backup?tab=import", browser: browser);

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("automatic Backup", cut.Find("[data-testid='import-errors'][role='alert']").TextContent));
        Assert.DoesNotContain(data.Snapshot.Plans, p => p.Name == "Winter block");

        // Download succeeds -> downloads pre-import Backup JSON and commits
        browser.DownloadResult = true;
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("[data-testid='import-applied']")));
        var downloaded = Assert.Single(browser.Downloads);
        Assert.Contains("before-import", downloaded.FileName);
        Assert.Equal("application/json", downloaded.ContentType);
        Assert.Contains(data.Snapshot.Plans, p => p.Name == "Winter block");
    }

    [Fact]
    public async Task Import_apply_handles_stale_preview_by_asking_to_recheck()
    {
        var newPlan = new Plan(
            "autumn",
            "Autumn block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Legs", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)])]);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [PageFixture.Exercises[0]],
                null,
                Plans: [newPlan]));

        var (cut, data, _) = await RenderAsync("http://localhost/backup?tab=import");

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        // Mutate Data after preview was built
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(new WeighIn("wi-stale", Today, 80.0))]));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").Click();

        cut.WaitForAssertion(() => Assert.Contains("changed since this preview", cut.Find("[data-testid='import-errors'][role='alert']").TextContent));
        Assert.DoesNotContain(data.Snapshot.Plans, p => p.Name == "Autumn block");
    }

    [Fact]
    public async Task Import_apply_is_disabled_with_ReadOnlyWell_when_read_only()
    {
        var env = new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere };
        var data = await AppData.OpenAsync(new InMemoryDataStore(), env);
        Wire(this, data);
        Services.GetRequiredService<NavigationManager>().NavigateTo("http://localhost/backup?tab=import");
        var cut = Render<ExportImport>();

        Assert.NotNull(cut.Find("[role='status']"));

        var newPlan = new Plan(
            "autumn",
            "Autumn block",
            true,
            Today,
            null,
            null,
            2,
            [new Track("t1", "Main", new TrackRhythm.Weekly(3, 2, false), [])],
            [new Session("s1", "Legs", "t1", DefaultDay.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)])]);
        var json = Core.Serialisation.ExchangeFormat.Write(
            new ExchangeDocument(
                Core.Serialisation.ExchangeFormat.CurrentVersion,
                Now,
                "test",
                [PageFixture.Exercises[0]],
                null,
                Plans: [newPlan]));

        cut.Find("textarea[aria-label='Paste JSON']").Input(json);
        cut.FindAll("button").First(b => b.TextContent.Trim() == "Check").Click();

        Assert.True(cut.FindAll("button").First(b => b.TextContent.Trim() == "Apply").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Prompts_tab_renders_all_three_prompt_cards_with_descriptions_and_copy_buttons()
    {
        var (cut, _, _) = await RenderAsync("http://localhost/backup?tab=prompts");

        var headings = cut.FindAll(".fa-prompts__card h2").Select(h => h.TextContent.Trim()).ToArray();
        Assert.Equal(["Analyse my training", "Write me a Plan", "Write Guides"], headings);

        var cards = cut.FindAll(".fa-prompts__card");
        Assert.All(cards, card =>
        {
            Assert.NotEmpty(card.QuerySelector(".fa-prompts__desc")!.TextContent.Trim());
            Assert.Contains(card.QuerySelectorAll("button"), b => b.TextContent.Trim() == "Copy prompt");
        });
    }

    [Fact]
    public async Task Copy_prompt_on_Analyse_and_WritePlan_copies_PromptPack_text_and_shows_confirmation()
    {
        var workout = Finished("w1", Today, Set("s1", 60, 5));
        var (cut, data, browser) = await RenderAsync("http://localhost/backup?tab=prompts", workouts: [workout]);

        cut.FindAll(".fa-prompts__card")[0].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        var expectedRequest = ExportRequest.Default(Today);
        var expectedExport = Exporter.Export(data.Snapshot, expectedRequest, "test-build", Now);
        var expectedAnalyse = PromptPack.Analyse(data.Snapshot, expectedExport, expectedRequest.Range);

        Assert.Single(browser.CopiedTexts);
        Assert.Equal(expectedAnalyse, browser.CopiedTexts[0]);
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[0].QuerySelector("[role='status']")!.TextContent.Trim());

        cut.FindAll(".fa-prompts__card")[1].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Equal(2, browser.CopiedTexts.Count);
        Assert.Equal(PromptPack.WritePlan(data.Snapshot), browser.CopiedTexts[1]);
        Assert.Null(cut.FindAll(".fa-prompts__card")[0].QuerySelector("[role='status']"));
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[1].QuerySelector("[role='status']")!.TextContent.Trim());
    }

    [Fact]
    public async Task Copy_prompt_failure_shows_error_message()
    {
        var browser = new FakeBrowserCapabilities { CopyResult = false };
        var (cut, _, _) = await RenderAsync("http://localhost/backup?tab=prompts", browser: browser);

        cut.FindAll(".fa-prompts__card")[0].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Contains("Could not copy", cut.FindAll(".fa-prompts__card")[0].QuerySelector("[role='alert']")!.TextContent);
    }

    [Fact]
    public async Task Write_Guides_defaults_to_Exercises_without_a_Guide_and_respects_selection()
    {
        var squatGuide = new Guide(Squat, "Quads", null, null, null, [], [], null, null, [], null, null);
        var lungeNotesOnly = new Guide(Lunge, null, null, null, null, [], [], null, null, [], null, "My notes");
        var (cut, data, browser) = await RenderAsync(
            uri: "http://localhost/backup?tab=prompts",
            extra: [Change.Put(squatGuide), Change.Put(lungeNotesOnly)]);

        var squatName = data.Snapshot.Exercises.Single(e => e.Id == Squat).Name;
        var lungeName = data.Snapshot.Exercises.Single(e => e.Id == Lunge).Name;

        Assert.False(cut.Find($"input[aria-label='{squatName}']").HasAttribute("checked"));
        Assert.True(cut.Find($"input[aria-label='{lungeName}']").HasAttribute("checked"));

        cut.FindAll(".fa-prompts__card")[2].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Single(browser.CopiedTexts);
        Assert.Equal(PromptPack.WriteGuides(data.Snapshot), browser.CopiedTexts[0]);
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[2].QuerySelector("[role='status']")!.TextContent.Trim());

        cut.Find($"input[aria-label='{squatName}']").Change(true);
        cut.Find($"input[aria-label='{lungeName}']").Change(false);
        cut.FindAll(".fa-prompts__card")[2].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Equal(2, browser.CopiedTexts.Count);
        Assert.Contains($"\"id\":\"{Squat}\"", browser.CopiedTexts[1]);
        Assert.DoesNotContain($"\"id\":\"{Lunge}\"", browser.CopiedTexts[1]);
    }
}
