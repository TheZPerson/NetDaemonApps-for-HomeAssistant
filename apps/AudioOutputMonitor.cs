using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetDaemon.Extensions.Scheduler;
using System.Reactive.Concurrency;

namespace NetDaemonApps.apps
{
 
    public class AudioOutputMonitor : AppBase
    {
        private Dictionary<Entity, bool> isConnectedCondition = new Dictionary<Entity, bool>();
        private IDisposable? offtimer;
        public AudioOutputMonitor()
        {
            myEntities.Sensor.EnvyAudioDefaultDevice.StateChanges().Where(x => x?.New?.State == "Headphone (Realtek(R) Audio)").Subscribe(_ => CheckCondition(myEntities.Sensor.EnvyAudioDefaultDevice, true));
            myEntities.Sensor.EnvyAudioDefaultDevice.StateChanges().Where(x => x?.New?.State != "Headphone (Realtek(R) Audio)").Subscribe(_ => CheckCondition(myEntities.Sensor.EnvyAudioDefaultDevice, false));
            myEntities.Sensor.PcAudioDefaultDevice.StateChanges().Where(x => x?.New?.State == "Speakers (Realtek(R) Audio)").Subscribe(_ => CheckCondition(myEntities.Sensor.PcAudioDefaultDevice, true));
            myEntities.Sensor.PcAudioDefaultDevice.StateChanges().Where(x => x?.New?.State != "Speakers (Realtek(R) Audio)").Subscribe(_ => CheckCondition(myEntities.Sensor.PcAudioDefaultDevice, false));
            myEntities.BinarySensor.PortableHeadphoneSensors.StateChanges().Where(x => (bool)(x?.New?.IsOn())).Subscribe(_ => CheckCondition(myEntities.BinarySensor.PortableHeadphoneSensors, true));
            myEntities.BinarySensor.PortableHeadphoneSensors.StateChanges().Where(x => (bool)(x?.New?.IsOff())).Subscribe(_ => CheckCondition(myEntities.BinarySensor.PortableHeadphoneSensors, false));

            isConnectedCondition.Add(myEntities.Sensor.EnvyAudioDefaultDevice, myEntities.Sensor.EnvyAudioDefaultDevice.State == "Headphone (Realtek(R) Audio)");
            isConnectedCondition.Add(myEntities.Sensor.PcAudioDefaultDevice, myEntities.Sensor.PcAudioDefaultDevice.State == "Speakers (Realtek(R) Audio)");
            isConnectedCondition.Add(myEntities.BinarySensor.PortableHeadphoneSensors, myEntities.BinarySensor.PortableHeadphoneSensors.IsOn());
        }

        private void CheckCondition(Entity trueConditionEntity, bool newState)
        {
            if (!isConnectedCondition.ContainsKey(trueConditionEntity)) isConnectedCondition.Add(trueConditionEntity, newState);
            isConnectedCondition[trueConditionEntity] = newState;

            CheckAllIsSleepConditions();

        }

        private bool check()
        {
        bool isConnected = false;

                foreach (bool cond in isConnectedCondition.Values)
                {
                    if (cond)
                    {
                        isConnected = true;
                        break;
                    };
                }
                return isConnected;
        }
        private void CheckAllIsSleepConditions()
        {
            if (myEntities.BinarySensor._19216801.IsOff()) return;
            bool isConnected = check();

            // If all conditions are true or false, we might need to change isSleep-state
            if (!isConnected)
            {
                offtimer?.Dispose();
                offtimer = myScheduler.Schedule(TimeSpan.FromMinutes(5), () => {

                    if (!check() && (myEntities.BinarySensor._19216801.IsOn()))
                    {
                            myEntities.Switch.FanPlug.TurnOff();
                    }
                });
                

            }
            else {
                offtimer?.Dispose();
                myEntities.Switch.FanPlug.TurnOn();
            }
        }
    }
}
