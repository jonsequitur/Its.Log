using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using Its.Recipes;

namespace Its.Log.Monitoring
{
    internal class TestUiScriptFormatter : MediaTypeFormatter
    {
        private readonly string bootstrapHtml;

        public TestUiScriptFormatter(string scriptUrl)
        {
            bootstrapHtml =
                @"<!doctype html>
<html lang=""en"">
    <head>
	    <meta charset=""UTF-8"">
	    <script src=""{scriptUrl}?monitoringVersion={version}""></script>
    </head>
    <body>
    </body>
</html>".Fill(new
                {
                    scriptUrl,
                    version = FileVersionInfo.GetVersionInfo(typeof (TestUiScriptFormatter).Assembly.Location).FileVersion
                });

            MediaTypeMappings.Add(
                new RequestHeaderMapping("Accept",
                                         "text/html",
                                         StringComparison.InvariantCultureIgnoreCase,
                                         false,
                                         "text/html"));
        }

        public override bool CanReadType(Type type)
        {
            return false;
        }

        public override bool CanWriteType(Type type)
        {
            return true;
        }

        public override Task WriteToStreamAsync(Type type,
                                                object value,
                                                Stream writeStream,
                                                HttpContent content,
                                                TransportContext transportContext,
                                                CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var writer = new StreamWriter(writeStream);
                writer.Write(bootstrapHtml);
                writer.Flush();
            });
        }
    }
}