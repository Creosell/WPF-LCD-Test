#if DEBUG
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// Wraps a service in a proxy that traces every call of its interface: the method, the arguments, the result (or the exception)
    /// and the time; an asynchronous method is traced again when its task completes. Reading of properties and event subscriptions are not traced.
    /// </summary>
    public class TracingProxy : DispatchProxy
        {
        private object _target = null!;
        private string _interfaceName = "";

        /// <summary>
        /// Wraps the service.
        /// </summary>
        /// <typeparam name="T">The interface of the service.</typeparam>
        /// <param name="target">The service.</param>
        /// <returns>The traced service that forwards every call to <paramref name="target"/>.</returns>
        public static T Wrap<T>(T target) where T : class
            {
            var proxy = Create<T, TracingProxy>();
            var tracing = (TracingProxy)(object)proxy;
            tracing._target = target;
            tracing._interfaceName = typeof(T).Name;
            return proxy;
            }

        /// <inheritdoc />
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
            if (targetMethod == null) return null;

            bool silent = targetMethod.Name.StartsWith("get_", StringComparison.Ordinal)
                || targetMethod.Name.StartsWith("add_", StringComparison.Ordinal)
                || targetMethod.Name.StartsWith("remove_", StringComparison.Ordinal);
            string call = $"{_interfaceName}.{targetMethod.Name}({FormatArguments(args)})";
            if (!silent) DebugTrace.Write("call", call);

            var clock = Stopwatch.StartNew();
            object? result;
            try
                {
                result = targetMethod.Invoke(_target, args);
                }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                DebugTrace.Write("error", $"{call} threw {DebugTrace.Format(ex.InnerException)}");
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
                }

            if (silent) return result;

            if (result is Task task)
                {
                task.ContinueWith(
                    t => DebugTrace.Write(t.IsFaulted ? "error" : "call", $"{call} {Outcome(t)} in {clock.ElapsedMilliseconds} ms"),
                    TaskContinuationOptions.ExecuteSynchronously);
                }
            else if (targetMethod.ReturnType != typeof(void))
                {
                DebugTrace.Write("call", $"{call} -> {DebugTrace.Format(result)} in {clock.ElapsedMilliseconds} ms");
                }

            return result;
            }

        private static string Outcome(Task task)
            {
            if (task.IsFaulted) return $"failed: {DebugTrace.Format(task.Exception?.GetBaseException())}";
            if (task.IsCanceled) return "canceled";

            var resultProperty = task.GetType().IsGenericType ? task.GetType().GetProperty("Result") : null;
            return resultProperty == null ? "completed" : $"completed -> {DebugTrace.Format(resultProperty.GetValue(task))}";
            }

        private static string FormatArguments(object?[]? args)
            {
            if (args == null || args.Length == 0) return "";

            var text = new StringBuilder();
            foreach (var arg in args)
                {
                if (text.Length > 0) text.Append(", ");
                text.Append(DebugTrace.Format(arg));
                }

            return text.ToString();
            }
        }
    }
#endif
