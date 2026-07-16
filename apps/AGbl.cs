using HomeAssistantGenerated;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class A0Gbl
    {

        public static A0Gbl? _instance;
        public static Entities? _myEntities { get; private set; }
        public static Services? _myServices { get; private set; }
        public static IObservable<Event>? _events { get; private set; }
        public static IScheduler? _myScheduler { get; private set; }
        public static Action? HourlyResetFunction { get; set; }
        public static Action? DailyResetFunction { get; set; }

        public static ITriggerManager TriggerManager { get; set; }
        public static IHaContext HaContext { get; private set; }


        public A0Gbl(IHaContext ha, IScheduler scheduler, ITriggerManager _triggerManager)
        {
            _instance = this;
            _myEntities = new Entities(ha);
            _myScheduler = scheduler;
            _myServices = new Services(ha);
            _events = ha.Events;
            HaContext = ha;
            TriggerManager = _triggerManager;
            _myScheduler.ScheduleCron("59 * * * *", hourlyResetFunction);
            _myScheduler.ScheduleCron("0 0 * * *", dailyResetFunction);
            DailyResetFunction += () => { _myEntities?.InputDatetime.Lastknowndate.SetDatetime(date:(DateTime.Now+ TimeSpan.FromMinutes(10)).Date.ToString("yyyy-MM-dd"));};
            AppBase.Init(this);
            Console.WriteLine("Is working");
            Task.Run(OnLateStart);
    
        }


        async Task OnLateStart()
        {
          await Task.Delay(1000);
          dateUpdate();

        }

        private void dateUpdate()
        {
            if (checkDateChanged())
            {
                DailyResetFunction?.Invoke();
            }
        }

        private void hourlyResetFunction()
        {

            Task.Run(async () => {
                await Task.Delay(TimeSpan.FromSeconds(55));
                dateUpdate();
                HourlyResetFunction?.Invoke();
            });
           
        }
      
        private void whatever()
        {
            string filePath = "D:/characternames.txt"; // Replace with your actual file path


            
            try
            {
                string[] lines = File.ReadAllLines(filePath);

                // Print each line or process them as needed
                foreach (string line in lines)
                {
                    bool addColons = line.Contains(" ");
                    string name = "$" + (addColons ? "(" : "") + line + (addColons ? ")" : "") + " <ppp:stn> $UniversalNegative(flip), $0/Anti-Thick(flip)<ppp:/stn>";
                    Console.WriteLine(name);
                    // Add further processing or manipulation of the lines here
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("Error reading file: " + e.Message);
            }
        }


        private void dailyResetFunction()
        {
            DailyResetFunction?.Invoke();
        }

        bool checkDateChanged()
        {
            DateTime dateTimeVariable = DateTime.ParseExact(_myEntities?.InputDatetime.Lastknowndate.State ?? "", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            return dateTimeVariable.Date != DateTime.Now.Date;
        }
    }
}
