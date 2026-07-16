using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    
    public class FridgeAutomation : AppBase
    {
        /*
        private int knownFridgeIndex;
        private int desiredFridgeIndex;
        private int pressTime = 5000;
        private bool switchingmode = false;
 

        public FridgeAutomation()
        {
            knownFridgeIndex = int.Parse(_0Gbl._myEntities.InputSelect.FridgeCoolingLevel.State);

            _0Gbl._myEntities.Switch.SwitchbotAirPurifierPower.StateChanges().Subscribe(x => {

                _0Gbl._myEntities.InputSelect.FridgeCoolingLevel.SelectNext(true);
                knownFridgeIndex = knownFridgeIndex + 1 == 7 ? 1 : knownFridgeIndex + 1;
                if (knownFridgeIndex != desiredFridgeIndex)
                {
                    switchingmode = true;
                    _0Gbl._myEntities.Switch.SwitchbotAirPurifierPower.Toggle();
                }
                else switchingmode = false;

            });

            _0Gbl._myEntities.InputSelect.FridgeCoolingLevel.StateChanges().Subscribe(x => {

                if (switchingmode) return;

                desiredFridgeIndex = int.Parse(_0Gbl._myEntities.InputSelect.FridgeCoolingLevel.State);
                if (knownFridgeIndex != desiredFridgeIndex)
                {
                    switchingmode = true;
                    _0Gbl._myEntities.Switch.SwitchbotAirPurifierPower.Toggle();
                }
                else switchingmode = false;
            } );

            _0Gbl._myEntities.InputSelect.FridgeCoolingLevel.SelectOption("6");

            
        }
        */
    }
}
