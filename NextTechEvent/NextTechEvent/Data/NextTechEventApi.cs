using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Data.Index;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Session.TimeSeries;
using InterfaceGenerator;
using NextTechEvent.Components;
using Raven.Client.Documents.Queries;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using NPOI.SS.Formula.Functions;
using Raven.Client.Documents.Operations;

namespace NextTechEvent.Data
{
    [GenerateAutoInterface]
    public class NextTechEventApi : INextTechEventApi
    {
        IDocumentStore _store;
        IHttpClientFactory _factory;
        public NextTechEventApi(IDocumentStore store,IHttpClientFactory factory)
        {
            _store = store;
            _factory = factory;
        }

        public async Task<Conference> SaveConferenceAsync(Conference conference)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            await session.StoreAsync(conference);
            await session.SaveChangesAsync();
            return conference;
        }
        
        public async Task<Status> SaveStatusAsync(Status status)
        {
            var savedstatus = await GetStatusAsync(status.ConferenceId, status.UserId);
            if (savedstatus != null)
            {
                status.Id = savedstatus.Id;
            }

            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            await session.StoreAsync(status);
            await session.SaveChangesAsync();
            return status;
        }

        
        public async Task<Calendar> SaveCalendarAsync(Calendar item)
        {
            if(item.Id==null)
            {
                item.Id = Guid.NewGuid().ToString().Replace("-", "");
            }

            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            await session.StoreAsync(item);
            await session.SaveChangesAsync();
            return item;
        }



        public async Task<Status> GetStatusAsync(string conferenceId,string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Status>().Where(c => c.ConferenceId == conferenceId && c.UserId==userId).FirstOrDefaultAsync();
        }


        public async Task<Calendar?> GetCalendarAsync(string id)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Calendar>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Calendar?> GetCalendarByUserIdAsync(string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var calendar = await session.Query<Calendar>().Where(c => c.UserId == userId ).FirstOrDefaultAsync();

            return calendar;
        }

        public async Task UpdateStatusBasedOnSessionizeCalendarAsync(Calendar calendar)
        {
            var calendarcontent=await _factory.CreateClient().GetStringAsync(calendar.SessionizeCalendarUrl);
            var sessionizecalendar =Ical.Net.Calendar.Load(calendarcontent);
            foreach (CalendarEvent item in sessionizecalendar.Events.Where(i=>i.Uid.StartsWith("SZEVENT")))
            {
                var eventId = item.Uid.Replace("SZEVENT","");
                var conf = await GetConferenceBySessionizeIdAsync(eventId);
                if (conf == null || conf.Id == null)
                    continue;
                var status =await GetStatusAsync(conf.Id, calendar.UserId);
                var state = GetStateFromCalendarEvent(item);
                if (status == null || (status!=null && status.State != state))
                {
                    if (status == null)
                    {
                        
                        
                        status = new()
                        {
                            ConferenceId = conf.Id,
                            UserId = calendar.UserId,
                            State = state
                        };

                    }
                    status.State = state;

                    await SaveStatusAsync(status);
                }
            }

        }

        private StateEnum GetStateFromCalendarEvent(CalendarEvent item)
        {
            switch (item.Status)
            {
                case "CONFIRMED":
                    return StateEnum.Accepted;
                case "TENTATIVE":
                    return StateEnum.Submitted;
                default:
                    return StateEnum.NotSet;
                    break;
            }
        }

        public async Task<List<ConferenceUserStatus>> GetConferencesByUserIdAsync(string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var data = await session.Query<Status>()
                .Include(c => c.ConferenceId)
                .Where(c => c.UserId == userId && c.State != StateEnum.NotSet && c.State != StateEnum.Rejected)
                .ToListAsync();


            List<ConferenceUserStatus> result = new();
            foreach(var c in data)
            {
                result.Add(await GetConferenceUserStatus(session, c.ConferenceId, c.State));
            }
            return result;
        }

        private async Task <ConferenceUserStatus> GetConferenceUserStatus(IAsyncDocumentSession session, string conferenceId, StateEnum state)
        {
            var conference = await session.LoadAsync<Conference>(conferenceId);
            return new() { ConferenceId = conferenceId, ConferenceName = conference.Name, EventStart = conference.EventStart, State = state };
        }

        public async Task<Ical.Net.Calendar> GetUserCalendarAsync(string userId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var data=await session.Query<Status>()
                .Include(c => c.ConferenceId)
                .Where(c => c.UserId == userId && c.State != StateEnum.NotSet && c.State != StateEnum.Rejected)
                .ToListAsync();
            
            var calendar = new Ical.Net.Calendar();
            calendar.Method= Ical.Net.CalendarMethods.Publish;
            calendar.AddProperty("X-WR-CALNAME", "NextTechEvent");
            calendar.AddProperty("CALNAME", "NextTechEvent");
            calendar.AddProperty("NAME", "NextTechEvent");

            foreach (var s in data)
            {
                var conference = await session.LoadAsync<Conference>(s.ConferenceId);
                var e = new CalendarEvent
                {
                    Uid = conference.Id,
                    Location = $"{conference.Venue}, {conference.City}, {conference.Country}",
                    Start = new CalDateTime(conference.EventStart.ToDateTime(new TimeOnly(0, 0))),
                    End = new CalDateTime(conference.EventEnd.AddDays(1).ToDateTime(new TimeOnly(23, 59))),
                    IsAllDay=true,
                    Summary = $"{conference.Name} - {s.State}",
                    Status = s.State == StateEnum.Accepted ? "CONFIRMED" : "TENTATIVE",
                    Description = $"https://nexttechevent.azurewebsites.net/Conferences/{s.ConferenceId}"
                };


                calendar.Events.Add(e);
            }
            return calendar;
        }


        public async Task<Conference> GetConferenceAsync(string id)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Conference> GetConferenceBySessionizeIdAsync(string sessionizeId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c => c.Identifier == sessionizeId && c.Source=="Sessionize").FirstOrDefaultAsync();
        }

        public async Task<List<Conference>> GetConferencesAsync()
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            return await session.Query<Conference>().Where(c =>c.NumberOfDays<10 && c.EventEnd > DateOnly.FromDateTime(DateTime.Now)).ToListAsync();
        }

        public async Task<List<Conference>> GetConferencesAsync(DateOnly startdate, DateOnly enddate)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var list = await session.Query<Conference>()
                .Where(c => c.NumberOfDays<10
                &&
                (
                    (c.EventStart>=startdate && c.EventStart<=enddate) 
                    || 
                    ( c.EventEnd>=startdate && c.EventEnd <= enddate)
                    || 
                    (startdate >= c.EventStart && startdate <= c.EventEnd)
                )
                ).ToListAsync();
            //Some values in the database are wrong, so we need to check all the dates
            return list.Where(c =>(c.EventEnd.DayNumber-c.EventStart.DayNumber)<10).ToList();
        }

        public async Task<List<Conference>> GetConferencesAsync(double latitude, double longitude, double radius,DateOnly startdate, DateOnly enddate)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var list = await session.Query<Conference>()
            .Spatial(
                factory => factory.Point(x => x.Latitude, x => x.Longitude),
                criteria => criteria.WithinRadius(radius, latitude, longitude))
                .Where(c => c.NumberOfDays < 10
                &&
                (
                    (c.EventStart >= startdate && c.EventStart <= enddate)
                    ||
                    (c.EventEnd >= startdate && c.EventEnd <= enddate)
                    ||
                    (startdate >= c.EventStart && startdate <= c.EventEnd)
                )
                ).ToListAsync();
            //Some values in the database are wrong, so we need to check all the dates
            return list.Where(c => (c.EventEnd.DayNumber - c.EventStart.DayNumber) < 10).ToList();
        }

        public async Task<List<ConferenceSearchTerm>> SearchConferencesAsync(string searchterm)
            {
                using IAsyncDocumentSession session = _store.OpenAsyncSession();

                var query = session.Query<ConferenceSearchTerm>("ConferenceBySearchTerm")
                .Search(x => x.SearchTerm, searchterm, @operator: SearchOperator.And)
                .Where(c=>c.NumberOfDays < 10);
                return await query.ToListAsync();
            }

        public async Task<List<Conference>> SearchActiveConferencesAsync(bool hasOpenCallforPaper, string searchterm)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();

            var query = session.Query<ConferenceSearchTerm>("ConferenceBySearchTerm").AsQueryable();
            if (!string.IsNullOrEmpty(searchterm))
            {
                query = query.Search(c => c.SearchTerm, searchterm, @operator: SearchOperator.And);
            }

            if (hasOpenCallforPaper)
            {
                query = query.Where(c => c.CfpEndDate.HasValue && c.CfpEndDate.Value > DateTime.Now);
            }

            query = query.Where(c => c.EventEnd > DateOnly.FromDateTime(DateTime.Now) &&  c.NumberOfDays < 10);

            return await query.ProjectInto<Conference>().ToListAsync();
        }

        public async Task<List<ConferenceCountByDate>> GetConferenceCountByDate(DateOnly start, DateOnly end, string searchterm)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var query = session.Query<ConferenceCountByDate>("ConferenceCountByDates");
            if (!string.IsNullOrEmpty(searchterm))
            {
                query = query.Search(x => x.SearchTerm, searchterm, @operator: SearchOperator.And);
            }
            return await query.Where(c => c.Date >= start && c.Date <= end).ToListAsync();
        }

        public async ValueTask<ItemsProviderResult<Conference>> GetConferencesWithOpenCfpAsync(ItemsProviderRequest request)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var confs =
                await session.Query<Conference>()
                .Statistics(out var stats)
                .Where(c => c.CfpEndDate > DateTime.Now && c.NumberOfDays < 10)
                .OrderBy(c => c.CfpEndDate)
                .Skip(request.StartIndex)
                .Take(request.Count).ToListAsync();

            return new ItemsProviderResult<Conference>(confs, stats.TotalResults);
        }

        public async ValueTask<ItemsProviderResult<Conference>> GetConferencesAsync(ItemsProviderRequest request)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            var confs =
                await session.Query<Conference>()
                .Statistics(out var stats)
                .Where(c => c.EventStart > DateOnly.FromDateTime(DateTime.Now) && c.NumberOfDays < 10)
                .OrderBy(c => c.EventStart)
                .Skip(request.StartIndex)
                .Take(request.Count).ToListAsync();

            return new ItemsProviderResult<Conference>(confs, stats.TotalResults);
        }

        public async Task<List<ConferenceWeather>> GetConferencesByWeatherAsync(double averageTemp)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();

            return await session.Query<ConferenceWeather>("ConferencesByWeather").Where(c => c.Average > averageTemp).ToListAsync();
        }

        public async Task<TimeSeriesEntry<WeatherData>[]> GetWeatherTimeSeriesAsync(string conferenceId)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();

            TimeSeriesEntry<WeatherData>[] val = await session.TimeSeriesFor<WeatherData>(conferenceId).GetAsync();
            return val;
        }
    }
}
