using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDaemon.Extensions.Scheduler;
using System.Reactive.Concurrency;

namespace NetDaemonApps.apps.Hue_Switches
{
 
    public class BedHueSwitch : HueSwitch
    {
        private IDisposable? keepOnRoutine = null;
        public BedHueSwitch() : base(){}

        protected override SensorEntity ObtainSwitch(Entities entities)
        {
            return null;
        }


        protected override void OnPowerPress()
        {
            base.OnPowerPress();

            if (myEntities.Light.ToiletLight1.IsOn())
            {
                myEntities.Light.ToiletLight1.TurnOffLight();
                if (keepOnRoutine != null)
                {
                    keepOnRoutine.Dispose();
                    Lights.Toiler_Light_Automation.forceLightOn = false;
                }
            }
            else
            {
                myEntities.Light.ToiletLight1.TurnOn();
                if (keepOnRoutine != null)
                {
                    keepOnRoutine.Dispose();
                }
                    keepOnRoutine = myScheduler.Schedule(TimeSpan.FromMinutes(5), () => {

                    if(myEntities.BinarySensor.ToiletSensorOccupancy.IsOff())
                    {
                        myEntities.Light.ToiletLight1.TurnOff();
                    }
                  
              
                });
                Lights.Toiler_Light_Automation.forceLightOn = true;
            }
       
        }

        protected override void OnAnyPress()
        {
            base.OnAnyPress();
          //  _0Gbl._myEntities.Switch.TvPowerMeter.TurnOn();
        }

        //Fan
        protected override void OnHueRelease()
        {
          //  _0Gbl._myEntities.Switch.BedMultiPlugL1.Toggle(); 
        }

        protected override void OnHueHoldRelease()
        {
          //  _0Gbl._myEntities.Script.ReadoutTime.TurnOn();
        }




    }
}
