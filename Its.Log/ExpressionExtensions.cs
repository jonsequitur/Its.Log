using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Its.Log.Instrumentation
{
    internal static class ExpressionExtensions
    {
        public static string MemberName<T, TValue>(this Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException("expression");
            }

            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression != null)
            {
                return memberExpression.Member.Name;
            }

            // when the return type of the expression is a value type, it contains a call to Convert, resulting in boxing, so we get a UnaryExpression instead
            var unaryExpression = expression.Body as UnaryExpression;
            if (unaryExpression != null)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
                if (memberExpression != null)
                {
                    return memberExpression.Member.Name;
                }
            }

            throw new ArgumentException(string.Format("Expression {0} does not specify a member.", expression));
        }

        public static MemberAccessor<T>[] GetMemberAccessors<T>(this MemberInfo[] forMembers)
        {
            return forMembers
                .Select(m => new MemberAccessor<T>(m))
                .ToArray();
        }

        public static IEnumerable<MemberInfo> GetAllMembers(this Type type, bool includeInternals = false)
        {
            var bindingFlags = includeInternals
                                   ? BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.NonPublic
                                   : BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public;

            return type.GetMembers(bindingFlags)
                .Where(m => !m.Name.Contains("<") && !m.Name.Contains("k__BackingField"))
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
                .Where(m => m.MemberType != MemberTypes.Property ||
                            (((PropertyInfo) m).CanRead && !((PropertyInfo) m).GetIndexParameters().Any()))
                .ToArray();
        }

        public static IEnumerable<MemberInfo> GetMembers<T>(
            this Type type,
            params Expression<Func<T, object>>[] forProperties)
        {
            var allMembers = typeof (T).GetAllMembers(true).ToArray();

            if (forProperties == null || !forProperties.Any())
            {
                return allMembers;
            }

            return
                forProperties
                    .Select(p =>
                    {
                        var memberName = p.MemberName();
                        return allMembers.Single(m => m.Name == memberName);
                    });
        }

    }
}