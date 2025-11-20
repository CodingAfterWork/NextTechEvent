using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using InterfaceGenerator;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using NextTechEvent.Client.Data;
using NextTechEvent.Client.Data.Index;
using NextTechEvent.Components;
using NextTechEvent.Data.Index;
using NPOI.SS.Formula.Functions;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Session.TimeSeries;
using System.Security.Claims;
using Telerik.SvgIcons;

namespace NextTechEvent.Data;


public partial class NextTechEventRepository
{
    IDocumentStore _store;
    IHttpClientFactory _factory;
    public NextTechEventRepository(IDocumentStore store, IHttpClientFactory factory)
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

    //public async Task<Status> SaveStatusAsync(Status status)
    //{
    //    var savedstatus = await GetStatusAsync(status.ConferenceId, status.UserId);
    //    if (savedstatus != null)
    //    {
    //        status.Id = savedstatus.Id;
    //    }

    //    using IAsyncDocumentSession session = _store.OpenAsyncSession();
    //    await session.StoreAsync(status);
    //    await session.SaveChangesAsync();
    //    _statuses = null;
    //    return status;
    //}

    public async Task<Settings> SaveSettingsAsync(Settings item)
    {
        if (item.Id == null)
        {
            item.Id = Guid.NewGuid().ToString().Replace("-", "");
        }

        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        await session.StoreAsync(item);
        await session.SaveChangesAsync();
        return item;
    }

    public async Task<Status?> GetStatusAsync(string conferenceId, ClaimsPrincipal user)
    {
        var userId = GetUserId(user);
        var statuses = await GetStatusesAsync(userId);
        return statuses.Where(c => c.ConferenceId == conferenceId && c.UserId == userId).FirstOrDefault();
    }

    List<Status>? _statuses;
    public async Task<List<Status>> GetStatusesAsync(string userId)
    {
        if (_statuses == null)
        {
            using IAsyncDocumentSession session = _store.OpenAsyncSession();
            _statuses = await session.Query<Status>().Where(c => c.UserId == userId).ToListAsync();
        }
        return _statuses ?? new();
    }

    public async Task<Settings?> GetSettingsAsync(string id)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        return await session.Query<Settings>().Where(c => c.Id == id).FirstOrDefaultAsync();
    }

    public async Task<Settings?> GetSettingsByUserIdAsync(string userId)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        var settings = await session.Query<Settings>().Where(c => c.UserId == userId).FirstOrDefaultAsync();

        return settings;
    }

    //public async Task UpdateStatusBasedOnSessionizeCalendarAsync(Settings settings)
    //{
    //    var calendarcontent = await _factory.CreateClient().GetStringAsync(settings.SessionizeCalendarUrl);
    //    var sessionizecalendar = Ical.Net.Calendar.Load(calendarcontent);
    //    foreach (CalendarEvent item in sessionizecalendar.Events.Where(i => i.Uid.StartsWith("SZEVENT")))
    //    {
    //        var eventId = item.Uid.Replace("SZEVENT", "");
    //        var conf = await GetConferenceBySessionizeIdAsync(eventId);
    //        if (conf == null || conf.Id == null)
    //            continue;
    //        var status = await GetStatusAsync(conf.Id, settings.UserId);
    //        var state = GetStateFromCalendarEvent(item);
    //        if (status == null || (status != null && status.State != state))
    //        {
    //            if (status == null)
    //            {


    //                status = new()
    //                {
    //                    ConferenceId = conf.Id,
    //                    UserId = settings.UserId,
    //                    State = state
    //                };

    //            }
    //            status.State = state;

    //            await SaveStatusAsync(status);
    //        }
    //    }

    //}

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

    private string GetUserId(ClaimsPrincipal userClaims)
    {
        return userClaims?.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ?? "";
    }

    public async Task<List<Conference>> GetConferencesByUserAsync(ClaimsPrincipal userClaims)
    {
        var userId = GetUserId(userClaims);
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        var data = await session.Query<Status>()
            .Include(c => c.ConferenceId)
            .Where(c => c.UserId == userId && c.State != StateEnum.NotSet && c.State != StateEnum.Rejected)
            .ToListAsync();


        List<Conference> result = new();
        foreach (var c in data)
        {
            var conference = await GetConferenceUserStatus(session, c.ConferenceId, c.State);
            if (conference != null)
            {
                result.Add(conference);
            }
        }
        return result;
    }

    private async Task<Conference> GetConferenceUserStatus(IAsyncDocumentSession session, string conferenceId, StateEnum state)
    {
        var conference = await session.LoadAsync<Conference>(conferenceId);
        return conference;
    }

    public async Task<Ical.Net.Calendar> GetUserCalendarAsync(string userId)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        var data = await session.Query<Status>()
            .Include(c => c.ConferenceId)
            .Where(c => c.UserId == userId && c.State != StateEnum.NotSet && c.State != StateEnum.Rejected)
            .ToListAsync();

        var calendar = new Ical.Net.Calendar();
        calendar.Method = Ical.Net.CalendarMethods.Publish;
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
                //IsAllDay = true,
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
        var conf = await session.Query<Conference>().Where(c => c.Id == id).FirstOrDefaultAsync();
        return conf;
    }

    public async Task<Conference> GetConferenceBySessionizeIdAsync(string sessionizeId)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        return await session.Query<Conference>().Where(c => c.Identifier == sessionizeId && c.Source == "Sessionize").FirstOrDefaultAsync();
    }

    public async Task<List<Conference>> GetConferencesAsync()
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        return await session.Query<Conference>().Where(c => c.NumberOfDays < 10 && c.EventEnd > DateOnly.FromDateTime(DateTime.Now)).ToListAsync();
    }

public async Task<ConferenceCounts> GetConferenceCountsAsync()
{
    using var session = _store.OpenAsyncSession();

    var now = DateTime.UtcNow;
    var today = DateOnly.FromDateTime(now);

    var openCfpFacet = new RangeFacet<ConferencesByDatesAndCounts.Result>
    {
        DisplayFieldName = "OpenCfp",
        Ranges = { x => x.CfpEndDate > now }
    };

    var upcomingFacet = new RangeFacet<ConferencesByDatesAndCounts.Result>
    {
        DisplayFieldName = "Upcoming",
        Ranges = { x => x.EventStart > today }
    };

    var new7Facet = new RangeFacet<ConferencesByDatesAndCounts.Result>
    {
        DisplayFieldName = "NewLast7Days",
        Ranges = { x => x.CreateDate > now.AddDays(-7) }
    };

    var new30Facet = new RangeFacet<ConferencesByDatesAndCounts.Result>
    {
        DisplayFieldName = "NewLast30Days",
        Ranges = { x => x.CreateDate > now.AddDays(-30) }
    };

    var results = await session.Query<ConferencesByDatesAndCounts.Result, ConferencesByDatesAndCounts>()
        .Where(x => x.EventEnd > today)
        .AggregateBy(openCfpFacet)
        .AndAggregateBy(upcomingFacet)
        .AndAggregateBy(new7Facet)
        .AndAggregateBy(new30Facet)
        .ExecuteAsync();

    long Get(string key) => results[key].Values.FirstOrDefault()?.Count ?? 0;

    return new ConferenceCounts(
        OpenCfp: Get("OpenCfp"),
        Upcoming: Get("Upcoming"),
        NewLast7Days: Get("NewLast7Days"),
        NewLast30Days: Get("NewLast30Days")
    );
}

public async Task<List<Conference>> GetConferencesAsync(DateOnly startdate, DateOnly enddate)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        var list = await session.Query<Conference>()
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

    public async Task<List<Conference>> GetConferencesAsync(double latitude, double longitude, double radius, DateOnly startdate, DateOnly enddate)
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
        .Where(c => c.NumberOfDays < 10);
        return await query.ToListAsync();
    }

    public async Task<List<Conference>> SearchActiveConferencesAsync(bool hasOpenCallforPaper, string searchterm, int pagesize, int page)
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

        query = query.Where(c => c.EventEnd > DateOnly.FromDateTime(DateTime.Now) && c.NumberOfDays < 10);


        if (hasOpenCallforPaper)
        {
            query = query.OrderBy(c => c.CfpEndDate);
        }
        else
        {
            query = query.OrderBy(c => c.EventStart);
        }

        return await query.Skip(page * pagesize).Take(pagesize).ProjectInto<Conference>().ToListAsync();
    }

    public async Task<List<ConferenceCountByDate>> GetConferenceCountByDate(DateOnly start, DateOnly end, string searchterm)
    {
        using IAsyncDocumentSession session = _store.OpenAsyncSession();
        var query = session.Query<ConferenceCountByDate>("ConferenceCountByDates");
        if (!string.IsNullOrEmpty(searchterm))
        {
            if (searchterm.Contains(","))
            {
                var terms = searchterm.Split(',');
                foreach (var term in terms)
                {
                    query = query.Search(x => x.SearchTerm, term, @operator: SearchOperator.And);
                }
            }
            else
            {
                query = query.Search(x => x.SearchTerm, searchterm, @operator: SearchOperator.And);
            }
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

        return new ItemsProviderResult<Conference>(confs, (int)stats.TotalResults);
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

        return new ItemsProviderResult<Conference>(confs, (int)stats.TotalResults);
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
