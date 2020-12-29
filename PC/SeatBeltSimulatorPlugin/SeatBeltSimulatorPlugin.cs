using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace SeatBeltSimulator
{
    public enum LifeCycle
    {
        stopped, starting, running, stopping
    }

    [PluginName("Seat Belt Simulator")]
    [PluginDescription("Seat Belt Braking Force Simulator")]
    [PluginAuthor("Ulrich Dinger")]
    public class SeatBeltSimulatorPlugin : IPlugin, IDataPlugin, IWPFSettings
    {

        public PluginManager PluginManager { get; set; }
        public SeatBeltSimulatorSettings Settings;

        private LifeCycle lifeCycleState = LifeCycle.stopped;

        private bool restartSerial = false;

        private double lastAcceleration;
        private double currentAcceleration;

        private DateTime? lastTs;
        private TimeSpan deltaTs;

        private double lastSpeed;
        private double currentSpeed;

        string currentGame = null;

        /// <summary>
        /// Called once after plugins startup
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void Init(PluginManager pluginManager)
        {
            SimHub.Logging.Current.Info("Starting plugin");

            // Load settings
            Settings = this.ReadCommonSettings<SeatBeltSimulatorSettings>("GeneralSettings", () => new SeatBeltSimulatorSettings());

            // define the properties we provider
            pluginManager.AddProperty("SeatBeltSimu.Computed.Acceleration", this.GetType(), 0);
            pluginManager.AddProperty("SeatBeltSimu.Computed.SeatBelt", this.GetType(), 0);

        }

        /// <summary>
        /// Called one time per game data update, contains all normalized game data, 
        /// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
        /// 
        /// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
        /// 
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <param name="data"></param>
        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Define the value of our property (declared in init)
            pluginManager.SetPropertyValue("CurrentDateTime", this.GetType(), DateTime.Now);

            if (data.GameRunning && !data.GamePaused)
            {
                if (currentGame == null)
                {
                    currentGame = data.GameName;
                }
                
                if (data.NewData != null)
                {
                    StartSerialIfNeeded();

                    if ("Automobilista2".Equals(currentGame))
                    {
                        useGlobalAcceleration(pluginManager, ref data, 2);
                        return;
                    }

                    if (tryUseGameRawData(pluginManager, ref data))
                    {
                        return;
                    }                   

                    calculateAcceleration(pluginManager, ref data);

                }
            } else
            {
                lastTs = null;
                currentGame = null;
                beltData = 0;
                StopSerial();
            }
        }
        
        /// <summary>
        /// Called at plugin manager stop, close/dispose anything needed here ! 
        /// Plugins are rebuilt at game change
        /// </summary>
        /// <param name="pluginManager"></param>
        public void End(PluginManager pluginManager)
        {
            // Save settings
            this.SaveCommonSettings("GeneralSettings", Settings);
            StopSerial();
        }

        /// <summary>
        /// Returns the settings control, return null if no settings control is required
        /// </summary>
        /// <param name="pluginManager"></param>
        /// <returns></returns>
        public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            return new SettingsControlDemo(this);
        }

        public void StartSerialIfNeeded()
        {
            if (lifeCycleState == LifeCycle.stopped)
            {
                Thread InstanceCaller = new Thread(new ThreadStart(SendAccelerationToDevice));
                InstanceCaller.Start();
            }
        }

        public void SetSerialPort(string port)
        {
            Settings.ComPort = port;
            restartSerial = true;
            StartSerialIfNeeded();
        }

        public double beltData = 0;
        public int sentValue = -1;

        public void GoIn()
        {
            beltData = 0;
            StartSerialIfNeeded();
        }

        public void GoOut()
        {
            beltData = 10;
            StartSerialIfNeeded();
        }

        public void StopSerial()
        {
            if (lifeCycleState == LifeCycle.running)
            {
                lifeCycleState = LifeCycle.stopping;
            }
        }

        public void SendAccelerationToDevice()
        {
            if (Settings.ComPort == null)
            {
                SimHub.Logging.Current.Info("No Com Port configured.");
                return;
            }
            if (lifeCycleState != LifeCycle.stopped)
            {
                return;
            }
            lifeCycleState = LifeCycle.starting;
            restartSerial = false;

            SimHub.Logging.Current.Info("Starting Serial Communication");
            SerialPort _serialPort = null;
            try
            {
                _serialPort = new SerialPort();
                _serialPort.PortName = Settings.ComPort;
                _serialPort.BaudRate = 57600;
                _serialPort.Open();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                SimHub.Logging.Current.Error("Error opening Serial Port: "+e.Message);
                _serialPort = null;
            }
            if (_serialPort == null)
            {
                lifeCycleState = LifeCycle.stopped;
                return;
            }
            lifeCycleState = LifeCycle.running;
            try
            {
                do
                {
                    int value = (int)beltData;
                    value = Math.Min(value, 9);
                    value = Math.Max(value, 0);

                    if (value != sentValue)
                    {
                        sentValue = value;
                        _serialPort.Write("" + value);
                        _serialPort.DiscardOutBuffer();
                    }
                    Thread.Sleep(250);

                } while (!restartSerial && lifeCycleState == LifeCycle.running);
            }
            catch (IOException ioe)
            {
                SimHub.Logging.Current.Warn("IO Exception, restarting serial connection after 1s.");
                restartSerial = true;
                Thread.Sleep(1000);
            }
            SimHub.Logging.Current.Info("Stopping Serial Communication");
            _serialPort.Dispose();
            _serialPort.Close();
            _serialPort = null;

            lifeCycleState = LifeCycle.stopped;

            if (restartSerial)
            {
                SendAccelerationToDevice();
            }
        }
        
        private bool tryUseGameRawData(PluginManager pluginManager, ref GameData data)
        {
            var acc = pluginManager.GetPropertyValue("GameRawData.LongitudinalAcceleration");
            if (acc != null)
            {
                double accd = Double.Parse(acc.ToString());
                if (accd > 0)
                {
                    accd = 0;
                }
                pluginManager.SetPropertyValue("SeatBeltSimu.Computed.Acceleration", this.GetType(), accd);

                beltData = -accd * 4;
                pluginManager.SetPropertyValue("SeatBeltSimu.Computed.SeatBelt", this.GetType(), beltData);

                return true;
            }
            return false;
        }

        private void useGlobalAcceleration(PluginManager pluginManager, ref GameData data, double divider)
        {
            var accd2 = data.NewData.GlobalAccelerationG;
            if (accd2 > 0)
            {
                accd2 = 0;
            }
            pluginManager.SetPropertyValue("SeatBeltSimu.Computed.Acceleration", this.GetType(), accd2);

            beltData = -accd2 / divider;
            pluginManager.SetPropertyValue("SeatBeltSimu.Computed.SeatBelt", this.GetType(), beltData);
        }

        // ***************** calculate acceleration by myself if property not found
        private void calculateAcceleration(PluginManager pluginManager, ref GameData data)
        {
            // first time?!?
            if (lastTs == null)
            {
                lastTs = data.FrameTime;
                lastSpeed = data.NewData.SpeedKmh / 3.6;
                return;
            }

            // how old is last value = delta T
            deltaTs = data.FrameTime.Subtract(lastTs.Value);
            double deltaSeconds = (double)deltaTs.Seconds + (((double)deltaTs.Milliseconds) / 1000);
            if (deltaSeconds < 0.100)
            {
                return;
            }
            currentSpeed = data.NewData.SpeedKmh / 3.6;
            double deltaSpeed = currentSpeed - lastSpeed;
            lastSpeed = currentSpeed;

            if (deltaSpeed > 0)
            {
                currentAcceleration = 0;
            }
            else
            {
                currentAcceleration = -deltaSpeed / deltaSeconds;
            }

            if (lastAcceleration != currentAcceleration)
            {
                pluginManager.SetPropertyValue("SeatBeltSimu.Computed.Acceleration", this.GetType(), currentAcceleration);
                lastAcceleration = currentAcceleration;

                beltData = currentAcceleration * 1000.0;
                pluginManager.SetPropertyValue("SeatBeltSimu.Computed.SeatBelt", this.GetType(), beltData);
            }
        }
    }

}