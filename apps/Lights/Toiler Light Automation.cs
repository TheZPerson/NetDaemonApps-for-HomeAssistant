using HomeAssistantGenerated;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps.Lights
{
    [NetDaemonApp]
    public class Toiler_Light_Automation : AppBase
    {
        private List<LightEntity> targetLights = new List<LightEntity>();
        private List<LightEntity> lightsThatWereOn = new List<LightEntity>();

        private readonly TimeSpan lightOnTimeOutSpan = new TimeSpan(1, 0, 0);
        public static bool forceLightOn = false;
        private readonly TimeSpan motionSensorTimeOutSpan = new TimeSpan(0, 0, 10);

        public Toiler_Light_Automation()
        {
            targetLights.Add(myEntities.Light.BedLight);
            targetLights.Add(myEntities.Light.PcMultipowermeterL1);

            targetLights.Add(myEntities.Light.LivingRoomLight);
            targetLights.Add(myEntities.Light.DesktopLight);
            targetLights.Add(myEntities.Light.MonitorLight);

            myEntities.BinarySensor.ToiletSensorOccupancy.StateChanges().Subscribe(_ =>
             {
                  if (myEntities.InputBoolean.SensorsActive.IsOn() && myEntities.BinarySensor.ToiletSensorOccupancy.IsOn()) myEntities.Light.ToiletLight1.TurnOnLight();
                  else if (myEntities.BinarySensor.ToiletSensorOccupancy.IsOff() && !isSeatOpen()) myEntities.Light.ToiletLight1.TurnOffLight();
             });


            myEntities.BinarySensor.ToiletSeatSensorContact.StateChanges().Where(x=>x.New.IsOn()).Subscribe(_ => {

               if (myEntities.InputBoolean.SensorsActive.IsOff() || myEntities.InputBoolean.GuestMode.IsOn()) return;
                   OnToiledLidOpen();
               });

            myEntities.BinarySensor.ToiletSeatSensorContact.StateChanges().WhenStateIsFor(x => x.IsOff(),TimeSpan.FromSeconds(2), myScheduler).Subscribe(_ => {
                if (myEntities.InputBoolean.SensorsActive.IsOff() || myEntities.InputBoolean.GuestMode.IsOn()) return;
                OnToiledLidClose();
            });



            /*
                        _myEntities.BinarySensor.HallwaySensorOccupancy.StateChanges()
                        .Where(e => e.New.IsOn() && _myEntities.InputBoolean.SensorsActive.IsOn() && _myEntities.InputBoolean.GuestMode.IsOff() && _myEntities.BinarySensor.ToiletSensorOccupancy.IsOff())
                        .Subscribe(_ =>
                        {
                            if (!isSeatOpen() && !forceLightOn)
                               _myEntities.Light.ToiletLight1.TurnOffLight();
                        });
            */

        }
      
        private bool isSeatOpen()
        {
            return myEntities.BinarySensor.ToiletSeatSensorContact.IsOn() ? true : false;
        }



    private void OnToiledLidOpen()
        {
            myEntities.Light.ToiletLight1.TurnOnLight();
            if (myEntities.InputBoolean.GuestMode.IsOn()) return;
            lightsThatWereOn.Clear();
            myEntities.Light.HallwayLight.TurnOffLight();
            foreach (LightEntity light in targetLights)
            {
                if (light != null && !light.IsUnavailable() && light.IsOn())
                {
                    lightsThatWereOn.Add(light);
                    light.TurnOffLight();
                }
            }

            //Turn off PC Monitors while in toilet
            /*if (_0Gbl._myEntities.Switch.PcPlug.IsOn())
                _0Gbl._myEntities.Button.PcTurnoffmonitors.Press();
            */


        }


        private void OnToiledLidClose()
        {
            if (myEntities.InputBoolean.GuestMode.IsOn()) return;

            foreach (LightEntity light in lightsThatWereOn)
            {
                light.TurnOnLight();
            }

            lightsThatWereOn.Clear();

            if(myEntities.BinarySensor.ToiletSensorOccupancy.IsOff()) myEntities.Light.ToiletLight1.TurnOffLight();

            //Turn on PC Monitors
            /*
            if (_0Gbl._myEntities.Switch.PcPlug.IsOn())
                _0Gbl._myEntities.Button.PcWakekey.Press();
            */
        }


    }
}
