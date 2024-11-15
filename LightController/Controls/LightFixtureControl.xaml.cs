using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LightController.Controls
{
    /// <summary>
    /// Interaction logic for LightFixtureControl.xaml
    /// </summary>
    public partial class LightFixtureControl : UserControl//, INotifyPropertyChanged
    {
        public string FixtureName
        {
            get { return (string)GetValue(FixtureNameProperty); }
            set { SetValue(FixtureNameProperty, value); }
        }
        public static readonly DependencyProperty FixtureNameProperty =
            DependencyProperty.Register("FixtureName", typeof(string), typeof(LightFixtureControl), new PropertyMetadata(""));



        public double Percent
        {
            get { return (double)GetValue(PercentProperty); }
            set { SetValue(PercentProperty, value); }
        }
        public static readonly DependencyProperty PercentProperty =
            DependencyProperty.Register("Percent", typeof(double), typeof(LightFixtureControl), new PropertyMetadata(0.0));



        public SolidColorBrush Color
        {
            get { return (SolidColorBrush)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(SolidColorBrush), typeof(LightFixtureControl), new PropertyMetadata(new SolidColorBrush(Colors.Black)));


        public LightFixtureControl()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
