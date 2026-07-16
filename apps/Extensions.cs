using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using NetDaemonApps.apps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NetDaemonApps
{
    public static class Extensions
    {
        private static readonly string[] bans = { "unavailable", "unknown" };
        public static int getBrightness(this LightEntity light)
        {
            int brightness = 0;

            try
            {
                brightness = ((JsonElement)light?.EntityState.Attributes.Brightness).GetInt32();
            }
            catch { }
            return brightness;
            
        }

        public static DateTime GetDateTime(this InputDatetimeEntity inputDatetime)
        {
            return new DateTime((int)inputDatetime.Attributes.Year, (int)inputDatetime.Attributes.Month, (int)inputDatetime.Attributes.Day, (int)inputDatetime.Attributes.Hour, (int)inputDatetime.Attributes.Minute, (int)inputDatetime.Attributes.Second );
        }

        public static TimeSpan GetTimeSpan(this InputDatetimeEntity inputDatetime)
        {
            return new TimeSpan((int)inputDatetime.Attributes.Hour, (int)inputDatetime.Attributes.Minute,0);
        }
        public static void TurnOnLight(this LightEntity light)
        {
            SetLightState(light, true);
        }
        public static void TurnOffLight(this LightEntity light)
        {
            SetLightState(light, false);
        }

        private static void SetLightState(LightEntity light, bool state)
        {
            if (light == null) return;

            if(Notifications._sensorsOnBooleanEntity == null || Notifications._sensorsOnBooleanEntity.IsOn())
            {
                if (state) light.TurnOn();
                else light.TurnOff();
            }
        }

        public static bool IsUnavailable(this Entity? ent)
        {
            if (ent == null) return true;

            return IsUnavailable(ent.EntityState);

        }
        public static bool IsUnavailable(this EntityState? state)
        {
            if (state == null) return true;

            return bans.Contains(state.State);

        }
        public static void TurnOnWithSensor(this LightEntity light, NumericSensorEntity? luminanceSensorEntity, double maxFlux = double.MaxValue)
        {
            if (light == null) return;


            if ((luminanceSensorEntity?.State ?? 0) < maxFlux || luminanceSensorEntity == null || luminanceSensorEntity.State == null)
            {
                light.TurnOnLight();
            }

        }

        public static List<LightEntity> lightEntitiesFromSelectionDropdown(this InputSelectEntity slectionEntity)
        {
            if (slectionEntity == null) return new List<LightEntity>();
            List<LightEntity> list = new List<LightEntity>();
            foreach (string entityId in slectionEntity.Attributes.Options)
            {
                list.Add(new LightEntity(A0Gbl.HaContext, entityId));

            }
            return list;
        }
        public static List<string> stringsFromSelectionDropdown(this InputSelectEntity slectionEntity)
        {
            return slectionEntity.Attributes.Options.ToList();
        }


        public static List<LightEntity> LightgroupToEntytiList(IEnumerable<string> idList)
        {
            List<LightEntity> list = new List<LightEntity>();

            foreach (string entityId in idList)
            {
                list.Add(new LightEntity(A0Gbl.HaContext, entityId));
            }
            return list;
        }

        public static void AddValue(this InputNumberEntity entity, double value)
        {
            if (entity == null) return;
            entity.SetValue((entity.State ?? 0) + value);
        }


        public static bool StateFor(this Entity entity, TimeSpan time)
        {
           return entity.EntityState.LastChanged < DateTime.Now - time;
        }
    }
}
