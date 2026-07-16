using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

//Rename to run after globals

namespace NetDaemonApps.apps
{
    public class AqaraFP1Extender : AppBase
    {
        /*
        public List<Region> Regions = new List<Region>();
        public List<Region> occupiedRegions= new List<Region>();
        public List<Region> enteredRegions = new List<Region>();
        public BinarySensorEntity sensorPresence;
        public SensorEntity presenseEventSensor;
        public SensorEntity actionSensor;

        public class Region
        {
            public int id;
            public string name;
            public InputBooleanEntity haIsEnteredBoolean;
            public delegate void RegionDelegate(FP1EventInfo eventInfo);
            public Region(int id, string name, InputBooleanEntity haIsEnteredBoolean)
            {
                this.id = id;
                this.name = name;


                callbacks.onOccupy += SetOccupyStatus;
                callbacks.onUnOccupy += SetOccupyStatus;

                callbacks.onEnter += SetEnterStatus;
                callbacks.onExit += SetEnterStatus;
              

                this.haIsEnteredBoolean = haIsEnteredBoolean;
                haIsEnteredBoolean.TurnOff();
            }

            public TimeSpan? TimeSinceLastExited()
            {
                return haIsEnteredBoolean.IsOn() ? TimeSpan.Zero : DateTime.Now - haIsEnteredBoolean?.EntityState?.LastUpdated;
            }

            public bool occupied { get; private set; }

            public Callbacks callbacks = new Callbacks();

            public void SetOccupyStatus(FP1EventInfo eventInfo)
            {
                if (eventInfo.eventType == Fp1EventType.Occupied) occupied = true;
                else if (eventInfo.eventType == Fp1EventType.Unoccupied) occupied = false;
            }
            public void SetEnterStatus(FP1EventInfo eventInfo)
            {
                if (eventInfo.eventType == Fp1EventType.Enter) haIsEnteredBoolean.TurnOn();
                else if (eventInfo.eventType == Fp1EventType.Leave) haIsEnteredBoolean.TurnOff();
            }
            public class Callbacks
            {
                public RegionDelegate? onEnter;
                public RegionDelegate? onExit;
                public RegionDelegate? onOccupy;
                public RegionDelegate? onUnOccupy;
            }
        }

        public Region LastEnteredRegion()
        {
            return enteredRegions.FirstOrDefault(defaultValue: null);
        }

        public virtual void initializeRegionDictionary(List<InputBooleanEntity> inputbooleans)
        {
            Regions.Add(new Region(1, "1 PC-Area", inputbooleans[0]));
            Regions.Add(new Region(2, "2 Kitchen Enter Area", inputbooleans[1]));
            Regions.Add(new Region(3, "3 Hallway Area", inputbooleans[2]));
            Regions.Add(new Region(4, "4 Off Bed area", inputbooleans[3]));
            Regions.Add(new Region(5, "5 Bed Area", inputbooleans[4]));
            Regions.Add(new Region(6, "Region 6", inputbooleans[5]));
            Regions.Add(new Region(7, "Region 7", inputbooleans[6]));
            Regions.Add(new Region(8, "Region 8", inputbooleans[7]));
            Regions.Add(new Region(9, "Region 9", inputbooleans[8]));
            Regions.Add(new Region(10, "Region 10", inputbooleans[9]));
        }

        public enum Fp1EventType {
            Enter, 
            Leave, 
            Occupied, 
            Unoccupied
        };
        public enum Fp1PrecenseEventType
        {
            Enter,
            Leave,
            Left_Enter,
            Right_Enter,
            Left_Leave,
            Right_Leave,
            Approach,
            Away
        };

        public AqaraFP1Extender(BinarySensorEntity presence, SensorEntity presenceEventSensor, SensorEntity actionSensor, List<InputBooleanEntity> haEntities) {
            initializeRegionDictionary(haEntities);
            this.sensorPresence = presence;
            this.presenseEventSensor = presenceEventSensor;
            this.actionSensor = actionSensor;


            //presence.StateAllChanges().Subscribe();

            actionSensor.StateChanges().Subscribe(x => TranslateAction(x?.New?.State));

        }

        public void TranslateAction(string? action)
        {
            if (string.IsNullOrEmpty(action)) return;
            if (action == "None") return;
            FP1EventInfo eventInfo = new FP1EventInfo();
            eventInfo.sensorData = this;

       
            Debug.WriteLine(action);


            string numericPhone = new String(action.Where(Char.IsDigit).ToArray());

            if (!int.TryParse(numericPhone, out eventInfo.regionId)) return;

       
            eventInfo.region = Regions[eventInfo.regionId-1];

            int underscoreIndex = action.LastIndexOf("_")+1;
            string eventType = action.Substring(underscoreIndex, action.Length - underscoreIndex);
            eventInfo.eventType = ParseEventType(eventType);
            switch (eventInfo.eventType)
            {
                case Fp1EventType.Enter:
                    Debug.WriteLine("Enter");
                    OnEnterRegion(eventInfo);
                break;

                case Fp1EventType.Leave:
                    Debug.WriteLine("Leave");
                    OnLeaveRegion(eventInfo);
                break;

                case Fp1EventType.Occupied:
                    OnOccupyRegion(eventInfo);
                break;

                case Fp1EventType.Unoccupied:
                    OnUnoccupyRegion(eventInfo);
                    break;

            }
            
   
        }


        private Fp1EventType ParseEventType(string eventString)
        {
            Fp1EventType t = Fp1EventType.Unoccupied;

            switch (eventString)
            {
                case "enter":
                    t = Fp1EventType.Enter;
                break;

                case "leave":
                    t = Fp1EventType.Leave;
                    break;

                case "occupied":
                    t = Fp1EventType.Occupied;
                    break;

                case "unoccupied":
                    t = Fp1EventType.Unoccupied;
                    break;
            }

            return t;

        }

        public void OnEnterRegion(FP1EventInfo evtInfo)
        {
            if (enteredRegions.Contains(evtInfo.region)) return;
            enteredRegions.Add(evtInfo.region);

            evtInfo.region.callbacks.onEnter?.Invoke(evtInfo);
        }

        public void OnLeaveRegion(FP1EventInfo evtInfo)
        {
            if (!enteredRegions.Contains(evtInfo.region)) return;
            enteredRegions.Remove(evtInfo.region);

            evtInfo.region.callbacks.onExit?.Invoke(evtInfo);
        }

        public void OnOccupyRegion(FP1EventInfo evtInfo)
        {
            if (occupiedRegions.Contains(evtInfo.region)) return;
            occupiedRegions.Add(evtInfo.region);

            evtInfo.region.callbacks.onOccupy?.Invoke(evtInfo);
        }
        public void OnUnoccupyRegion(FP1EventInfo evtInfo)
        {
            if (!occupiedRegions.Contains(evtInfo.region)) return;
            occupiedRegions.Remove(evtInfo.region);

            evtInfo.region.callbacks.onUnOccupy?.Invoke(evtInfo);
        }

        public struct FP1EventInfo
        {
            public Region region;
            public int regionId;
            public Fp1EventType eventType;
            public AqaraFP1Extender sensorData;
        }
         */
    }

}
