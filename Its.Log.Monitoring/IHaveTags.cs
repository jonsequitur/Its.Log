namespace Its.Log.Monitoring
{
    public interface IHaveTags : IMonitoringTest
    {
        string[] Tags { get; }
    }
}