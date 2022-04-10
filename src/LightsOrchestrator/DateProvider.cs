namespace LightsOrchestrator
{
    public interface IDateProvider
    {
        DateTime Now { get; }
    }

    public class DateProvider : IDateProvider
    {
        public DateTime Now { get { return DateTime.Now;}}
    }
}