﻿using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

namespace NextTechEvent.Data.Index;

public class ConferenceCountByDates : AbstractIndexCreationTask<Conference, ConferenceCountByDate>
{
    public ConferenceCountByDates()
    {
        Map = cs =>
        from c in cs
        where c.NumberOfDays < 10 && c.NumberOfDays > 0
        from d in Enumerable.Range(0, c.NumberOfDays).Cast<object>()
        select new ConferenceCountByDate()
        {
            Date = c.EventStart.AddDays((int)d),
            SearchTerm = c.Name + "," + c.City + "," + c.Country + "," + c.Venue + "," + string.Join(",", c.Tags),
            Count = 1
        };

        Reduce = results => from result in results
                            group result by new { result.Date, result.SearchTerm } into g
                            select new
                            {
                                Date = g.Key.Date,
                                SearchTerm = g.Key.SearchTerm,
                                Count = g.Sum(x => x.Count)
                            };
        Index(x => x.SearchTerm, FieldIndexing.Search);
    }

}
