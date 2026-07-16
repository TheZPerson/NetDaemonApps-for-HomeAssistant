using HomeAssistantGenerated;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetDaemonApps.apps.Lights
{
    public class LightCycler : AppBase
    {
        private List <LightEntity> lightEntities;
        private LightEntity _currentLight = null;
        private InputBooleanEntity guestModeBoolean;
        private InputBooleanEntity toggleBoolean;

        public LightCycler(InputBooleanEntity guestmode, InputBooleanEntity toggleBoolean, params LightEntity[] lights)
        {
          
            guestModeBoolean = guestmode;
            this.toggleBoolean = toggleBoolean;
            lightEntities = lights.ToList();
            FindActiveLight();

            foreach (var l in lightEntities) { 
                SetListener(l);
                if (l.IsOn() && l != _currentLight && IsEnabled()) l.TurnOff();
                    
                        }
        }

        protected virtual bool IsEnabled()
        {
            return myEntities.InputBoolean.GuestMode.IsOff() && (toggleBoolean == null || toggleBoolean.IsOn());
        }

        private void FindActiveLight()
        {
            _currentLight = lightEntities.FirstOrDefault(x => x != null && x.IsOn(), lightEntities.First());

        }

        public LightEntity GetCurrentLight()
        {
            return _currentLight;
        }

        public void Reset()
        {

            foreach (var l in lightEntities)
            {
                l.TurnOff();
            }
            _currentLight = null;
            NextLight();
        }

        private void SetListener(LightEntity light)
        {
            if (guestModeBoolean.IsOn())
            {
                _currentLight = lightEntities[0];
                return;
            }
            else {
   
            light.StateChanges().Where(x => x?.New?.State == "on").Subscribe(x => { 
                if(_currentLight != null && _currentLight != light && IsEnabled())
                {
                    _currentLight?.TurnOff();
                }
               
                _currentLight = light; 
            });

            light.StateChanges().Where(x => x?.New?.State != "on").Subscribe(x => {
                if (_currentLight != null && _currentLight == light)
                {
                    _currentLight = null;
                }

            });
            }
        }


        public void NextLight()
        {
            if (!IsEnabled())
            {
                _currentLight = lightEntities[0];
                lightEntities[0]?.Toggle();
                return;
            }
            else
            {
                var nLight = GetNext();
                if (nLight != null && nLight != _currentLight) nLight.TurnOn();
                else _currentLight?.TurnOff();
            }

     
    
        }
        public void PreviousLight()
        {
            if (!IsEnabled())
            {
                lightEntities[0]?.Toggle();
                return;
            }
            else
            GetPrevious()?.TurnOn();
        }

        public void TurnOff()
        {

            foreach (var l in lightEntities)
            {
                l.TurnOff();
            }


        }

        private LightEntity GetNext()
        {

            LightEntity nLight = null;
            int startIndex = currentLightIndex();
            int nextIndex = startIndex + 1 < lightEntities.Count ? startIndex + 1 : -1;
            if (nextIndex == -1) return null;

            while(nLight == null && nextIndex != startIndex) {
                LightEntity tLight =  lightEntities[nextIndex];
                if(tLight?.State != "unavailable")
                {
                    nLight = tLight;

                    Console.WriteLine(nextIndex);
                    break;
                }
                else
                {
                    nextIndex = nextIndex + 1 < lightEntities.Count ? nextIndex + 1 : -1;
                    if (nextIndex == -1) break;

                }

            }
            return nLight ?? lightEntities.First();
        }
        private LightEntity GetLoop()
        {

            LightEntity nLight = null;
            int startIndex = currentLightIndex();
            int nextIndex = startIndex + 1 < lightEntities.Count ? startIndex + 1 : 0;
            int maxloop = lightEntities.Count * 2;
            int currentloop = 0;

            while (nLight == null && nextIndex != startIndex && currentloop < maxloop)
            {
                LightEntity tLight = lightEntities[nextIndex];
                if (tLight?.State != "unavailable")
                {
                    nLight = tLight;

                    Console.WriteLine(nextIndex);
                    break;
                }
                else
                {
                    nextIndex = nextIndex + 1 < lightEntities.Count ? nextIndex + 1 : 0;

                }
                currentloop++;

            }

            return nLight;
        }
        private LightEntity GetPrevious()
        {

            LightEntity nLight = null;
            int startIndex = currentLightIndex();
            int nextIndex = startIndex - 1 < 0 ? -1 : startIndex - 1;
            if (nextIndex == -1) return null;

            while (nLight == null || nextIndex == startIndex)
            {
                LightEntity tLight = lightEntities[nextIndex];
                if (tLight?.State != "unavailable")
                {
                    nLight = tLight;

                    break;
                }
                else
                {
                    nextIndex = nextIndex - 1 < 0 ? -1 : nextIndex -1;
                    if (nextIndex == -1) break;

                }

            }

            return nLight;
        }

        private int currentLightIndex()
        {
            return _currentLight == null ? -1 : lightEntities.IndexOf(_currentLight);
        }



    }
}
