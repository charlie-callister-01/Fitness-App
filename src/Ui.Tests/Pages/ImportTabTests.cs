using Bunit;
using Core.Exchange;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using static Ui.Tests.Pages.ExportTabTests;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Import tab and ImportPreview (design S-10, C-16, F-05, F-10).</summary>
public sealed class ImportTabTests : BunitContext
{
    private Task<(IRenderedComponent<ExportImport> Cut, AppData Data, FakeBrowserCapabilities Browser)> RenderAsync(
        string uri = "http://localhost/backup?tab=import",
        Plan? plan = null,
        Workout[]? workouts = null,
        Change[]? extra = null,
        FakeBrowserCapabilities? browser = null,
        IDataStore? store = null) =>
        RenderBackupPageAsync(this, uri, plan, workouts, extra, browser, store);

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
        var (cut, _, _) = await RenderAsync();

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
        var (cut, data, _) = await RenderAsync();
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

        var (cut, _, _) = await RenderAsync();

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

        var (cut, data, _) = await RenderAsync();
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

        var (cut, data, _) = await RenderAsync(extra: [Change.Put(existingGuide)]);

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
        var applied = cut.Find("[data-testid='import-applied']");
        Assert.Null(applied.GetAttribute("role"));
        var announced = Services.GetRequiredService<Ui.Accessibility.Announcer>().Message;
        Assert.StartsWith("Import applied.", announced);
        Assert.All(applied.QuerySelectorAll("li"), line => Assert.Contains(line.TextContent.Trim(), announced));

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
        var (cut, data, _) = await RenderAsync(browser: browser);

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

        var (cut, data, _) = await RenderAsync();

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
}
