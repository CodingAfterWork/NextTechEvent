using ModelContextProtocol.Server;
using NextTechEvent.Components;
using NextTechEvent.Data;
using System.ComponentModel;

namespace NextTechEvent.Tools;

[McpServerToolType]
public class ConferenceTool (NextTechEventRepository repo)
{
    [McpServerTool, Description("Gets a list of upcoming conferences based on a search term")]
    public async Task<List<ConferenceSearchTerm>> GetConferences(string searchTerm)
    {
        return await repo.SearchConferencesAsync(searchTerm);
    }

}
