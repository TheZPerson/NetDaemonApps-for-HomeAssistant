using HomeAssistantGenerated;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{

    [NetDaemonApp]
    public class Starkvind : AppBase
    {
        private readonly List<string>airqualityValues = new List<string> { "excellent", "good", "moderate", "poor", "unhealthy", "hazardous", "out_of_range" };
        private readonly TimeSpan minAnnoucmentInterval = TimeSpan.FromMinutes(10);

        private DateTime nextAllowedAnnoucmentTime = DateTime.MinValue;
        
        private IDisposable annoucmentCancel = null;
        private int lastNotificationIndex = -1;
        private string queuedMessage;
        public Starkvind(IHaContext ha) {

           // myScheduler.ScheduleCron("30 0 * * * *", HourlyTurnOnCheck,true);
            myEntities.Sensor.StarkvindAirQuality.StateAllChanges().Where(x => !x.Old.IsUnavailable() && airqualityValues.Contains(x.New.State.ToLower())).Subscribe(x=>Notify(x.Old.State,x.New.State));
            myEntities.InputSelect.DebugDropdownhelper.StateAllChanges().Where(x => !x.Old.IsUnavailable() && airqualityValues.Contains(x.New.State.ToLower())).Subscribe(x => Notify(x.Old.State, x.New.State));
            myEntities.InputBoolean.Ishome.StateAllChanges().Where(x => !x.Old.IsUnavailable()).Subscribe(x => HandleIsHome(x.New.State));



        }

        private void HandleIsHome(string state)
        {
            if (myEntities.Fan.Starkvind.IsOff()) return;


        }


        private void Notify(string oldState, string newState)
        {
            if (oldState == newState) return;

            int indexOfold = airqualityValues.IndexOf(oldState.ToLower());
            int indexOfnew = airqualityValues.IndexOf(newState.ToLower());

            string direction = indexOfnew < indexOfold ? "improved" : "fell";

            queuedMessage = "Air Quality " + direction + " to:" + newState;

            if( DateTime.Now > nextAllowedAnnoucmentTime)
            {
                Read();
                annoucmentCancel?.Dispose();
            }
            else
            {
                annoucmentCancel?.Dispose();
                annoucmentCancel =  myScheduler.Schedule(nextAllowedAnnoucmentTime,Read);
            }
        }

        private string getMessage()
        {
            return queuedMessage;
        }

        private void Read()
        {
            int notificationIndex = airqualityValues.IndexOf(myEntities.Sensor.StarkvindAirQuality.State);
          
            if(lastNotificationIndex != notificationIndex)
            {
                lastNotificationIndex = notificationIndex;
                nextAllowedAnnoucmentTime = DateTime.Now + minAnnoucmentInterval;
                TTS.Speak(getMessage());
            }

        }


        private void HourlyTurnOnCheck()
        {
            if (ShouldTurnOn())
            {
                if (myEntities.Fan.Starkvind.IsOff())
                {
                    TurnOn();
                    myEntities.InputBoolean.StarkvindOnbyauto.TurnOn();

                }
                else
                {
                    myEntities.InputBoolean.StarkvindOnbyauto.TurnOff();
                }
            }
            else if (myEntities.InputBoolean.StarkvindOnbyauto.IsOn())
            {
                TurnOff();
                myEntities.InputBoolean.StarkvindOnbyauto.TurnOff();
            }
            else if (myEntities.BinarySensor.CheapElectricity.IsOff())
            {
                TurnOff();
            }
        }

        private bool ShouldTurnOn()
        {
            return myEntities.BinarySensor.CheapElectricity.IsOn() && myEntities.Sensor.Nordpool.State == myEntities.Sensor.Nordpool.Attributes.Min;
        }

        private void TurnOn()
        {
            myEntities.Fan.Starkvind.SetPercentage(myEntities.InputBoolean.Ishome.IsOff() ? 100 : (myEntities.InputBoolean.Isasleep.IsOff() ?  (long)myEntities.InputNumber.StarkvindDefaultSpeed.State : (long)myEntities.InputNumber.StarkvindSleepSpeed.State));
        }
        private void TurnOff()
        {
            myEntities.Fan.Starkvind.TurnOff();

        }

    }
}
