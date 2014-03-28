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
        private int _intervalSeconds;
        public EasyTimer
                    (
                        int intervalSeconds
                        , int times
                        , Action timerAction = null
                        , bool autoStart = true            
                        , Action<Exception> exceptionAction = null
                        
                    )
        {
            if (timerAction == null)
            {
                return;
            }
            _intervalSeconds = intervalSeconds * 1000;
            _timer = new Timer(100);
            _timer.Elapsed += new ElapsedEventHandler
                                        (
                                            (x, y) =>
                                            {
                                                TimerActionProcess(times, timerAction, exceptionAction);
                                            }
                                        );
            if (autoStart)
            {
                Start();
            }
        }
        private void TimerActionProcess(int times, Action timerAction, Action<Exception> exceptionAction)
        {
            if (_timer.Interval < _intervalSeconds)
            {
                _timer.Interval = _intervalSeconds;
            }
            if (timerAction == null)
            {
                return;
            }
            _timer.Enabled = false;
            _timer.Stop();
            DateTime begin;
            do
            {
                begin = DateTime.Now;
                try
                {
                    timerAction();
                }
                catch (Exception e)
                {
                    if (exceptionAction != null)
                    {
                        exceptionAction(e);
                    }
                }

            } while (Math.Abs(DateTimeHelper.SecondsDiffNow(begin)) > times * _intervalSeconds);
            _timer.Start();
            _timer.Enabled = true;
        }
    }
}