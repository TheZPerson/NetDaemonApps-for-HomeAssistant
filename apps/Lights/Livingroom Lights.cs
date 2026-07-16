using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System.Collections.Generic;

namespace NetDaemonApps.apps.Lights
{
    [NetDaemonApp]
    public class Livingroom_Lights : AllowOnlyOneLightGroup
    {
        public Livingroom_Lights() : base() { 
        
           
      
        }


        protected override bool isEnabled()
        {
            return base.isEnabled() && myEntities.InputBoolean.LightgroupLivingroomEnabled.IsOn();
        }

        protected override LightEntity[] SetLights()
        {
            return myEntities.InputSelect.LivingRoomLights.lightEntitiesFromSelectionDropdown().ToArray();
        }

    }
}
