using HomeAssistantGenerated;

namespace NetDaemonApps.apps
{
 
    public abstract class AqaraCube : AppBase
    {
        protected readonly SensorEntity? cubeEntity;
        protected readonly SensorEntity? cubeSideEntity;
        protected string? lastKnownSide;

        private readonly string[] events = { "rotate_left", "rotate_right", "tap", "flip90", "flip180", "shake" };



        public AqaraCube()
        {
            cubeEntity = SetCubeActionEntity();
            cubeSideEntity = SetCubeSideEntity();


            
           // cubeEntity?.StateChanges().Subscribe(x => DetermineAction(x?.Entity?.State ?? "Unknown"));



            foreach (var e in events)
            {
                var triggerObservable = myTriggerManager.RegisterTrigger(
                 new
                 {
                     platform = "state",
                     entity_id = new string[] { cubeEntity.EntityId },
                     to = e
                 });
                triggerObservable.Subscribe(n => DetermineAction(e)
                   );
            }

            cubeSideEntity?.StateChanges().Subscribe(x => OnSideChaged(x?.Old?.State, x?.New?.State));

            lastKnownSide = cubeSideEntity?.State;

          

        }

        protected virtual SensorEntity? SetCubeActionEntity() { return null; }
        protected virtual SensorEntity? SetCubeSideEntity() { return null; }



        protected void DetermineAction(string stateName)
        {
            switch (stateName)
            {
               
                case "rotate_left":
                    OnRotateLeft();
                    break;

                case "rotate_right":
                    OnRotateRight();
                    break;

                case "tap":
                    OnTap();
                    break;

                case "flip90":
                    OnFlip90();
                    break;


                case "shake":
                    OnShake();
                    break;

                case "flip180":
                    OnFlip180();
                    break;

            }

        }
        /// <summary> Power-button Press Down</summary>
        protected virtual void OnRotateLeft() {}
        /// <summary> Power-button Press Release</summary>
        protected virtual void OnRotateRight() {}
        /// <summary> Power-button Hold</summary>
        protected virtual void OnTap() { }
        /// <summary> Power-button Hold release</summary>
        protected virtual void OnFlip90() {}

        /// <summary> Brighness Up-button Press Down</summary>
        protected virtual void OnShake() {  }
        /// <summary> Brighness Up-button Press Release</summary>
        protected virtual void OnFlip180() {}

        protected virtual void OnSideChaged(string? fromSide, string? toSide) {

            if (fromSide == null) fromSide = toSide;

        }


    }
}
