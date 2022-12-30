using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

namespace NextTechEvent.Data.Index;

public class ConferenceWithUserStatus : AbstractIndexCreationTask<Status, ConferenceUserStatus>
{
    public ConferenceWithUserStatus()
    {
        Map = cs =>
        from c in cs
        let conference = LoadDocument<Conference>(c.ConferenceId)
        where c.State != StateEnum.NotSet && c.State != StateEnum.Rejected
        select new ConferenceUserStatus()
        {
            UserId=c.UserId,
            ConferenceId = c.ConferenceId,
            ConferenceName = conference.Name,
            EventStart = conference.EventStart,
            EventEnd = conference.EventEnd,
            State = c.State
        };
    }
}
