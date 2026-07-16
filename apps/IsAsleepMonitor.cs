using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System.Collections.Generic;
using System.Linq;
using NetDaemon.Extensions.Scheduler;
using System.Reactive.Concurrency;
using System.Globalization;
using System;
using System.Diagnostics;
using NetDaemon.HassModel;
using System.Reactive.Linq;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using System.Reflection.Metadata.Ecma335;

namespace NetDaemonApps.apps
{
    /// <summary>
    /// Uses multiple conditions to determine whetever occupant is asleep and runs action accordingle.
    /// </summary>
    [NetDaemonApp]
    public class IsAsleepMonitor : AppBase
    {
        private readonly TimeSpan toSleepSpareTime = TimeSpan.FromMinutes(6);

        private IDisposable? alarmTimer;
        private IDisposable? alarmSubscription = null;
        private IDisposable? alarmSubscription2 = null;
        private IDisposable? lightSleepWaiter = null;

        private IDisposable? isAsleepOnTimer = null;
        private IDisposable? isAsleepOffTimer = null;

        private IDisposable? rebootTimer = null;
        private IDisposable? modentimer = null;

        private bool ringingAlarm = false;
        private bool modemAutoTurnedOff = false;

        private static Dictionary<string, Func<string>> morningTTSfunctions = new Dictionary<string, Func<string>>();

        private class MonitorMember
        {
            public bool currentState;
            private Func<bool> condition;
            public string name;
            public MonitorMember(Func<bool> condition, string debugName)
            {
                this.name = debugName;
                this.condition = condition;
                currentState = CheckState();
            }

            public bool CheckState()
            {

                bool oldState = currentState;
                currentState = condition.Invoke();
                return oldState != currentState;
            }
        }

        public static void RegisterMorningTTS(string id, Func<string> function)
        {
            morningTTSfunctions.Remove(id);
            morningTTSfunctions.Add(id, function);
        }
        
        private void ReadMorningTTS()
        {
            string msg = "Good Morning,";
            int originalLenght = msg.Length;
            foreach (var kvp in morningTTSfunctions)
            {
                if (kvp.Value == null) continue;
                string part = kvp.Value.Invoke();
                if (part.Length > 0)
                {
                    if (msg.Length > originalLenght)
                    {
                        part = ". Also," + part;                
                    }
                    msg += part;
                }
            }

            if(msg.Length>originalLenght)
            TTS.Speak(msg);
        }

        public static void Awake()
        {
            myEntities.InputBoolean.Isasleep.TurnOff();
        }

        public static void ToggleMode()
        {
            myEntities.InputSelect.AlarmSleepMode.SelectNext();
            string nextmode;
            switch (myEntities.InputSelect.AlarmSleepMode.State)
            {
                case "Normal":
                    nextmode = "Nap";
                        break;

                case "Nap":
                    nextmode = "Free";
                    break;

                case "Free":
                    nextmode = "Exact Time";
                    break;

                case "Exact Time":
                    nextmode = "Normal";
                    break;

                default:
                    nextmode = "Unkwon";
                    break;

            }
            TTS.Speak("Sleep Mode set to" +nextmode, TTS.TTSPriority.IgnoreAll);
        }
        public void Resub(bool reboot = false, string mode = null)
        {

            alarmSubscription?.Dispose();
            alarmSubscription2?.Dispose();

            modentimer?.Dispose();

            void AlarmFunction()
            {
                if (myEntities.InputBoolean.NotificationAlarm.IsOff()) return;
                if (myEntities.InputBoolean.GuestMode.IsOn()) return;
                if (myEntities.InputBoolean.Isasleep.IsOff()) return;
                var alarmnumber = 1;
                myEntities.Script.Actiontodoatalarm.TurnOn(); 
                
                ringingAlarm = true;
                myEntities.Script.Playmoomin.TurnOn();
                alarmTimer = myScheduler.RunEvery(TimeSpan.FromMinutes(10), DateTime.Now, () => {

                    TimeSpan? timeDiff = DateTime.Now - myEntities?.InputDatetime.Lastisasleeptime.GetDateTime();
                    string ttsTime;
                    if (alarmnumber == 1) ttsTime = "Good Morning";
                    ttsTime = "its " + DateTime.Now.ToString("H:mm", CultureInfo.InvariantCulture) + ", you have been sleeping for " + timeDiff?.Hours + " hours" + (timeDiff?.Minutes > 0 ? " and " + timeDiff?.Minutes + "minutes" : ". ");

                    if (alarmnumber > 1)
                        ttsTime += "It has been " + (alarmnumber - 1) * 10 + " minutes";

                    TTS.Speak(ttsTime, TTS.TTSPriority.IgnoreAll, null);
                    myEntities.Script.Playmoomin.TurnOn();
                    alarmnumber++;
                });
                rebootTimer?.Dispose();
            }

            TimeSpan awakeSpan = TimeSpan.Zero;
            TimeSpan awakeAfter = TimeSpan.Zero;

            mode = mode == null ? myEntities.InputSelect.AlarmSleepMode.State : mode;

            switch (mode)
            {
                case "Normal" :
                    awakeSpan = TimeSpan.FromMinutes(45);
                    awakeAfter = TimeSpan.FromMinutes(15);
                    break;
                case "Exact Time":
                    awakeSpan = TimeSpan.FromMinutes(15);
                    break;

                case "Nap":
                    awakeSpan = TimeSpan.FromMinutes(15);
                    awakeAfter = TimeSpan.FromMinutes(15);
                    break;
            }

            modentimer = myScheduler.Schedule((reboot ? myEntities.InputDatetime.AlarmTargetTime.GetDateTime(): DetermineAlarmTagetTime(mode))- TimeSpan.FromMinutes(45), x =>
            {              

                if (myEntities.InputBoolean.NotificationAlarm.IsOff()) return;
                if (myEntities.InputBoolean.GuestMode.IsOn()) return;
                if (myEntities.InputBoolean.Isasleep.IsOff()) return;
                if (myEntities.Switch.BrightLightPlug.IsOn()) return;
                
                myEntities.Switch.BrightLightPlug.TurnOn();

            });


            alarmSubscription = myScheduler.Schedule((reboot ? myEntities.InputDatetime.AlarmTargetTime.GetDateTime() : DetermineAlarmTagetTime(mode)), x => {
                alarmSubscription?.Dispose();
                alarmSubscription2?.Dispose();
                AlarmFunction();

            });



        }

        private DateTimeOffset DetermineAlarmTagetTime(string mode)
        {
    
            DateTimeOffset dto;
         
            switch (mode)
            {
                case "Normal":
                    dto =  DateTime.Now + new TimeSpan((int)myEntities.InputDatetime.AlarmSleepDuration.Attributes.Hour, (int)myEntities.InputDatetime.AlarmSleepDuration.Attributes.Minute, 0);
             break;

                case "Nap":
                    dto = DateTime.Now + new TimeSpan((int)myEntities.InputDatetime.AlarmNapDuration.Attributes.Hour, (int)myEntities.InputDatetime.AlarmNapDuration.Attributes.Minute, 0);
                    break;

                case "Free":
                    dto = DateTime.Now + TimeSpan.FromDays(1);
                    break;

                case "Exact Time":
                    DateTime day = DateTime.Now.Hour<=(int)myEntities.InputDatetime.Alarmtime.Attributes.Hour ?  DateTime.Today : DateTime.Today+TimeSpan.FromDays(1);
                    dto = new DateTime(day.Year,day.Month, day.Day, (int)myEntities.InputDatetime.Alarmtime.Attributes.Hour, (int)myEntities.InputDatetime.Alarmtime.Attributes.Minute, 0);
                    break;

                default:
                    dto = new DateTimeOffset(myEntities.InputDatetime.AlarmTargetTime.GetDateTime());
                    break;

            }
          
            // Get the unix timestamp in seconds
            long unixTime = dto.ToUnixTimeSeconds();


            myEntities.InputDatetime.AlarmTargetTime.SetDatetime(timestamp: unixTime);

            return dto;

        }

        public IsAsleepMonitor(IHaContext ha) {

            //DateTime d2 = DateTime.Parse(_00_Globals._myEntities.Sensor.EnvyLastactive.State ?? "", null, System.Globalization.DateTimeStyles.RoundtripKind);
            SleepStatusUpdated();
       
            Resub(true);


            myEntities.InputBoolean.Isasleep.StateChanges().Where(x => x?.New?.State == "off" && x?.Old.State == "on").Subscribe(x => {

                ringingAlarm = false;
                modemAutoTurnedOff = false;

                

                ReadMorningTTS();


                if (myEntities.InputSelect.AlarmSleepMode.State == "Nap" || myEntities.InputSelect.AlarmSleepMode.State == "Free")
                myEntities.InputSelect.AlarmSleepMode.SelectOption("Normal");

                // If alarm timer is on it means that this is after a long sleep
                if (alarmTimer != null)
                {
                    alarmTimer.Dispose();      
                }
                if (myEntities.InputBoolean.GuestMode.IsOn()) return;

                //Run default actions that run everytime isSleep is turned on
                // Get the offset from current time in UTC time
                DateTimeOffset dto = new DateTimeOffset(DateTime.Now);
                // Get the unix timestamp in seconds
                long unixTime = dto.ToUnixTimeSeconds();

                rebootTimer?.Dispose();
                myEntities.InputDatetime.Awoketime.SetDatetime(timestamp: unixTime);
            });

            myEntities.InputSelect.AlarmSleepMode.StateChanges().Where(x => !x.Old.IsUnavailable() ).Subscribe(x => {

                Resub(false, myEntities.InputSelect.AlarmSleepMode.State);

            });

            myEntities.InputDatetime.AlarmTargetTime.StateChanges().Where(x => !x.Old.IsUnavailable()).Subscribe(x => {

                Console.WriteLine("Alarm: " + myEntities.InputDatetime.AlarmTargetTime.GetDateTime());

            });

                
            myEntities.InputBoolean.Isasleep.StateChanges().Where(x => x?.New?.State == "on" && x?.Old.State == "off").Subscribe(x => {
                Resub();
                myEntities.InputDatetime.Lastisasleeptime.SetDatetime(timestamp: new DateTimeOffset(DateTime.Now).ToUnixTimeSeconds());

            });

            RegisterMorningTTS("clock", () => { return "It's " + DateTime.Now.ToString("H:mm", CultureInfo.InvariantCulture); }  );


            myScheduler.ScheduleCron("*/5 * * * *", SleepStatusUpdated);
        }
        private void SleepStatusUpdated()
        {


            if (myEntities.InputBoolean.Isasleep.IsOn() && myEntities.InputBoolean.Isasleep.StateFor(TimeSpan.FromMinutes(20) ) && modemAutoTurnedOff == false)
            {
                switch (myEntities.InputSelect.PowerSavingBehaviour.State)
                {
                    case "Modem Off":
                        if (!EcoFlowManager.isSometingPlanned) {
                            myEntities.Switch.BrightLightPlug.TurnOff();
                            modemAutoTurnedOff = true;
                        }
                        break;

                    case "Everything Off":
                        if (!EcoFlowManager.isSometingPlanned) myEntities.Script.TurnOffEverything.TurnOn();
                        break;
                }     
            }

            if (myEntities.InputBoolean.Isasleep.IsOn() == myEntities.BinarySensor.IsAsleepHelper.IsOn()) return;
            if (!myEntities.BinarySensor.IsAsleepHelper.StateFor(TimeSpan.FromMinutes(5))) return;

            if (myEntities.BinarySensor.IsAsleepHelper.IsOn()) myEntities.InputBoolean.Isasleep.TurnOn();
            else if (myEntities.BinarySensor.IsAsleepHelper.IsOff()) myEntities.InputBoolean.Isasleep.TurnOff();
        }
        private bool trainingLora()
        {
            return myEntities.Automation.TurnOffPcWhenLoraTrainingDone.IsOn() && (myEntities.InputSelect.Atloraended.State == "Shutdown" || myEntities.InputSelect.Atloraended.State == "Smart" ) ;
        }



        }

    
}
