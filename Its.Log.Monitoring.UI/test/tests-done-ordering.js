(function(mockTests) {
  if (typeof module === 'object') {
    module.exports = mockTests
  } else {
    var server = window.fakeServer
    server.getting('/tests/done/ordering')
          .succeedsWithJson(mockTests)
    server.getting('/tests/done/ordering/readmodels_are_caught_up')
          .succeedsWithJson({
            LastEventId: 45670,
            CaughtUpTo: 45666
          })
    server.getting('/tests/done/ordering/blob_storage_is_reachable')
          .succeedsWithJson({})
    server.getting('/tests/done/ordering/can-save-data-to-eap-storage')
          .succeedsWithJson({})
    server.getting('/tests/done/ordering/compute_only_call_works')
          .failsWithJson({
            Message: 'CTP timed out',
            Stack: ' at foobar.cs:41'
          })
    server.getting('/tests/done/ordering/basic_purchase-works')
          .failsWithJson({
            Message: 'CTP timed out',
            Stack: ' at foobar.cs:41'
          })
    server.getting('/tests/done/ordering/buggy_test')
          .failsWithJson({
            "Message": "An error has occurred.",
            "ExceptionMessage": "The 'ObjectContent`1' type failed to serialize the response body for content type 'application/json; charset=utf-8'.",
            "ExceptionType": "System.InvalidOperationException",
            "StackTrace": null,
            "InnerException": {
              "Message": "An error has occurred.",
              "ExceptionMessage": "This operation is only valid on generic types.",
              "ExceptionType": "System.InvalidOperationException",
              "StackTrace": "   at System.RuntimeType.GetGenericTypeDefinition()\r\n   at Newtonsoft.Json.Serialization.JsonArrayContract..ctor(Type underlyingType)\r\n   at Newtonsoft.Json.Serialization.DefaultContractResolver.CreateArrayContract(Type objectType)\r\n   at Newtonsoft.Json.Serialization.DefaultContractResolver.CreateContract(Type objectType)\r\n   at Newtonsoft.Json.Serialization.DefaultContractResolver.ResolveContract(Type type)\r\n   at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.SerializeDynamic(JsonWriter writer, IDynamicMetaObjectProvider value, JsonDynamicContract contract, JsonProperty member, JsonContainerContract collectionContract, JsonProperty containerProperty)\r\n   at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.SerializeDynamic(JsonWriter writer, IDynamicMetaObjectProvider value, JsonDynamicContract contract, JsonProperty member, JsonContainerContract collectionContract, JsonProperty containerProperty)\r\n   at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.Serialize(JsonWriter jsonWriter, Object value, Type objectType)\r\n   at Newtonsoft.Json.JsonSerializer.SerializeInternal(JsonWriter jsonWriter, Object value, Type objectType)\r\n   at System.Net.Http.Formatting.BaseJsonMediaTypeFormatter.WriteToStream(Type type, Object value, Stream writeStream, Encoding effectiveEncoding)\r\n   at System.Net.Http.Formatting.JsonMediaTypeFormatter.WriteToStream(Type type, Object value, Stream writeStream, Encoding effectiveEncoding)\r\n   at System.Net.Http.Formatting.BaseJsonMediaTypeFormatter.WriteToStreamAsync(Type type, Object value, Stream writeStream, HttpContent content, TransportContext transportContext, CancellationToken cancellationToken)\r\n--- End of stack trace from previous location where exception was thrown ---\r\n   at System.Runtime.CompilerServices.TaskAwaiter.ThrowForNonSuccess(Task task)\r\n   at System.Runtime.CompilerServices.TaskAwaiter.HandleNonSuccessAndDebuggerNotification(Task task)\r\n   at System.Web.Http.WebHost.HttpControllerHandler.<WriteBufferedResponseContentAsync>d__1b.MoveNext()"
            }
          })
  }
})({
  Tests: [{
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/readmodels_are_caught_up"
  }, {
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/blob_storage_is_reachable",
    Tags: ['configuration']
  }, {
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/compute_only_call_works"
  }, {
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/basic_purchase-works"
  }, {
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/buggy_test"
  }, {
    Environment: 'done',
    Application: 'ordering',
    Url: "/tests/done/ordering/can-save-data-to-storage",
    Tags: ['side-effecting', 'configuration']
  }]
})