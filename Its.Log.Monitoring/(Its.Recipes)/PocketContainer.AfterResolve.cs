// THIS FILE IS NOT INTENDED TO BE EDITED. 
// 
// It has been imported using NuGet from the Its.Recipes project (http://codebox/ItsRecipes). 
// 
// This file can be updated in-place using the Package Manager Console. To check for updates, run the following command:
// 
// PM> Get-Package -Updates

using System;
using System.Linq;

namespace Its.Recipes
{
    internal partial class PocketContainer
    {
        public PocketContainer AfterResolve<T>(Func<PocketContainer, T, T> then)
        {
            Func<Func<PocketContainer, object>> getRegistration = () => this.Where(f => f.Key == typeof (T))
                                                                            .Select(pair => pair.Value)
                                                                            .SingleOrDefault();

            var originalRegistration = getRegistration();

            if (originalRegistration == null)
            {
                // trigger the creation of a registration
                Resolve<T>();

                originalRegistration = getRegistration();
            }

            Register(typeof (T), c =>
            {
                var value = (T) originalRegistration(c);
                if (singletons.ContainsKey(typeof (T)))
                {
                    return value;
                }
                return then(c, value);
            });

            return this;
        }
    }
}