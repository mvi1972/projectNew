using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.Robots.MyBot.InsightScreener
{
    /// <summary>
    /// Логика взаимодействия для InsiSettings.xaml
    /// </summary>
    public partial class InsiSettings : Window
    {
        public InsiSettings(InsightScreener bot)
        {
            InitializeComponent();

            VM = new SettingsViewModel(bot);
            DataContext = VM;
        }

        private SettingsViewModel VM;
    }
}
