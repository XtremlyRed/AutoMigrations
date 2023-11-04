using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BF = System.Reflection.BindingFlags;

namespace AutoMigrations.Extensions
{
    internal static class InvokeExtensions
    {
        public static object? InvokeStatic<TType>(string methodName, params object[] parameters)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException(nameof(methodName));
            }

            Type type = typeof(TType);

            IEnumerable<MethodInfo> methods = type.GetMethods(
                    BF.Instance | BF.Public | BF.NonPublic | BF.Static
                )
                .Where(i => string.Compare(i.Name, methodName, true) == 0);

            var parametersTypes = parameters?.Select(i => i.GetType()).ToArray();

            if (parametersTypes != null && parametersTypes.Length > 0)
            {
                methods = methods.Where(i =>
                {
                    var ps = i.GetParameters();

                    if (ps.Length != parametersTypes.Length)
                    {
                        return false;
                    }

                    var result = false;

                    for (int item = 0, length = ps.Length; item < length; item++)
                    {
                        if (
                            (ps[item].ParameterType == parametersTypes[item])
                            || ps[item].ParameterType.IsAssignableFrom(parametersTypes[item])
                        )
                        {
                            result = true;
                            continue;
                        }

                        return false;
                    }

                    return result;
                });
            }

            var method = methods.FirstOrDefault();

            if (method is null)
            {
                throw new InvalidOperationException($"method:{methodName} not found");
            }

            return method.Invoke(null, parameters);
        }

        public static object? Invoke(this object obj, string methodName, params object[] parameters)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException(nameof(methodName));
            }

            Type type = obj.GetType();

            var methods = type.GetMethods(BF.Instance | BF.Public | BF.NonPublic);

            var parametersTypes = parameters?.Select(i => i.GetType());

            var method = methods.FirstOrDefault(
                i =>
                    i.Name == methodName
                    && Enumerable.SequenceEqual(
                        i.GetParameters().Select(i => i.ParameterType),
                        parametersTypes!
                    )
            );

            if (method is null)
            {
                throw new InvalidOperationException($"method:{methodName} not found");
            }

            return method.Invoke(obj, parameters);
        }
    }
}
