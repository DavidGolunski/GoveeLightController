using System;
using System.Timers;

namespace GoveeLightController {

    /*
     * This is a wrapper for a Timer.
     * It has two actions. One that triggers if the time ellapses and another one if the timer is stopped prematurely. 
     * This is especially usefull for buttons that should do different things, depending on how long it is pressed.
     */
    class RaceTimer {

        public int Interval { get; set; }

        private readonly Timer timer;
        public event Action TimeElapsed;
        public event Action StoppedPrematurely;

        public RaceTimer(int interval) {
            timer = new Timer();
            Interval = interval;
            timer.AutoReset = false;
            timer.Elapsed += OnTimeElapsed;
        }

        public void Start() {
            timer.Interval = Interval;
            timer.Start();
        }

        public void Stop(bool invokeStopEvent = true) {
            if(timer.Enabled && invokeStopEvent) {
                StoppedPrematurely?.Invoke();
            }
            timer.Stop();
        }

        private void OnTimeElapsed(object sender, ElapsedEventArgs e) {
            TimeElapsed?.Invoke();
        }
    }
}
