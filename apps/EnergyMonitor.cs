using HomeAssistantGenerated;
using NetDaemon.Extensions.Scheduler;
using NetDaemon.HassModel.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO.Pipelines;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Transactions;
// Use unique namespaces for your apps if you going to share with others to avoid
// conflicting names
namespace NetDaemonApps.apps;

/// <summary>
///     Showcase the scheduling capabilities of NetDaemon
/// </summary>
/// 
[NetDaemonApp]
public class EnergyMonitor : AppBase
{
    public static Dictionary<double, string> electiricityRanges;

    private List<ElectricityPriceInfo> hoursToday;

    private bool hiPeakAlertGiven = false;
    private bool loPeakAlertGiven = false;
    private bool solarChargingNotificationGiven = false;
    private bool solarChargingOffNotificationGiven = false;

    public static List<double>? electricityRangeKeys;

    private double abNormalEnergyIncreaseThreshold = 0.2;

    public static EnergyMonitor? _instance;
    ElectricityPriceInfo? infoForCurrentHour { get { return hoursToday[DateTime.Now.Hour]; } }

    private double marginOfErrorFix = 1.07;
    private bool skipThisHour = true;
    private static int lastCaclHour = -5;
    private DateTime timeOfLastSolarNotification = DateTime.MinValue;

    private double ecoflowCgargePriceFixHelper = 0.0;
    private double ecoflowUsagePriceFixer = 0.0;
    public static int Price2RangeIndx(double? price)
    {
        return (int)electricityRangeKeys?.FindLastIndex(x => x <= price);
    }

    protected DateTime dateFromHourIndex(int index)
    {
        return DateTime.Today.AddHours(index);
    }

    protected string Price2RangaNme(double price)
    {
        return electiricityRanges.Values.ElementAtOrDefault(Price2RangeIndx(price)) ?? "unknown";
    }



    private NumericSensorEntity nordPoolEntity
    {
        get { return myEntities.Sensor?.Nordpool; }
    }

    public EnergyMonitor()
    {
        _instance = this;
        myScheduler.ScheduleCron("14,29,44,59 * * * *", () => UpdatePriceQuarter(myEntities.Sensor.CurrentPrice.State ?? 0, myEntities?.Sensor.AllPowersEnergyQuarterHourly.State ?? 0));
        A0Gbl.DailyResetFunction += OnDayChanged;
        Notifications.RegisterStateNotification(myEntities.BinarySensor.LivingroomWindowSensorContact, "Solar Panels");
        IsAsleepMonitor.RegisterMorningTTS("energy", MorningTTS);
  
      

        void ResetRanges()
        {
            electiricityRanges = new Dictionary<double, string>();

            foreach (string s in myEntities.InputSelect.Electricityranges.stringsFromSelectionDropdown())
            {
                string[] stings = s.Split(';', StringSplitOptions.TrimEntries);
                electiricityRanges.TryAdd(double.Parse(stings[0].Replace(",",NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator).Replace(".", NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator)), stings[1]);
            }

            electricityRangeKeys = electiricityRanges.Keys.ToList();
        }

        ResetRanges();

        myEntities?.Sensor.NordpoolTomorrowValid.StateChanges().Where(x => x.New.State == "True" && x.Old.State == "False").Subscribe(_ => {
            ReadOutEnergyUpdate();
        });
        return;

        myEntities.InputSelect.Electricityranges.StateAllChanges().Subscribe(x => {
            ResetRanges();

            Console.WriteLine("Electricity Ranges Reset");
        });

        fillDays();

        myEntities?.InputNumber.ElectricityPriceFixer.StateChanges().Where(e => e.New.State != 0).Subscribe(e => {
            myEntities.InputNumber.EnergyCostDaily.AddValue(e.New.State.Value);
            e.Entity.SetValue(0);
        });

        myEntities.InputButton.DebugEnergyHourlyupdate.StateChanges().Where(e => !e.Old.IsUnavailable()).Subscribe(e => { EnergiPriceChengeAlert(true); } );
        myEntities.InputButton.DebugEnergyReadforecast.StateChanges().Where(e => !e.Old.IsUnavailable()).Subscribe(e => { ReadOutEnergyUpdate(); });

        /*
        myEntities.Sensor.EcoflowAcInputHourly.StateChanges().Where(x => x.Old.State == 0 && x.New.State > abNormalEnergyIncreaseThreshold).Subscribe(e => {

            ecoflowCgargePriceFixHelper = myEntities.Sensor.EcoflowAcInputHourly.State ?? 0;
        });
        myEntities.Sensor.EcoflowAcOutputHourly.StateChanges().Where(x => x.Old.State == 0 && x.New.State > abNormalEnergyIncreaseThreshold).Subscribe(e =>
        {

            ecoflowUsagePriceFixer = myEntities.Sensor.EcoflowAcOutputHourly.State ?? 0;
        });
        */
      // A0Gbl.HourlyResetFunction += () => UpdatePriceHourly( myEntities.Sensor.EnergyCostThisHour.State ?? 0, myEntities?.Sensor.AllPowersEnergyQuarterHourly.State ?? 0);

        myScheduler.ScheduleCron("50 * * * *", () => EnergiPriceChengeAlert());
        myScheduler.ScheduleCron("1 * * * *", () => {myServices.UtilityMeter.Calibrate(ServiceTarget.FromEntity(myEntities.Sensor.EcoflowAcInputHourly.EntityId), "0"); });
        



        solarChargingNotificationGiven = myEntities?.Sensor?.EcoflowSolarInPower.State >= 0;

       

        myEntities?.BinarySensor.SolarChargingLimit.StateChanges().Where(e => e.New.IsOn() && e.Old.IsOff()).Subscribe(_e => { 
        
        if(DateTime.Now > timeOfLastSolarNotification + TimeSpan.FromMinutes(30))
         {     
                if(myEntities.Sensor.EcoflowStatus.State != "online" || myEntities.BinarySensor.LivingroomWindowSensorContact.IsOff())
                {
                    if (myEntities.InputBoolean.NotificationEnergySolar.IsOn())
                    {
                        TTS.Instance?.SpeakTTS("Solar charging available",null, TTS.TTSPriority.PlayInGuestMode);
                        timeOfLastSolarNotification = DateTime.Now;
                    }

                }
         }
             
        });

        myScheduler.RunEvery(TimeSpan.FromHours(1)+TimeSpan.FromMinutes(30), DateTimeOffset.Now, () => {

            if (myEntities.BinarySensor.LivingroomWindowSensorContact.IsOn() && myEntities.Sensor.EcoflowSolarInPower.State == 0 && myEntities.BinarySensor.SolarChargingLimit.IsOff() && myEntities.BinarySensor.SolarChargingLimit.StateFor(TimeSpan.FromMinutes(30)) && myEntities.Sensor.EcoflowOutputPlugPower.State == 0)
            {
                TTS.Instance?.SpeakTTS("Solar panels are active",null, TTS.TTSPriority.PlayInGuestMode);
            }

        });

        skipThisHour = true;
        myEntities.BinarySensor.FritzBox6660CableConnection.StateChanges().Where(e => e.New.IsOn()).Subscribe(_e => { skipThisHour = true; });

        myEntities?.Sun.Sun.StateAllChanges().Where(x => (x?.New?.Attributes.Elevation <= 8 && DateTime.Now.Hour>17 && !solarChargingOffNotificationGiven && myEntities.Sensor.EcoflowSolarInPower.State ==0 && myEntities.BinarySensor.LivingroomWindowSensorContact.IsOn())).Subscribe(x => { TTS.Instance.SpeakTTS("Solar Charging Ended",null, TTS.TTSPriority.PlayInGuestMode); solarChargingOffNotificationGiven = true; });
        myEntities?.Sensor.EcoflowSolarInPower.StateChanges().Where(x => x?.New.State > 0 && !solarChargingNotificationGiven).Subscribe(_ => {
            TTS.Instance.SpeakTTS("Solar Charging On",null, TTS.TTSPriority.PlayInGuestMode); solarChargingNotificationGiven = true;
        });
 
       



    }



    private void fillDays()
    {
        hoursToday?.Clear();
        var tmplist = new List<ElectricityPriceInfo>();

        var todayList = nordPoolEntity?.EntityState?.Attributes?.Today;
        var tomorrowList = nordPoolEntity?.Attributes.TomorrowValid.Value == true ? JsonSerializer.Deserialize<double[]>(nordPoolEntity?.EntityState?.Attributes?.Tomorrow.ToString()) : new double[0];

       var combinelist = todayList.Concat(tomorrowList).ToArray();

        for (int i = 0; i < combinelist.Length; i++)
        {
            var price = combinelist.ElementAt(i);
            var peak = price == nordPoolEntity?.EntityState?.Attributes?.Max ? 1 : 0;
            peak = price == nordPoolEntity?.EntityState?.Attributes.Min ? -1 : peak;
            var info = new ElectricityPriceInfo(DateTime.Today + TimeSpan.FromHours(i), price, peak);
           
            if(i != 0)
            {
                tmplist.Last().nexthour = info;
            } 
            tmplist.Add (info);
        }

        hoursToday = tmplist;

    }

    private void DailyReset()
    {

    }

    private struct EnergyForecastInfo
    {
        public double avarage;
        public double max;
        public double min;
        public int majorityRange;
        public int averageRange;
        public bool isAllSameRange;
        public int subZeroCount;

    }




    public void ReadOutEnergyUpdate()
    {
        if (myEntities.InputBoolean.NotificationEnergyDailyUpdate.IsOff()) return;
        string message = "Energy update!";

            EnergyForecastInfo energyForecastInfo = GetEnergyForecast(nordPoolEntity?.Attributes.TomorrowValid.Value == true ? JsonSerializer.Deserialize<double[]>(nordPoolEntity?.EntityState?.Attributes?.Tomorrow.ToString()) : new double[0]);

        message += "Tomorrows Prices will be" + (energyForecastInfo.isAllSameRange ? " all " : " mostly ") + "in " + Price2RangaNme(energyForecastInfo.avarage);
            message += " and  " + PercentageDifference(nordPoolEntity?.EntityState?.Attributes?.Average ?? 0, energyForecastInfo.avarage) + "% " + (nordPoolEntity?.EntityState?.Attributes?.Average > energyForecastInfo.avarage ? "lower" : "higher") + " than today.";

            if (energyForecastInfo.subZeroCount > 0 && energyForecastInfo.majorityRange != 0) message += " There will also be " + energyForecastInfo.subZeroCount + " sub zero quarters" + (energyForecastInfo.subZeroCount > 1 ? "s." : ".");
        

            TTS.Speak(message, TTS.TTSPriority.PlayInGuestMode);


    }

    double PercentageDifference(double a, double b)
    {
        return Math.Round((b - a) / ((a + b) / 2) * 100);
    }


    private EnergyForecastInfo GetEnergyForecast(double[] list, int startFrom = 0)
    {
        EnergyForecastInfo energyForecastInfo = new EnergyForecastInfo();
        if (list == null) return energyForecastInfo;

        //Because Tomorrow values come as strings, we must make sure we convert values to doubles first

        Dictionary<int, int> foundPerRange = new Dictionary<int, int>();

        double avg = list.Average();
        double max = list.Max();
        double min = list.Min();
        int subZeroCount = 0;

        for (int i = startFrom; i < list.Length - 1; i++)
        {

            var rangeForPrice = Price2RangeIndx(list.ElementAt(i));

            if (!foundPerRange.ContainsKey(rangeForPrice)) foundPerRange.Add(rangeForPrice, 1);
            else foundPerRange[rangeForPrice]++;

            if(list.ElementAt(i) <= 0)subZeroCount ++;

        }


        energyForecastInfo.avarage = avg;
        energyForecastInfo.max = max;
        energyForecastInfo.min = min;
        energyForecastInfo.averageRange = Price2RangeIndx(avg);
        energyForecastInfo.majorityRange = foundPerRange.MaxBy(x => x.Value).Key;
        energyForecastInfo.isAllSameRange = foundPerRange.Count == 1;
        energyForecastInfo.subZeroCount = subZeroCount;

        return energyForecastInfo;

    }

    public string MorningTTS()
    {
        if (myEntities.InputBoolean.NotificationEnergyPriceChange.IsOff() && (myEntities.InputBoolean.NotificationEnergySolar.IsOff() && myEntities.Sensor.EcoflowSolarInPower.State == 0)) return "";
        if (Price2RangeIndx(myEntities.Sensor.CurrentPrice.State ?? 0) == 0) return "";
        string TTSMessage = "";
  
          
        PriceChangeType priceChange = comparePrice(myEntities.Sensor.CurrentPrice.State ?? 0, myEntities.Sensor.NextPrice.State ?? 0 );
        bool addAlso = false;
      
        if (Price2RangeIndx(myEntities.Sensor.CurrentPrice.State ?? 0) != 0 || priceChange != PriceChangeType.NoChange)
        {
            TTSMessage += " Current Electricity Cost is at " + Price2RangaNme(myEntities.Sensor.CurrentPrice.State ?? 0) + ". ";
            addAlso = true;
        }

        if (myEntities.BinarySensor.SolarChargingLimit.IsOn() && myEntities.Sensor.EcoflowSolarInPower.State == 0)
        {
            TTSMessage += "There is " + (addAlso ? "also" : "") + "potential for solar charging";
        }

        return TTSMessage;

    }
    
    private double ecoflowCacl(double hourlyUsedEnergy)
    {
        return hourlyUsedEnergy - (double.Max(0,myEntities.Sensor.EcoflowAcOutputHourly.AsNumeric().State ?? 0)) - (myEntities.Sensor.EcoflowSolarInputHourly.AsNumeric().State ?? 0);
    }

    private PriceChangeType comparePrice(ElectricityPriceInfo priceA, ElectricityPriceInfo priceB)
    {
        int rangeOfA = priceA?.range ?? 0; 
        int rangeofB = priceB?.range ?? 0;


        if (rangeOfA < rangeofB) return PriceChangeType.Increase;
        else if (rangeOfA > rangeofB) return PriceChangeType.Descrease;
        else return PriceChangeType.NoChange;
    }
    private PriceChangeType comparePrice(double priceA, double priceB)
    {
        int rangeOfA = Price2RangeIndx(priceA);
        int rangeofB = Price2RangeIndx(priceB);


        if (rangeOfA < rangeofB) return PriceChangeType.Increase;
        else if (rangeOfA > rangeofB) return PriceChangeType.Descrease;
        else return PriceChangeType.NoChange;
    }
    private void EnergiPriceChengeAlert(bool force = false)
    {
  
       // UpdateNextChangeHourTime();
        if (myEntities.InputBoolean.NotificationEnergyPriceChange.IsOff()) return;
        if (myEntities.InputBoolean.Isasleep.State == "on") return;

        // No need for alert on low price days
        if (nordPoolEntity.Attributes.Max < electiricityRanges.Keys.ToArray()[1] && !force) return;

        ElectricityPriceInfo inFoForNextHour = infoForCurrentHour?.nexthour;
        PriceChangeType priceChange = comparePrice(infoForCurrentHour, inFoForNextHour);
       
        string TTSMessage = null;

        if (force)
        {
        Console.WriteLine("Current Price: " + infoForCurrentHour.price);
        Console.WriteLine("Next Price: " + inFoForNextHour.price);
        Console.WriteLine("Price Range: " + inFoForNextHour.range);
        Console.WriteLine("Price Change: " + priceChange.ToString());
        
        Console.WriteLine("Range Change in: " + FindWhenElectricityRangeChanges(inFoForNextHour.nexthour)?.dateTime);
        }


        

        if ((priceChange == PriceChangeType.NoChange && inFoForNextHour.peak == 0 
            || (priceChange == PriceChangeType.NoChange && infoForCurrentHour.peak == -1 && loPeakAlertGiven) 
            || (priceChange == PriceChangeType.NoChange && infoForCurrentHour.peak == 1 && hiPeakAlertGiven)) && !force) return;

        if (nordPoolEntity.Attributes.Average < 4 && inFoForNextHour.range < 2) return;

            bool isWarning = priceChange == PriceChangeType.Increase || inFoForNextHour.peak == 1;

        if (infoForCurrentHour.range + inFoForNextHour.range <= 1 && inFoForNextHour.peak == 0 && isWarning && !force) return;

        TTSMessage = "Electricity " + (isWarning ? "Warning. " : "Notice. ") + "The Price is About to ";
        int ttsLenght = TTSMessage.Length;


        if (priceChange != PriceChangeType.NoChange || force)
        {
            TTSMessage += (priceChange == PriceChangeType.Increase ? "increase to " : "fall to ") + (electiricityRanges.Values.ElementAtOrDefault(inFoForNextHour.range) ?? "unknown") + ".";

            var hoursTillChange =  FindWhenElectricityRangeChanges(inFoForNextHour.nexthour);


            PriceChangeType priceChangeType = comparePrice(inFoForNextHour, hoursTillChange);

            if (hoursTillChange == null || priceChangeType == PriceChangeType.NoChange)
            {
                TTSMessage += "And stays like that for while.";
            }
            else if (priceChange == PriceChangeType.Increase || priceChange == PriceChangeType.Descrease)
            {
                var timeDiff = hoursTillChange.dateTime - inFoForNextHour.dateTime;
                TTSMessage += "And will " + (priceChangeType == PriceChangeType.Increase ? "increase to " : "fall to ") + (electiricityRanges.Values.ElementAtOrDefault(hoursTillChange.range) ?? "unknown") + " after " + GetHoursAndMinutesFromTimeSpan(timeDiff);
            }

            if (inFoForNextHour.peak != 0)
            {
                TTSMessage += ". This will also ";
            }

            if(priceChange == PriceChangeType.Increase && inFoForNextHour.range > 0 && myEntities.Sensor.EcoflowStatus.State.ToLower() == "online")
            {
                myEntities.Switch.EcoflowAcEnabled.TurnOn();

            }

        }

            if (inFoForNextHour.peak == 1 && !hiPeakAlertGiven)
            {
                TTSMessage += "be the peak price of the day.";
            hiPeakAlertGiven = true;
            }
            if (inFoForNextHour.peak == -1 && !loPeakAlertGiven)
            {
                TTSMessage += "be the lowest price of the day";
            loPeakAlertGiven = true;

            }

            //Fix for weird bug that prevents Peak Alert from triggering
        if (ttsLenght == TTSMessage.Length) return;
        if(force)Console.WriteLine(TTSMessage);
        if (TTSMessage!= null)TTS.Speak(TTSMessage, TTS.TTSPriority.PlayInGuestMode);
    }

    private string GetHoursAndMinutesFromTimeSpan(TimeSpan input)
    {

        var hour = input.Hours > 0 ? input.Hours.ToString() + " hour" + (input.Hours > 1 ? "s" : "") : "";
        var and = input.Hours > 0 && input.Minutes > 0 ? " and " : "";
        var minute = input.Minutes > 0 ? input.Minutes.ToString() + " minute" + (input.Minutes > 1 ? "s" : "") : "";


        return hour + and + minute;
    }

    private enum PriceChangeType
    {
        NoChange,
        Increase,
        Descrease,
    }

 
    private class ElectricityPriceInfo
    {

        public double? price;
        public DateTime dateTime;
        public int range;
        public int peak = 0;

        public ElectricityPriceInfo nexthour = null;

        public ElectricityPriceInfo(DateTime dateTime, double price, int peak)
        {
            this.dateTime = dateTime;
            this.price = price;
            this.peak = peak;
            this.range = Price2RangeIndx(price);

        }

        public PriceChangeType Compare(ElectricityPriceInfo endPoint)
        {
            if (range < endPoint.range) return PriceChangeType.Increase;
            if (range > endPoint.range) return PriceChangeType.Descrease;

            return PriceChangeType.NoChange;
        }
    }

    private ElectricityPriceInfo FindWhenElectricityRangeChanges(ElectricityPriceInfo startInfo)
    {
        if (startInfo == null) return null;

        ElectricityPriceInfo nextInfo = startInfo;
        do
        {
          nextInfo = nextInfo.nexthour;

        } while (nextInfo.range == startInfo.range && nextInfo.nexthour != null);

        return nextInfo;
    }

    private void UpdatePriceQuarter(double cost, double energy)
    {
        var x = myScheduler.Schedule(TimeSpan.FromSeconds(55), () => {

            myEntities.InputNumber.DailyEnergySaveHelper.AddValue((myEntities.Sensor.EcoflowCostThisHour.State ?? 0) / 100);
            myEntities.InputNumber.EnergyCostDaily.AddValue((myEntities.Sensor.EnergyCostThisHour.State ?? 0) / 100);
            myEntities.Sensor.AllPowersEnergyQuarterHourly.ResetEnergy();
            myEntities.Sensor.EcoflowAcOutputQuarter.ResetEnergy();
            lastCaclHour = DateTime.Now.Hour;
            ecoflowCgargePriceFixHelper = 0;
            ecoflowUsagePriceFixer = 0;
        });
    }
    private void UpdatePriceHourly(double cost,  double energy)
    {
        if (lastCaclHour == DateTime.Now.Hour) return;

        myEntities.InputNumber.DailyEnergySaveHelper.AddValue((myEntities.Sensor.EcoflowCostThisHour.State ?? 0)/100);
        myEntities.InputNumber.EnergyCostDaily.AddValue((myEntities.Sensor.EnergyCostThisHour.State ?? 0) / 100);
        lastCaclHour = DateTime.Now.Hour;
        ecoflowCgargePriceFixHelper = 0;
        ecoflowUsagePriceFixer= 0;
    }

    private void OnDayChanged()
    {

        hiPeakAlertGiven = false;
        loPeakAlertGiven = false;
        myEntities.InputNumber.EnergyCostHourly.SetValue(0);
        myEntities.InputNumber.EnergyCostDaily.SetValue(0);
        myEntities.InputNumber.EnergyAtStartOfHour.SetValue(0);

        solarChargingNotificationGiven = false;
        solarChargingOffNotificationGiven = false;

        // _0Gbl._myEntities.InputNumber.DailyEnergySaveHelper.SetValue(0);
        myEntities.InputNumber.EcoflowCharingCost.SetValue(0);

        var x = myScheduler.Schedule(TimeSpan.FromSeconds(10), () => {
            myEntities.Sensor.EcoflowAcInputDaily.ResetEnergy();
            myEntities.Sensor.EcoflowAcInputHourly.ResetEnergy();
            fillDays();
           // UpdateNextChangeHourTime();
        });
    }

  

}