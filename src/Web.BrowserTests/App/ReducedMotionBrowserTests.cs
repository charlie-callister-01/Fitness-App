using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>Reduced motion (spec B11): with it requested the unlock toast does not fade in and a pressed button does not move; without it, both do.</summary>
[Collection(AppCollection.Name)]
public sealed class ReducedMotionBrowserTests(AppFixture app)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reduced_motion_switches_off_the_unlock_toast_fade_and_the_button_press_nudge(bool reducedMotion)
    {
        await using var page = await app.OpenAsync("/settings", new AppPageOptions { ReducedMotion = reducedMotion });

        // An unlock toast as the published styles define it: an element matching the toast's own (scoped) rule.
        var toastAnimation = await page.Page.EvaluateAsync<string>(
            """
            () => {
              const rulesOf = sheet => [...sheet.cssRules].flatMap(r => r.styleSheet ? rulesOf(r.styleSheet) : [r]);
              const rule = [...document.styleSheets].flatMap(rulesOf)
                .find(r => r.selectorText && r.selectorText.includes('.fa-unlocktoast'));
              const scope = rule.selectorText.match(/\[(b-[a-z0-9]+)\]/)[1];
              const toast = document.createElement('a');
              toast.className = 'fa-well fa-unlock fa-unlocktoast';
              toast.setAttribute(scope, '');
              document.querySelector('#main-content').append(toast);
              return getComputedStyle(toast).animationName;
            }
            """);

        var button = page.Page.Locator("#main-content .fa-btn:not(:disabled)").First;
        var box = (await button.BoundingBoxAsync())!;
        await page.Page.Mouse.MoveAsync(box.X + (box.Width / 2), box.Y + (box.Height / 2));
        await page.Page.Mouse.DownAsync();
        var pressedTransform = await button.EvaluateAsync<string>("b => getComputedStyle(b).transform");
        await page.Page.Keyboard.PressAsync("Escape");

        if (reducedMotion)
        {
            Assert.Equal("none", toastAnimation);
            Assert.Equal("none", pressedTransform);
        }
        else
        {
            Assert.StartsWith("fa-unlocktoast-in", toastAnimation, StringComparison.Ordinal);
            Assert.NotEqual("none", pressedTransform);
        }
    }
}
