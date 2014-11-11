// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet. The original source is located in the Its.Log project (http://codebox/ItsLog). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Recipes
{
    #if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal class AssemblyVersionSensor
    {
        private static readonly Lazy<BuildInfo> buildInfo = new Lazy<BuildInfo>(() =>
        {
            var assembly = typeof (AssemblyVersionSensor).Assembly;

            var info = new BuildInfo
            {
                AssemblyName = assembly.GetName().Name,
                AssemblyFileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion,
                BuildVersion = assembly.GetName().Version.ToString(),
                BuildDate = new FileInfo(new Uri(assembly.CodeBase).LocalPath).CreationTimeUtc.ToString("o")
            };

            return info;
        });

        [Export("DiagnosticSensor")]
        public static IDictionary<string, object> Version()
        {
            return new Dictionary<string, object>
            {
                { "Assembly", buildInfo.Value.AssemblyName },
                { "Build version", buildInfo.Value.BuildVersion },
                { "Build date", buildInfo.Value.BuildDate },
                { "File version", buildInfo.Value.AssemblyFileVersion },
            };
        }

        private class BuildInfo
        {
            public string BuildVersion;
            public string BuildDate;
            public string AssemblyFileVersion { get; set; }
            public string AssemblyName { get; set; }
        }
    }
}