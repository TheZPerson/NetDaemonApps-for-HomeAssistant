using HomeAssistantGenerated;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps.Lights
{
    [NetDaemonApp]
    public class LightsAndSensors : AppBase
    {
        public static readonly TimeSpan defaulMotionTimeout = new TimeSpan(0, 0, 10);

        public static readonly double defaultFluz = 700;
        DateTime lastStorageOff = DateTime.MinValue;
        public LightsAndSensors()
        {
       
            SubcribeLightOn(myEntities.BinarySensor.HallwaySensorOccupancy, myEntities.Light.HallwayLight);
            SubcribeLightOff(myEntities.BinarySensor.HallwaySensorOccupancy, myEntities.Light.HallwayLight, defaulMotionTimeout);

            myEntities.BinarySensor.StorageSensorOccupancy.StateChanges().Where(x => x.New.IsOff()).Subscribe(_ => {

                lastStorageOff = DateTime.Now;

            });
            SubcribeLightOn(myEntities.BinarySensor.StorageSensorAqaraOccupancy, myEntities.Light.StorageLight2);

            SubcribeLightOff(myEntities.BinarySensor.StorageSensorAqaraOccupancy, myEntities.Light.StorageLight2, new TimeSpan(0, 0, 5));

        }

        private void SubcribeLightOn(BinarySensorEntity sensor, LightEntity light, Func<bool>? extraConditions = null, NumericSensorEntity? fluzSensor = null, double maxFlux = double.MaxValue)
        {          
            sensor.StateChanges().Where(e => e.New?.State == "on" && myEntities.InputBoolean.SensorsActive.IsOn() && (extraConditions == null || extraConditions.Invoke())).Subscribe(_ => { light.TurnOnWithSensor(fluzSensor, maxFlux); });
        }

        private void SubcribeLightOff(BinarySensorEntity sensor, LightEntity light, TimeSpan offTime, Func<bool>? extraConditions = null)
        {
            sensor.StateChanges().Where(e => extraConditions != null ? extraConditions.Invoke() : true).WhenStateIsFor(e => e.IsOff() && myEntities.InputBoolean.SensorsActive.IsOn(), offTime,myScheduler).Subscribe(e => { light.TurnOffLight(); });
            //extraConditions != null ? extraConditions.Invoke() : true
        }


    }
}
