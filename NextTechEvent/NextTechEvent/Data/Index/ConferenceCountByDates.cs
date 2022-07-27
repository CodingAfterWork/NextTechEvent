using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;

namespace NextTechEvent.Data.Index;

public class ConferenceCountByDates : AbstractIndexCreationTask<Conference, ConferenceCountByDate>
{
    public ConferenceCountByDates()
    {
        Map = cs =>
        from c in cs
        where c.NumberOfDays < 10 && c.NumberOfDays > 0
        from d in Enumerable.Range(0, 1 + c.NumberOfDays).Cast<object>()
        select new ConferenceCountByDate()
        {
            Date = c.EventStart.AddDays((int)d),
            Count = 1
        };

        Reduce = results => from result in results
                            group result by result.Date into g
                            select new
                            {
                                Date = g.Key,
                                Count = g.Sum(x => x.Count)
                            };
    }

}
