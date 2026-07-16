using HomeAssistantGenerated;
using NetDaemon.Extensions.Scheduler;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using NetDaemon.HassModel.Entities;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class Notifications : AppBase
    {
        public static InputBooleanEntity? _sensorsOnBooleanEntity = null;

        public DateTime lastMemoryAlert = DateTime.MinValue;

        public Notifications() {

            _sensorsOnBooleanEntity = myEntities.InputBoolean.SensorsActive;

            myEntities.InputBoolean.Ishome.StateChanges().Where(x => x.New.IsOn()).Subscribe(_ => {
                if (myEntities.BinarySensor.SolarChargingLimit.IsOn() && myEntities.Sensor.EcoflowSolarInPower.State == 0)
                 {
               TTS.Speak("There is potential for solar charging");
                 }
            });

        }

        public static void RegisterStateNotification(Entity ent, string entityName)
        {
            ent.StateChanges().Where(x => !x.Old.IsUnavailable() && !x.New.IsUnavailable() ).Subscribe(_ => {

                TTS.Speak(entityName + " " + ent.State);
            
            });
        }

        public record ActionableNotificationResponseData
        {
            [JsonPropertyName("action")] public string? action { get; init; }    
            [JsonPropertyName("data")] public JsonElement? data { get; init; }

            [JsonPropertyName("tag")] public string? tag { get; init; }


        }

        public class Actionable_NotificationData
        {
          
            public string message { get; set; }
            public Dictionary<string, List<ActionableData>> data { get; set; }
           

            public Actionable_NotificationData(string message)
            {
                this.message = message;
                data = new Dictionary<string, List<ActionableData>>();
             
             
            }


            public void PushToData(string key, List <ActionableData> value)
            {
                data.Add(key, value);
            }


        }


        [JsonDerivedType(typeof(TapableAction))]
        [JsonDerivedType(typeof(TimeoutData))]
        public class ActionableData
        {

        }
        public class TimeoutData : ActionableData
        {

            public int timeout { get; set; }
            public TimeoutData(int timeout)
            {
                this.timeout = timeout;
            }

        

            public override string ToString()
            {
                return timeout.ToString();
            }
        }

            public class TapableAction : ActionableData
        {
            public string? tag { get; set; } = null;
            public string? action { get; set; } = null;
            public string? title { get; set; } = null;

            public int? timeout { get; set; } = 5;

            public TapableAction(string? action = null, string? title = null, string? tag =  null, int? timeout = null)
            {
                this.action = action;
                this.title = title;
                this.tag = tag;
                this.timeout = timeout;

            }


            
        }


    }
}
