using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace DisableWin10AutoUpdate
{
    public partial class DisableAutoUpdate : ServiceBase
    {
        private static System.Timers.Timer _Timer;
        private static int _lower;
        private static int _upper;
        private static bool _runOnce = true;

        public DisableAutoUpdate()
        {
            //temp:
            _lower = 8;
            _upper = 20;

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _runOnce = true;

            _Timer = new System.Timers.Timer();
            //_Timer.Interval = 3600000; // 60 minutes
            _Timer.Interval = 10000; // 10 seconds
            _Timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            _Timer.Start();
        }

        protected override void OnStop()
        {
            _Timer.Stop();
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //update values for _lower and _upper from config file
            ReadConfiguration();

            //only if a change to the registry is required, we actually perform the change
            if(IsRegChangeRequired())
            {
                ChangeWorkingTime(_lower, _upper);
            }
        }

        public bool IsRegChangeRequired()
        {
            //TODO: implement actual logic

            if(_runOnce)
            {
                _runOnce = false;
                return true;
            }
            else
            {
                return false;
            }            
        }

        public void ChangeWorkingTime(int start, int end)
        {
            //RegKey: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings
            //Value-Name: ActiveHoursStart, ActiveHoursEnd
            //Value-Type: REG_DWORD
            //Value-Data: hexadecimal

            const string settingsKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings";

            try
            {                
                Registry.SetValue(settingsKey, "ActiveHoursStart", start, RegistryValueKind.DWord);
                Registry.SetValue(settingsKey, "ActiveHoursEnd", end, RegistryValueKind.DWord);
            }
            catch(Exception)
            {
                //...
            }
            finally
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\\users\\ewers\\service_log.txt", true))
                {
                    int startValue = (int)Registry.GetValue(settingsKey, "ActiveHoursStart", 0);
                    int startEnd = (int)Registry.GetValue(settingsKey, "ActiveHoursEnd", 0);

                    file.WriteLine("ActiveHoursStart: " + startValue.ToString());
                    file.WriteLine("ActiveHoursEnd: " + startEnd.ToString());
                }
            }            
        }

        public void ReadConfiguration()
        {
        }
    }
}
