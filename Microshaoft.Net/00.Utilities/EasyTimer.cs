namespace Microshaoft
{
    using System;
    using System.Timers;
    public class EasyTimer
    {
        private Timer _timer;
        public void Start()
        {
            if (_timer != null)
            {
                _timer.Start();
            }
        }
        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
        }
        private int _intervalSeconds;
        public EasyTimer
                    (
                        int intervalSeconds
                        , int times         //action 耗时倍数
                        , Action timerProcessAction = null
                        , bool autoStart = true
                        , bool skipFirstTimerProcessAction = true
                        , Func<EasyTimer, Exception, bool> onCaughtExceptionProcessFunc = null

                    )
        {
            if (timerProcessAction == null)
            {
                return;
            }
            _intervalSeconds = intervalSeconds * 1000;
            //first 主线程
            if (!skipFirstTimerProcessAction)
            {
                TimerProcessAction(times, timerProcessAction, onCaughtExceptionProcessFunc);
            }
            _timer = new Timer(_intervalSeconds);
            _timer.Elapsed += new ElapsedEventHandler
                                        (
                                            (x, y) =>
                                            {
                                                TimerProcessAction(times, timerProcessAction, onCaughtExceptionProcessFunc);
                                            }
                                        );
            if (autoStart)
            {
                Start();
            }
        }
        private object _locker = new object();
        private void TimerProcessAction
                        (
                            int times
                            , Action timerAction
                            , Func<EasyTimer, Exception, bool> onCaughtExceptionProcessFunc)
        {
            if (timerAction == null)
            {
                return;
            }
            if (_timer != null)
            {
                lock (_locker)
                {
                    _timer.Stop();
                    _timer.Enabled = false;
                }
            }
            DateTime begin;
            do
            {
                begin = DateTime.Now;
                TryCatchFinallyProcessHelper
                    .TryProcessCatchFinally
                        (
                            true
                            , () =>
                            {
                                timerAction();
                            }
                            , false
                            , (x) =>
                            {
                                var reThrowException = false;
                                if (onCaughtExceptionProcessFunc != null)
                                {
                                    reThrowException = onCaughtExceptionProcessFunc(this, x);
                                }
                                return reThrowException;
                            }
                            , null
                        );
            } while (Math.Abs(DateTimeHelper.SecondsDiffNow(begin)) > times * _intervalSeconds);
            if (_timer != null)
            {
                lock (_locker)
                {
                    _timer.Start();
                    _timer.Enabled = true;
                }
            }
        }
    }
}