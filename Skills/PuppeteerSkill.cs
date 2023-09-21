using ElectricRaspberry.Models;
using ElectricRaspberry.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.SkillDefinition;
using PuppeteerSharp;
using PuppeteerSharp.Dom;
using System.ComponentModel;

namespace ElectricRaspberry.Skills
{
  public class PuppeteerSkill
  {
    private readonly IUser user;
    private readonly IStorageService storageService;
    private readonly ConnectOptions config;

    public PuppeteerSkill(IUser user, IOptions<BrowserlessOptions> browserlessOptions, IStorageService storageService)
    {
      this.user = user;
      this.storageService = storageService;
      this.config = new ConnectOptions() { BrowserWSEndpoint = $"wss://chrome.browserless.io?token={browserlessOptions.Value.ApiKey}" };
    }

    //[SKFunction, Description("Take control of a remote browser via puppeteer and navigate to the given URL will return the status text for that URL.")]
    //public async Task<string> NavigateAsync([Description("The URL to navigate to")] string url)
    //{
    //  await using var browser = await Puppeteer.ConnectAsync(config);
    //  await using var page = await browser.NewPageAsync();
    //  await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
    //  var response = await page.GoToAsync(url);
    //  return response.StatusText;
    //}

    [SKFunction, Description("Take control of a remote browser via puppeteer and navigate to the given URL, and capture a Screenshot of the selector (defaults to body if not supplied).")]
    public async Task<string> CaptureImageAsync([Description("The URL to navigate to")] string url, [Description("The selector to capture")] string? selector = null)
    {
      await using var browser = await Puppeteer.ConnectAsync(config);
      await using var page = await browser.NewPageAsync();
      await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
      await page.GoToAsync(url);
      var screenshotOptions = new ScreenshotOptions()
      {
        Type = ScreenshotType.Png,
        OmitBackground = false,
      };
      await using var element = await page.QuerySelectorAsync(selector ?? "body");
      var data = await element.ScreenshotDataAsync(screenshotOptions);
      var name = Path.ChangeExtension(Path.GetRandomFileName(), "png");
      var path = await this.storageService.Upload(data, name, "image/png", user.Id);
      return path;
    }

    [SKFunction, Description("Take control of a remote browser via puppeteer and navigate to the given URL, and capture the text of the selector  (defaults to body if not supplied).")]
    public async Task<string> CaptureTextAsync([Description("The URL to navigate to")] string url, [Description("The selector to capture")] string? selector = null)
    {
      await using var browser = await Puppeteer.ConnectAsync(config);
      await using var page = await browser.NewPageAsync();
      await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
      await page.GoToAsync(url);
      await using var element = await page.QuerySelectorAsync<HtmlElement>(selector ?? "body");
      var innerText = await element.GetInnerTextAsync();
      if ((innerText.Length / 4) > 1000) throw new InvalidOperationException("Error: The browser was able to capture the text it is too large to load into context.");
      return innerText;
    }

    [SKFunction, Description("Take control of a remote browser via puppeteer and navigate to the given URL to inject text into the selector.")]
    public async Task InjectTextAsync([Description("The URL to navigate to")] string url, [Description("The selector to inject into")] string selector, [Description("The text to inject")] string text)
    {
      await using var browser = await Puppeteer.ConnectAsync(config);
      await using var page = await browser.NewPageAsync();
      await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
      await page.GoToAsync(url);
      await using var element = await page.QuerySelectorAsync<HtmlInputElement>(selector);
      await element.TypeAsync(text);
    }

    [SKFunction, Description("Take control of a remote browser via puppeteer and navigate to the given URL to click on on the selector.")]
    public async Task ClickElementAsync([Description("The URL to navigate to")] string url, [Description("The selector to click")] string selector)
    {
      await using var browser = await Puppeteer.ConnectAsync(config);
      await using var page = await browser.NewPageAsync();
      await page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 768 });
      await page.GoToAsync(url);
      await using var element = await page.QuerySelectorAsync<HtmlElement>(selector);
      await element.ClickAsync();
    }
  }
}

// Clipboard?