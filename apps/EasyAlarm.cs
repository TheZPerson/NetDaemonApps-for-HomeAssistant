using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeAssistantGenerated;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel.Entities;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class EasyAlarm : AppBase
    {
        private List<AlarmRow> alarmRows = new List<AlarmRow>();
        public EasyAlarm() {

            alarmRows.Add(new AlarmRow(myEntities.InputBoolean.AlarmS1Enabled, myEntities.InputBoolean.AlarmS1Repeat, myEntities.InputText.AlarmS1Message, myEntities.InputBoolean.AlarmS1Priority, myEntities.InputDatetime.AlarmS1Time));
            alarmRows.Add(new AlarmRow(myEntities.InputBoolean.AlarmS2Enabled, myEntities.InputBoolean.AlarmS2Repeat, myEntities.InputText.AlarmS2Message, myEntities.InputBoolean.AlarmS2Priority, myEntities.InputDatetime.AlarmS2Time));
            alarmRows.Add(new AlarmRow(myEntities.InputBoolean.AlarmS2Enabled, myEntities.InputBoolean.AlarmS3Repeat, myEntities.InputText.AlarmS3Message, myEntities.InputBoolean.AlarmS3Priority, myEntities.InputDatetime.AlarmS3Time));
            alarmRows.Add(new AlarmRow(myEntities.InputBoolean.AlarmS2Enabled, myEntities.InputBoolean.AlarmS4Repeat, myEntities.InputText.AlarmS4Message, myEntities.InputBoolean.AlarmS4Priority, myEntities.InputDatetime.AlarmS4Time));

        }
        private class AlarmRow
        {
            public InputBooleanEntity IsEnabled { get; private set; }
            public InputBooleanEntity isRepeating { get; private set; }
            public InputBooleanEntity Priority { get; private set; }
            public InputTextEntity Message { get; private set; }
            public InputDatetimeEntity alarmTime { get; private set; }


            private IDisposable alarmroutine;

            public AlarmRow(InputBooleanEntity enabled, InputBooleanEntity repeatEntity, InputTextEntity msg, InputBooleanEntity isPriority, InputDatetimeEntity timeEntity ) {
            
                this.IsEnabled = enabled;
                this.isRepeating = repeatEntity;
                this.Message = msg;
                this.alarmTime = timeEntity;
                this.Priority = isPriority;

                IsEnabled.StateChanges().Where(x => x.New.IsOff()).Subscribe(x=>DisableAlarm());
                IsEnabled.StateChanges().Where(x => x.New.IsOn()).Subscribe(x => EnableAlarm());
                isRepeating.StateChanges().Subscribe(x => EnableAlarm());
                alarmTime.StateChanges().Subscribe(x => EnableAlarm());

                if (IsEnabled.IsOn()) EnableAlarm();

                if(alarmTime.Attributes.HasDate.Value == true && this.alarmTime.GetDateTime() < DateTime.Now )
                {
                    DisableAlarm();
                }

            }

            public void EnableAlarm()
            {
                if (this.IsEnabled.IsOff())
                {
                    this.IsEnabled.TurnOn();
                    return;
                }
                alarmroutine?.Dispose();
                if(alarmTime.Attributes.HasDate.Value == true)
                {
                    Console.WriteLine("Alarm anabled long");
                    alarmroutine = myScheduler.Schedule(new DateTimeOffset(alarmTime.GetDateTime()), RunAlarm);
                }
                   
                else
                {
                    Console.WriteLine("Alarm enabled short: " + alarmTime.GetTimeSpan().Minutes + " " + alarmTime.GetTimeSpan().Hours + " * * *");
                    alarmroutine = myScheduler.ScheduleCron(alarmTime.GetTimeSpan().Minutes + " " + alarmTime.GetTimeSpan().Hours + " * * *", RunAlarm);
                }
            }
            public void DisableAlarm()
            {
                Console.WriteLine("Alarm disabled");
                alarmroutine?.Dispose();
                IsEnabled.TurnOff();
            }

            public void RunAlarm() { 

                if(!isRepeating.IsOff() || alarmTime.Attributes.HasDate.Value != true)
                {
                    alarmroutine?.Dispose();
                    this.IsEnabled.TurnOff();

                }
                Console.WriteLine("Alarm runs");
                TTS.Speak(Message.State == "" ? "Custom Alarm" : Message.State, this.Priority.IsOn() ? TTS.TTSPriority.IgnoreSleep : TTS.TTSPriority.Default );
            
            }

        }



    }
}
