namespace Its.Log.Monitoring
{
    public interface IApplyToApplication : IMonitoringTest
    {
        bool AppliesToApplication(string application);
    }
}