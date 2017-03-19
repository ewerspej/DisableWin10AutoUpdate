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
using System.IO;

namespace DisableWin10AutoUpdate
{
    public partial class DisableAutoUpdate : ServiceBase
    {
        private const int FIRST_HALF_START = 0;
        private const int FIRST_HALF_END = 12;
        private const int SECOND_HALF_START = 12;
        private const int SECOND_HALF_END = 0;

        private const string _settingsKey = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\WindowsUpdate\\UX\\Settings";
        private const string _filePath = @"C:\\service_log.txt";

        private static System.Timers.Timer _Timer;
        private static int _setStart = FIRST_HALF_START;
        private static int _setEnd   = FIRST_HALF_END;

        public DisableAutoUpdate()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _Timer = new System.Timers.Timer();
            _Timer.Interval = 1800000; // check every 30 minutes
            _Timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            _Timer.Start();
        }

        protected override void OnStop()
        {
            _Timer.Stop();
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            //only if a change to the registry is required, we actually perform the change
            if(IsRegChangeRequired())
            {
                //change values in registry
                ChangeWorkingTime();
            }
        }

        public bool IsRegChangeRequired()
        {
            int hours = DateTime.Now.TimeOfDay.Hours;
            _setStart = (int)Registry.GetValue(_settingsKey, "ActiveHoursStart", 0);
            _setEnd   = (int)Registry.GetValue(_settingsKey, "ActiveHoursEnd",   0);

            int correctedEnd = _setEnd;
            if(_setEnd == 0)
            {
                correctedEnd = 24;
            }

            if (!(_setStart <= hours && hours < correctedEnd))
            {
                return true;
            }

            return false;
        }

        public void ChangeWorkingTime()
        {
            //RegKey: HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings
            //Value-Name: ActiveHoursStart, ActiveHoursEnd
            //Value-Type: REG_DWORD
            //Value-Data: hexadecimal

            if(_setStart == FIRST_HALF_START && _setEnd == FIRST_HALF_END)
            {
                _setStart = SECOND_HALF_START;
                _setEnd = SECOND_HALF_END;
            }
            else
            {
                _setStart = FIRST_HALF_START;
                _setEnd = FIRST_HALF_END;
            }

            try
            {                
                Registry.SetValue(_settingsKey, "ActiveHoursStart", _setStart, RegistryValueKind.DWord);
                Registry.SetValue(_settingsKey, "ActiveHoursEnd", _setEnd, RegistryValueKind.DWord);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error setting registry key: {0}", ex.Message);
            }
            finally
            {
                try
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(_filePath, true))
                    {
                        int startValue = (int)Registry.GetValue(_settingsKey, "ActiveHoursStart", 0);
                        int startEnd = (int)Registry.GetValue(_settingsKey, "ActiveHoursEnd", 0);

                        file.WriteLine("{0} ActiveHoursStart: {1}", DateTime.Now.ToString(), startValue.ToString());
                        file.WriteLine("{0} ActiveHoursEnd: {1}", DateTime.Now.ToString(), startEnd.ToString());
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Error writing to service log: {0}", ex.Message);
                }
            }            
        }
    }
}
