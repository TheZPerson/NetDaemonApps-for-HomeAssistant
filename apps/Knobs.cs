using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using NetDaemonApps.apps.Lights;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class Knobs : AppBase
    {
        List<Knob> knobs = new List<Knob>();
        public Knobs() {

            knobs.Add(new DesktopKnob(myEntities.Sensor.KnobDesktopAction, myEntities.Sensor.KnobDesktopDelta));
            knobs.Add(new BedKnob(myEntities.Sensor.KnobBedAction, myEntities.Sensor.KnobBedDelta));
            knobs.Add(new SofaKnob(myEntities.Sensor.KnobCouchAction, myEntities.Sensor.KnobCouchDelta));

        }





        private abstract class Knob{

            SensorEntity action;
            protected bool flipDialDirection = false;
            SensorEntity knobDelta;

            int lasktKnownStep = 0;
            IDisposable knobStepWaiter;
            protected LightCycler lightCycler;

            public Knob(SensorEntity knobAction, SensorEntity _knobDelta)
            {
                action = knobAction;
                knobDelta = _knobDelta;

                knobDelta.StateChanges().Subscribe(x => OnBrightnesssDelta());

                action.StateAllChanges().Subscribe(x=>DetermineAction(x.New.State ?? ""));
       
            }


            protected virtual void OnBrightnesssDelta()
            {
             
                if (lightCycler.GetCurrentLight() == null) return;

                if (lightCycler.GetCurrentLight() != null && lightCycler.GetCurrentLight()?.Attributes?.SupportedFeatures != 0)
                {
                    long step;
                    if (long.TryParse(knobDelta.State, out step))
                    {
                        step = (int)Math.Round(step * (flipDialDirection ? -1 : 1) * myEntities.InputNumber.SettingsKnobSensitivity.State ?? 1);
       
                        long minBrightnessFix = Math.Min(((long)lightCycler.GetCurrentLight().getBrightness() + step), (long)255);
                        minBrightnessFix = (Math.Max(minBrightnessFix, (long)10));
                        lightCycler.GetCurrentLight().TurnOn(brightnessPct: (long)(((float)minBrightnessFix / 255f) * 100), transition:1);
                    }

                 
                }
            }
            protected virtual void OnHold()
            {
                myServices.Script.PlayInterfaceSound();
            }

            protected virtual void OnHoldRelease() { }

            private void DetermineAction(string stateName)
            {
                knobStepWaiter?.Dispose();
                if (IsHomeManager.CancelIsHome()) return;
                switch (stateName)
                {
                    case "brightness_step_down":
                        OnStepUp();
                    break;

                    case "rotate_left":
                        OnStepUp();
                     break;
                    case "rotate_right":
                        OnStepDown();
                        break;
                    case "brightness_step_up":
                        OnStepDown();
                    break;
                    case "single":
                        OnPress();
                        break;

                    case "toggle":
                        OnPress();
                        break;
                    case "double":
                        lightCycler.GetCurrentLight()?.TurnOffLight();
                        break;
                    case "hold":
                        OnHold();
                        break;

                    case "hue_move":
                        OnHold();
                        break;

                    case "hue_stop":
                        OnHoldRelease();
                        break;

                }

            }

            protected virtual void OnPress()
            {
                lightCycler?.NextLight();

            }

            protected virtual void OnStepUp() {

         
            }
            protected virtual void OnStepDown()
            {


            }
        }

        private class DesktopKnob : Knob
        {
            public DesktopKnob(SensorEntity knobAction, SensorEntity knobStep) : base(knobAction, knobStep)
            {
                flipDialDirection = false;
                lightCycler = new LightCycler(myEntities.InputBoolean.GuestMode, myEntities.InputBoolean.LightgroupLivingroomEnabled ,myEntities.InputSelect.DesktopKnobLights.lightEntitiesFromSelectionDropdown().ToArray());
            }

            protected override void OnPress()
            {
                base.OnPress();
                if (myEntities.InputBoolean.GuestMode.IsOff() || myEntities.InputBoolean.LightgroupLivingroomEnabled.IsOff() )
                {
                    myEntities.Light.BedLight.TurnOff();
                    myEntities.Light.DesktopLight.TurnOff();
                }
            }


            protected override void OnBrightnesssDelta()
            {
                return;
            }

        }


        private class BedKnob : Knob
        {
            public BedKnob(SensorEntity knobAction, SensorEntity knobStep) : base(knobAction, knobStep)
            {
                lightCycler = new LightCycler(myEntities.InputBoolean.GuestMode, myEntities.InputBoolean.LightgroupLivingroomEnabled , myEntities.InputSelect.BedKnobLights.lightEntitiesFromSelectionDropdown().ToArray());
            }

            protected override void OnHold()
            {
                base.OnHold();
            }
        }
        private class SofaKnob : Knob
        {
            public SofaKnob(SensorEntity knobAction, SensorEntity knobStep) : base(knobAction, knobStep)
            {
               lightCycler = new LightCycler(myEntities.InputBoolean.GuestMode, myEntities.InputBoolean.LightgroupLivingroomEnabled, myEntities.InputSelect.SofaKnobLights.lightEntitiesFromSelectionDropdown().ToArray());
            }
            protected override void OnBrightnesssDelta()
            {
                return;
            }
            protected override void OnPress()
            {
                base.OnPress();
               
            }
        }
    }
}
