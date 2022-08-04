using OsEngine.Entity;
using OsEngine.Indicators;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Journal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OsEngine.Robots.MyBot.InsightScreener;

namespace OsEngine.Robots.MyBot.InsightScreener
{
    public class InsiRobot : ViewModBase
    {
        public BotTabSimple TabSimple;

        #region Переменные =========================================================================

        /// <summary>
        ///  время включения бумаги
        /// </summary>
        private DateTime timeOnPapir = DateTime.MaxValue;

        /// <summary>
        /// цена трейлинг стопа
        /// </summary>
        private decimal PriceTrailStop = 0;

        /// <summary>
        /// цена трейлинг профита
        /// </summary>
        private decimal PriceTrelingProfit = 0;

        /// <summary>
        /// минимальная цена начала фазы роста
        /// </summary>
        public decimal minPriceGrowthPhase;

        /// <summary>
        /// значение фазы роста
        /// </summary>
        public bool phaseGrowth;

        /// <summary>
        /// рыночная цена бумаги
        /// </summary>
        public decimal marketPrice;

        /// <summary>
        /// объем сделки
        /// </summary>
        public decimal VolumePosition;

        /// <summary>
        /// расчет фазы роста вкл/выкл
        /// </summary>
        public bool _calculationGP;

        /// <summary>
        /// время последнего трейда
        /// </summary>
        public DateTime real_time;

        private int _numberRobot = 0;

        #endregion Переменные =========================================================================

        #region Properties============================= Properties ====================================

        /// <summary>
        ///торговать ли бумагу
        /// </summary>
        public bool TradePaper
        {
            get => _tradePaper;

            set
            {
                _tradePaper = value;
                OnPropertyChanged(nameof(TradePaper));
                //Save();
            }
        }

        private bool _tradePaper = true;

        /// <summary>
        ///Тикет класс средств в портфеле
        /// </summary>
        public string SecurClass
        {
            get => _securClass;
            set
            {
                _securClass = value;
                OnPropertyChanged(nameof(SecurClass));
            }
        }

        private string _securClass;

        /// <summary>
        /// свойство объем в баксах расчет при изменении значения
        /// </summary>
        public decimal InBucks
        {
            get
            {
                return _inBucks;
            }
            set
            {
                if (value == _inBucks) return;

                _inBucks = value;

                if (InPercent == 0)
                {
                    Save();
                }
                else
                {
                    InPercent = 0;
                    Save();
                }

                // изменилось - рассчитываем объем
                //СalculationVolumPosInSecur();
                OnPropertyChanged(nameof(InBucks));
                //
                string str = "Объем позиции = " + VolumePosition.ToString() + " монет \n";
                Debug.WriteLine(str);
            }
        }

        private decimal _inBucks;

        /// <summary>
        /// свойство объем в процентах расчет при изменении значения
        /// </summary>
        public decimal InPercent
        {
            get
            {
                return _inPercent;
            }
            set
            {
                if (value == _inPercent) return;
                _inPercent = value;
                if (InBucks == 0)
                {
                    Save();
                }
                else
                {
                    InBucks = 0;
                    Save();
                }

                // изменилось - рассчитываем объем
                //СalculationVolumPosInPersent();
                OnPropertyChanged(nameof(InPercent));
                //
                string str = "Объем позиции = " + VolumePosition.ToString() + " монет \n";
                Debug.WriteLine(str);
            }
        }

        private decimal _inPercent;

        /// <summary>
        /// Название бумаги
        /// </summary>
        public string SecurityName
        {
            get
            {
                // TabSimple.IsConnected &&
                if (TabSimple.IsConnected && TabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    return TabSimple.Securiti.Name;
                }
                //OnPropertyChanged(nameof(SecurityName));

                if (TabSimple.StartProgram == StartProgram.IsTester)
                {
                    return TabSimple.Securiti.Name;
                }
                //OnPropertyChanged(nameof(SecurityName));
                else return "нет данных";
            }
        }

        /// <summary>
        /// децимал бумаги
        /// </summary>
        public int SecurityDecimals
        {
            get
            {
                //TabSimple.IsConnected &&
                if (TabSimple.IsConnected && TabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    return TabSimple.Securiti.DecimalsVolume;
                }
                if (TabSimple.StartProgram == StartProgram.IsTester)
                {
                    return TabSimple.Securiti.DecimalsVolume;
                }
                else return 3;
            }
        }

        public int _securityDecimals;

        /// <summary>
        /// расчеты по хвостам
        /// </summary>
        public bool Tail
        {
            get => _tail;

            set
            {
                _tail = value;
                OnPropertyChanged(nameof(Tail));
                Save();
            }
        }

        private bool _tail = false;

        /// <summary>
        /// вкл/выкл Тэйк профит
        /// </summary>
        public bool IsOnTakeProfit
        {
            get => _isOntakePofit;

            set
            {
                _isOntakePofit = value;
                OnPropertyChanged(nameof(IsOnTakeProfit));
                Save();
            }
        }

        private bool _isOntakePofit = false;

        /// <summary>
        /// вкл/выкл трейлинг стопа
        /// </summary>
        public bool IsOnTrelingStopLoss
        {
            get => _isOnTrelingStopLoss;

            set
            {
                _isOnTrelingStopLoss = value;
                OnPropertyChanged(nameof(IsOnTrelingStopLoss));
                Save();
            }
        }

        private bool _isOnTrelingStopLoss = true;

        /// <summary>
        /// вкл/выкл трейлинг профит
        /// </summary>
        public bool IsOnTrelingProfit
        {
            get => _isOnTrelingProfit;

            set
            {
                _isOnTrelingProfit = value;
                OnPropertyChanged(nameof(IsOnTrelingProfit));
                Save();
            }
        }

        private bool _isOnTrelingProfit = true;

        /// <summary>
        ///  вкл.выкл задержка ордеров
        /// </summary>
        public bool IsOnDelay
        {
            get => _isOnDelay;
            set
            {
                _isOnDelay = value;
                OnPropertyChanged(nameof(IsOnDelay));
                Save();
            }
        }

        private bool _isOnDelay = false;

        #endregion Properties============================= Properties ====================================

        #region ========== Конструктор ============================================================

        private InsightScreener _scalper;

        public InsiRobot(BotTabSimple newTab, InsightScreener scalper, int num)
        {
            _numberRobot = num;

            TabSimple = newTab;
            TabSimple.NewTickEvent += TabSimple_NewTickEvent;
            TabSimple.CandleFinishedEvent += MainLogicCandleFinishedEvent; //закрытие свечи главный вход в логику
            TabSimple.PositionOpeningSuccesEvent += TabSimple_PositionOpeningSuccesEvent;
            TabSimple.PositionClosingSuccesEvent += TabSimple_PositionClosingSuccesEvent;

            _scalper = scalper;
            Load();
        }

        #endregion ========== Конструктор ============================================================

        #region Логика ===============================================================================

        /// <summary>
        ///  логика входа
        /// </summary>
        private void MainLogicCandleFinishedEvent(List<Candle> candles)
        {
            GetNameSecuretiClass(TabSimple); // для теста
            List<Position> positions = TabSimple.PositionsOpenAll;

            if (_scalper.IsOn.ValueBool == false) // если робот выключен
            {
                return;
            }
            IndicatorDSR(candles, TabSimple); // проверка состояния индикатора DSR
            if (positions.Count == 0) // логика входа
            {
                СalculationPhaseGrowthExtremeCandels(candles, TabSimple); // расчет фазы роста

                if (phaseGrowth == false)  // если не фаза роста
                {
                    return;
                }
                if (_tradePaper == false)
                {
                    return;
                }
                decimal indent = minPriceGrowthPhase * _scalper.riseFromLow.ValueDecimal / 100;
                if (TabSimple.PriceBestAsk > minPriceGrowthPhase + indent && minPriceGrowthPhase != 0)
                {
                    СalculationVolumPosBase(SecurityDecimals); // рассчитываем объем для открываемой позиции
                    TabSimple.BuyAtMarket(VolumePosition); // вход
                }
            }
        }

        /// <summary>
        ///  базовый расчет объема позиции
        /// </summary>
        private void СalculationVolumPosBase(int DecimalsVolume)
        {
            if (TabSimple.StartProgram == StartProgram.IsTester)
            {
                VolumePosition = 1;
            }
            if (TabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                if (InPercent == 0)
                {
                    СalculationVolumPosInSecur(DecimalsVolume);
                }
                if (InBucks == 0)
                {
                    СalculationVolumPosInPersent(DecimalsVolume);
                }
            }
        }

        /// <summary>
        ///  расчет объема позиции в валюте
        /// </summary>
        private void СalculationVolumPosInSecur(int DecimalsVolume)
        {
            if (TabSimple.StartProgram == StartProgram.IsTester)
            {
                VolumePosition = 1;
            }
            if (TabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                decimal zna = InBucks;
                VolumePosition = Rounding(zna / marketPrice, DecimalsVolume);
                string str = "VolumePosition = " + VolumePosition.ToString() + "\n";
                Debug.WriteLine(str);
            }
        }

        /// <summary>
        ///  расчет объема позиции в проценте
        /// </summary>
        private void СalculationVolumPosInPersent(int DecimalsVolume)
        {
            if (TabSimple.StartProgram == StartProgram.IsTester)
            {
                VolumePosition = 1;
            }
            if (TabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                if (GetBalans(TabSimple) != 0)
                {
                    decimal depo = GetBalans(TabSimple);
                    decimal bucks = depo / 100 * InPercent;
                    VolumePosition = Rounding(bucks / marketPrice, DecimalsVolume);
                    string str = "VolumePosition = " + VolumePosition.ToString() + "\n";
                    Debug.WriteLine(str);
                }
            }
        }

        /// <summary>
        /// расчет фазы роста по цене крайних свечей
        /// </summary>
        private void СalculationPhaseGrowthExtremeCandels(List<Candle> candles, BotTabSimple TabSimple)
        {
            int canBack = _scalper.candleBack.ValueInt;
            if (candles.Count < canBack + 1)
            {
                return;
            }
            if (_calculationGP == false)// отключает метод что бы зафиксировать цену начала фазы роста
            {
                return;
            }
            decimal maxBodyClose = candles[candles.Count - 1].Close; //максимальное значение закрытия последней свечи периода
            decimal minBodyOpen = candles[candles.Count - 1 - canBack].Open; //минимальное значение открытия первой свечи периода
            decimal highPriceOutPeriod = candles[candles.Count - 1].High; // цена хая последней свечи периода
            decimal lowPriceInPerod = candles[candles.Count - 1 - canBack].Low; // цена  лоя начальной свечи периода

            if (_tail == false) //если галочка по хвостам ложь считаем по телам
            {
                decimal rost = maxBodyClose - minBodyOpen;
                decimal rostPers = rost / minBodyOpen * 100;
                if (rostPers >= _scalper.growthPercent.ValueDecimal)
                {
                    phaseGrowth = true; //  ставим в phaseGrowth значение тру
                                        //
                    string str = "Значение фазы роста = " + phaseGrowth.ToString() + "\n";
                    Debug.WriteLine(str);
                    minPriceGrowthPhase = marketPrice; // записываем значение цены в minPriceGrowthPhase
                    _calculationGP = false;
                }
                else phaseGrowth = false;
            }
            if (_tail == true) // расчет по свечам с хвостами
            {
                decimal rost = highPriceOutPeriod - lowPriceInPerod;
                decimal rostPers = rost / lowPriceInPerod * 100;
                if (rostPers >= _scalper.growthPercent.ValueDecimal)
                {
                    phaseGrowth = true; //  ставим в phaseGrowth значение тру
                                        //
                    string str = "Значение фазы роста = " + phaseGrowth.ToString() + "\n";
                    Debug.WriteLine(str);
                    minPriceGrowthPhase = marketPrice; // записываем значение цены в minPriceGrowthPhase
                    _calculationGP = false;
                }
                else phaseGrowth = false;
            }
        }

        /// <summary>
        /// событие новый тик(трейд)
        /// </summary>
        private void TabSimple_NewTickEvent(Trade trade)
        {
            marketPrice = trade.Price;
            real_time = trade.Time;
            string name = trade.SecurityNameCode; //===============================================================================
            GetMinPriceGP(name);
            List<Position> positions = TabSimple.PositionsOpenAll;
            if (positions.Count != 0)
            {
                TrelingStopLossAndProfit(TabSimple);
                Profit(TabSimple, positions[0]);
            }

            if (real_time > timeOnPapir)// время возобновления торгов по бумаге пришло
            {
                TradePaper = true;
                timeOnPapir = DateTime.MaxValue;
            }
        }

        /// <summary>
        /// перерасчет минимальной цены зоны
        /// </summary>
        private void GetMinPriceGP(string nameCod)
        {
            if (phaseGrowth == true)
            {
                if (marketPrice < minPriceGrowthPhase)// цена ниже начала зоны роста
                {
                    minPriceGrowthPhase = marketPrice;
                    //-------------------------------------------------------------------------------------------sms-----
                    string str = "Минимум фазы роста " + nameCod + " сменился и = " + minPriceGrowthPhase.ToString() + "\n";
                    Debug.WriteLine(str);
                }
            }
        }

        /// <summary>
        ///отключение фазы роста согласно значению DSR
        /// </summary>
        private void IndicatorDSR(List<Candle> candles, BotTabSimple TabSimple)
        {
            if (candles.Count > _scalper.candleBack.ValueInt + 1)
            {
                Aindicator _dsr = (Aindicator)TabSimple.Indicators[0];
                decimal _trendDSR = _dsr.DataSeries[0].Last; // последние значение индикатора DSR

                if (_trendDSR == 0) // если 0 тренд вниз - фаза роста закончилась
                {
                    phaseGrowth = false; //выключаем
                    _calculationGP = true; //  разрешаем считать заново
                }
            }
        }

        /// <summary>
        /// Взять название класса (квотируемой валюты) подключенной бумаги
        /// </summary>
        private void GetNameSecuretiClass(BotTabSimple TabSimple)
        {
            if (TabSimple.StartProgram == StartProgram.IsTester)
            {
                string str = TabSimple.Connector.SecurityClass;
                _securClass = str;
            }
            if (TabSimple.IsConnected && TabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                string str = TabSimple.Connector.SecurityClass;
                _securClass = str;
            }
        }

        /// <summary>
        /// взять баланс квотируемой валюты
        /// </summary>
        private decimal GetBalans(BotTabSimple TabSimple)
        {
            if (_securClass != null && TabSimple.IsConnected && TabSimple.StartProgram == StartProgram.IsOsTrader)
            {
                decimal balans = TabSimple.Portfolio.GetPositionOnBoard().Find(pos =>
                pos.SecurityNameCode == _securClass).ValueCurrent;
                return balans;
            }
            return 0;
        }

        /// <summary>
        /// входящее событие о том что открылась некая сделка ставим стоп
        /// </summary>
        private void TabSimple_PositionOpeningSuccesEvent(Position position)
        {
            StopLoss(position);
            PriceTrailStop = 0;
            PriceTrelingProfit = 0;
        }

        /// <summary>
        /// закрылась позиция, обнуляю переменные тут
        /// </summary>
        private void TabSimple_PositionClosingSuccesEvent(Position position)
        {
            minPriceGrowthPhase = 0;
            _calculationGP = true; //  разрешаем считать заново
            PriceTrailStop = 0;
            PriceTrelingProfit = 0;
            Risk_1(TabSimple); // проверяем риск 1
        }

        /// <summary>
        /// выставляет стоп лосс
        /// </summary>
        private void StopLoss(Position position)  // выставляем стоп по отступу в обход вызова из метода окончания свечи
        {
            decimal indent = _scalper.stopLoss1.ValueDecimal * marketPrice / 100;  // отступ для стопа
            decimal priceOpenPos = TabSimple.PositionsLast.EntryPrice;  // цена открытия позиции
            decimal priceStopLoss1 = priceOpenPos - indent;
            position.StopOrderIsActiv = true;

            List<Position> positionClose = TabSimple.PositionsCloseAll;
            if (positionClose.Count > 2)
            {
                // 2. Если  в течении 40 минут [2 Профита] было закрыто 2 прибыльных сделки,
                // то в следующих сделках "Стоп-лосс" устанавливается на 5% [Стоп-лосс2].
                // 3. После выключения "Фазы роста", когда будет следующая "Фаза роста",
                // "Стоп-лосс" снова будет ставиться на 3% [Стоп-лосс1].

                decimal profitLast = positionClose[positionClose.Count - 1].ProfitPortfolioPunkt;// профит последней следки
                decimal profitLast2 = positionClose[positionClose.Count - 2].ProfitOperationPunkt;// профит предпоследней сделки
                DateTime timeBackTrade = positionClose[positionClose.Count - 2].TimeClose; // время закрытия предп позы
                DateTime timePeriodBack = timeBackTrade.AddMinutes(_scalper.minutBackStopLoss2.ValueInt); // время учтенного периода

                if (profitLast > 0 && profitLast2 > 0 && real_time < timePeriodBack && phaseGrowth == true)
                {
                    indent = _scalper.stopLoss2.ValueDecimal * marketPrice / 100;  // отступ для стопа
                    priceOpenPos = TabSimple.PositionsLast.EntryPrice;  // цена открытия позиции
                    decimal priceStopLoss2 = priceOpenPos - indent;

                    position.StopOrderPrice = priceStopLoss2;
                    if (position.StopOrderPrice == 0 ||
                        position.StopOrderPrice < priceStopLoss2)
                    {
                        if (TabSimple.StartProgram == StartProgram.IsOsTrader && position.State == PositionStateType.Open)
                        {
                            position.StopOrderIsActiv = false;
                            TabSimple.CloseAtMarket(position, position.OpenVolume);
                        }
                        TabSimple.CloseAtStop(position, priceStopLoss2, priceStopLoss2 - TabSimple.Securiti.PriceStep);
                    }

                    if (position.StopOrderIsActiv == false)
                    {
                        if (position.StopOrderRedLine - TabSimple.Securiti.PriceStep * 10 > TabSimple.PriceBestAsk)
                        {
                            if (TabSimple.StartProgram == StartProgram.IsOsTrader)
                            {
                                if (TabSimple.PriceBestAsk < position.StopOrderPrice && position.State == PositionStateType.Open)
                                {
                                    position.StopOrderIsActiv = false;
                                    TabSimple.CloseAtMarket(position, position.OpenVolume);
                                }
                            }
                            TabSimple.CloseAtLimit(position, TabSimple.PriceBestAsk, position.OpenVolume);
                            return;
                        }
                        position.StopOrderIsActiv = true;
                    }
                }
            }

            if (position.StopOrderPrice == 0 ||
                position.StopOrderPrice < priceStopLoss1)
            {
                // проверяем значение галочки
                if (IsOnDelay == true)
                {
                    int sek = _scalper.timeOutOrder.ValueInt;
                    Thread.Sleep(sek * 1000);                       // задержка
                    decimal lastPrice = TabSimple.PriceBestAsk;
                    if (lastPrice <= position.StopOrderPrice && TabSimple.StartProgram == StartProgram.IsOsTrader)
                    {
                        if (marketPrice < priceStopLoss1 && position.State == PositionStateType.Open)
                        {
                            position.StopOrderIsActiv = false;
                            TabSimple.CloseAtMarket(position, position.OpenVolume);
                            return;
                        }
                    }
                }
                if (TabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    if (marketPrice < priceStopLoss1 && position.State == PositionStateType.Open)
                    {
                        position.StopOrderIsActiv = false;
                        TabSimple.CloseAtMarket(position, position.OpenVolume);
                        return;
                    }
                }
                else TabSimple.CloseAtStop(position, priceStopLoss1, priceStopLoss1 - TabSimple.Securiti.PriceStep);
                position.StopOrderPrice = priceStopLoss1;
                position.StopOrderIsActiv = true;
                string str = "Цена СТОП Ордера = " + position.StopOrderPrice.ToString() + "\n";
                Debug.WriteLine(str);
            }
        }

        /// <summary>
        /// расчет цены  трейлинг стоп лоса
        /// </summary>
        private void CalculateTrelingStopLoss()
        {
            // PriceTrailStop = 0;
            decimal OpenPrice = TabSimple.PositionsLast.EntryPrice;
            decimal lastPrice = TabSimple.PriceBestAsk;
            decimal distans = OpenPrice * _scalper.TrailStopLossLength.ValueDecimal / 100;
            decimal startPrice = OpenPrice - distans;
            if (PriceTrailStop == 0) PriceTrailStop = startPrice;
            if (lastPrice > PriceTrailStop + distans)
            {
                PriceTrailStop = lastPrice - distans;
                string str = "Цена трейлин стопа = " + PriceTrailStop.ToString() + "\n";
                Debug.WriteLine(str);
            }
        }

        // проверить -----------------------!!!!!!!!!!!-----------------------------------------
        /// <summary>
        /// расчет цены тэйк профита
        /// </summary>
        private void CalculateTrelingProfit()
        {
            // PriceTrelingProfit = 0;
            decimal OpenPrice = TabSimple.PositionsLast.EntryPrice;
            decimal lastPrice = TabSimple.PriceBestAsk;
            decimal distans = OpenPrice * _scalper.TrailProfitLength.ValueDecimal / 100;
            decimal startPrice = OpenPrice - distans;
            if (PriceTrelingProfit == 0)
            {
                PriceTrelingProfit = startPrice;
            }
            if (lastPrice > PriceTrelingProfit + distans)
            {
                PriceTrelingProfit = lastPrice - distans;
                string str = "Цена трейлинг профита = " + PriceTrelingProfit.ToString() + "\n";
                Debug.WriteLine(str);
            }
        }

        /// <summary>
        ///  трейлинг стоп лосс и профит
        /// </summary>
        private void TrelingStopLossAndProfit(BotTabSimple TabSimple)
        {
            List<Position> position = TabSimple.PositionsOpenAll;
            if (position.Count == 0)
            {
                return;
            }
            //position[0].StopOrderIsActiv = true;

            if (_isOnTrelingStopLoss == true)
            {
                CalculateTrelingStopLoss();
                decimal lastPrice = TabSimple.PriceBestAsk;
                //decimal PriceTrailStop = CalculateTrelingStopLoss();
                if (lastPrice <= PriceTrailStop && TabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    // проверяем значение галочки
                    if (IsOnDelay == true)
                    {
                        int sek = _scalper.timeOutOrder.ValueInt;
                        Thread.Sleep(sek * 1000);                   // задержка
                        lastPrice = TabSimple.PriceBestAsk;
                        if (lastPrice <= PriceTrailStop)
                        {
                            TabSimple.CloseAtMarket(position[0], position[0].OpenVolume);
                            string stroka = "Закрываем по Трейлинг стоп с задержкой " + PriceTrailStop.ToString() + "\n";
                            Debug.WriteLine(stroka);
                        }
                    }
                    CalculateTrelingStopLoss();
                    if (lastPrice <= PriceTrailStop && position[0].State == PositionStateType.Open)
                    {
                        position[0].StopOrderIsActiv = false;
                        TabSimple.CloseAtMarket(position[0], position[0].OpenVolume);
                        string str = "Закрываем по ТрейлингСтоп =" + PriceTrailStop.ToString() + "\n";
                        Debug.WriteLine(str);
                    }
                }
                decimal trailActiv = lastPrice - lastPrice * _scalper.TrailStopLossLength.ValueDecimal / 100;
                decimal trailOrder = trailActiv - 5 * TabSimple.Securiti.PriceStep;
                TabSimple.CloseAtTrailingStop(position[0], trailActiv, trailOrder);
                //return; для того если включены одновременно и трейлигн стоп и трейлинг профит работает только первый
            }
            if (_isOnTrelingProfit == true)
            {
                // Трейлинг профит
                // При росте цены минимум на 3 % [Тейк профит] от точки входа,
                // а затем снижении на 1 % [Трейлинг профит] от "образовавшегося максимума",
                // позиция закрывается рыночным ордером.
                decimal lastPrice = TabSimple.PriceBestAsk;
                if (TabSimple.StartProgram == StartProgram.IsOsTrader)
                {
                    CalculateTrelingProfit();
                    decimal OpenPrice = TabSimple.PositionsLast.EntryPrice;

                    decimal priceProfit = OpenPrice + OpenPrice * _scalper.ProfitLength.ValueDecimal / 100;
                    // проверить условие логику
                    if (OpenPrice != 0 && lastPrice > priceProfit)
                    {
                        decimal trailActiv = marketPrice - OpenPrice * _scalper.TrailProfitLength.ValueDecimal / 100;
                        decimal trailOrder = trailActiv - 5 * TabSimple.Securiti.PriceStep;
                        TabSimple.CloseAtTrailingStop(position[0], trailActiv, trailOrder);
                        string str = "Пересчитали TрейлингПрофит =" + priceProfit.ToString() + "\n";
                        Debug.WriteLine(str);
                    }
                    CalculateTrelingProfit();
                    if (lastPrice <= PriceTrelingProfit && position[0].State == PositionStateType.Open)
                    {
                        position[0].StopOrderIsActiv = false;
                        TabSimple.CloseAtMarket(position[0], position[0].OpenVolume);
                        string str = "Закрываем по ТрейлингПрофит =" + PriceTrelingProfit.ToString() + "\n";
                        Debug.WriteLine(str);
                        //return;
                    }
                }
                else
                {
                    CalculateTrelingProfit();
                    decimal OpenPrice = TabSimple.PositionsLast.EntryPrice;
                    decimal priceProfit = OpenPrice + OpenPrice * _scalper.ProfitLength.ValueDecimal / 100;
                    if (OpenPrice != 0 && lastPrice > priceProfit)
                    {
                        decimal trailActiv = marketPrice - OpenPrice * _scalper.TrailProfitLength.ValueDecimal / 100;
                        decimal trailOrder = trailActiv - 5 * TabSimple.Securiti.PriceStep;
                        TabSimple.CloseAtTrailingStop(position[0], trailActiv, trailOrder);
                        string str = "Пересчитали TрейлингПрофит =" + priceProfit.ToString() + "\n";
                        Debug.WriteLine(str);
                    }
                }
            }
        }

        /// <summary>
        ///  тэйк профит
        /// </summary>
        private void Profit(BotTabSimple TabSimple, Position position)
        {
            List<Position> pos = TabSimple.PositionsOpenAll;

            if (IsOnTakeProfit == true)
            {
                if (pos.Count == 0)
                {
                    return;
                }
                else
                {
                    decimal lastPrice = TabSimple.PriceBestAsk;
                    decimal openPos = TabSimple.PositionsLast.EntryPrice;  // цена открытия позиции
                    decimal profit = openPos + openPos * _scalper.ProfitLength.ValueDecimal / 100;
                    if (openPos != 0)
                    {
                        TabSimple.CloseAtProfit(position, profit, profit);
                    }
                    if (TabSimple.StartProgram == StartProgram.IsOsTrader)
                    {
                        if (lastPrice > profit && position.State == PositionStateType.Open)
                        {
                            position.StopOrderIsActiv = false;
                            TabSimple.CloseAtMarket(position, position.OpenVolume);
                            string stroka = "Закрытие по Профиту " + profit.ToString() + "\n";
                            Debug.WriteLine(stroka);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Подсчет профита (убытка) по бумаге за время
        /// </summary>
        public void Risk_1(BotTabSimple TabSimple)
        {
            List<Position> positionClose = TabSimple.PositionsCloseAll;
            int n = positionClose.Count;
            decimal profitInSecurInPersent = 0;
            //decimal volPositions = 0;
            //decimal persentRisk_1 = 0;
            if (n <= 0) return;
            for (int i = n; i >= 1; i--)
            {
                DateTime timeLastPose = positionClose[positionClose.Count - 1].TimeClose; // время закрытия последней позы
                DateTime timePeriodPose = timeLastPose.AddHours(-_scalper.timeDrawdown1.ValueInt); // время поз учтенного периода
                DateTime time = positionClose[i - 1].TimeClose; // время позиций из диапазона

                if (time < timePeriodPose)
                {
                    break;
                }
                if (timePeriodPose < time)
                {
                    profitInSecurInPersent += positionClose[i - 1].ProfitOperationPersent; // считаем профит всех сделок в %
                    //volPositions += positionClose[i - 1].MaxVolume;
                    //persentRisk_1 = volPositions * _scalper.percentDrawdown1.ValueDecimal / 100; // расчет допустимого риска в %
                }
                string str = "Профит по бумаге сменился и = " + profitInSecurInPersent.ToString() + "\n";
                Debug.WriteLine(str);
            }
            if (profitInSecurInPersent < -_scalper.percentDrawdown1.ValueDecimal) // проверяем риск
            {
                // риск 1 превышен , блокируем вход по бумаге на заданное время
                DateTime timeOfPapir = real_time;
                OnPapirTimer(timeOfPapir);
            }
        }

        /// <summary>
        /// отключение бумаги от торгов по риску 1
        /// </summary>
        public void OnPapirTimer(DateTime time)
        {
            // отключаем бумагу
            TradePaper = false;
            // рассчитываем время включения
            timeOnPapir = time.AddHours(_scalper.timeOutDrawdown1.ValueInt);
        }

        #endregion Логика ===============================================================================

        #region ================================= Сервис ============================

        /// <summary>
        /// округляет децимал до n чисел после запятой
        /// </summary>
        public decimal Rounding(decimal vol, int n) // округляет децимал до n чисел после запятой
        {
            decimal value = vol;
            int N = n;
            decimal chah = decimal.Round(value, N, MidpointRounding.ToEven);
            return chah;
        }

        /// <summary>
        /// сохранение настроек и свойств
        /// </summary>
        public void Save()
        {
            string nameFile = "SettingsBot_" + TabSimple.TabName + ".txt";

            try
            {
                using (StreamWriter writer = new StreamWriter(nameFile, false))
                {
                    //writer.WriteLine(TradePaper);
                    writer.WriteLine(InBucks);
                    writer.WriteLine(InPercent);
                    writer.WriteLine(Tail);
                    writer.WriteLine(IsOnTakeProfit);
                    writer.WriteLine(IsOnTrelingStopLoss);
                    writer.WriteLine(IsOnTrelingProfit);
                    writer.WriteLine(IsOnDelay);

                    writer.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        /// <summary>
        /// выгрузка  настроек и свойств
        /// </summary>
        public void Load()
        {
            string nameFile = "SettingsBot_" + TabSimple.TabName + ".txt";

            if (!File.Exists(nameFile))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(nameFile))
                {
                    //TradePaper = Convert.ToBoolean(reader.ReadLine());
                    InBucks = Convert.ToDecimal(reader.ReadLine());
                    InPercent = Convert.ToDecimal(reader.ReadLine());
                    Tail = Convert.ToBoolean(reader.ReadLine());
                    IsOnTakeProfit = Convert.ToBoolean(reader.ReadLine());
                    IsOnTrelingStopLoss = Convert.ToBoolean(reader.ReadLine());
                    IsOnTrelingProfit = Convert.ToBoolean(reader.ReadLine());
                    IsOnDelay = Convert.ToBoolean(reader.ReadLine());

                    reader.Close();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

        #endregion ================================= Сервис ============================
    }
}
