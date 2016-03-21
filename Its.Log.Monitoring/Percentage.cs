using System;

namespace Its.Log.Monitoring
{
    public class Percentage : IComparable<Percentage>
    {
        private int value;

        public Percentage(int value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return string.Format("{0}%", value);
        }

        public int CompareTo(Percentage other)
        {
            return value.CompareTo(other.value);
        }
    }
}