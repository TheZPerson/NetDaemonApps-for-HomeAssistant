using HomeAssistantGenerated;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    public abstract class AppBase
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8601 

        protected static A0Gbl _instance;

        protected static Entities myEntities { get; private set; }
        protected static Services myServices { get; private set; }
        protected static IScheduler myScheduler { get; private set; }
        protected static IObservable<Event> myEvents { get; private set; }
        protected static ITriggerManager myTriggerManager { get; set; }
        protected static IHaContext myHaContext { get; private set; }

        public static void Init(A0Gbl instance)
        {
            _instance = instance;
            myEntities = A0Gbl._myEntities;
            myServices = A0Gbl._myServices;
            myEvents = A0Gbl._events;
            myScheduler = A0Gbl._myScheduler;
            myTriggerManager = A0Gbl.TriggerManager;
            myHaContext = A0Gbl.HaContext;
        }

    }
#pragma warning restore CS8601
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
}
