using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Playwright.Axe;
using Xunit.Abstractions;

namespace NextTechEvent.Tests.PlaywrightTests.Tests;

[Collection("PlaywrightTests")]
public class AccessibilityTests
{
    private readonly string _serverAddress;
    private readonly ITestOutputHelper _output;

    public AccessibilityTests(CustomWebApplicationFactory fixture, ITestOutputHelper output)
    {
        _serverAddress = fixture.ServerAddress;
        _output = output;
    }

    [Fact]
    public async Task Navigate_to_HomePage()
    {
        //Arrange
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        //Act
        await page.GotoAsync(_serverAddress);
		await page.WaitForSelectorAsync("text=Find your next tech event");
		
        //Assert
		AxeResults axeResults = await page.RunAxe();
        axeResults.AssertNoViolations(_output);
    }
}
