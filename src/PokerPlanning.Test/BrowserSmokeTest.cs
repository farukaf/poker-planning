using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace PokerPlanning.Test;

public sealed class BrowserFactAttribute : FactAttribute
{
    public BrowserFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("POKER_BASE_URL")))
            Skip = "Set POKER_BASE_URL to run against a real application.";
    }
}

public class BrowserSmokeTest
{
    [BrowserFact]
    public void TwoPlayersVoteRevealAndStartAnotherRound()
    {
        using var first = CreateDriver();
        using var second = CreateDriver();
        try
        {
            first.Navigate().GoToUrl(Environment.GetEnvironmentVariable("POKER_BASE_URL")!);
            SetName(first, "Alice Smoke");
            Click(first, "//button[normalize-space()='Create Room']");
            Click(first, "//dialog[@data-id='createRoomDialog']//button[normalize-space()='Confirm']");
            Wait(first).Until(d => d.Url.Contains("/room/"));
            second.Navigate().GoToUrl(first.Url);
            SetName(second, "Bob Smoke");
            Wait(first).Until(d => d.FindElements(By.CssSelector(".player-name")).Count == 2);
            Click(first, "//fieldset//div[contains(@class,'card')][b[text()='3']]");
            Click(second, "//fieldset//div[contains(@class,'card')][b[text()='5']]");
            Wait(first).Until(d => d.FindElements(By.CssSelector(".player-block .card.active")).Count == 2);
            Assert.Empty(first.FindElements(By.CssSelector(".player-block .card b")));
            Click(first, "//button[normalize-space()='Show Cards']");
            foreach (var driver in new[] { first, second })
            {
                Wait(driver).Until(d => d.FindElements(By.CssSelector(".player-block .card b")).Count == 2);
                Assert.Equal(new[] { "3", "5" }, driver.FindElements(By.CssSelector(".player-block .card b")).Select(e => e.Text).Order().ToArray());
                Assert.Equal("4", driver.FindElement(By.CssSelector(".coffee-table-content h2")).Text);
            }
            Click(second, "//button[normalize-space()='New Game']");
            Wait(first).Until(d => d.FindElements(By.XPath("//button[normalize-space()='Show Cards']")).Count == 1);
            Assert.Empty(first.FindElements(By.CssSelector(".player-block .card b")));
        }
        catch
        {
            var output = Environment.GetEnvironmentVariable("POKER_BROWSER_ARTIFACTS") ?? "TestResults/browser";
            Directory.CreateDirectory(output);
            SaveFailure(first, output, "first");
            SaveFailure(second, output, "second");
            throw;
        }
    }

    private static IWebDriver CreateDriver()
    {
        var options = new ChromeOptions();
        options.AddArguments("--headless=new", "--no-sandbox", "--disable-dev-shm-usage", "--window-size=1440,1000");
        options.SetLoggingPreference(LogType.Browser, LogLevel.All);
        var remote = Environment.GetEnvironmentVariable("SELENIUM_REMOTE_URL");
        return string.IsNullOrEmpty(remote) ? new ChromeDriver(options) : new RemoteWebDriver(new Uri(remote), options);
    }

    private static WebDriverWait Wait(IWebDriver driver) => new(driver, TimeSpan.FromSeconds(30));

    private static void Click(IWebDriver driver, string xpath)
    {
        Wait(driver).Until(d =>
        {
            var element = d.FindElements(By.XPath(xpath)).FirstOrDefault(e => e.Displayed && e.Enabled);
            if (element is null) return false;
            element.Click();
            return true;
        });
    }

    private static void SetName(IWebDriver driver, string name)
    {
        Wait(driver).Until(d => d.FindElements(By.CssSelector("dialog[data-id='confirmModal'] input")).Any(e => e.Displayed));
        driver.FindElement(By.CssSelector("dialog[data-id='confirmModal'] input")).SendKeys(name);
        Click(driver, "//dialog[@data-id='confirmModal']//button[normalize-space()='Confirm']");
    }

    private static void SaveFailure(IWebDriver driver, string output, string name)
    {
        try
        {
            ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(Path.Combine(output, name + ".png"));
            File.WriteAllText(Path.Combine(output, name + ".html"), driver.PageSource);
            File.WriteAllLines(Path.Combine(output, name + ".log"), driver.Manage().Logs.GetLog(LogType.Browser).Select(e => e.ToString()));
        }
        catch (WebDriverException) { /* Preserve the original test failure if the browser has disconnected. */ }
    }
}
