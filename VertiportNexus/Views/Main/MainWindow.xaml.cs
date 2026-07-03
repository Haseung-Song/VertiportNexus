using System.Windows;
using VertiportNexus.ViewModels.Main;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면
    /// 
    /// [MainWindow.xaml]에 대한 상호 작용 논리를 처리한다.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region [Fields]

        /// <summary>
        /// [Main] 화면 [ViewModel]
        /// 
        /// [XAML]의 [Binding] 대상 객체이다.
        /// </summary>
        private readonly MainViewModel _viewModel =
            new MainViewModel();

        #endregion

        #region [Constructor]

        /// <summary>
        /// [MainWindow] 생성자
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            DataContext = _viewModel;
        }
        #endregion
    }

}
