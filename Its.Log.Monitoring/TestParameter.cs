namespace Its.Log.Monitoring
{
    internal class TestParameter
    {
        public string Name { get; private set; }
        public object DefaultValue { get; private set; }

        public TestParameter(string name, object defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}