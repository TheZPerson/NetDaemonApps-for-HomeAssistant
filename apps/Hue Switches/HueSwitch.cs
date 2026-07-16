using HomeAssistantGenerated;
using NetDaemon.HassModel;

namespace NetDaemonApps.apps.Hue_Switches
{
    public abstract class HueSwitch : AppBase
    {
        protected SensorEntity ?hueSwitchEntity;

        private readonly string[] events = { "on_press", "on_press_release", "on_hold", "on_hold_release", "up_press", "up_press_release", "up_hold", "up_hold_release", "down_press", "down_press_release", "down_hold", "down_hold_release", "off_press", "off_press_release", "off_hold", "off_hold_release" };
        public HueSwitch() {
            hueSwitchEntity = ObtainSwitch(myEntities);


            if (hueSwitchEntity == null) return;

          //  hueSwitchEntity.StateAllChanges().Subscribe(x => DetermineAction(x?.Entity?.State ?? "Unknown"));

            foreach (var e in events)
            {
                var triggerObservable = myTriggerManager.RegisterTrigger(
                 new
                 {
                     platform = "state",
                     entity_id = new string[] { hueSwitchEntity.EntityId },
                     to = e
                 });
                 triggerObservable.Subscribe(n => DetermineAction(e)
                    );
            }

        }

        private void DetermineAction(string stateName)
        {
            Console.WriteLine("State:" + stateName);
            if (IsHomeManager.CancelIsHome()) return;
            switch (stateName)
            {
                // Power Button
                case "on_press":
                    OnPowerPress();
                break;

                case "on_press_release":
                    OnPowerRelease();
                break;

                case "on_hold":
                    OnPowerHold();
                break;

                case "on_hold_release":
                    OnPowerHoldRelease();
                break;

                //Brightness Up
                case "up_press":
                    OnUpPress();
                break;

                case "up_press_release":
                    OnUpPressRelease();
                break;

                case "up_hold":
                    OnUpHold();
                break;

                case "up_hold_release":
                    OnUpHoldRelease();
                break;

                //Brightness Down
                case "down_press":
                    OnDownPress();
                    break;

                case "down_press_release":
                    OnDownPressRelease();
                    break;

                case "down_hold":
                    OnDownHold();
                    break;

                case "down_hold_release":
                    OnDownHoldRelease();
                break;


                // Hue Button
                case "off_press":
                    OnHuePress();
                    break;

                case "off_press_release":
                    OnHueRelease();
                    break;

                case "off_hold":
                    OnHueHold();
                    break;

                case "off_hold_release":
                    OnHueHoldRelease();
                    break;


            }

        }

        protected virtual SensorEntity ObtainSwitch(Entities entities)
        {
            return null;
        }

        protected virtual void OnAnyPress() { OnAny();  }
        protected virtual void OnAnyPressRelease() { OnAny(); }
        protected virtual void OnAnyHold() { OnAny(); }
        protected virtual void OnAnyHoldRelease() { OnAny(); }

        protected virtual void OnAny()
        {

        }
        /// <summary> Power-button Press Down</summary>
        protected virtual void OnPowerPress() { OnAnyPress(); }
        /// <summary> Power-button Press Release</summary>
        protected virtual void OnPowerRelease() { OnAnyPressRelease(); }
        /// <summary> Power-button Hold</summary>
        protected virtual void OnPowerHold() { OnAnyHold(); }
        /// <summary> Power-button Hold release</summary>
        protected virtual void OnPowerHoldRelease() { OnAnyHoldRelease(); }

        /// <summary> Brighness Up-button Press Down</summary>
        protected virtual void OnUpPress() { OnAnyPress(); }
        /// <summary> Brighness Up-button Press Release</summary>
        protected virtual void OnUpPressRelease() { OnAnyPressRelease(); }
        /// <summary> Brighness Up-button Hold</summary>
        protected virtual void OnUpHold() { OnAnyHold(); }
        /// <summary> Brighness Up-button Hold release</summary>
        protected virtual void OnUpHoldRelease() { OnAnyHoldRelease(); }

        /// <summary> Brighness Down-button Press Down</summary>
        protected virtual void OnDownPress() { OnAnyPress(); }
        /// <summary> Brighness Down-button Press Release</summary>
        protected virtual void OnDownPressRelease() { OnAnyPressRelease(); }
        /// <summary> Brighness Down-button</summary>
        protected virtual void OnDownHold() { OnAnyHold(); }
        protected virtual void OnDownHoldRelease() { OnAnyHoldRelease(); }

        /// <summary> Hue-button Press Down</summary>
        protected virtual void OnHuePress(){ OnAnyPress(); }
        /// <summary> Hue-button Press Release</summary>
        protected virtual void OnHueRelease() { OnAnyPressRelease(); }
        /// <summary> Hue-button Hold</summary>
        protected virtual void OnHueHold(){ OnAnyHold(); }
        /// <summary> Hue-button Hold release</summary>
        protected virtual void OnHueHoldRelease(){ OnAnyHoldRelease(); }





 
    }
}
