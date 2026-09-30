using System.Globalization;
using Flurl.Http;
using LibCalTypes;
using Newtonsoft.Json.Linq;
using Flurl.Http.Newtonsoft;

class LibCalClient
{
    public LibCalClient()
    {
        Client = new FlurlClient("https://pitt.libcal.com");
        Client.Settings.JsonSerializer = new NewtonsoftJsonSerializer();
    }

    IFlurlClient Client { get; }

    /// <summary>
    /// Send a client id and secret to get an access token and save it to the client.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="clientSecret"></param>
    /// <exception cref="Exception"></exception>
    public async Task Authorize(string clientId, string clientSecret)
    {
        var response = await Client.Request("/1.1/oauth/token")
            .PostUrlEncodedAsync(new
            {
                client_id = clientId,
                client_secret = clientSecret,
                grant_type = "client_credentials"
            }).ReceiveJson<JObject>();
        var accessToken = response["access_token"]?.ToString() ??
                          throw new Exception("Auth response missing access token.");
        Client.WithOAuthBearerToken(accessToken);
    }

    /// <summary>
    /// Get all calendar ids.
    /// </summary>
    /// <returns></returns>
    public async Task<List<int>> GetCalendarIds()
    {
        var response = await Client.Request("/1.1/calendars")
            .GetJsonAsync<JObject>();
        return response["calendars"].Select(cal => (int)cal["calid"]).ToList();
    }

    /// <summary>
    /// Get events from a calendar between two given dates, inclusive.
    /// </summary>
    /// <param name="calendarId"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <returns></returns>
    public async Task<List<Event>> GetEvents(int calendarId, DateTime fromDate, DateTime toDate)
    {
        return await GetInDateInterval<EventsResponse, Event>(
            Client.Request("1.1/events").SetQueryParam("cal_id", calendarId),
            fromDate,
            toDate,
            resp => resp.Events);
    }

    /// <summary>
    /// Given a list of event ids, gets registrant info for those events.
    /// </summary>
    /// <param name="eventIds">
    /// List of event ids.
    /// Unclear from the API docs what the upper limit is on the number of ids per call, but a single
    /// request with ~12,000 characters of concatenated ids (about 1300 events) got back an HTTP 414
    /// (URI Too Long), so this pages the ids into chunks that keep the URL well under that.
    /// </param>
    /// <returns></returns>
    public async Task<List<RegistrationsResponse>> GetRegistrations(IEnumerable<long> eventIds)
    {
        var results = new List<RegistrationsResponse>();
        foreach (var chunk in ChunkIdsByLength(eventIds))
        {
            results.AddRange(await Client.Request("api/1.1/events", string.Join(',', chunk), "registrations")
                .GetJsonAsync<List<RegistrationsResponse>>());
        }

        return results;
    }

    /// <summary>
    /// Get details for an Appointments user.
    /// </summary>
    /// <returns></returns>
    public async Task<AppointmentUser> GetAppointmentUser(long userId)
    {
        return await Client.Request("/1.1/appointments")
            .SetQueryParam("user_id", userId)
            .GetJsonAsync<AppointmentUser>();
    }

    /// <summary>
    /// Gets all appointment bookings between two dates, inclusive.
    /// </summary>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <returns></returns>
    public Task<List<AppointmentBooking>> GetAppointmentBookings(DateTime fromDate, DateTime toDate)
    {
        return GetInDateInterval<AppointmentBooking>(Client.Request("/1.1/appointments/bookings"), fromDate, toDate);
    }

    /// <summary>
    /// Get the questions associated with an appointment. Ids are chunked the same way as
    /// <see cref="GetRegistrations"/>, for the same reason - this builds the same kind of
    /// comma-joined URL segment and so is susceptible to the same HTTP 414.
    /// </summary>
    /// <param name="questionIds"></param>
    /// <returns></returns>
    public async Task<List<AppointmentQuestion>> GetAppointmentQuestions(IEnumerable<long> questionIds)
    {
        var results = new List<AppointmentQuestion>();
        foreach (var chunk in ChunkIdsByLength(questionIds))
        {
            results.AddRange(await Client.Request("/api/1.1/appointments/question", string.Join(',', chunk))
                .GetJsonAsync<List<AppointmentQuestion>>());
        }

        return results;
    }

    /// <summary>
    /// Get the space bookings for a given date interval.
    /// </summary>
    /// <returns></returns>
    public Task<List<SpaceBooking>> GetSpaceBookings(DateTime fromDate, DateTime toDate, string locationId)
    {
        var request = Client.Request("/1.1/space/bookings");
        if (!string.IsNullOrEmpty(locationId))
        {
            request.SetQueryParam("lid", locationId);
        }
        return GetInDateIntervalPaged<SpaceBooking>(request, fromDate, toDate);
    }

        /// <summary>
    /// Splits a list of ids into chunks whose comma-joined string representation stays under
    /// <paramref name="maxLength"/> characters, rather than a fixed count - id values could in
    /// principle vary enough in digit length that a fixed-count batch either overshoots the URL
    /// length limit or (for small ids) sends far more, smaller requests than it needs to.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="maxLength">
    /// Kept comfortably under the ~2000-2048 character URL length some servers/proxies enforce -
    /// this bounds only the joined id-list portion, so the fixed parts of the URL (host, path) still
    /// have headroom on top of it.
    /// </param>
    static IEnumerable<List<long>> ChunkIdsByLength(IEnumerable<long> ids, int maxLength = 2048)
    {
        var chunk = new List<long>();
        var chunkLength = 0;
        foreach (var id in ids)
        {
            var idText = id.ToString(CultureInfo.InvariantCulture);
            // +1 accounts for the comma that will separate this id from the previous one in the chunk
            var addedLength = idText.Length + (chunk.Count > 0 ? 1 : 0);
            if (chunk.Count > 0 && chunkLength + addedLength > maxLength)
            {
                yield return chunk;
                chunk = new List<long>();
                addedLength = idText.Length;
                chunkLength = 0;
            }

            chunk.Add(id);
            chunkLength += addedLength;
        }

        if (chunk.Count > 0) { yield return chunk; }
    }

    /// <summary>
    /// Get all data between two given dates by splitting them into periods.
    /// Each individual call has a limit of 500, so if you are hitting that limit, try reducing the period.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="mapFunc"></param>
    /// <param name="periodLengthInDays"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    static async Task<List<TResult>> GetInDateInterval<TResponse, TResult>(IFlurlRequest request, DateTime fromDate,
        DateTime toDate, Func<TResponse, IEnumerable<TResult>> mapFunc, int periodLengthInDays = 30)
    {
        var results = new List<TResult>();
        var currentDate = fromDate;
        do
        {
            // Period length -1 because otherwise it would double-count some days
            var days = Math.Min((toDate - currentDate).Days, periodLengthInDays - 1);
            var response = await request
                .SetQueryParams(new
                {
                    date = currentDate.ToString("yyyy-M-d"),
                    days,
                    limit = 500
                })
                .GetJsonAsync<TResponse>();
            results.AddRange(mapFunc(response));
            currentDate = currentDate.AddDays(periodLengthInDays);
        } while (currentDate < toDate);

        return results;
    }

    /// <summary>
    /// Get all data between two given dates by splitting them into periods.
    /// Each individual call has a limit of 500, so if you are hitting that limit, try reducing the period.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="periodLengthInDays"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    static Task<List<TResponse>> GetInDateInterval<TResponse>(IFlurlRequest request, DateTime fromDate, DateTime toDate,
        int periodLengthInDays = 30)
    {
        return GetInDateInterval<List<TResponse>, TResponse>(request, fromDate, toDate, x => x, periodLengthInDays);
    }

    /// <summary>
    /// Get all data between two given dates using pagination.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <param name="mapFunc"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    static async Task<List<TResult>> GetInDateIntervalPaged<TResponse, TResult>(IFlurlRequest request, DateTime fromDate,
        DateTime toDate, Func<TResponse, IEnumerable<TResult>> mapFunc)
    {
        var results = new List<TResult>();
        var page = 1;
        while (true) {
            var response = await request
                .SetQueryParams(new
                {
                    date = fromDate.ToString("yyyy-M-d"),
                    days = (toDate - fromDate).Days,
                    page,
                    limit = 500
                })
                .GetJsonAsync<TResponse>();
            var resultCount = results.Count;
            results.AddRange(mapFunc(response));
            if (resultCount == results.Count) {
                // no new results were returned, i.e. no more pages.
                return results;
            }
            page++;
        }  
    }

    /// <summary>
    /// Get all data between two given dates using pagination.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="fromDate"></param>
    /// <param name="toDate"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    static Task<List<TResponse>> GetInDateIntervalPaged<TResponse>(IFlurlRequest request, DateTime fromDate, DateTime toDate)
    {
        return GetInDateIntervalPaged<List<TResponse>, TResponse>(request, fromDate, toDate, x => x);
    }
}