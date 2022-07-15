using OsEngine.Robots.MyBot.InsightScreener;
using System.Collections.ObjectModel;
using OsEngine.OsTrader.Panels.Tab;
using System;

namespace OsEngine.Robots.MyBot.InsightScreener
{
    public class SettingsViewModel : ViewModBase
    {
        public ObservableCollection<InsiRobot> Robots { get; set; } = new ObservableCollection<InsiRobot>();

        private InsightScreener _bot = null;

        public SettingsViewModel(InsightScreener bot)
        {
            _bot = bot;
            Init();
        }

        // для того что бы окно обновлялось всей пачкой роботов
        public void Init()
        {
            if (_bot == null) return;

            ObservableCollection<InsiRobot> robots = new ObservableCollection<InsiRobot>();

            foreach (BotTabSimple tab in _bot._tabScreen.Tabs)
            {
                foreach (InsiRobot robot in _bot.Robots)
                {
                    if (tab.TabName == robot.TabSimple.TabName)
                    {
                        robots.Add(robot);
                        break;
                    }
                }
            }

            Robots = robots;
            OnPropertyChanged(nameof(Robots));
        }

    }
}