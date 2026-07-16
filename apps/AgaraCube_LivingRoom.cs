using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{

    public class AgaraCube_LivingRoom : AqaraCube
    {

        public AgaraCube_LivingRoom() : base()
        {



        }

        protected override SensorEntity? SetCubeActionEntity() { return null; }
        protected override SensorEntity? SetCubeSideEntity() { return myEntities?.Sensor.CubeSide; }


        protected override void OnShake()
        {
            base.OnShake();

            if (myEntities.Sensor.EcoflowStatus.State.ToLower() != "online")
            {

                myEntities.Switch.SwitchbotEcoflow.Toggle();

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

                    myEntities.Switch.EcoflowAcEnabled.TurnOn();


                });



            }
            else if (myEntities.Sensor.EcoflowOutputPlugPower.State == 0)
            {
                myEntities.Switch.EcoflowPlug.TurnOff();
                Task.Run(async () =>
                {
                    await Task.Delay(5000);

                    myEntities.Switch.SwitchbotEcoflow.Toggle();


                });
              

                if(myEntities.Sensor.EcoflowSolarInPower.State > 0)
                {
                    TTS.Speak("Solar Panels are active", TTS.TTSPriority.Default);
                }

            }

        }
    }

}
