using OsEngine.Entity;
using OsEngine.Journal.Internal;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.MyBot.InsightScreener;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Robots.MyBot.InsightScreener
{
    public class InsightScreener : BotPanel
    {
        /// <summary>
        /// вкладка скринера
        /// </summary>
        public BotTabScreener _tabScreen;

        /// <summary>
        ///  время окончания тайм-аута
        /// </summary>
        private DateTime timeCanselTimeOut = DateTime.MaxValue;

        #region ========== Настройки на параметрах=========================================

        /// <summary>
        /// вкл.выкл скринера
        /// </summary>
        public StrategyParameterBool IsOn;

        /// <summary>
        /// процент для просадки 1
        /// </summary>
        public StrategyParameterDecimal percentDrawdown1;

        /// <summary>
        /// процент для просадки 2
        /// </summary>
        public StrategyParameterDecimal percentDrawdown2;

        /// <summary>
        /// время расчета 1 просадки
        /// </summary>
        public StrategyParameterInt timeDrawdown1;

        /// <summary>
        /// время расчета 2 просадки
        /// </summary>
        public StrategyParameterInt timeDrawdown2;

        /// <summary>
        /// величина задержки 1 просадки
        /// </summary>
        public StrategyParameterInt timeOutDrawdown1;

        /// <summary>
        /// величина задержки 2 просадки
        /// </summary>
        public StrategyParameterInt timeOutDrawdown2;

        /// <summary>
        /// величина задержки ордеров
        /// </summary>
        public StrategyParameterInt timeOutOrder;

        /// <summary>
        /// расстояние до стоп лосса 1
        /// </summary>
        public StrategyParameterDecimal stopLoss1;

        /// <summary>
        /// расстояние до стоп лосса 2
        /// </summary>
        public StrategyParameterDecimal stopLoss2;

        /// <summary>
        /// количество минут на стоп 2
        /// </summary>
        public StrategyParameterInt minutBackStopLoss2;

        /// <summary>
        /// количество свечей зоны роста
        /// </summary>
        public StrategyParameterInt candleBack;

        /// <summary>
        /// процентная величина зоны роста
        /// </summary>
        public StrategyParameterDecimal growthPercent;

        /// <summary>
        /// процентная величина от минимума зоны роста
        /// </summary>
        public StrategyParameterDecimal riseFromLow;

        /// <summary>
        /// расстояние до стоп лосса в процентах
        /// </summary>
        public StrategyParameterDecimal TrailStopLossLength;

        /// <summary>
        /// расстояние до трейлинг профита в процентах
        /// </summary>
        public StrategyParameterDecimal TrailProfitLength;

        /// <summary>
        /// расстояние до профита в процентах
        /// </summary>
        public StrategyParameterDecimal ProfitLength;

        #endregion ========== Настройки на параметрах=========================================

        /// <summary>
        /// список роботов в скринере
        /// </summary>
        public List<InsiRobot> Robots = new List<InsiRobot>();

        #region Конструктор ============================ Конструктор===================================

        public InsightScreener(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Screener);
            _tabScreen = TabsScreener[0];
            _tabScreen.NewTabCreateEvent += _tabScreen_NewTabCreateEvent;

            // настройки
            IsOn = CreateParameter("Включить", false, "Вход");
            timeOutOrder = CreateParameter("Время задержки ордеров", 3, 1, 10, 1, "Вход");

            candleBack = CreateParameter("Зона роста сколько свечей", 10, 5, 20, 1, "Вход");
            growthPercent = CreateParameter("Процент зоны роста бумаги", 3m, 2, 10, 1, "Вход");
            riseFromLow = CreateParameter("От зоны роста входим через", 1m, 1, 10, 1, "Вход");
            ProfitLength = CreateParameter("Процент до профита ", 3m, 1, 10, 1, " Выход");
            TrailStopLossLength = CreateParameter("Процент Трейлинг стопа", 3m, 2, 10, 1, " Выход");
            TrailProfitLength = CreateParameter("Процент Трейлинг профита ", 1m, 1, 10, 1, " Выход");
            stopLoss1 = CreateParameter("Процент Cтоп лосс ", 3m, 2, 10, 1, " Выход");
            stopLoss2 = CreateParameter("Процент Cтоп лосс 2 ", 5m, 2, 10, 1, " Выход");
            minutBackStopLoss2 = CreateParameter("Минут назад для стоп 2", 40, 10, 10, 600, " Выход");
            timeDrawdown1 = CreateParameter("Часов расчета 1 просадки ", 24, 1, 24, 1, "Риски");
            timeOutDrawdown1 = CreateParameter("Часов задержки 1 просадки ", 6, 1, 24, 1, "Риски");
            percentDrawdown1 = CreateParameter("Процент 1 просадки ", 5.5m, 1, 10, 1, "Риски");
            timeDrawdown2 = CreateParameter("Часов расчета 2 просадки ", 24, 1, 24, 1, "Риски");
            timeOutDrawdown2 = CreateParameter("Часов задержки по 2 просадке ", 4, 1, 24, 1, "Риски");
            percentDrawdown2 = CreateParameter("Процент 2 просадки ", 3m, 1, 10, 1, "Риски");

            // создание  индюка
            _tabScreen.CreateCandleIndicator(1, "DSR", new List<string>() { "9", "7", "1" });
        }

        #endregion Конструктор ============================ Конструктор===================================

        /// <summary>
        /// Событие создания нового скринера
        /// </summary>
        private void _tabScreen_NewTabCreateEvent(BotTabSimple newTab)
        {
            InsiRobot robotScalper = new InsiRobot(newTab, this, Robots.Count);

            Robots.Add(robotScalper);

            newTab.PositionClosingSuccesEvent += (Position position) =>
            {
                NewTab_PositionClosingSuccesEvent(position);
            };
            // пришли тики
            newTab.NewTickEvent += (Trade trade) =>
            {
                NewTab_NewTickEvent(trade);
            };
        }

        /// <summary>
        /// пришел новый тик
        /// </summary>
        private void NewTab_NewTickEvent(Trade trade)
        {
            DateTime time = trade.Time;
            if (time > timeCanselTimeOut)       // проверяем время включения торговли
            {
                IsOn.ValueBool = true;
                timeCanselTimeOut = DateTime.MaxValue;
            }
        }

        /// <summary>
        /// Список закрытых позиций
        /// </summary>
        private List<Position> AllClosePositions = new List<Position>();

        /// <summary>
        /// Закрылась позиция
        /// </summary>
        private void NewTab_PositionClosingSuccesEvent(Position position)
        {
            AllClosePositions.Add(position); // собираем закрытые позиции
            DateTime timePosLast = position.TimeClose;
            Risk_2(timePosLast);
        }

        /// <summary>-
        /// взять профит за период
        /// </summary>
        public void Risk_2(DateTime time)
        {
            List<Position> deals = AllClosePositions;

            if (deals == null)
            {
                return;
            }
            DateTime timePeriodPose = time.AddHours(-timeDrawdown2.ValueInt); // время поз учтенного периода

            decimal profitPercent = 0;

            for (int i = deals.Count - 1; i >= 1; i--)
            {
                if (profitPercent < -percentDrawdown2.ValueDecimal)
                {
                    // риск превышен отключаем торговлю
                    DateTime timeOfBot = time;
                    if (IsOn.ValueBool == false)  // если скринер уже отключен отключать его ещё раз не нужно
                    {
                        return;
                    }
                    IsOnTimerTred(timeOfBot); // отключаем
                    break;
                }
                if (time < timePeriodPose)
                {
                    break;
                }
                if (timePeriodPose < time)
                {
                    profitPercent += deals[i].ProfitOperationPersent;
                    //decimal a = deals[i].ProfitOperationPersent;
                }
            }
        }

        /// <summary>
        /// отключение торгов по риску 2
        /// </summary>
        public void IsOnTimerTred(DateTime time)
        {
            // отключаем торговлю всех роботов
            IsOn.ValueBool = false;
            // рассчитываем время включения скринера
            timeCanselTimeOut = time.AddHours(timeOutDrawdown2.ValueInt);
        }

        #region Сервис========================= Сервис================================

        public override string GetNameStrategyType()
        {
            return "InsightScreener";
        }

        /// <summary>
        /// показать окно индивидуальных настроек
        /// </summary>
        public override void ShowIndividualSettingsDialog()
        {
            InsiSettings Uiss = new InsiSettings(this);
            Uiss.Show();
        }

        #endregion Сервис========================= Сервис================================
    }
}