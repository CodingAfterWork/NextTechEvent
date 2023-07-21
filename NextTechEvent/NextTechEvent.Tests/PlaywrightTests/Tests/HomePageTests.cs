using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;

namespace NextTechEvent.Tests.PlaywrightTests.Tests;

[Collection("PlaywrightTests")]
public class HomePageTests
{
    private readonly string _serverAddress;
    public HomePageTests(CustomWebApplicationFactory fixture)
    {
        _serverAddress = fixture.ServerAddress;
    }

    const bool RunHeadless = true;
    [Fact]
    public async Task Navigate_to_HomePage()
    {
        //Arrange
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = RunHeadless });
        var page = await browser.NewPageAsync();

        //Act
        await page.GotoAsync(_serverAddress);

        //Assert
        var element = await page.WaitForSelectorAsync("text=Find your next tech event");
        element.Should().NotBeNull();
    }
}
