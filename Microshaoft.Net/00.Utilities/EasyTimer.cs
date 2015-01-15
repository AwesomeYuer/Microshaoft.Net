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

        public int IntervalSeconds
        {
            get { return _intervalSeconds; }
            //set { _intervalSeconds = value; }
        }
        public void SetIntervalSeconds(int seconds)
        {
            _intervalSeconds = seconds;
            _timer.Interval = seconds * 1000;
        }

        public EasyTimer
                    (
                        int intervalSeconds
                        , int times         //action 耗时倍数
                        , Action<EasyTimer> timerProcessAction = null
                        , bool autoStart = true
                        , bool skipFirstTimerProcessAction = true
                        , Func<EasyTimer, Exception, bool> onCaughtExceptionProcessFunc = null

                    )
        {
            if (timerProcessAction == null)
            {
                return;
            }
            _intervalSeconds = intervalSeconds;
            //2015-01-08 解决第一次 Null
            _timer = new Timer(_intervalSeconds * 1000);
            //first 主线程
            if (!skipFirstTimerProcessAction)
            {
                TimerProcessAction(times, timerProcessAction, onCaughtExceptionProcessFunc);
            }

            _timer.Elapsed += new ElapsedEventHandler
                                        (
                                            (x, y) =>
                                            {
                                                TimerProcessAction
                                                    (
                                                        times
                                                        , timerProcessAction
                                                        , onCaughtExceptionProcessFunc
                                                    );
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
                            , Action<EasyTimer> timerAction
                            , Func<EasyTimer, Exception, bool> onCaughtExceptionProcessFunc
                        )
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
                                timerAction(this);
                            }
                            , false
                            , (x, y) =>
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