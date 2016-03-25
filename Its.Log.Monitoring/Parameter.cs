using System;
using static System.String;

namespace Its.Log.Monitoring
{
    internal class Parameter
    {
        public string Name { get; private set; }
        public object DefaultValue { get; private set; }

        public Parameter(string name, object defaultValue)
        {
            if (IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Argument is null or whitespace", nameof(name));
            }
            Name = name;
            DefaultValue = defaultValue;
        }
    }
}