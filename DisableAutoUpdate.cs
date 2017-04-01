using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Timers;

namespace DisableWin10AutoUpdate
{
    public partial class DisableAutoUpdate : ServiceBase
    {
        #region Constants
        private const int FIRST_HALF_START  = 0;
        private const int FIRST_HALF_END    = 12;
        private const int SECOND_HALF_START = 12;
        private const int SECOND_HALF_END   = 0;
        private const string KEYVAL_ACTIVE_START = "ActiveHoursStart";
        private const string KEYVAL_ACTIVE_END   = "ActiveHoursEnd";
        private const string SETTINGS_KEY        = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings";
        #endregion

        #region Member Variables
        private static int _setStart = FIRST_HALF_START;
        private static int _setEnd   = FIRST_HALF_END;
        private static int _hours;
        private static Timer _timer;
        #endregion

        #region Public Methods
        public DisableAutoUpdate()
        {
            this.AutoLog = true;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _timer = new Timer();
            _timer.Interval = 1200000; // check every 20 minutes
            _timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            _timer.Start();

            EventLog.WriteEntry("Disable AutoUpdate Service started.");

            //as soon as service starts, check if change is required
            ChangeIfRequired();
        }

        protected override void OnStop()
        {
            _timer.Stop();

            EventLog.WriteEntry("Disable AutoUpdate Service stopped.");
        }

        private void ChangeIfRequired()
        {
            //only if a change to the registry is required, we actually perform the change
            if (IsChangeRequired())
            {
                //change values in registry
                ChangeWorkingTime();
            }
        }

        public void OnTimer(object sender, ElapsedEventArgs args)
        {
            ChangeIfRequired();
        }
        #endregion

        #region Service Functionality
        private bool IsChangeRequired()
        {
            _hours = DateTime.Now.TimeOfDay.Hours;

            int currentStart = (int)Registry.GetValue(SETTINGS_KEY, KEYVAL_ACTIVE_START, 0);
            int currentEnd   = (int)Registry.GetValue(SETTINGS_KEY, KEYVAL_ACTIVE_END,   0);

            if(currentEnd == 0)
            {
                currentEnd = 24;
            }

            if (currentStart <= _hours && _hours < currentEnd)
            {
                return false;
            }

            return true;
        }

        private void ChangeWorkingTime()
        {
            if(_setStart == FIRST_HALF_START && _setEnd == FIRST_HALF_END)
            {
                _setStart = SECOND_HALF_START;
                _setEnd   = SECOND_HALF_END;
            }
            else
            {
                _setStart = FIRST_HALF_START;
                _setEnd   = FIRST_HALF_END;
            }

            try
            {
                Registry.SetValue(SETTINGS_KEY, KEYVAL_ACTIVE_START, _setStart, RegistryValueKind.DWord);
                Registry.SetValue(SETTINGS_KEY, KEYVAL_ACTIVE_END,   _setEnd,   RegistryValueKind.DWord);

                EventLog.WriteEntry(string.Format("{0}: {1}: {2}", DateTime.Now.ToLongTimeString(), KEYVAL_ACTIVE_START, _setStart.ToString()));
                EventLog.WriteEntry(string.Format("{0}: {1}: {2}", DateTime.Now.ToLongTimeString(), KEYVAL_ACTIVE_END, _setEnd.ToString()));
            }
            catch(Exception ex)
            {
                EventLog.WriteEntry(string.Format("Error setting registry key: {0}", ex.Message));
            }           
        }
        #endregion
    }
}