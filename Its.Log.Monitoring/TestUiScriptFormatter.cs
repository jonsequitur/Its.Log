// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;

namespace Its.Log.Monitoring
{
    internal class TestUiScriptFormatter : MediaTypeFormatter
    {
        private readonly string bootstrapHtml;

        public TestUiScriptFormatter(string scriptUrl, IEnumerable<string> libraryUrls)
        {
            var version = FileVersionInfo.GetVersionInfo(typeof (TestUiScriptFormatter).Assembly.Location).FileVersion;
            var libraryScriptRefs = string.Join("\n", libraryUrls.Select(u => $@"<script src=""{u}""></script>"));

            bootstrapHtml =
                $@"<!doctype html>
<html lang=""en"">
    <head>
	    <meta charset=""UTF-8"">
        {libraryScriptRefs}
	    <script src=""{scriptUrl}?monitoringVersion={version}""></script>
    </head>
    <body>
    </body>
</html>";

            MediaTypeMappings.Add(
                new RequestHeaderMapping("Accept",
                                         "text/html",
                                         StringComparison.InvariantCultureIgnoreCase,
                                         false,
                                         "text/html"));
        }

        public override bool CanReadType(Type type) => false;

        public override bool CanWriteType(Type type) => true;

        public override async Task WriteToStreamAsync(
            Type type,
            object value,
            Stream writeStream,
            HttpContent content,
            TransportContext transportContext,
            CancellationToken cancellationToken)
        {
            var writer = new StreamWriter(writeStream);
            await writer.WriteAsync(bootstrapHtml);
            await writer.FlushAsync();
        }
    }
}