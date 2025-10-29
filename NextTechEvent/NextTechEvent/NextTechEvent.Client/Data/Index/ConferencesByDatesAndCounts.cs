using NextTechEvent.Data;
using Raven.Client.Documents.Indexes;

namespace NextTechEvent.Client.Data.Index;

public class ConferencesByDatesAndCounts : AbstractIndexCreationTask<Conference>
{
    public class Result
    {
        public DateOnly EventEnd { get; set; }
        public DateOnly EventStart { get; set; }
        public DateTime CfpEndDate { get; set; }
        public DateTime CreateDate { get; set; }
    }

    public ConferencesByDatesAndCounts()
    {
        Map = conferences => from c in conferences
                             where c.NumberOfDays < 10
                             select new
                             {
                                 c.EventEnd,
                                 c.EventStart,
                                 c.CfpEndDate,
                                 c.CreateDate
                             };
    }
}

