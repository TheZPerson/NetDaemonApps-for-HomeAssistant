using HomeAssistantGenerated;
using NetDaemon.HassModel;
using NetDaemon.HassModel.Entities;
using System.Linq;


namespace NetDaemonApps.apps.Lights
{
    /// <summary>
    /// Base class that that makes it so that only one light of given collection can be on at one. 
    /// Once one light is turned on the others lights are automatically turned off.
    /// </summary>
    public abstract class AllowOnlyOneLightGroup : AppBase
    {
        protected readonly LightEntity[] lights;

        public AllowOnlyOneLightGroup()
        {
            lights = SetLights();

            foreach (LightEntity light in lights)
            {
                light.StateChanges().Where(x => x?.New?.State == "on" && isEnabled()).Subscribe(_ => OnLightTurnOn(light));
            }

        }


        protected virtual bool isEnabled()
        {
            return myEntities.InputBoolean.GuestMode.IsOff();
        }

        private void OnLightTurnOn(LightEntity poweredLight)
        {
            if(myEntities.InputBoolean.GuestMode.IsOn())return;

            foreach (LightEntity light in lights)
            {
                if (light != poweredLight && !light.IsUnavailable()) light.TurnOff();
            }
        }

        protected virtual LightEntity[] SetLights()
        {
            return new LightEntity[0];
        }

    }
}
