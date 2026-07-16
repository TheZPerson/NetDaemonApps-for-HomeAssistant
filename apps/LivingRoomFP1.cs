using HomeAssistantGenerated;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    public class LivingRoomFP1 : AppBase
    {
        /*
        public static AqaraFP1Extender LivingRoomFP1;
        private InputTextEntity inputText;
        public _01_LivingRoomFP1() {

            Entities e = _0Gbl._myEntities;
            inputText = e.InputText.Fp1debugger;


            List<InputBooleanEntity> booleans = new List<InputBooleanEntity>();
            booleans.Add(e.InputBoolean.Pf1LrR1);
            booleans.Add(e.InputBoolean.Pf1LrR2);
            booleans.Add(e.InputBoolean.Pf1LrR3);
            booleans.Add(e.InputBoolean.Pf1LrR4);
            booleans.Add(e.InputBoolean.Pf1LrR5);
            booleans.Add(e.InputBoolean.Pf1LrR6);
            booleans.Add(e.InputBoolean.Pf1LrR7);
            booleans.Add(e.InputBoolean.Pf1LrR8);
            booleans.Add(e.InputBoolean.Pf1LrR9);
            booleans.Add(e.InputBoolean.Pf1LrR110);

            LivingRoomFP1 = new AqaraFP1Extender(e.BinarySensor.Livingroomfp1Presence, e.Sensor.Livingroomfp1PresenceEvent, e.Sensor.Livingroomfp1Action, booleans);
            
            foreach(var region in LivingRoomFP1.Regions)
            {
                region.callbacks.onEnter += FillDebug;
                region.callbacks.onExit += FillDebug;
            }

        }


        private void FillDebug(AqaraFP1Extender.FP1EventInfo info)
        {
            string text = "Entered: \r\n";
            foreach(var r in info.sensorData.enteredRegions)
            {
                text += r.name + "\r\n";
            }
            inputText.SetValue(text);

        }
        */
    }
}
