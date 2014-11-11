namespace Its.Log.Monitoring
{
    public interface IApplyToEnvironment : IMonitoringTest
    {
        bool AppliesToEnvironment(string environment);
    }
}