namespace Its.Log.Monitoring
{
    public interface IApplyToTarget : IMonitoringTest
    {
        // FIX: (IApplyToTarget) make this async
        bool AppliesToTarget(TestTarget target);
    }
}