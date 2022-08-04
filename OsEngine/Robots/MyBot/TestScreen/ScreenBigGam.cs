using System.Collections.Generic;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Indicators;

namespace OsEngine.Robots.MyBot.TestScreen
{
    internal class ScreenBigGam : BotPanel
    {
        /// <summary>
        /// вкладка скринера
        /// </summary>
        BotTabScreener _screenerTab;

        /// <summary>
        /// вкладка для  кластеров 
        /// </summary>
        private BotTabCluster _tabClusterSpot;
        private BotTabCluster _tabClusterSpotDelta;
        public ScreenBigGam(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Screener);
            _screenerTab = TabsScreener[0];

            TabCreate(BotTabType.Cluster); // вкладка для кластеров расчет средней
            _tabClusterSpot = TabsCluster[0];

            _screenerTab.NewTabCreateEvent += _screenerTab_NewTabCreateEvent1;
           // _tabClusterSpotDelta.MaxSellLineChangeEvent += _tabClusterSpotDelta_MaxSellLineChangeEvent;


           Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            Volume = CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);
          
        }

        private void _screenerTab_NewTabCreateEvent1(BotTabSimple obj)
        {
            throw new System.NotImplementedException();
        }

        private void _tabClusterSpotDelta_MaxSellLineChangeEvent(HorizontalVolumeLine line)
        {
            
        }

        public override string GetNameStrategyType()
        {
            return "ScreenBigGam";
        }

        public override void ShowIndividualSettingsDialog()
        {
            
        }

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;

        /// <summary>
        /// volume for entry
        /// объём для входа
        /// </summary>
        public StrategyParameterDecimal Volume;

        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;

        /// <summary>
        /// Кол-во свечек которые мы смотрим с конца
        /// </summary>
        public StrategyParameterInt CandlesLookBack;

        /// <summary>
        /// Событие создания новой вкладки
        /// </summary>
        private void _screenerTab_NewTabCreateEvent(BotTabCluster newTab)
        {
            newTab.MaxDeltaLineChangeEvent += (HorizontalVolumeLine line) =>
            {
                _tabClusterSpotDelta_MaxSellLineChangeEvent(line);
            };
        }

        /// <summary>
        /// событие завершения свечи
        /// </summary>
        private void NewCandleEvent(List<Candle> candles, BotTabSimple tab)
        {
 
            if (Regime.ValueString == "Off")
            {
                return;
            }

    
        }
    }
}
