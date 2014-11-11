using System.Net.Http;
using System.Net.Http.Formatting;

namespace Its.Log.Monitoring.UnitTests
{
    internal class JsonContent : ObjectContent
    {
        public JsonContent(object value) : base(value.GetType(), value, new JsonMediaTypeFormatter())
        {
        }
    }
}