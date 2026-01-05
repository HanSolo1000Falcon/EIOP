using EIOP.Core;

namespace EIOP.Tab_Handlers;

public class TimeHandler : TabHandlerBase
{
    private void Start()
    {
        transform.GetChild(0).AddComponent<EIOPButton>().OnPress = () => BetterDayNightManager.instance.SetTimeOfDay(1);
        transform.GetChild(1).AddComponent<EIOPButton>().OnPress = () => BetterDayNightManager.instance.SetTimeOfDay(3);
        transform.GetChild(2).AddComponent<EIOPButton>().OnPress = () => BetterDayNightManager.instance.SetTimeOfDay(7);
        transform.GetChild(3).AddComponent<EIOPButton>().OnPress = () => BetterDayNightManager.instance.SetTimeOfDay(0);
        transform.GetChild(4).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       for (int i = 1;
                                                                            i < BetterDayNightManager.instance
                                                                                   .weatherCycle.Length;
                                                                            i++)
                                                                           BetterDayNightManager.instance.weatherCycle[
                                                                                   i] = BetterDayNightManager
                                                                                  .WeatherType.Raining;
                                                                   };

        transform.GetChild(5).AddComponent<EIOPButton>().OnPress = () =>
                                                                   {
                                                                       for (int i = 1;
                                                                            i < BetterDayNightManager.instance
                                                                                   .weatherCycle.Length;
                                                                            i++)
                                                                           BetterDayNightManager.instance.weatherCycle[
                                                                                   i] = BetterDayNightManager
                                                                                  .WeatherType.None;
                                                                   };
    }
}