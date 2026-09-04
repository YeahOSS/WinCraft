using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WinCraft.Infrastructure
{
    [SuppressMessage("SonarAnalyzer.CSharp", "S3011", Justification = "Required for internal API access and cross-framework compatibility.")]
    internal static class ReflectionHelper
    {
        public static bool TryGetNonPublicProperty<T>(this object instance, string name, out T value) =>
            instance.TryGetNonPublicProperty(instance?.GetType(), name, out value);

        public static bool TryGetNonPublicProperty<T>(this object instance, Type type, string name, out T value)
        {
            value = default;
            if (instance == null || type == null)
                return false;

            var property = type.GetProperty(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
                return false;

            try
            {
                var result = property.GetValue(instance, null);
                if (result is T typedResult)
                {
                    value = typedResult;
                    return true;
                }

                return result == null;
            }
            catch (TargetInvocationException)
            {
                return false;
            }
            catch (MethodAccessException)
            {
                return false;
            }
        }

        public static bool TryGetNonPublicField<T>(this object instance, string name, out T value) =>
            instance.TryGetNonPublicField(instance?.GetType(), name, out value);

        public static bool TryGetNonPublicField<T>(this object instance, Type type, string name, out T value)
        {
            value = default;
            if (instance == null || type == null)
                return false;

            var field = type.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return false;

            try
            {
                var result = field.GetValue(instance);
                if (result is T typedResult)
                {
                    value = typedResult;
                    return true;
                }

                return result == null;
            }
            catch (TargetInvocationException)
            {
                return false;
            }
            catch (MethodAccessException)
            {
                return false;
            }
        }

        public static bool TryInvokeStaticMethod(this Type type, string name, params object[] arguments) =>
            TryInvokeMethod(null, type, BindingFlags.Static, name, arguments);

        public static bool TryInvokeStaticMethod(
            this Assembly assembly, string typeName, string methodName, params object[] arguments)
        {
            if (assembly == null)
                return false;
            var type = assembly.GetType(typeName);
            return type != null && type.TryInvokeStaticMethod(methodName, arguments);
        }

        public static bool TrySetNonPublicStaticField(this Type type, string name, object value)
        {
            if (type == null)
                return false;

            var field = type.GetField(
                name,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
                return false;

            try
            {
                field.SetValue(null, value);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (FieldAccessException)
            {
                return false;
            }
        }

        public static bool TryInvokeInstanceMethod(
            this object instance, string name, params object[] arguments) =>
            TryInvokeMethod(instance, instance?.GetType(), BindingFlags.Instance, name, arguments);

        private static bool TryInvokeMethod(
            object target, Type type, BindingFlags flags, string name, object[] arguments)
        {
            if (type == null)
                return false;

            foreach (var method in type.GetMethods(
                flags | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name != name || method.GetParameters().Length != arguments.Length)
                    continue;

                try
                {
                    method.Invoke(target, arguments);
                    return true;
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            return false;
        }
    }
}
