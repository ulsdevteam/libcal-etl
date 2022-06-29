using Newtonsoft.Json;

namespace LibCalTypes;

public partial class Event
{
    [JsonIgnore]
    public List<Registrant> Registrants { get; set; }
}

public partial class Category
{
    [JsonIgnore]
    public long EventId { get; set; }
}

public class QuestionAnswer
{
    public long BookingId { get; set; }
    public long QuestionId { get; set; }
    public string Answer { get; set; }
}

public class QuestionOption
{
    public long QuestionId { get; set; }
    public string Option { get; set; }
}