using NextTechEvent.Components;
using NextTechEvent.Client.Components;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextTechEvent.Client.Pages;

namespace NextTechEvent.Tests.ComponentTests;

[Collection("ComponentTests")]
public class OverlappingConferencesTests
{
    NextTextEventTestFixture _fixture { get; set; }
    public OverlappingConferencesTests(NextTextEventTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public Task OverlappingConferencesShouldRenderConferenceItem()
    {
        DateOnly start = DateOnly.FromDateTime(DateTime.Now);
        DateOnly end = DateOnly.FromDateTime(DateTime.Now.AddDays(10));

        // Arrange
        var cut = _fixture.Context.RenderComponent<OverlappingConferences>(
            parameters => parameters
            .Add(p => p.Start, start)
            .Add(p => p.End, end)
            );
        cut.WaitForState(() => cut.FindComponents<ConferenceItem>().Count > 0, TimeSpan.FromSeconds(5));

        // Assert that content of the paragraph shows counter at zero
        Assert.Equal(11, cut.FindComponents<ConferenceItem>().Count);
        return Task.CompletedTask;
    }
}
