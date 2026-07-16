using HomeAssistantGenerated;
using Microsoft.AspNetCore.Builder;
using NetDaemon.Extensions.Tts;
using NetDaemon.HassModel.Entities;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace NetDaemonApps.apps
{
    [NetDaemonApp]
    public class TTS : AppBase
    {
        public enum TTSPriority
        {
            Default,
            IgnoreSleep,
            PlayInGuestMode,
            DoNotPlayInGuestMode,
            IgnoreDisabled,
            IgnoreAll

        }

        private static TTS? instance;
        protected readonly ITextToSpeechService tts;
        protected readonly InputBooleanEntity isAsleepEntity;
        protected DateTime lastAnnounsmentTime = DateTime.MinValue;
        protected TimeSpan timeBetweenAnnounsments = TimeSpan.FromSeconds(15);
 

        public static TTS? Instance { get => instance; set => instance = value; }

        public TTS(ITextToSpeechService ttsService) {

            lastAnnounsmentTime = DateTime.MinValue;
            isAsleepEntity = myEntities.InputBoolean.Isasleep;
            this.tts = ttsService;
            Instance = this;
        }



        public static void Speak(string text, TTSPriority overrider = TTSPriority.Default, InputBooleanEntity? inputBoolean = null, Action callback = null)
        {
            if (Instance != null)
            {
                if(inputBoolean == null || inputBoolean.IsOn())
                {
                    Instance.SpeakTTS(text, callback, overrider);

                    if (callback != null)
                    {

                    }
                }
                
                else Console.WriteLine("(Notification Disabled) " + text);

            }
            else
            {
                Console.WriteLine("(TTS unavailable) " +text);
            }
            

        }

        bool allowTTS(TTSPriority[] overriders)
        {
   
            bool paramsContain(TTSPriority target)
            {
                return overriders.Contains(target);
            }

            if (overriders.Length == 0) return true;
            if (paramsContain(TTSPriority.IgnoreAll)) return true;
           
            if (myEntities.InputBoolean.HydrationCheckActive.IsOn())
            {
               if( paramsContain(TTSPriority.IgnoreDisabled)) return true;

                if (myEntities.InputBoolean.Isasleep.IsOn() && paramsContain(TTSPriority.IgnoreSleep) && 
                   (myEntities.InputBoolean.GuestMode.IsOff() || paramsContain(TTSPriority.PlayInGuestMode))) return true;

                if (myEntities.InputBoolean.GuestMode.IsOn() && (DateTime.Now.Hour < 7 || DateTime.Now.Hour == 23)) return false;

                if (myEntities.InputBoolean.GuestMode.IsOn() && paramsContain(TTSPriority.PlayInGuestMode)) return true;
                if (myEntities.InputBoolean.GuestMode.IsOn() && paramsContain(TTSPriority.DoNotPlayInGuestMode)) return false;

                if (myEntities.InputBoolean.Isasleep.IsOn() && !paramsContain(TTSPriority.IgnoreSleep)) return false;


                return true;
            }

             return false;

        }

        public void SpeakTTS(string text,Action callback = null, params TTSPriority[] overriders)
        {

            if (allowTTS(overriders))
            {
               /*
                if (lastAnnounsmentTime + timeBetweenAnnounsments > DateTime.Now)
                {
                    _00_Globals._myEntities.MediaPlayer.VlcTelnet.PlayMedia("media-source://media_source/local/tos_shipannouncement.mp3", "audio/mpeg");
                    await Task.Delay(1000);

                }
               */

                lastAnnounsmentTime = DateTime.Now;


                MediaPlayerEntity entity = new MediaPlayerEntity(myHaContext, myEntities.Sensor.PreferredMediaplayer.State ?? "media_player.local_music_assistant_telnet");

                myServices.Script.ClearAllPlaylists();
   

                if (myEntities.BinarySensor.FritzBox6660CableConnection.State == "on")
                myServices.Tts.GoogleTranslateSay(entity.EntityId, text);
                else
                myServices.Tts.PicottsSay(entity.EntityId, text);

                /*
                _myScheduler.Schedule(TimeSpan.FromSeconds(2),() => {
                    IDisposable disp = null;

                    MediaPlayerEntity ent = new MediaPlayerEntity(HaContext, _myEntities.Sensor.PreferredMediaplayer.State ?? "media_player.vlc_telnet");
                   disp = ent.StateChanges().Where(x => x.New?.State != "playing").Subscribe(x => {

                        callback?.Invoke();
                        
                       disp?.Dispose();
                    });
                    _myScheduler.Schedule(TimeSpan.FromSeconds(30), () => { disp?.Dispose(); });
                });
                */
               
                }
            else
            {
                callback?.Invoke();
                Console.WriteLine("(TTS ignored) " + text);
            }
               
        }

       private string getTTSService()
        {
            return myEntities.BinarySensor.FritzBox6660CableConnection.State == "on" ? "google_translate_say" : "picotts_say";
        }

    }
}
