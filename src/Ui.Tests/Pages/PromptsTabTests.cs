using Bunit;
using Core.Exchange;
using Core.Model;
using Core.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using static Ui.Tests.Pages.ExportTabTests;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Prompt pack tab on /backup (design S-10).</summary>
public sealed class PromptsTabTests : BunitContext
{
    private Task<(IRenderedComponent<ExportImport> Cut, AppData Data, FakeBrowserCapabilities Browser)> RenderAsync(
        Workout[]? workouts = null,
        Change[]? extra = null,
        FakeBrowserCapabilities? browser = null) =>
        RenderBackupPageAsync(this, "http://localhost/backup?tab=prompts", workouts: workouts, extra: extra, browser: browser);

    [Fact]
    public async Task Prompts_tab_renders_all_three_prompt_cards_with_descriptions_and_copy_buttons()
    {
        var (cut, _, _) = await RenderAsync();

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
        var (cut, data, browser) = await RenderAsync(workouts: [workout]);

        cut.FindAll(".fa-prompts__card")[0].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        var expectedRequest = ExportRequest.Default(Today);
        var expectedExport = Exporter.Export(data.Snapshot, expectedRequest, "test-build", Now);
        var expectedAnalyse = PromptPack.Analyse(data.Snapshot, expectedExport, expectedRequest.Range);

        Assert.Single(browser.CopiedTexts);
        Assert.Equal(expectedAnalyse, browser.CopiedTexts[0]);
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[0].QuerySelector(".fa-prompts__status")!.TextContent.Trim());
        Assert.Equal("Copied.", Services.GetRequiredService<Ui.Accessibility.Announcer>().Message);
        Assert.Null(cut.FindAll(".fa-prompts__card")[0].QuerySelector(".fa-prompts__status")!.GetAttribute("role"));

        cut.FindAll(".fa-prompts__card")[1].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Equal(2, browser.CopiedTexts.Count);
        Assert.Equal(PromptPack.WritePlan(data.Snapshot), browser.CopiedTexts[1]);
        Assert.Null(cut.FindAll(".fa-prompts__card")[0].QuerySelector(".fa-prompts__status"));
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[1].QuerySelector(".fa-prompts__status")!.TextContent.Trim());
    }

    [Fact]
    public async Task Copy_prompt_failure_shows_error_message()
    {
        var browser = new FakeBrowserCapabilities { CopyResult = false };
        var (cut, _, _) = await RenderAsync(browser: browser);

        cut.FindAll(".fa-prompts__card")[0].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Contains("Could not copy", cut.FindAll(".fa-prompts__card")[0].QuerySelector("[role='alert']")!.TextContent);
    }

    [Fact]
    public async Task Write_Guides_defaults_to_Exercises_without_a_Guide_and_respects_selection()
    {
        var squatGuide = new Guide(Squat, "Quads", null, null, null, [], [], null, null, [], null, null);
        var lungeNotesOnly = new Guide(Lunge, null, null, null, null, [], [], null, null, [], null, "My notes");
        var (cut, data, browser) = await RenderAsync(
            extra: [Change.Put(squatGuide), Change.Put(lungeNotesOnly)]);

        var squatName = data.Snapshot.Exercises.Single(e => e.Id == Squat).Name;
        var lungeName = data.Snapshot.Exercises.Single(e => e.Id == Lunge).Name;

        Assert.False(cut.Find($"input[aria-label='{squatName}']").HasAttribute("checked"));
        Assert.True(cut.Find($"input[aria-label='{lungeName}']").HasAttribute("checked"));

        cut.FindAll(".fa-prompts__card")[2].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Single(browser.CopiedTexts);
        Assert.Equal(PromptPack.WriteGuides(data.Snapshot), browser.CopiedTexts[0]);
        Assert.Equal("Copied.", cut.FindAll(".fa-prompts__card")[2].QuerySelector(".fa-prompts__status")!.TextContent.Trim());

        cut.Find($"input[aria-label='{squatName}']").Change(true);
        cut.Find($"input[aria-label='{lungeName}']").Change(false);
        cut.FindAll(".fa-prompts__card")[2].QuerySelectorAll("button").First(b => b.TextContent.Trim() == "Copy prompt").Click();

        Assert.Equal(2, browser.CopiedTexts.Count);
        Assert.Contains($"\"id\":\"{Squat}\"", browser.CopiedTexts[1]);
        Assert.DoesNotContain($"\"id\":\"{Lunge}\"", browser.CopiedTexts[1]);
    }
}
