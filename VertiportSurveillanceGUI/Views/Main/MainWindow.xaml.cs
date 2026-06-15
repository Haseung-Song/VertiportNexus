using System.Windows;
using VertiportSurveillanceGUI.ViewModels.Main;

namespace VertiportSurveillanceGUI
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// [Main] 화면 -> [ViewModel] : [XAML]의 [Binding] 연결
        /// </summary>
        private readonly MainViewModel vm = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = vm;
        }

    }

}
