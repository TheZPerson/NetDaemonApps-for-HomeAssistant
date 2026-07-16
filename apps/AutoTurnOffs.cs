using HomeAssistantGenerated;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using NetDaemonApps.apps.Lights;
using System.Collections;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Reactive.Concurrency;
using System.Collections.Generic;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class AutoTurnOffs : AppBase
    {
        private IDisposable? toiletLightOffWaiterPC = null;
        private IDisposable? toiletLightOffWaiterLaptop = null;
        private IDisposable? kitchenLightOffWaiterPc = null;
        private IDisposable? kitchenLightOffWaiterLaptop = null;
        Dictionary<LightEntity, AutorunDisposables> lightOffDisposables = new Dictionary<LightEntity, AutorunDisposables>(); 

        protected class AutorunDisposables
        {
            public IDisposable pc = null;
            public IDisposable laptop = null;
            public IDisposable disposer = null;
        }
        public AutoTurnOffs() {

            myEntities.Sensor.PcDisplayDisplayCount.StateChanges().Subscribe(_ => { myEntities.Button.PcResetbrigtness.Press(); });
            //_0Gbl._myEntities.Sensor.PcDisplayDisplay1Resolution.StateChanges().Subscribe(_ => { _0Gbl._myEntities.Button.PcResetbrigtness.Press(); });

            myEntities.Light.KitchenLight2.StateChanges().WhenStateIsFor(x => x?.State == "on" && myEntities.InputBoolean.GuestMode.IsOff() , TimeSpan.FromHours(1), myScheduler)
                .Subscribe(x => { myEntities.Light.KitchenLight2.TurnOffLight(); });

            myEntities.Light.ToiletLight1.StateChanges().WhenStateIsFor(x => x?.State == "on", TimeSpan.FromHours(1), myScheduler)
               .Subscribe(x => { myEntities.Light.ToiletLight1.TurnOff();});

            void sub(LightEntity light)
            {
                lightOffDisposables.TryAdd(light, new AutorunDisposables());
                
                light.StateChanges()
                        .Subscribe(x => {
                            lightOffDisposables[light].pc?.Dispose();
                            lightOffDisposables[light].laptop?.Dispose();
                            lightOffDisposables[light].disposer?.Dispose();

                            if (light.IsOn() && myEntities.InputBoolean.GuestMode.IsOff() && myEntities.InputBoolean.SensorsActive.IsOn())
                            {
                               myScheduler.Schedule(TimeSpan.FromSeconds(60), () => {
                                   if (light.IsOn())
                                   {
                                       lightOffDisposables[light].pc = myEntities.Sensor.PcLastactive.StateChanges().Where(x => !x.Old.IsUnavailable() && !x.New.IsUnavailable()).Subscribe(x => {
                                           light.TurnOff();
                                           lightOffDisposables[light].pc?.Dispose();
                                           lightOffDisposables[light].pc = null;
                                       });
                                       lightOffDisposables[light].laptop = myEntities.Sensor.EnvyLastactive.StateChanges().Where(x => !x.Old.IsUnavailable() && !x.New.IsUnavailable() && myEntities.Sensor.EnvyNetworkTotalNetworkCardCount.State == "1").Subscribe(x => {

                                           light.TurnOff();
                                           lightOffDisposables[light].laptop?.Dispose();
                                           lightOffDisposables[light].laptop = null;

                                       });
                                   }

                               });
                                    lightOffDisposables[light].disposer= myScheduler.Schedule(TimeSpan.FromSeconds(600), () => {
                                    if(lightOffDisposables[light].pc != null)
                                    {
                                        lightOffDisposables[light].pc?.Dispose();
                                        lightOffDisposables[light].laptop?.Dispose();
                                    }
                                });
                            }



                            if (light.IsOff())
                            {
                                lightOffDisposables[light].pc?.Dispose();
                                lightOffDisposables[light].laptop?.Dispose();
                                lightOffDisposables[light].disposer?.Dispose();
                            }


                        });
            }
           // sub(_myEntities.Light.ToiletLight1);
           // sub(_myEntities.Light.KitchenLight2);
           // sub(_myEntities.Light.HallwayLight);
           // sub(_myEntities.Light.StorageLight2);

            myEntities.Sensor.EnvyBatteryChargeRemainingPercentage.StateChanges().Where(x => x?.New?.State < myEntities.InputNumber.SettingsLaptopchargerturnonpercent.State)
                .Subscribe(_ => {

                myEntities.Switch.PcMultipowermeterLaptop.TurnOn();          
            });

            myEntities.Sensor.EnvyBatteryChargeRemainingPercentage.StateChanges().Where(x => x?.New?.State >= 99)
                .Subscribe(_ => {
                    myEntities.Switch.PcMultipowermeterLaptop.TurnOff();
                });

            myEntities.BinarySensor.LivingroomWindowSensorContact.StateChanges().Where(x => x?.New?.State == "on" && x?.Old?.State == "off").Subscribe(_ => {
              //  _00_Globals._myEntities.Switch.TbdPowermeter.TurnOff();
            });

            myEntities.BinarySensor.LivingroomWindowSensorContact.StateChanges().Where(x => x?.New?.State == "off" && x?.Old?.State == "on").Subscribe(_ => {
               // _00_Globals._myEntities.Switch.TbdPowermeter.TurnOn();
            });

        }
    }
}
