using NextTechEvent.Data;
using Raven.Client.Documents.Indexes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextTechEvent.Components;

public class ConferenceBySearchTerm :AbstractIndexCreationTask<Conference, ConferenceSearchTerm>
{
	public ConferenceBySearchTerm()
	{
        Map = cs =>
        from c in cs
        select new ConferenceSearchTerm()
        {
            Id = c.Id,
            Name = c.Name,
            EventStart = c.EventStart,
            CfpEndDate = c.CfpEndDate,
            SearchTerm = c.Name + "," + c.City + "," + c.Country + "," + c.Venue + "," + string.Join(",", c.Tags)
        };

        Index(x => x.SearchTerm, FieldIndexing.Search);
	}
}

public class ConferenceSearchTerm
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string SearchTerm { get; set; }
    public DateOnly EventStart { get; set; }
    public DateTime? CfpEndDate { get; set; } = DateTime.Now;
}
