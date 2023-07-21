using Playwright.Axe;
using Xunit.Abstractions;

namespace NextTechEvent.Tests.PlaywrightTests.Tests;
[Collection("PlaywrightTests")]
public static class AxeResultExtensions
{
    public static void AssertNoViolations(this AxeResults axeResults, ITestOutputHelper output)
    {
        output.WriteLine("------- Violations -------");
        foreach (var v in axeResults.Violations)
        {
            //return all the errors to the test output
            output.WriteLine(v.Help);
            output.WriteLine(v.Description);
            output.WriteLine(v.HelpUrl.ToString());
            foreach (var n in v.Nodes)
            {
                output.WriteLine(n.Html);
            }
            output.WriteLine("-------------");
        }

        //return all the incomplete to the test output
        output.WriteLine("------- Incomplete -------");
        foreach (var v in axeResults.Incomplete)
        {
            output.WriteLine(v.Help);
            output.WriteLine(v.Description);
            output.WriteLine(v.HelpUrl.ToString());
            foreach (var n in v.Nodes)
            {
                output.WriteLine(n.Html);
            }
            output.WriteLine("-------------");
        }


        if (axeResults.Violations.Count > 0)
        {
            Assert.Fail($"Expected no violations, but got {axeResults.Violations.Count}");
        }
    }
}