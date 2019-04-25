using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Unbreakable;
using Unbreakable.Runtime;
using SharpLab.Runtime.Internal;

namespace SharpLab.Server.Execution {
    public static class IsolatedExecutorCore {
        public static unsafe ExecutionResultWithException Execute(Assembly assembly, Guid guardTokenGuid, int processId) {
            try {
                Console.SetOut(Output.Writer);
                InspectionSettings.CurrentProcessId = processId;

                var main = assembly.EntryPoint;
                using (new RuntimeGuardToken(guardTokenGuid).Scope(NewRuntimeGuardSettings())) {
                    var args = main.GetParameters().Length > 0 ? new object[] { new string[0] } : null;
                    var stackStart = stackalloc byte[1];
                    InspectionSettings.StackStart = (ulong)stackStart;
                    var result = main.Invoke(null, args);
                    if (main.ReturnType != typeof(void))
                        result.Inspect("Return");
                    return new ExecutionResultWithException(new ExecutionResult(Output.Stream, Flow.Steps), null);
                }
            }
            catch (Exception ex) {
                if (ex is TargetInvocationException invocationEx)
                    ex = invocationEx.InnerException;

                if (ex is RegexMatchTimeoutException)
                    ex = new TimeGuardException("Time limit reached while evaluating a Regex.\r\nNote that timeout was added by SharpLab — in real code this would not throw, but might run for a very long time.", ex);

                if (ex is StackGuardException sgex)
                    throw new Exception($"{sgex.Message} {sgex.StackBaseline} {sgex.StackOffset} {sgex.StackLimit} {sgex.StackSize}");

                Flow.ReportException(ex);
                ex.Inspect("Exception");
                return new ExecutionResultWithException(new ExecutionResult(Output.Stream, Flow.Steps), ex);
            }
        }

        private static RuntimeGuardSettings NewRuntimeGuardSettings() {
            #if DEBUG
            if (Debugger.IsAttached)
                return new RuntimeGuardSettings { TimeLimit = TimeSpan.MaxValue };
            #endif
            return null;
        }
    }
}