using System;
using System.Globalization;
using System.Windows;


namespace OsEngine.Robots.MyBot.Insight
{
    /// <summary>
    /// Interaction logic for StartTrelUi.xaml
    /// </summary>
    public partial class InsightUi : Window
    {
        public Insight _strategy; // поле класса робота Insight

        public InsightUi(Insight strategy)
        {
            InitializeComponent();
            _strategy = strategy;
            DataContext = strategy;

            CultureInfo culture = new CultureInfo("ru-RU");

            TextBoxDistLongInit.Text = _strategy.DistLongInit.ToString(culture);
            TextBoxLongAdj.Text = _strategy.LongAdj.ToString(culture);
            TextBoxDistShortInit.Text = _strategy.DistShortInit.ToString(culture);
            TextBoxShortAdj.Text = _strategy.ShortAdj.ToString(culture);
            //TextBox_Big.Text = _strategy.PriceLargeEntry.ToString(culture);


        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            _strategy.DistLongInit = Convert.ToDecimal(TextBoxDistLongInit.Text);
            _strategy.LongAdj = Convert.ToDecimal(TextBoxLongAdj.Text);
            _strategy.DistShortInit = Convert.ToDecimal(TextBoxDistShortInit.Text);
            _strategy.ShortAdj = Convert.ToDecimal(TextBoxShortAdj.Text);
            //_strategy.PriceLargeEntry = Convert.ToDecimal(TextBox_Big.Text);

            _strategy.Save();
            Close();
        }

        private void Button_Click_сlose(object sender, RoutedEventArgs e)
        {
            _strategy.ClosePosicion();
        }

    }
}