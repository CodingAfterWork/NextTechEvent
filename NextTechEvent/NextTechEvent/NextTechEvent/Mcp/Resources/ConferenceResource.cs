using ModelContextProtocol.Server;
using NextTechEvent.Components;
using NextTechEvent.Data;
using System.ComponentModel;

namespace NextTechEvent.Resources;

[McpServerResourceType]
public class ConferenceResource(NextTechEventRepository repo)
{
    [McpServerResource]
    [Description("List of conferences matching a search term, it can be conference name, city, country")]
    public async Task<List<ConferenceSearchTerm>> GetConferences(string searchTerm)
    {
        return await repo.SearchConferencesAsync(searchTerm);
    }
}