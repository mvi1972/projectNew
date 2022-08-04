using OsEngine.Alerts;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.Logging;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.Integration;

namespace OsEngine.Robots.MyBot.Insight
{
    public class Insight : BotPanel, INotifyPropertyChanged
    {

        /// <summary>
        /// КОНСТРУКТОР 
        /// </summary>
        public Insight(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);// вкладка для торговли
            _tabSimple = TabsSimple[0];
            TabCreate(BotTabType.Cluster); // вкладка для кластеров расчет средней
            _tabClusterSpot = TabsCluster[0];
            TabCreate(BotTabType.Cluster); // вкладка для кластеров расчет дельты
            _tabClusterSpotDelta = TabsCluster[1];

            DistLongInit = 6;
            LongAdj = 0.1m;
            DistShortInit = 6;
            ShortAdj = 0.1m;

             
            IsOn = CreateParameter("IsOn", false, "Входные");
            VolumeInBaks = CreateParameter("Объем позиции в $ ", 11, 7, 7,5, "Входные");
            PartsInput = CreateParameter("Сколько частей на вход", 2, 1, 10, 1, "Входные"); // набирать позицию столькими частями 
            PercentOnEntry = CreateParameter("Набирать позицию в %", 1.4m, 0.7m, 2, 0.1m, "Входные");

            SlipageOpenFirst = CreateParameter("Велич.проскаль.при 1м открытии ", 0, 0, 15, 1, "Ордеров");// проскальзывание при 1м открытии
            SlipageCloseFirst = CreateParameter("Велич.проскаль.при 1м закрытии ", 0, 0, 15, 1, "Ордеров");// проскальзывание при закрытии первым ордером
            SlipageOpenSecond = CreateParameter("Велич.проскаль.при 2 открытии ", 2, 0, 15, 1, "Ордеров");// проскальзывание при открытии позиции
            SlipageCloseSecond = CreateParameter("Велич.проскаль.при 2 закрытии ", 2, 0, 15, 1, "Ордеров");// проскальзывание на закрытии

            vklBigCluster = CreateParameter("Входить ли по большому объему", false, "Входные");
            bigvolume = CreateParameter("Объем монет в кластере большой если их >", 100000, 100000, 500000, 50000, "Входные");
            coefficient = CreateParameter("Коэффицент увеличения покупок лим.", 0.3m, 0.1m, 3, 0.1m, "Входные");// во сколько раз должны больше купили лимиткой 
            toProfit = CreateParameter("Забирать профит от %", 2, 0.5m, 50m, 0.5m, "Выхода");
            slippage = CreateParameter("Велич.проскаль.Стопа и профита ", 5, 1, 200, 5, "Ордеров");
            lengthStartStop = CreateParameter("стартовая (начальная) для стопа %", 2, 0.6m, 50m, 0.5m, "Выхода"); // стартовая (начальная) величина отступа для стоп приказа  в процентах от цены открытия позиции
            lengthToPiramid = CreateParameter(" расстояние до пирамиды в %", 0.3m, 0.1m, 3, 0.1m, "Входные");
            vklRasCandl = CreateParameter("Считать стоп по свечам?", false, "Выхода");
            vklRasKauf = CreateParameter("Прибавить к стопу Волатильность?", false, "Выхода");
            TimeFrom = CreateParameter("время начала торговли роботом", 0, 0, 24, 1, "Входные"); // время начала торговли роботом
            TimeTo = CreateParameter("время окончания торговли роботом", 0, 0, 24, 1, "Входные"); // время завершения торговли роботом
            LagTimeToOpenClose = CreateParameterTimeOfDay("секунд на исполнение ордеров", 0, 0, 10, 0, "Ордеров");
            LagPunctToOpenClose = CreateParameter("Пунктов измен цены до отзыва ордера", 20, 20, 200, 5, "Ордеров");// откат цены от цены ордера, после чего он будет отозван
            SlipageReversClose = CreateParameter("Отклонение активации стопОрдера на выход", 20, 20, 200, 5, "Ордеров");// обратное проскальзывание для цены активации стопОрдера на закрытии
            SlipageReversOpen = CreateParameter("Отклонение стопОрдера входа в поз", 20, 20, 200, 5, "Ордеров"); // обратное проскальзывание для цены активации стопОрдера на открытии
            BarOldPeriod = CreateParameter("количество свечей на вход", 3, 2, 20, 1, "Входные");// количество свечей которые рассчитываем для входа
            windows = CreateParameterButton("кнопка выполнить назн фун", "Входные");

            ParametrsChangeByUser += Start_ParametrsChangeByUser; // событие изменения параметров пользователем
            windows.UserClickOnButtonEvent += UserClickOnButtonEvent; // обработка события нажатия на кнопку в панели параметров

            _tabClusterSpotDelta.MaxSellLineChangeEvent += _tabClusterSpotDelta_MaxSellLineChangeEvent; // изменилась макс. линия Sell

            _tabClusterSpotDelta.MaxBuyLineChangeEvent += _tabClusterSpotDelta_MaxBuyLineChangeEvent; // изменилась макс. линия Buy

            _tabSimple.CandleFinishedEvent += CandleFinishedEvent;
            _tabSimple.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent; //событие открытия позиции
            _tabSimple.PositionOpeningFailEvent += _tab_PositionOpeningFailEvent; // ошибка открытия позиции
            _tabSimple.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent; // закрылась позиция 
            _tabSimple.OrderUpdateEvent += _tab_OrderUpdateEvent; // новый ордер 
            _tabSimple.NewTickEvent += _tab_NewTickEvent;
            _tabSimple.PositionClosingFailEvent += _tab_PositionClosingFailEvent; // ошибка закрытия позиции 
            //_tabSimple.MarketDepthUpdateEvent += _tab_MarketDepthUpdateEvent;

            Load();

            Thread worker = new Thread(TimeWatcherArea);
            worker.IsBackground = true;
            worker.Start();

            Thread worker2 = new Thread(WatcherOpenPosition);
            worker2.IsBackground = true;
            worker2.Start();

            Thread worker3 = new Thread(AreaCloserPositionThread);
            worker3.IsBackground = true;
            worker3.Start();

        }

        #region ============== Свойства ====================================================

        /// <summary>
        /// название бумаги
        /// </summary>
        public string SecurityName
        {
            get
            {
                //TabSimple.IsConnected &&
                if (_tabSimple.IsConnected && _tabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    return _tabSimple.Securiti.Name;
                }
                if (_tabSimple.StartProgram == StartProgram.IsTester)
                {
                    return _tabSimple.Securiti.Name;
                }
                else return "";
            }
        }

        public string _securityName;
        /// <summary>
        /// децимал бумаги
        /// </summary>
        public int SecurityDecimals
        {
            get
            {
                //TabSimple.IsConnected &&
                if (_tabSimple.IsConnected && _tabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    return _tabSimple.Securiti.DecimalsVolume;
                }
                if (_tabSimple.StartProgram == StartProgram.IsTester)
                {
                    return _tabSimple.Securiti.DecimalsVolume;
                }
                else return 3;
            }
        }

        public int _securityDecimals;
        /// <summary>
        /// поле для тестов 1
        /// </summary>
        private decimal _test; // поле 
        public decimal Test
        {
            get => _test;
            set => Set(ref _test, value);
        }
        /// поле для тестов 2
        /// </summary>
        private decimal _test2; // поле 
        public decimal Big
        {
            get => _test2;
            set => Set(ref _test2, value);
        }
        /// <summary>
        /// поле хранения цены 
        /// </summary>
        private decimal _price; // поле хранения 
        public decimal Price
        {
            get => _price;
            set => Set(ref _price, value);
        }

        /// <summary>
        /// средний объем за n времени назад 
        /// </summary>
        private decimal _volumN; // поле хранения объема за период
        public decimal AverageVolumeBaсk
        {
            get => _volumN;
            set => Set(ref _volumN, value);
        }

        #endregion

        #region ===================Поля ================================================
        public decimal stopBuy;
        public decimal stopSell;
        public decimal indent;
        /// <summary>
        /// объем сделки
        /// </summary>
        public decimal VolumePosition = 0;
        /// <summary>
        /// объем крупного 
        /// </summary>
        public decimal VolumeBigGamer = 0;
        /// <summary>
        /// объем максимальной  линии покупок
        /// </summary>
        decimal VolumeBigLineBuy = 0; // 
        public decimal DistLongInit;
        public decimal LongAdj;
        public decimal DistShortInit;
        public decimal ShortAdj;

        /// <summary>
        /// вкладка для торговли и кластеров 
        /// </summary>
        private BotTabSimple _tabSimple;
        private BotTabCluster _tabClusterSpot;
        private BotTabCluster _tabClusterSpotDelta;

        /// <summary>
        /// настройки на параметрах  
        /// </summary>
        private StrategyParameterBool IsOn; // включение робота
        private StrategyParameterInt VolumeInBaks; // объем позиции в баксах
        private StrategyParameterInt SlipageOpenFirst;// проскальзывание на открытие первый ордер
        private StrategyParameterInt SlipageCloseFirst; // проскальзывание при 1м закрытии первый ордер
        private StrategyParameterInt SlipageOpenSecond; // проскальзывание при 2 открытии
        private StrategyParameterInt SlipageCloseSecond; // проскальзывание на 2 закрытии
        private StrategyParameterInt TimeFrom;// время начала торговли роботом
        private StrategyParameterInt TimeTo;// время завершения торговли роботом
        private StrategyParameterInt PartsInput; // количество частей на вход в объем позиции 
        private StrategyParameterDecimal PercentOnEntry; // на сколько процентов растягивать набор позиции

        private StrategyParameterBool vklBigCluster; // использовать  объем большого 
        private StrategyParameterInt bigvolume; // какой объем в кластере считать большим 
        private StrategyParameterDecimal coefficient; // коэффицент увеличения объема покупок 
        private StrategyParameterInt slippage; // величина проскальзывание при установки ордеров стоп и профит 
        private StrategyParameterDecimal lengthStartStop;// стартовая (начальная) величина отступа для стоп приказа  в процентах от цены открытия позиции

        /// <summary>
        /// растояние до пирмиды 
        /// </summary>
        private StrategyParameterDecimal lengthToPiramid;
        private StrategyParameterDecimal toProfit; // расстояние от цены до трейлинг стопа в %
        private StrategyParameterBool vklRasCandl; // включать ли расчет стопа по свечам
        private StrategyParameterBool vklRasKauf; // прибавлять в расчетах показание индикатора кауфмана - волотильности 
        private StrategyParameterTimeOfDay LagTimeToOpenClose; // количество секунд на исполнение ордеров, после чего они будут отзываться
        private StrategyParameterInt LagPunctToOpenClose;// откат цены от цены ордера, после чего он будет отозван
        private StrategyParameterInt SlipageReversClose;// обратное проскальзывание для цены активации стопОрдера на закрытии
        private StrategyParameterInt SlipageReversOpen; // обратное проскальзывание для цены активации стопОрдера на открытии
        private StrategyParameterInt BarOldPeriod;// количество свечей (периодов) за которые рассчитываем средний объем кластеров
        private StrategyParameterButton windows;
        #endregion

        #region ====================== Логика ============================

        public void ClosePosicion() // закрытие всех позиций по маркету 
        {
            if (_tabSimple.PositionsOpenAll.Count != 0)
            {
                _tabSimple.CloseAllAtMarket();
            }
        }

        /// <summary>
        /// вычисляет средний объем за определенный период 
        /// </summary>
        private void AverageVolumePeriod(int period)
        {
            if (_tabClusterSpot.VolumeClusters.Count < BarOldPeriod.ValueInt + 2) // защита от отсутствия необходимых данных
            {
                return;
            }
            decimal volumBackPeriod = 0; // объем за период, обнуляем в начале

            int startIndex = _tabClusterSpot.VolumeClusters.Count - 2;
            int endIndex = _tabClusterSpot.VolumeClusters.Count - 2 - period;

            for (int i = startIndex; i > endIndex; i--)
            {
                HorizontalVolumeCluster clasterPeriod = _tabClusterSpot.VolumeClusters[i]; // объём  в кластере
                HorizontalVolumeLine vol = clasterPeriod.MaxSummVolumeLine; // линия с максимальным объемом
                volumBackPeriod += vol.VolumeSumm; // суммируем за весь период
            }
            decimal zn = Okruglenie(volumBackPeriod / period, 4); // вычисляем среднее и
            AverageVolumeBaсk = zn;                              // отправляем данные в переменную например 
        }

        decimal _volumeLineSell = 0; // объем максимальной  линии продаж
        public decimal _priceMaxDeltaLineSell; // цена наибольшей линии
        public bool _cameBigCluster = false; // вошел крупный
        /// <summary>
        /// смотрит пришла ли (есть) большая линия дельта (кластер)
        /// </summary>
        private void СameBigCluster()
        {
            if (AverageVolumeBaсk != 0 && // среднее значение не нуль и
                _volumeLineSell != 0 &&  // максимальная линия не пуста и
                AverageVolumeBaсk * coefficient.ValueDecimal < _volumeLineSell || // MaxSellLine больше средней * коэф или
                bigvolume.ValueInt < _volumeLineSell && vklBigCluster.ValueBool == true)
            {
                if (_cameBigCluster != true)
                {
                    PriceLargeEntry = 0;
                    PriceLargeEntry = _priceMaxDeltaLineSell;   // записываем цену входа крупного объема,
                    VolumeBigGamer = _volumeLineSell; // объем крупного 
                    _cameBigCluster = true;
                }

                string strokPrint = SecurityName + " Цена входа КРУПНОГО = " + PriceLargeEntry.ToString() + "\n";
                Debug.WriteLine(strokPrint);
            }
            else _cameBigCluster = false;
        }

        /// <summary>
        /// изменилась линия объёма максимальным суммарным объёмом Sell тут ждем MaxSellLine больше средней * коэф
        /// </summary>
        private void _tabClusterSpotDelta_MaxSellLineChangeEvent(HorizontalVolumeLine Line)
        {
            AverageVolumePeriod(BarOldPeriod.ValueInt);// запускаю расчет средней 
            _volumeLineSell = Line.VolumeSell; // суммарный объем продаж линии
            _priceMaxDeltaLineSell = Line.Price;  // цена наибольшей линии
            СameBigCluster(); // проверяет большой кластер 
            SendTextView(SecurityName + " обЪем Макс линии " + _volumeLineSell.ToString());
        }

        /// <summary>
        /// изменился максимальный обем линни покупок
        /// </summary>
        private void _tabClusterSpotDelta_MaxBuyLineChangeEvent(HorizontalVolumeLine line)
        {
            VolumeBigLineBuy = line.VolumeBuy;
            if (VolumeBigLineBuy > VolumeBigGamer && _cameBigCluster == true) // проверка выхода большого обема (закрывающего)
            {
                VolumeBigGamer = 0;
                _cameBigCluster = false;
                SendTextView(SecurityName + " КРУПНЯК закрылся на " + VolumeBigLineBuy.ToString());
            }
        }
        /// <summary>
        /// входящее событие о том что открылась некая сделка, выставляем стоп
        /// </summary>
        void _tab_PositionOpeningSuccesEvent(Position position)
        {
            _tabSimple.BuyAtStopCancel();
            _tabSimple.SellAtStopCancel();

            // выставляем стоп по отступу в обход вызова из метода окончания свечи
            indent = lengthStartStop.ValueDecimal * Price / 100;  // отступ для стопа
            decimal priceOpenPos = _tabSimple.PositionsLast.EntryPrice;  // цена открытия позиции

            if (position.Direction == Side.Buy)
            {
                stopBuy = Math.Round(priceOpenPos - indent, _tabSimple.Securiti.Decimals);
                decimal lineSell = priceOpenPos - indent;

                decimal priceOrderSell = lineSell - _tabSimple.Securiti.PriceStep * SlipageCloseFirst.ValueInt;
                decimal priceRedLineSell = lineSell + _tabSimple.Securiti.PriceStep * SlipageReversClose.ValueInt;

                if (priceRedLineSell - _tabSimple.Securiti.PriceStep * SlipageReversClose.ValueInt > _tabSimple.PriceBestAsk)
                {
                    _tabSimple.CloseAtLimit(position, _tabSimple.PriceBestAsk, position.OpenVolume);
                    return;
                }

                if (position.StopOrderPrice == 0 ||
                    position.StopOrderPrice < priceRedLineSell)
                {
                    _tabSimple.CloseAtStop(position, stopBuy, priceOrderSell);
                }

                if (position.StopOrderIsActiv == false)
                {
                    if (position.StopOrderRedLine - _tabSimple.Securiti.PriceStep * SlipageCloseFirst.ValueInt > _tabSimple.PriceBestAsk)
                    {
                        _tabSimple.CloseAtLimit(position, _tabSimple.PriceBestAsk, position.OpenVolume);
                        return;
                    }
                    position.StopOrderIsActiv = true;
                }
            }
        }

        /// <summary>
        /// основной вход в логику робота. Вызывается когда завершилась свеча (логика роботы робота)
        /// </summary>
        void CandleFinishedEvent(List<Candle> candles)
        {
            if (IsOn.ValueBool == false)
            {
                return;
            }

            TryOpenPosition(); // разрешено  открывать

            List<Position> positions = _tabSimple.PositionsOpenAll;

            DateTime lastTradeTime = candles[candles.Count - 1].TimeStart;  // берем время закрытия последней свечи

            if (positions != null && positions.Count != 0)
            {
                TryClosePosition(positions[0], candles);
                Min_loss();
            }
            /*else
            {
                TryOpenPosition(); // разрешено  открывать позу (candles)
            }*/
        }
        /// <summary>
        /// все условия для входа в позицию соблюдены - входим 
        /// </summary>
        private void TryOpenPosition()  // (List<Candle> candles)
        {
            Big = PriceLargeEntry;    // для теста вывожу в окно 

            if (PriceLargeEntry != 0 )
            {
                 RecruitingPosition(PriceLargeEntry);
            }
        }
        /// <summary>
        ///  расчет объема позиции в валюте
        /// </summary>
        private void СalculationVolumPosInSecur(int DecimalsVolume)
        {
            if (_tabSimple.StartProgram == StartProgram.IsTester)
            {
                VolumePosition = 1;
            }
            if (_tabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                VolumePosition = 0;
                decimal zna = VolumeInBaks.ValueInt;
                VolumePosition = Okruglenie(zna / Price, DecimalsVolume);
                //string str = "VolumePosition = " + VolumePosition.ToString() + "\n";
                //Debug.WriteLine(str);
            }
        }

        decimal _startPriceRecPos = 0; // цена начала набора позиции
        decimal _stopPriceRecPos = 0;  // цена окончания набора
        decimal _stepRecPos = 0; // шаг набора 
        public decimal PriceLargeEntry = 0; //цена входа крупного
        public decimal lineOpenPos = 0; // уровень входа

        /// <summary>
        /// набираем позицию 
        /// </summary> 
        private void RecruitingPosition(decimal _priceLargeEntry)
        {
            List<Position> positions = _tabSimple.PositionsOpenAll;
            if (_tabSimple.PositionsOpenAll.Count == 0) // нету открытых позиций
            {
                _startPriceRecPos = _priceLargeEntry;
                _stopPriceRecPos = _startPriceRecPos - (_priceLargeEntry / 100 * PercentOnEntry.ValueDecimal);
                _stepRecPos = (_startPriceRecPos - _stopPriceRecPos) / PartsInput.ValueInt;
                lineOpenPos = _startPriceRecPos;

                if (lineOpenPos == 0 || _startPriceRecPos == 0 || _stepRecPos == 0 || VolumeBigGamer==0)
                {
                    return;
                }
                if (Price < lineOpenPos)
                {
                    СalculationVolumPosInSecur(SecurityDecimals);
                    _tabSimple.BuyAtLimit(VolumePosition / PartsInput.ValueInt, lineOpenPos - SlipageOpenFirst.ValueInt * _tabSimple.Securiti.PriceStep);
                }
            }
            if (_tabSimple.PositionsOpenAll.Count != 0) // есть открытая позиция добираем объем
            {
                if (Price > lineOpenPos)
                {
                    Piramiding();
                }
                decimal OpenVolume = 0;
                if (positions != null && positions.Count > 0)
                {
                    OpenVolume = GetVolumePos(positions);
                }
                СalculationVolumPosInSecur(SecurityDecimals);
                if ( OpenVolume > VolumePosition)// проверка набранного обема 
                {
                    //SendTextView("обем позиции превышен "+ VolumePosition.ToString());
                    return;
                }
                if ( VolumeBigGamer == 0) // наличие крупного объема 
                {
                    return;
                }
                if (OpenVolume ==0)
                {
                    return;
                }
                decimal PriceLowerTrade = 0;
                if (positions != null && positions.Count > 0)
                {
                    PriceLowerTrade = GetPriceLowerTrade(positions); // цена нижнего трейда позиции
                }
                if (PriceLowerTrade == 0 || _stepRecPos == 0)
                {
                    return;
                }
                decimal lineAddnPos = PriceLowerTrade - _stepRecPos;

                if (Price < _stopPriceRecPos)
                {
                    return;  //чтобы больше не покупать 
                }
                if (Price < lineAddnPos && PriceLowerTrade != 0)
                {
                    if (OpenVolume == 0)
                    {
                        return;
                    }
                    if (positions[0].OpenActiv == true) // зашита от перебора 
                    {
                        return;
                    }
                    СalculationVolumPosInSecur(SecurityDecimals);
                    _tabSimple.BuyAtLimitToPosition(positions[0], _tabSimple.PriceBestAsk, VolumePosition / PartsInput.ValueInt);
                    SendTextView(SecurityName + " ДОБРАЛ по " + lineAddnPos.ToString());
                }
            }
        }
        /// <summary>
        /// берем объем моент в открытой позиции
        /// </summary>
        private decimal GetVolumePos(List<Position> positions)
        {
            if (positions != null && positions.Count > 0)
            {
                decimal OpenVolume = 0;
                OpenVolume = positions[0].MaxVolume;
                if (OpenVolume > 0)
                {
                    return OpenVolume;
                }
            }
 
            return 0;
        }

        /// <summary>
        /// взять цену последнего трейда 
        /// </summary>
        private decimal GetPriceLastTrade(List<Position> positions)
        {
            if (positions != null && positions.Count > 0)
            {
                decimal PriceLastTrade = 0;
                int sumTred = 0;
                sumTred = positions[0].MyTrades.Count; // количество трейдов в позиции
                if (sumTred != 0)
                {
                    PriceLastTrade = positions[0].MyTrades[sumTred-1].Price; // цена последнего трейда позиции
                    //_tabSimple.PositionsLast.MyTrades[sumTred - 1].Price; // цена последнего трейда 
                    if (PriceLastTrade != 0)
                    {
                        return PriceLastTrade;
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// взять цену нижнего трейда позиции 
        /// </summary>
        private decimal GetPriceLowerTrade(List<Position> positions)
        {
            if (positions != null && positions.Count > 0)
            {
                decimal PriceLowerTrade = 0; // цена нижнего трйда
                decimal PriceLastTrade = 0; // цена последнего трйда
                int CountTred = 0;
                CountTred = positions[0].MyTrades.Count; // количество трейдов в позиции
                if (CountTred != 0)
                    for (int i = 0; i < CountTred; i++)
                    {
                        PriceLastTrade = positions[0].MyTrades[i].Price; // цена трейда позиции
                        if (PriceLastTrade !=0 && PriceLowerTrade == 0) 
                        {
                            PriceLowerTrade = PriceLastTrade;
                        }
                        if (PriceLowerTrade > PriceLastTrade)
                        {
                            PriceLowerTrade = PriceLastTrade;
                        }
                    }
                if (PriceLowerTrade != 0)
                {
                    return PriceLowerTrade;
                }
            }
            return 0;
        }

        /// <summary>
        /// взять цену верхнего трейда сделки
        /// </summary>
        private decimal GetPriceUpperTrade(List<Position> positions)
        {
            if (positions != null && positions.Count > 0)
            {
                decimal PriceUpeerTrade = 0;
                decimal PriceLastTrade = 0;
                int CountTred = 0;
                CountTred = positions[0].MyTrades.Count; // количество трейдов в позиции
                if (CountTred != 0)
                    for (int i = 0; i < CountTred; i++)
                    {
                        PriceLastTrade = positions[0].MyTrades[i].Price; // цена трейда позиции
                        if (PriceLastTrade !=0 && PriceUpeerTrade == 0)
                        {
                            PriceUpeerTrade = PriceLastTrade;    
                        }
                        if (PriceUpeerTrade < PriceLastTrade)
                        {
                            PriceUpeerTrade = PriceLastTrade;
                        }
                    }
                if (PriceUpeerTrade != 0)
                {
                    return PriceUpeerTrade;
                }

            }
            return 0;
        }
        /// <summary>
        /// Добор обема позиции пирамидой
        /// </summary>
        private void Piramiding()
        {
            List<Position> positions = _tabSimple.PositionsOpenAll;
            decimal OpenVolume = 0;
            if (positions !=null)
            {
                OpenVolume = GetVolumePos(positions);
            }
            decimal pricePos = positions[0].EntryPrice;
            СalculationVolumPosInSecur(SecurityDecimals);

            if (_cameBigCluster ==  true 
                && positions.Count > 0 
                && positions != null 
                && OpenVolume != 0) // уже есть открытый обем 
            {
                if (OpenVolume < VolumePosition // если объем не набран
                     && VolumeBigGamer != 0 // объем крупного есть
                     && pricePos !=0) // цена позиции есть
                {
                    decimal PricUpperTrade = 0;
                    PricUpperTrade = GetPriceUpperTrade(positions); // цена верхнего трейда позиции
                    decimal PricePiram = PricUpperTrade + (Price / 100 * lengthToPiramid.ValueDecimal);

                    if (Price > PricePiram  && PricUpperTrade != 0 )
                    {
                        if (OpenVolume == 0)
                        {
                            return;
                        }
                        if (positions[0].OpenActiv == true) // зашита от перебора 
                        {
                            return;
                        }
                        _tabSimple.BuyAtLimitToPosition(positions[0], _tabSimple.PriceBestBid, VolumePosition / PartsInput.ValueInt);
                        SendTextView(SecurityName + " Добрались пирамидой" );
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// проверить условия на выход из позиции - пересчитываются стоп ордера
        /// </summary> 
        private void TryClosePosition(Position position, List<Candle> candles) // тут пересчитываются стоп ордера 
        {
            if (position.Direction == Side.Buy)
            {
                if (vklRasCandl.ValueBool == false)
                {
                    return;
                }
                decimal lineSell = GetPriceToStopOrder(position.TimeCreate, position.Direction, candles, candles.Count - 1);

                if (lineSell == 0)
                {
                    return;
                }

                decimal priceOrderSell = lineSell - _tabSimple.Securiti.PriceStep * SlipageCloseFirst.ValueInt; // ЗДЕСЬ!!!!!!!!!!!!!!
                decimal priceRedLineSell = lineSell + _tabSimple.Securiti.PriceStep * SlipageReversClose.ValueInt;

                if (priceRedLineSell - _tabSimple.Securiti.PriceStep * slippage.ValueInt > _tabSimple.PriceBestAsk)
                {
                    _tabSimple.CloseAtLimit(position, _tabSimple.PriceBestAsk, position.OpenVolume);
                    return;
                }

                if (position.StopOrderPrice == 0 ||
                    position.StopOrderPrice < priceRedLineSell)
                {
                    _tabSimple.CloseAtStop(position, priceRedLineSell, priceOrderSell);
                }

                if (position.StopOrderIsActiv == false)
                {
                    if (position.StopOrderRedLine - _tabSimple.Securiti.PriceStep * slippage.ValueInt > _tabSimple.PriceBestAsk)
                    {
                        _tabSimple.CloseAtLimit(position, _tabSimple.PriceBestAsk, position.OpenVolume);
                        return;
                    }
                    position.StopOrderIsActiv = true;
                }
            }
        }

        /// <summary>
        /// событие - пришел новый стакан
        /// </summary>
        private void _tab_MarketDepthUpdateEvent(MarketDepth marketDepth)
        {
            if (_tabSimple.IsConnected == false)
            {
                return;
            }
        }

        /// <summary>
        /// ТЕСТИРУЮ ПЕРЕМЕННЫЕ ТУТ !! (происходит с каждым новым тиком в системе) 
        /// </summary>
        void _tab_NewTickEvent(Trade trade)
        {
            // для теста
            TryOpenPosition();
            // !!!!! тут присвоение для теста 
            Test = lineOpenPos;

            Price = trade.Price;

            ChekReActivator(trade);

            if (_tabSimple.PositionsOpenAll.Count != 0) // если есть позиция
            {
                To_stop_profit(); // включает трейлинг стоп 
                Min_loss();
            }
        }

        /// <summary>
        /// закрылась позиция обнуляем входные данные 
        /// </summary>
        private void _tab_PositionClosingSuccesEvent(Position position)
        {
            PriceLargeEntry = 0; // цена входа крупного объема 
            lineOpenPos = 0; //  обнуляем уровень входа
            VolumePosition = 0; // обем входа в позу
            VolumeBigGamer = 0; 
        }

        /// <summary>
        /// для сдвига стопа  
        /// </summary>
        public void Min_loss()
        {
            indent = lengthStartStop.ValueDecimal * Price / 100;  // отступ для стопа
            decimal priceOpenPos = 0; // цена открытия позиции

            if (_tabSimple.PositionsOpenAll.Count != 0)
            {
                priceOpenPos = _tabSimple.PositionsLast.EntryPrice;
            }
            if (priceOpenPos == 0)
            {
                return;
            }
            if (_tabSimple.PositionsLast.Direction == Side.Buy)
            {
                if (Price < priceOpenPos)
                {
                    return;
                }

                if (priceOpenPos + indent < Price) // если цена выросла  больше допустимого стопа переносим стоп сделки в безубыток
                {
                    stopBuy = priceOpenPos + indent;
                }
            }
        }

        public bool dinamik;
        /// <summary>
        /// для движения трлейлинг стопа  
        /// </summary>
        public void To_stop_profit()
        {
            decimal priceOpenPos = 0; // цена открытия позиции

            if (vklRasKauf.ValueBool == true)
            {
                dinamik = true;
            }
            else
            {
                dinamik = false;
            }
            if (_tabSimple.PositionsOpenAll.Count != 0)
            {
                priceOpenPos = _tabSimple.PositionsLast.EntryPrice;
            }
            if (priceOpenPos == 0)
            {
                return;
            }
            decimal komis = Price / 100 * 0.04m;
            if (_tabSimple.PositionsLast.Direction == Side.Buy)
            {
                if (Price < priceOpenPos)
                {
                    return;
                }
                decimal stopActivacion = Price - Price * (lengthStartStop.ValueDecimal / 100);
                decimal stopOrderPrice = stopActivacion - slippage.ValueInt * _tabSimple.Securiti.PriceStep;
                if (stopActivacion < priceOpenPos + komis) // пока стоп ниже безубытка , поднимаем его
                {
                    stopActivacion = Price - Price * (lengthStartStop.ValueDecimal / 100);
                    _tabSimple.CloseAtTrailingStop(_tabSimple.PositionsLast, stopActivacion, stopOrderPrice);
                    stopBuy = stopActivacion;
                    //SendTextView("Стоп цена = " + stopBuy.ToString());
                }
                // когда стоп выше безубытка позиции
                if (Price > priceOpenPos + komis + Price * (toProfit.ValueDecimal / 100))
                {
                    if (vklRasKauf.ValueBool == true)    // динамика
                    {
                        stopActivacion = Price - Price * ((toProfit.ValueDecimal) / 100);
                        stopOrderPrice = stopActivacion - slippage.ValueInt * _tabSimple.Securiti.PriceStep;
                        _tabSimple.CloseAtTrailingStop(_tabSimple.PositionsLast, stopActivacion, stopOrderPrice);
                        stopBuy = stopActivacion;
                    }
                    else
                    {
                        stopActivacion = Price - Price * (toProfit.ValueDecimal / 100);
                        stopOrderPrice = stopActivacion - slippage.ValueInt * _tabSimple.Securiti.PriceStep;
                        _tabSimple.CloseAtTrailingStop(_tabSimple.PositionsLast, stopActivacion, stopOrderPrice);
                        stopBuy = stopActivacion;
                    }
                }
            }
        }
        #endregion

        #region ==================== Сопровождение позицмй ======================================
        void _tab_PositionOpeningFailEvent(Position position)
        {
            if (!string.IsNullOrWhiteSpace(position.Comment)) // если есть комментарий 
            {
                return;
            }

            if (position.OpenVolume != 0)  // если есть купленный объем
            {
                return;
            }

            if (StartProgram == StartProgram.IsTester) // если в тестере
            {
                return;
            }

            if (position.OpenOrders.Count > 1 || // если есть ордер открывающий позицию или комментарием секонд
            position.Comment == "Second")
            {
                return;
            }

            List<Position> openPos = _tabSimple.PositionsOpenAll;
            if (openPos != null && openPos.Count > 1 ||
               openPos != null && openPos.Count == 1 &&
                openPos[0].Direction == position.Direction) // если есть открытые позиции
            {
                return;
            }

            _tabSimple.BuyAtStopCancel(); // отменяются все заявки по пробитию 
            _tabSimple.SellAtStopCancel();

            if (position.Direction == Side.Buy)
            {
                decimal price = _tabSimple.PriceBestBid + SlipageOpenSecond.ValueInt * _tabSimple.Securiti.PriceStep;

                Position pos = _tabSimple.BuyAtLimit(position.OpenOrders[0].Volume, price); // открываем
                pos.Comment = "Second";
            }
            else if (position.Direction == Side.Sell)
            {
                decimal price = _tabSimple.PriceBestAsk - SlipageOpenSecond.ValueInt * _tabSimple.Securiti.PriceStep;

                Position pos = _tabSimple.SellAtLimit(position.OpenOrders[0].Volume, price);  // открываем
                pos.Comment = "Second";
            }
        }

        /// <summary>
        /// ошибка с закрытием заявки
        /// </summary>
        void _tab_PositionClosingFailEvent(Position position)
        {
            if (position.OpenVolume > 0)
            {
                position.State = PositionStateType.Open;
            }
            if (position.OpenVolume < 0)
            {
                position.State = PositionStateType.ClosingSurplus;
            }
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }
            if (_positionToClose != null && _positionToClose.Number == position.Number)
            {
                return;
            }

            if (position.OpenVolume == 0)
            {
                return;
            }

            if (position.CloseOrders.Count > 1)
            {
                return;
            }

            if (position.Direction == Side.Buy)
            {
                decimal price = _tabSimple.PriceBestAsk - SlipageCloseSecond.ValueInt * _tabSimple.Securiti.PriceStep;
                _tabSimple.CloseAtLimit(position, price, position.OpenVolume);
            }
            else if (position.Direction == Side.Sell)
            {
                decimal price = _tabSimple.PriceBestBid + SlipageCloseSecond.ValueInt * _tabSimple.Securiti.PriceStep;
                _tabSimple.CloseAtLimit(position, price, position.OpenVolume);
            }
        }

        /// <summary>
        /// место работы потока который отключает робота в нерабочее время
        /// </summary>
        private void TimeWatcherArea()
        {
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }
            while (true)
            {
                Thread.Sleep(1000);

                DateTime lastTradeTime = DateTime.Now;

                if (lastTradeTime.Hour < TimeFrom.ValueInt && TimeFrom.ValueInt != 0 ||
                    lastTradeTime.Hour > TimeTo.ValueInt && TimeTo.ValueInt != 0)
                {
                    List<Position> positions = _tabSimple.PositionsOpenAll;

                    if (positions == null || positions.Count == 0)
                    {
                        continue;
                    }

                    for (int i = 0; i < positions.Count; i++)
                    {
                        Position pos = positions[i];

                        if (pos.StopOrderIsActiv == true ||
                            pos.ProfitOrderIsActiv == true)
                        {
                            pos.StopOrderIsActiv = false;
                            pos.ProfitOrderIsActiv = false;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// получить (берем) цену для выхода из позиции
        /// </summary>
        private decimal GetPriceToStopOrder(DateTime positionCreateTime, Side side, List<Candle> candles, int index)
        {
            if (candles == null)
            {
                return 0;
            }

            if (side == Side.Buy)
            { // рассчитываем цену стопа при Лонге
              // 1 находим максимум за время от открытия сделки и до текущего
                decimal maxHigh = 0;
                int indexIntro = 0;
                DateTime openPositionTime = positionCreateTime;

                if (openPositionTime == DateTime.MinValue)
                {
                    openPositionTime = candles[index - 2].TimeStart;
                }

                for (int i = index; i > 0; i--)
                { // смотрим индекс свечи, после которой произошло открытие позы
                    if (candles[i].TimeStart <= openPositionTime)
                    {
                        indexIntro = i;
                        break;
                    }
                }

                for (int i = indexIntro; i < index + 1; i++)
                { // смотрим максимум после открытия

                    if (candles[i].High > maxHigh)
                    {
                        maxHigh = candles[i].High;
                    }
                }

                // 2 рассчитываем текущее отклонение для стопа

                decimal distanse = lengthStartStop.ValueDecimal;

                for (int i = indexIntro; i < index + 1; i++)
                { // смотрим коэффициент

                    DateTime lastTradeTime = candles[i].TimeStart;

                    if (lastTradeTime.Hour < TimeFrom.ValueInt && TimeFrom.ValueInt != 0 ||
                        lastTradeTime.Hour > TimeTo.ValueInt && TimeTo.ValueInt != 0 ||
                        TimeFrom.ValueInt == 10 && lastTradeTime.Minute == 0)
                    {
                        continue;
                    }
                    if (vklRasCandl.ValueBool == false) // вкючено ли слежение по свечам
                    {
                        return stopBuy;
                    }
                    else
                    {
                        distanse = DistLongInit;

                        {
                            distanse -= 2.0m * LongAdj;
                        }
                    }
                }

                // 3 рассчитываем цену Стопа
                Min_loss();  // расчет стопа

                decimal stopCandel = maxHigh; // стоп рассчитываемый по свечам
                if (stopCandel < stopBuy)
                {
                    return stopBuy;
                }
                if (stopCandel > stopBuy)
                {
                    if (vklRasCandl.ValueBool == true)
                    {
                        return stopCandel;
                    }
                    else
                    {
                        return stopBuy;
                    }
                }
            }
            return 0;
        }

        // отложенное закрытие позиции. Чтобы при выходе по эмулятору дать системе время отозвать ордер
        private Position _positionToClose;

        private DateTime _timeToClose;

        private void AreaCloserPositionThread()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (_positionToClose == null)
                {
                    continue;
                }

                if (DateTime.Now < _timeToClose)
                {
                    continue;
                }

                if (_positionToClose.OpenVolume != 0 && _positionToClose.Direction == Side.Buy)
                {
                    _tabSimple.CloseAtLimit(_positionToClose, _tabSimple.PriceBestAsk - _tabSimple.Securiti.PriceStep * 10, _positionToClose.OpenVolume);
                }
                _positionToClose = null;
            }
        }

        // отзыв заявок по времени и отступу

        /// <summary>
        /// слежение за выставленными и ещё не исполненными ордерами
        /// </summary>
        void WatcherOpenPosition()
        {
            while (true)
            {
                Thread.Sleep(1000);
                // этот метод создан для того, чтобы инициализировать закрытие 
                // не полностью открытых ордеров в конце периода
                if (StartProgram == StartProgram.IsTester)
                { // если тестируем
                    return;
                }

                Thread.Sleep(1000);

                try
                {
                    List<Position> positions = _tabSimple.PositionsOpenAll;

                    if (positions == null ||
                        positions.Count == 0)
                    {
                        continue;
                    }

                    // смотрим первый выход - 3 секунды 

                    List<Order> myOrderToFirstClose = new List<Order>();

                    for (int i = 0; i < positions.Count; i++)
                    {
                        if (positions[i].OpenOrders.Count == 1 &&
                            positions[i].OpenOrders[0].State == OrderStateType.Activ &&
                            positions[i].Comment != "Second")
                        {
                            myOrderToFirstClose.Add(positions[i].OpenOrders[positions[i].OpenOrders.Count - 1]);
                        }

                        if (positions[i].CloseOrders != null && positions[i].CloseOrders.Count == 1 &&
                            positions[i].CloseOrders[positions[i].CloseOrders.Count - 1].State == OrderStateType.Activ)
                        {
                            myOrderToFirstClose.Add(positions[i].CloseOrders[positions[i].CloseOrders.Count - 1]);
                        }
                    }

                    for (int i = 0; i < myOrderToFirstClose.Count; i++)
                    {
                        if (myOrderToFirstClose[i].TimeCallBack.AddSeconds(3) < _tabSimple.TimeServerCurrent)
                        {
                            _reActivatorIsOn = false;
                            _tabSimple.CloseOrder(myOrderToFirstClose[i]);
                        }
                    }

                    // смотрим классический выход

                    List<Order> myOrder = new List<Order>();

                    for (int i = 0; i < positions.Count; i++)
                    {
                        if (positions[i].OpenOrders[positions[i].OpenOrders.Count - 1].State == OrderStateType.Activ)
                        {
                            myOrder.Add(positions[i].OpenOrders[positions[i].OpenOrders.Count - 1]);
                        }

                        if (positions[i].CloseOrders != null && positions[i].CloseOrders[positions[i].CloseOrders.Count - 1].State == OrderStateType.Activ)
                        {
                            myOrder.Add(positions[i].CloseOrders[positions[i].CloseOrders.Count - 1]);
                        }
                    }

                    for (int i = 0; i < myOrder.Count; i++)
                    {
                        Order order = myOrder[i];
                        // бежим по коллекции ордеров
                        if (order.State != OrderStateType.Done &&
                            order.State != OrderStateType.Fail &&
                            order.State != OrderStateType.None)
                        {
                            // если какойто не исполнен полностью

                            DateTime startTime = order.TimeCallBack;
                            DateTime marketTime = _tabSimple.TimeServerCurrent;

                            if (startTime == DateTime.MinValue ||
                                startTime == DateTime.MaxValue)
                            {
                                continue;
                            }

                            if (startTime.AddSeconds(LagTimeToOpenClose.Value.Second) < marketTime)
                            {
                                _tabSimple.CloseOrder(order);
                                Thread.Sleep(2000);
                                AlertMessageManager.ThrowAlert(Properties.Resources.wolf01, NameStrategyUniq, "Отзываем ордер по времени");
                                _tabSimple.SetNewLogMessage("Отзываем ордер по времени", LogMessageType.System);
                            }
                            else
                            {
                                decimal priceBid = _tabSimple.PriceBestBid;
                                decimal priceAsk = _tabSimple.PriceBestAsk;

                                if (order.Side == Side.Buy &&
                                    order.Price + LagPunctToOpenClose.ValueInt * _tabSimple.Securiti.PriceStep < priceAsk)
                                {
                                    _tabSimple.CloseOrder(order);
                                    Thread.Sleep(2000);
                                }

                                if (order.Side == Side.Sell &&
                                    order.Price - LagPunctToOpenClose.ValueInt * _tabSimple.Securiti.PriceStep > priceBid)
                                {
                                    _tabSimple.CloseOrder(order);
                                    Thread.Sleep(2000);
                                }
                            }
                        }
                        else if (order.State == OrderStateType.Fail)
                        {
                            AlertMessageManager.ThrowAlert(Properties.Resources.wolf01, NameStrategyUniq, "Ошибка выставления ордера");
                            myOrder.Remove(order);
                            i--;
                        }
                    }
                }
                catch (Exception)
                {
                    // ignore
                }
            }
        }

        // уровень переоткрытия ордеров и уровней для пробоя

        /// <summary>
        /// включена ли реАктивация ордера
        /// </summary>
        private bool _reActivatorIsOn;

        /// <summary>
        /// время до которого реактивация возможна
        /// </summary>
        private DateTime _reActivatorMaxTime;

        /// <summary>
        /// цена реАктивации
        /// </summary>
        private decimal _reActivatorPrice;

        /// <summary>
        /// ордер который следует реАктивировать
        /// </summary>
        private Order _reActivatorOrder;

        /// <summary>
        /// поставить на слежение новый ордер
        /// </summary>
        private void AlarmReActivator(Order order, decimal activatePrice, DateTime maxTime)
        {
            if (StartProgram == StartProgram.IsTester)
            {
                return;
            }
            _reActivatorOrder = order;
            _reActivatorMaxTime = maxTime;
            _reActivatorPrice = activatePrice;
            _reActivatorIsOn = true;
        }

        /// <summary>
        /// прогрузить реАктивартор новым трейдом
        /// </summary>
        private void ChekReActivator(Trade trade)
        {
            // если ордер отозван
            // и цена пересекла цену переактивации
            // и время активации не кончалось
            // вызываем переактивацию стопов и уровней на пробой
            // отключаем активатор

            if (_reActivatorIsOn == false)
            {
                return;
            }

            if (_reActivatorOrder.State == OrderStateType.Fail ||
                 _reActivatorOrder.State == OrderStateType.Done ||
                _reActivatorOrder.VolumeExecute != 0)
            { // ордер с ошибкой или уже частично исполнен
                _reActivatorIsOn = false;
                return;
            }

            if (_reActivatorOrder.State != OrderStateType.Done &&
                _reActivatorOrder.State != OrderStateType.Cancel)
            { // ордер ещё выставлен
                return;
            }

            if (DateTime.Now > _reActivatorMaxTime)
            {
                _reActivatorIsOn = false;
                return;
            }

            if (_reActivatorOrder.Side == Side.Buy &&
                trade.Price <= _reActivatorPrice)
            {
                _reActivatorIsOn = false;
                ReActivateOrder(_reActivatorOrder);
            }
        }

        /// <summary>
        /// реактивировать ордер
        /// </summary>
        private void ReActivateOrder(Order order)
        {
            // 1 находим позицию по которой прошёл ордер

            List<Position> allPositions = _tabSimple.PositionsAll;

            if (allPositions == null)
            {
                return;
            }

            Position myPosition = null;


            for (int i = allPositions.Count - 1; i > -1; i--)
            {
                if (allPositions[i].OpenOrders.Find(order1 => order1.NumberUser == order.NumberUser) != null)
                {
                    myPosition = allPositions[i];
                    break;
                }
                if (allPositions[i].CloseOrders != null && allPositions[i].CloseOrders.Find(order1 => order1.NumberUser == order.NumberUser) != null)
                {
                    myPosition = allPositions[i];
                    break;
                }
            }

            if (myPosition == null)
            {
                return;
            }

            if (myPosition.OpenVolume == 0)
            {
                if (_reActivatorOrder.Side == Side.Buy)
                {
                    _tabSimple.BuyAtLimit(Convert.ToInt32(_reActivatorOrder.Volume), _reActivatorOrder.Price);
                }
            }
            else if (myPosition.OpenVolume != 0)
            {
                _tabSimple.CloseAtLimit(myPosition, order.Price, order.Volume);
            }
        }

        /// <summary>
        /// ордера
        /// </summary>
        private List<Order> _myOrders;

        /// <summary>
        /// в системе новый ордер
        /// </summary>
        void _tab_OrderUpdateEvent(Order order)
        {
            if (_myOrders == null)
            {
                _myOrders = new List<Order>();
            }

            if (_myOrders.Find(order1 => order1.NumberUser == order.NumberUser) == null)
            {
                _myOrders.Add(order);
                AlarmReActivator(order, order.Price, DateTime.Now.AddSeconds(LagTimeToOpenClose.Value.Second));
            }
        }


        #endregion

        #region ================= Сервис =============================================
        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt", false)
                    )
                {
                    writer.WriteLine(DistLongInit);
                    writer.WriteLine(LongAdj);
                    writer.WriteLine(DistShortInit);
                    writer.WriteLine(ShortAdj);
                    writer.WriteLine(PriceLargeEntry);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// загрузить настройки из файла
        /// </summary>
        private void Load()
        {
            if (!File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
                {
                    DistLongInit = Convert.ToDecimal(reader.ReadLine());
                    LongAdj = Convert.ToDecimal(reader.ReadLine());
                    DistShortInit = Convert.ToDecimal(reader.ReadLine());
                    ShortAdj = Convert.ToDecimal(reader.ReadLine());
                    PriceLargeEntry = Convert.ToDecimal(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// взять имя робота
        /// </summary>
        public override string GetNameStrategyType()
        {
            return "Insight";
        }
        /// <summary>
        /// показать окно настроек робота
        /// </summary>
        public override void ShowIndividualSettingsDialog()
        {
            InsightUi ui = new InsightUi(this);
            ui.Show();
        }


        private void UserClickOnButtonEvent() // нажал на кнопку в панели параметров 
        {

        }

        private void Start_ParametrsChangeByUser() // событие изменения параметров пользователем
        {

        }

        /// <summary>
        /// Отправка текста  в окно дебагера 
        /// </summary>
        public void SendTextView(string zna)
        {
            // string strokPrint = "Цена входа КРУПНОГО = " + zna.ToString() + "\n";
            //string strokPrint = zna + "\n";
            Debug.WriteLine(zna);
        }

        /// <summary>
        /// округляет decimal до n чисел после запятой
        /// </summary>
        public static decimal Okruglenie(decimal vol, int n)
        {
            decimal value = vol;
            int N = n;
            decimal chah = decimal.Round(value, N, MidpointRounding.ToEven);
            return chah;
        }

        #endregion

        #region ======================= реализация INotifyPropertyChanged ====================================

        /// <summary>
        /// обработчик события изменения свойств
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void СallUpdate(string name)  // сигнализирует об изменении свойств
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        ///  сверяет значения данных и выдает сигнал об изменении 
        /// </summary>
        protected void Set<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!field.Equals(value))
            {
                field = value;
                СallUpdate(name);
            }
        }
        #endregion
    }
}
