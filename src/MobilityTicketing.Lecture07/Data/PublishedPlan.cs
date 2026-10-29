namespace MobilityTicketing.Lecture07.Data;

public class PublishedPlan
{
    public string Id { get; set; } = "";
    public DateTime FromUtc { get; set; }
    public DateTime ToUtc { get; set; }
    public DateTime PublishedAtUtc { get; set; }
    public virtual List<PublishedPlanRoute> Routes { get; set; } = [];
}