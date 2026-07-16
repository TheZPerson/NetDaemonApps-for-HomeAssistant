using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    public class NedisRemote : AppBase
    {

        public NedisRemote() {

            RemoteEntity?.StateChanges().Subscribe(state => { handleSensor(state.New.State ?? ""); });
        }
        public virtual SensorEntity? RemoteEntity { get; private set; }

        private void handleSensor(string state)
        {
            if (RemoteEntity == null) return;

            switch (state)
            {
                case "emergency":
                    On_O();
                    break;
                case "arm_day_zones":
                    On_A();
                    break;
                case "arm_all_zones":
                    On_I();
                    break;
                case "disarm":
                    On_B();
                    break;

            }



        }
        protected virtual void OnAny() { }

        protected virtual void On_O() { OnAny(); }

        protected virtual void On_A() { OnAny(); }

        protected virtual void On_I() { OnAny(); }
        protected virtual void On_B() { OnAny(); }

    }


    public class TreadmillRemote : NedisRemote
    {

    }



        /*
        public class TreadmillRemote : NedisRemote
        {


            public override SensorEntity? RemoteEntity => myEntities.Sensor.KeychainAction;

            public TreadmillRemote() {

                myEntities.Sensor.ModemAutoOnPlugPower.StateChanges().WhenStateIsFor(x => x?.State < 19, TimeSpan.FromMinutes(10), myScheduler).Subscribe(x => {

                    DeActivateRunningPad();
                    runnerSwitch?.TurnOff();

                });


                myEntities.BinarySensor.WalkingpadContactContact.StateChanges().Where(x => x.New.IsOff()).Subscribe(x => { runnerSwitch?.TurnOn(); });
                myEntities.BinarySensor.WalkingpadContactContact.StateChanges().Where(x => x.New.IsOn()).Subscribe(x => { runnerSwitch?.TurnOff(); });

                runnerSwitch?.StateChanges().Where(x => x.New.IsOn()).Subscribe(x => { ActivateRunningPad(); });


            }

            protected bool hasProcess()
            {
                return myEntities.Sensor.PcWalkingpadActive2.State == "1";
            }
            protected SwitchEntity runnerSwitch { get { return myEntities.Switch.BedMultiPlugL2; } }
            protected void ActivateRunningPad()
            {
                bool wasPlugOn = true;
                if (runnerSwitch.IsOff())
                {
                    runnerSwitch.TurnOn();
                    wasPlugOn = false;
                }

                if (!hasProcess())
                {
                    myScheduler.Schedule(TimeSpan.FromSeconds(wasPlugOn ? 0 : 5), myEntities.Button.PcStartrunningpadprocess.Press);
                }


            }

            protected void DeActivateRunningPad()
            {
                bool wasRunning = false;
                if (myEntities.Sensor.ModemAutoOnPlugPower.State > 15)
                {
                    ToggleRunner();
                    wasRunning = true;
                }

                if (hasProcess())
                {
                    myScheduler.Schedule(TimeSpan.FromSeconds(wasRunning ? 30 : 0), myEntities.Button.PcStopwalkingpadprocess.Press);
                }

            }

            protected void ToggleRunner()
            {
                if (hasProcess())
                {
                    Console.WriteLine("Toggle");
                    myEntities.Button.WalkingpadToggle.Press();
                }
            }

            protected override void On_O()
            {
                base.On_O();
                if (runnerSwitch.IsOff() || myEntities.Sensor.PcWalkingpadActive.State != "1")
                {
                    ActivateRunningPad();
                    return;
                }
                else
                {
                    ToggleRunner();
                }
            }

            protected override void On_A()
            {
                base.On_A();
                if (myEntities.Sensor.ModemAutoOnPlugPower.State < 15)
                    runnerSwitch.Toggle();
                else
                {
                    DeActivateRunningPad();
                }
            }

            protected override void On_I()
            {
                base.On_I();
                myEntities.Button.WalkingpadUp.Press();
            }

            protected override void On_B()
            {
                base.On_B();
                myEntities.Button.WalkingpadDown.Press();
            }


        }    */
    }
