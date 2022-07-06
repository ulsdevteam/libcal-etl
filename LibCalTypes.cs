// Generated with https://quicktype.io

using Newtonsoft.Json;

namespace LibCalTypes;

public class EventsResponse
{
    [JsonProperty("events")]
    public List<Event> Events { get; set; }
}

public class Event
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("allday")]
    public bool AllDay { get; set; }

    [JsonProperty("start")]
    public DateTimeOffset Start { get; set; }

    [JsonProperty("end")]
    public DateTimeOffset End { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("url")]
    public Url Url { get; set; }

    [JsonProperty("location")]
    public Location Location { get; set; }

    [JsonProperty("campus")]
    public Campus Campus { get; set; }

    [JsonProperty("category")]
    public List<Category> Category { get; set; }

    [JsonProperty("owner")]
    public Owner Owner { get; set; }

    [JsonProperty("presenter")]
    public string Presenter { get; set; }

    [JsonProperty("calendar")]
    public Calendar Calendar { get; set; }

    [JsonProperty("seats")]
    public long? Seats { get; set; }

    [JsonProperty("registration")]
    public bool Registration { get; set; }

    [JsonProperty("has_registration_opened")]
    public bool HasRegistrationOpened { get; set; }

    [JsonProperty("has_registration_closed")]
    public bool HasRegistrationClosed { get; set; }

    [JsonProperty("physical_seats")]
    public long PhysicalSeats { get; set; }

    [JsonProperty("physical_seats_taken")]
    public long PhysicalSeatsTaken { get; set; }

    [JsonProperty("online_seats")]
    public long OnlineSeats { get; set; }

    [JsonProperty("online_seats_taken")]
    public long OnlineSeatsTaken { get; set; }

    [JsonProperty("seats_taken")]
    public long SeatsTaken { get; set; }

    [JsonProperty("wait_list")]
    public bool WaitList { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; }

    [JsonProperty("featured_image")]
    public Uri FeaturedImage { get; set; }

    [JsonProperty("geolocation")]
    public object Geolocation { get; set; }

    [JsonProperty("future_dates")]
    public List<FutureDate> FutureDates { get; set; }

    [JsonProperty("more_info")]
    public string MoreInfo { get; set; }

    [JsonProperty("setup_time")]
    public long SetupTime { get; set; }

    [JsonProperty("teardown_time")]
    public long TeardownTime { get; set; }

    [JsonProperty("online_user_id")]
    public long OnlineUserId { get; set; }

    [JsonProperty("zoom_email")]
    public string ZoomEmail { get; set; }

    [JsonProperty("online_meeting_id")]
    public string OnlineMeetingId { get; set; }

    [JsonProperty("online_host_url")]
    public Uri OnlineHostUrl { get; set; }

    [JsonProperty("online_join_url")]
    public Uri OnlineJoinUrl { get; set; }

    [JsonProperty("online_join_password")]
    public string OnlineJoinPassword { get; set; }

    [JsonProperty("online_provider")]
    public string OnlineProvider { get; set; }

    [JsonIgnore]
    public List<Registrant> Registrants { get; set; }
}

public class Calendar
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("public")]
    public Uri Public { get; set; }

    [JsonProperty("admin")]
    public Uri Admin { get; set; }
}

public class Campus
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Category
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonIgnore]
    public long EventId { get; set; }
}

public class Owner
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Location
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("type")]
    public long Type { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }
}

public class Url
{
    [JsonProperty("public")]
    public Uri Public { get; set; }

    [JsonProperty("admin")]
    public Uri Admin { get; set; }
}

public class FutureDate
{
    [JsonProperty("event_id")]
    public long FutureEventId { get; set; }

    [JsonProperty("start")]
    public DateTimeOffset Start { get; set; }
}

public class RegistrationsResponse
{
    [JsonProperty("event_id")]
    public long EventId { get; set; }

    [JsonProperty("registrants")]
    public List<Registrant> Registrants { get; set; }
}

public class Registrant
{
    [JsonProperty("booking_id")]
    public long BookingId { get; set; }

    [JsonProperty("registration_type")]
    public string RegistrationType { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("barcode")]
    public string Barcode { get; set; }

    [JsonProperty("phone")]
    public string Phone { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("registered_date")]
    public DateTimeOffset RegisteredDate { get; set; }

    [JsonProperty("attendance")]
    public string Attendance { get; set; }
}

public class AppointmentBooking
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("fromDate")]
    public DateTimeOffset FromDate { get; set; }

    [JsonProperty("toDate")]
    public DateTimeOffset ToDate { get; set; }

    [JsonProperty("firstName")]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    public string LastName { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("userId")]
    public long UserId { get; set; }

    [JsonProperty("location")]
    public string Location { get; set; }

    [JsonProperty("locationId")]
    public long LocationId { get; set; }

    [JsonProperty("group")]
    public string Group { get; set; }

    [JsonProperty("groupId")]
    public long GroupId { get; set; }

    [JsonProperty("categoryId")]
    public long CategoryId { get; set; }

    [JsonProperty("directions")]
    public string Directions { get; set; }

    [JsonProperty("cancelled")]
    public bool Cancelled { get; set; }

    [JsonProperty("answers")]
    [JsonConverter(typeof(AnswerConverter))]
    public List<QuestionAnswer> Answers { get; set; }
}

public class QuestionAnswer
{
    public long BookingId { get; set; }
    public long QuestionId { get; set; }
    public string Answer { get; set; }
}

class AnswerConverter : JsonConverter<List<QuestionAnswer>>
{
    public override List<QuestionAnswer> ReadJson(JsonReader reader, Type objectType,
        List<QuestionAnswer> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        /* Answers is either an object containing question ids and answers, or an empty array. For example:
           "answers": {
                "q20355": "Pennsylvania Resident",
                "q20356": "Growth and Establishment",
                "q20357": "Advertising and Promotional Material ",
                "q20358": "Industry Reports and Data, Consumer Demographics and Psychographics",
                "q40080": "-",
                "q20359": "Other"
            }
            
            OR
            
            "answers": []
        */
        if (reader.TokenType == JsonToken.StartArray)
        {
            // Expect the array to be empty, so skip its token, and return an empty list
            reader.Skip();
            return new List<QuestionAnswer>();
        }

        var answers = serializer.Deserialize<Dictionary<string, string>>(reader)
                      ?? throw new Exception("Failed to deserialize Answers object");
        var questionAnswers = answers.Select(x => new QuestionAnswer
        {
            QuestionId = long.Parse(x.Key.TrimStart('q')),
            Answer = x.Value
        });
        return hasExistingValue ? existingValue.Concat(questionAnswers).ToList() : questionAnswers.ToList();
    }

    public override void WriteJson(JsonWriter writer, List<QuestionAnswer> value, JsonSerializer serializer)
    {
        // This likely won't be called ever, but this should change the answers back to the API format if needed
        if (value is not null && value.Any())
        {
            serializer.Serialize(writer, value.ToDictionary(q => $"q{q.QuestionId}", q => q.Answer));
        }
        else
        {
            serializer.Serialize(writer, Array.Empty<object>());
        }
    }
}

public class AppointmentUser
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("first_name")]
    public string FirstName { get; set; }

    [JsonProperty("last_name")]
    public string LastName { get; set; }

    [JsonProperty("nickname")]
    public string Nickname { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("url")]
    public Uri Url { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }
}

public class AppointmentQuestion
{
    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("label")]
    public string Label { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("required")]
    public bool Required { get; set; }

    [JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(OptionConverter))]
    public List<QuestionOption> Options { get; set; }
}

public class QuestionOption
{
    public long QuestionId { get; set; }
    public string Option { get; set; }
}

class OptionConverter : JsonConverter<List<QuestionOption>>
{
    public override List<QuestionOption> ReadJson(JsonReader reader, Type objectType,
        List<QuestionOption> existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        // Options is just an array of strings, which is wrapped so it can be a database entity
        var options = serializer.Deserialize<List<string>>(reader) ??
                      throw new Exception("Failed to deserialize options");
        return options.Select(option => new QuestionOption { Option = option }).ToList();
    }

    public override void WriteJson(JsonWriter writer, List<QuestionOption> value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value?.Select(x => x.Option).ToList());
    }
}

public class SpaceBooking
{
    [JsonProperty("bookId")]
    public string BookId { get; set; }

    [JsonProperty("id")]
    public long Id { get; set; }

    [JsonProperty("eid")]
    public long ItemId { get; set; }

    [JsonProperty("cid")]
    public long CategoryId { get; set; }

    [JsonProperty("lid")]
    public long LocationId { get; set; }

    [JsonProperty("fromDate")]
    public DateTimeOffset FromDate { get; set; }

    [JsonProperty("toDate")]
    public DateTimeOffset ToDate { get; set; }

    [JsonProperty("created")]
    public DateTimeOffset Created { get; set; }

    [JsonProperty("firstName")]
    public string FirstName { get; set; }

    [JsonProperty("lastName")]
    public string LastName { get; set; }

    [JsonProperty("email")]
    public string Email { get; set; }

    [JsonProperty("account")]
    public string Account { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("location_name")]
    public string LocationName { get; set; }

    [JsonProperty("category_name")]
    public string CategoryName { get; set; }

    [JsonProperty("item_name")]
    public string ItemName { get; set; }

    [JsonProperty("nickname", NullValueHandling = NullValueHandling.Ignore)]
    public string Nickname { get; set; }

    [JsonProperty("cancelled", NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? Cancelled { get; set; }
}

public class ArchivedSpaceBooking
{
    public long Id { get; set; }
    public string BookingId { get; set; }
    public string SpaceId { get; set; }
    public string SpaceName { get; set; }
    public string Location { get; set; }
    public string Zone { get; set; }
    public string Category { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PublicNickname { get; set; }
    public string Account { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string EventId { get; set; }
    public string EventTitle { get; set; }
    public DateTime? EventStart { get; set; }
    public DateTime? EventEnd { get; set; }
    public string Status { get; set; }
    public string CancelledByUser { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string ShowedUp { get; set; }
    public DateTime? CheckedInDate { get; set; }
    public DateTime? CheckedOutDate { get; set; }
    public string Cost { get; set; }
    public string BookingFormAnswers { get; set; }
}