using HomeAssistantGenerated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
using NetDaemon.Extensions.Scheduler;
using System.Text.Json;
using System.Diagnostics;
using NetDaemon.HassModel.Entities;
using System.Threading;
using Microsoft.AspNetCore.Http.Timeouts;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class EcoFlowManager : AppBase
    {
        private double chargeTimeThreshold = 1.25;
        private double peakTimeThreshold = 0.8;

        private int maxOnHours = 5;
        private int maxChargeHours = 3;

        private List<int> plannedChargeHoursToday = new List<int>();
        private List<int> plannedOnHoursToday= new List<int>();
        private List<int> plannedChargeHoursTomorrow = new List<int>();
        private List<int> plannedOnHoursTomorrow = new List<int>();

        private List<KeyValuePair<int,double>> todayHoursRaw = new List<KeyValuePair<int,double>>();
        private List<KeyValuePair<int, double>> tomorrowHoursRaw = new List<KeyValuePair<int, double>>();

        private static EcoflowPanel ecoflowPanel;
        public static bool isSometingPlanned { get { return ecoflowPanel.isAnythingPlanned(); } }

        protected class EcoflowPanel
        {


            private List<EcoflowPanelRow> ecoflowPanelList = new List<EcoflowPanelRow>();
            public bool isAnythingPlanned()
            {
                return ecoflowPanelList.FirstOrDefault(x => x.enabled.IsOn() && x.mode.State == "On", null) != null;
            }

            public EcoflowPanel() { }

            public void RegisterRow(InputBooleanEntity enabledEntity, InputSelectEntity modeEntity, InputDatetimeEntity timeEntity, InputNumberEntity powerEntity)
            {
                ecoflowPanelList.Add(new EcoflowPanelRow(enabledEntity, modeEntity, timeEntity, powerEntity));
            }




            protected class EcoflowPanelRow
            {
                public InputBooleanEntity enabled;
                public InputSelectEntity mode;
                public InputDatetimeEntity time;
                public InputNumberEntity power;
                private IDisposable schedculedEvent;




                public EcoflowPanelRow (InputBooleanEntity enabledEntity, InputSelectEntity modeEntity, InputDatetimeEntity timeEntity, InputNumberEntity powerEntity)
                {
                    enabled = enabledEntity;
                    mode = modeEntity;
                    time = timeEntity;
                    power = powerEntity;
                    RegisterListeners();
                }


                private void RegisterListeners()
                {

                    enabled.StateChanges().Subscribe(_ => { RegisterTimeListener();});
                    time.StateChanges().Subscribe(_ => { RegisterTimeListener(); OnChanged(); });
                    power.StateChanges().Subscribe(_ => { OnChanged(); });
                    mode.StateChanges().Subscribe(_ => { OnChanged(); });
                    RegisterTimeListener();
                }


                private void OnChanged()
                {
                    enabled.TurnOn();
                }

                private void RegisterTimeListener()
                {
                    if (schedculedEvent != null) schedculedEvent.Dispose();

                    if (enabled.IsOff()) return;

                    DateTime dateTimeVariable = DateTime.ParseExact(time.EntityState.State ?? "", "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                    // Get the current time.
                    DateTime now = DateTime.Now;

                    // Create a DateTime object representing the target time today.
                    DateTime targetTimeToday = new DateTime(now.Year, now.Month, now.Day, dateTimeVariable.Hour, dateTimeVariable.Minute, 0);

                    // Check if the target time has already passed today.
                    if (targetTimeToday < now)
                    {
                        // If so, add a day to get the target time for tomorrow.
                        targetTimeToday = targetTimeToday.AddDays(1);
                    }

                    // Calculate the timespan until the target time.
                    TimeSpan timeSpan = targetTimeToday - now;

                 

                    schedculedEvent = myScheduler.Schedule(timeSpan, TriggerEvent);
                }

                private void TriggerEvent()
                {
                    if (enabled.IsOff()) return;

                    switch (mode.State)
                    {
                        case "On":
                            TurnOn();
                            SetPower();
                        break;

                        case "Off":
                            TurnOff();
                         break;

                        case "Power Only":
                            SetPower();
                        break;

                    }

                    enabled.TurnOff();
                }


                private void TurnOn()
                {
                    if (myEntities.Switch.EcoflowPlug.IsOff())
                        myEntities.Switch.EcoflowPlug.TurnOn();
                }
                private void TurnOff()
                {
                        myEntities.Switch.EcoflowPlug.TurnOff();
                }

                private void SetPower()
                {
                    if (myEntities.Switch.BrightLightPlug.IsOff()) return;
                    Task.Run(async () =>
                    {
                        await Task.Run(async () =>
                        {
                            var waitTask = Task.Run(async () =>
                            {
                                while (myEntities.Sensor.EcoflowStatus.State != "online") await Task.Delay(1000);
                            });

                            if (waitTask != await Task.WhenAny(waitTask, Task.Delay(120000)))
                                throw new TimeoutException();
                        });

                        myEntities.Number.EcoflowAcChargingPower.SetValue(power.State.ToString());
                        myEntities.Switch.EcoflowPrioSolarCharging.TurnOff();
                    });
                }
            }


        }

        public EcoFlowManager()
        {

            myEntities.Sensor.EcoflowBatteryLevel.StateChanges().Where(x => x.New?.State >= myEntities.Number.EcoflowBackupReserveLevel.State && myEntities.Switch.EcoflowBackupReserveEnabled.IsOn()).Subscribe(x => {
                myEntities.Switch.EcoflowPlug.TurnOff();
            });


            myEntities.Sensor.EcoflowStatus.StateChanges().Where(x => x.New.State == "Online").Subscribe(x =>
            {
                myEntities.Switch.EcoflowAcEnabled.TurnOn();
            });


            /*
            myEntities.Sensor.EcoflowPlugPower.StateChanges().Subscribe(x => { myEntities.InputNumber.EcoflowChargeHelper.SetValue((double)(myEntities.Sensor.EcoflowPlugPower.State ?? 0));}); 
            myScheduler.ScheduleCron("2 * * * *", () => {

                if (myEntities.Sensor.EcoflowPlugPower.State > 0)
                {
                    myEntities.InputNumber.EcoflowChargeHelper.SetValue((double)(myEntities.Sensor.EcoflowPlugPower.State - 1));
                }
                else
                {
                    myEntities.InputNumber.EcoflowChargeHelper.SetValue(0);
                }
            
            });
         
            myScheduler.ScheduleCron("58 * * * *", () => {

                if (myEntities.Sensor.EcoflowAcInPower.State > 0)
                {
                    myEntities.InputNumber.EcoflowChargeHelper.SetValue((double)(myEntities.Sensor.EcoflowPlugPower.State + 1));
                }
                else
                {
                    myEntities.InputNumber.EcoflowChargeHelper.SetValue(0);
                }

            });
       */

            ecoflowPanel = new EcoflowPanel();
            ecoflowPanel.RegisterRow(myEntities.InputBoolean.EcopanelS1Enabled, myEntities.InputSelect.EcopanelS1Mode, myEntities.InputDatetime.EcopanelS1Time, myEntities.InputNumber.EcopanelS1Power);
            ecoflowPanel.RegisterRow(myEntities.InputBoolean.EcopanelS2Enabled, myEntities.InputSelect.EcopanelS2Mode, myEntities.InputDatetime.EcopanelS2Time, myEntities.InputNumber.EcopanelS2Power);
            ecoflowPanel.RegisterRow(myEntities.InputBoolean.EcopanelS3Enabled, myEntities.InputSelect.EcopanelS3Mode, myEntities.InputDatetime.EcopanelS3Time, myEntities.InputNumber.EcopanelS3Power);
            ecoflowPanel.RegisterRow(myEntities.InputBoolean.EcopanelS4Enabled, myEntities.InputSelect.EcopanelS4Mode, myEntities.InputDatetime.EcopanelS4Time, myEntities.InputNumber.EcopanelS34Power);
            /*
            scheduler.ScheduleCron("0 * * * *", () => {





                if (plannedOnHoursToday.Contains(DateTime.Now.Hour))
                {
                    if (_00_Globals._myEntities.InputSelect.SettingsEcoflowMode.State == "Manual") return;
                    
                    if(plannedOnHoursToday.IndexOf(DateTime.Now.Hour) != 0 && _00_Globals._myEntities.InputSelect.SettingsEcoflowMode.State == "Auto")
                    {
                        if((DateTime.Now + TimeSpan.FromMinutes(_00_Globals._myEntities.Sensor.EcoflowDischargeRemainingTime.State ?? 0)).TimeOfDay.TotalSeconds < _00_Globals._myEntities.InputDatetime.NextPlannedEcocharge.Attributes?.Timestamp )
                        _00_Globals._myEntities.Switch.EcoflowPlug.TurnOn();
                    } 
                    else if(plannedOnHoursToday.IndexOf(DateTime.Now.Hour) == 0)
                    _00_Globals._myEntities.Switch.EcoflowPlug.TurnOn();
                }
                DetermineNextChargeTime();
            });

            scheduler.ScheduleCron("0 0 * * *", () => {
                plan(_00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255.Attributes?.Today, todayHoursRaw, plannedOnHoursToday);
            });


            _00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255.StateChanges().Where(x => x?.New?.State == _00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255.Attributes?.Min).Subscribe(_ => { _00_Globals._myEntities.Switch.EcoflowPlug.TurnOn(); });
            _00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255.StateAllChanges().Where(x => x?.New?.Attributes?.TomorrowValid == true && x.Old?.Attributes?.TomorrowValid == false).Subscribe(_ => { DetermineNextChargeTime(); });




            plan(_00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255.Attributes?.Today, todayHoursRaw, plannedOnHoursToday);
            if (_00_Globals._myEntities?.Sensor.NordpoolKwhFiEur3100255?.Attributes?.TomorrowValid ?? false )
            {
                var list = JsonSerializer.Deserialize<List<double>?>(_00_Globals._myEntities.Sensor?.NordpoolKwhFiEur3100255?.EntityState?.Attributes?.Tomorrow.ToString());
                plan(list, tomorrowHoursRaw, plannedChargeHoursTomorrow);
            }

            DetermineNextChargeTime();
            */
        }
        private void plan(IEnumerable<double>? hours, List<KeyValuePair<int, double>> rawList, List<int> planList)
        {
            rawList.Clear();

            List<double>? tmp = hours?.ToList();

            for (int i = 0; i < tmp?.Count; i++)
            {
                rawList.Add(new KeyValuePair<int, double>(i, tmp[i]));
            }


            planList.Clear();
            List<KeyValuePair<int, double>> orderedHours = rawList.OrderByDescending(x => x.Value).Reverse().ToList();

            KeyValuePair<int, double> lowest = orderedHours.First();
       
            KeyValuePair<int, double> highest = orderedHours.Last();

            KeyValuePair<int, double> middayChargingHour = orderedHours.FirstOrDefault(x => x.Value > 12 && x.Key < 20, highest);
            bool shoudMiddayBeUsed = middayChargingHour.Value * 2 < highest.Value;

            planList.Add(lowest.Key);
            if(shoudMiddayBeUsed) planList.Add(middayChargingHour.Key);


        }

        private void DetermineNextChargeTime()
        {
            int hour = -1;
            hour = plannedOnHoursToday.LastOrDefault(x => x > DateTime.Now.Hour,-1);
            myEntities.InputDatetime.NextPlannedEcocharge.SetDatetime(datetime:DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss"));
            
            if (hour != -1)
            {
                myEntities.InputDatetime.NextPlannedEcocharge.SetDatetime(datetime: new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, 0, 0).ToString(@"yyyy-MM-dd HH\:mm\:ss"));
            }
            else if(myEntities?.Sensor.Nordpool?.Attributes?.TomorrowValid ?? false)
            {
                hour = plannedChargeHoursTomorrow.FirstOrDefault(-1);
            
                DateTime tmrw = DateTime.Now.AddDays(1);
                if(hour != -1)
                myEntities.InputDatetime.NextPlannedEcocharge.SetDatetime(datetime: new DateTime(tmrw.Year, tmrw.Month, tmrw.Day, hour, 0, 0).ToString(@"yyyy-MM-dd HH\:mm\:ss"));

            }


        }



    }
}
