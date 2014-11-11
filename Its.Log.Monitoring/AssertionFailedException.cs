using System;

namespace Its.Log.Monitoring
{
    [Serializable]
    public class AssertionFailedException : Exception
    {
        public AssertionFailedException(string error) : base(error)
        {
        }
    }
}