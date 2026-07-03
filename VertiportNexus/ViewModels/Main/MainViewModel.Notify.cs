using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Property Changed
    /// [XAML] Binding 갱신을 위한 INotifyPropertyChanged 구현을 관리한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [INotifyPropertyChanged]

        /// <summary>
        /// [Property] 변경 이벤트
        /// 
        /// [XAML] 바인딩 속성 갱신 시 사용한다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// [XAML] 바인딩 갱신 알림
        /// </summary>
        /// <param name="propertyName">
        /// 변경된 [Property] 이름
        /// </param>
        private void OnPropertyChanged(
            [CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

}
