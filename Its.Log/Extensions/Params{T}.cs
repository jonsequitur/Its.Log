using System;
using System.IO;

namespace Its.Log.Instrumentation.Extensions
{
    /// <summary>
    /// Represents additional parameters to be written.
    /// </summary>
    /// <typeparam name="TAnonymous">The type of an object (typically anonymous) that captures the variables to be logged.</typeparam>
    public class Params<TAnonymous> : Params
    {
        private Func<TAnonymous> paramsAccessor;
        private static Action<Params<TAnonymous>, TextWriter> formatter;

        static Params()
        {
            Formatter.Clearing += (o, e) => RegisterFormatters();
            RegisterFormatters();
        }

        private static void RegisterFormatters()
        {
            if (ShouldGenerateFormatter())
            {
                Formatter<TAnonymous>.RegisterForMembers();
            }
            formatter = (p, writer) => writer.Write(p.paramsAccessor().ToLogString());
            Formatter<Params<TAnonymous>>.Default = formatter;
        }

        private static bool ShouldGenerateFormatter()
        {
            if (Formatter<TAnonymous>.IsCustom)
            {
                return false;
            }

            if (typeof(TAnonymous).IsArray)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the parameter values.
        /// </summary>
        /// <remarks><see cref="Values" /> returns the current parameter vales at the time the method is called. These values may have been changed since the <see cref="Params{TAnonymous}" /> instance or containing <see cref="LogEntry" /> was created.</remarks>
        public object Values
        {
            get
            {
                return paramsAccessor();
            }
        }

        internal override Delegate ParamsAccessor
        {
            get
            {
                return paramsAccessor;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.ToLogString();
        }

        internal void SetAccessor(Func<TAnonymous> accessor)
        {
            paramsAccessor = accessor;
        }
    }
}