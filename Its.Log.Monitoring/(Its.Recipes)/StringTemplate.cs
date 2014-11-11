// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet from the Its.Recipes project (http://codebox/ItsRecipes). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Its.Recipes
{
#if !RecipesProject
    [System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal static class StringTemplate
    {
        private static readonly Regex ParamsRegex = new Regex(
            @"{(?<key>[^{}:]*)(?:\:(?<format>.+))?}",
            RegexOptions.IgnoreCase
            | RegexOptions.Multiline
            | RegexOptions.CultureInvariant
            | RegexOptions.Compiled
            );

        /// <summary>
        ///     Detokenizes the specified message template, filling in bracketed strings with parameters.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="parameters">The parameters to fill into the template.</param>
        /// <returns>A string with the tokens replaced with values from the supplied dictionary.</returns>
        /// <remarks>The tokens should be surrouned by single curly braces, e.g. "The password must contain only the characters {allowed-characters}."</remarks>
        public static string Fill(this string template, object parameters)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }
            if (parameters == null)
            {
                return template;
            }

            var formattedMsg = new StringBuilder(template);

            var matches = ParamsRegex.Matches(template);
            foreach (Match match in matches)
            {
                var tokenName = match.Groups["key"].Captures[0].Value;
                var replacementTarget = match.Value;

                var value = GetValue(parameters, tokenName);

                var formatStr = match.Groups["format"].Success ? match.Groups["format"].Captures[0].Value : null;
                string formattedParam = null;
                if (!string.IsNullOrEmpty(formatStr))
                {
                    var formattableParamValue = value as IFormattable;
                    if (formattableParamValue != null)
                    {
                        formattedParam = formattableParamValue.ToString(formatStr, CultureInfo.CurrentCulture);
                    }
                }

                if (formattedParam == null)
                {
                    formattedParam = Format(value);
                }

                formattedMsg.Replace(replacementTarget, formattedParam);
            }

            return formattedMsg.ToString();
        }

        private static dynamic GetValue(dynamic parameters, string tokenName)
        {
            object value;
            var dictionary = parameters as IDictionary<string, object>;
            if (dictionary != null && dictionary.TryGetValue(tokenName, out value))
            {
                return value;
            }

            return DataBinder.Eval(parameters, tokenName);
        }

        private static string Format(object objectToFormat)
        {
            if (objectToFormat == null)
            {
                return "";
            }

            return objectToFormat.ToString();
        }
    }
}