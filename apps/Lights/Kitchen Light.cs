using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps.Lights
{
    [NetDaemonApp]
    public class Kitchen_Light : AppBase
    {
        public static NumericSensorEntity luxSensorEntity;

        public void TurnOn()
        {
            if (myEntities.InputBoolean.GuestMode.IsOn()) return;
            myEntities.Light.KitchenLight2.TurnOnLight();
        }
        public void TurnOff()
        {
            if (myEntities.InputBoolean.GuestMode.IsOn()) return;
            if (myEntities.Sensor.Livingroomfp1PresenceEvent.State == "approach") return;
            if (myEntities.BinarySensor.FridgeContactSensorContact.IsOn() ) return;
            myEntities.Light.KitchenLight2.TurnOffLight();
        }
        public Kitchen_Light() {


            myEntities.BinarySensor.FridgeContactSensorContact.StateChanges().Where(x => ((bool)x?.New.IsOn())).Subscribe(_ => {
                TurnOn();
                IsHomeManager.CancelIsHome();
            });

            myEntities.BinarySensor.FridgeContactSensorContact.StateChanges().WhenStateIsFor(x => ((bool)x?.IsOff()), TimeSpan.FromSeconds(50), myScheduler).Subscribe(_ => {
                TurnOff();
            });

            myEntities.Sensor.Livingroomfp1PresenceEvent.StateChanges().Where(x => x.New?.State == "approach").Subscribe(_ => {
                TurnOn();
            });

            myEntities.BinarySensor.Livingroomfp1Presence.StateChanges().Where(x => x.New.IsOff() && myEntities.BinarySensor.FridgeContactSensorContact.IsOff()).Subscribe(_ => {

                TurnOff();
            });

            myEntities.Sensor.Livingroomfp1PresenceEvent.StateChanges().Where(x => (x?.New?.State != "approach")).Subscribe(_ => {
                TurnOff();
            });

        }

    }
}
