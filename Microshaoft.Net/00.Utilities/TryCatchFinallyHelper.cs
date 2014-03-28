namespace Microshaoft
{
    using System;
    using System.Diagnostics;
    using System.Reflection;
#if NET45
    using System.Threading.Tasks;
#endif
    public static class TryCatchFinallyProcessHelper
    {
#if NET45
        public static async Task<T> TryProcessCatchFinallyAsync<T>
                                    (
                                        bool needTry
                                        , Func<Task<T>> onTryProcessFunc
                                        , bool reThrowException = false
                                        , Func<Exception, bool> onCaughtExceptionProcessFunc = null
                                        , Action<bool, Exception> onFinallyProcessAction = null
                                    )
        {
            T r = default(T);
            //if (onTryProcessAction != null)
            {
                if (needTry)
                {
                    Exception exception = null;
                    var caughtException = false;
                    try
                    {
                        r = await onTryProcessFunc();
                        return r;
                    }
                    catch (Exception e)
                    {
                        caughtException = true;
                        exception = e;
                        var currentCalleeMethod = MethodInfo.GetCurrentMethod();
                        var currentCalleeType = currentCalleeMethod.DeclaringType;
                        StackTrace stackTrace = new StackTrace();
                        StackFrame stackFrame = stackTrace.GetFrame(1);
                        var callerMethod = stackFrame.GetMethod();
                        var callerType = callerMethod.DeclaringType;
                        var frame = (stackTrace.FrameCount > 1 ? stackTrace.FrameCount - 1 : 1);
                        stackFrame = stackTrace.GetFrame(frame);
                        var originalCallerMethod = stackFrame.GetMethod();
                        var originalCallerType = originalCallerMethod.DeclaringType;
                        var innerExceptionMessage = string.Format
                                (
                                    "Rethrow caught [{1}] Exception{0} at Callee Method: [{2}]{0} at Caller Method: [{3}]{0} at Original Caller Method: [{4}]"
                                    , "\r\n\t"
                                    , e.Message
                                    , string.Format("{1}{0}{2}", "::", currentCalleeType, currentCalleeMethod)
                                    , string.Format("{1}{0}{2}", "::", callerType, callerMethod)
                                    , string.Format("{1}{0}{2}", "::", originalCallerType, originalCallerMethod)
                                );
                        Console.WriteLine(innerExceptionMessage);
                        if (onCaughtExceptionProcessFunc != null)
                        {
                            reThrowException = onCaughtExceptionProcessFunc(e);
                        }
                        if (reThrowException)
                        {
                            throw
                                new Exception
                                        (
                                            innerExceptionMessage
                                            , e
                                        );
                        }
                        return r;
                    }
                    finally
                    {
                        if (onFinallyProcessAction != null)
                        {
                            onFinallyProcessAction(caughtException, exception);
                        }
                    }
                }
                else
                {
                    return await onTryProcessFunc();
                }
            }
        }
#endif
        public static void TryProcessCatchFinally
                                    (
                                        bool needTry
                                        , Action onTryProcessAction
                                        , bool reThrowException = false
                                        , Func<Exception, bool> onCaughtExceptionProcessFunc = null
                                        , Action<bool, Exception> onFinallyProcessAction = null
                                    )
        {
            if (onTryProcessAction != null)
            {
                if (needTry)
                {
                    Exception exception = null;
                    var caughtException = false;
                    try
                    {
                        onTryProcessAction();
                    }
                    catch (Exception e)
                    {
                        caughtException = true;
                        exception = e;
                        var currentCalleeMethod = MethodInfo.GetCurrentMethod();
                        var currentCalleeType = currentCalleeMethod.DeclaringType;
                        StackTrace stackTrace = new StackTrace(e, true);
                        StackFrame stackFrame = stackTrace.GetFrame(1);
                        var callerMethod = stackFrame.GetMethod();
                        var callerType = callerMethod.DeclaringType;
                        var frame = (stackTrace.FrameCount > 1 ? stackTrace.FrameCount - 1 : 1);
                        stackFrame = stackTrace.GetFrame(frame);
                        var originalCallerMethod = stackFrame.GetMethod();
                        var originalCallerType = originalCallerMethod.DeclaringType;
                        var innerExceptionMessage = string.Format
                                (
                                    "Rethrow caught [{1}] Exception{0} at Callee Method: [{2}]{0} at Caller Method: [{3}]{0} at Original Caller Method: [{4}]"
                                    , "\r\n\t"
                                    , e.Message
                                    , string.Format("{1}{0}{2}", "::", currentCalleeType, currentCalleeMethod)
                                    , string.Format("{1}{0}{2}", "::", callerType, callerMethod)
                                    , string.Format("{1}{0}{2}", "::", originalCallerType, originalCallerMethod)
                                );
                        //Console.WriteLine(innerExceptionMessage);
                        if (onCaughtExceptionProcessFunc != null)
                        {
                            reThrowException = onCaughtExceptionProcessFunc(e);
                        }
                        if (reThrowException)
                        {
                            throw
                                new Exception
                                        (
                                            innerExceptionMessage
                                            , e
                                        );
                        }
                    }
                    finally
                    {
                        //Console.WriteLine("Finally");
                        if (onFinallyProcessAction != null)
                        {
                            onFinallyProcessAction(caughtException, exception);
                        }
                    }
                }
                else
                {
                    onTryProcessAction();
                }
            }
        }
    }
}