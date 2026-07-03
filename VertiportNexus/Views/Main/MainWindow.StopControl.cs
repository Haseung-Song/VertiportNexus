using System.Windows.Input;

namespace VertiportNexus.Views.Main
{
    /// <summary>
    /// [Main] 화면 - Stop Mouse Event
    /// 
    /// 화면 버튼 조작 중 MouseUp / MouseLeave 상황에서
    /// 연속 이동 정지 요청을 [MainViewModel]로 전달한다.
    /// </summary>
    public partial class MainWindow
    {
        #region [Stop Mouse Event Methods]

        /// <summary>
        /// [MouseUp] 공통 정지 처리
        /// 
        /// 화면 버튼을 통해 시작된 연속 이동을 정지한다.
        /// </summary>
        private void MoveStop_MouseUp(
            object sender,
            MouseEventArgs e)
        {
            _viewModel
                .StopContinuousMove();
        }

        /// <summary>
        /// [이동 정지] MouseLeave 처리
        /// 
        /// 연속 이동 버튼을 누른 상태에서
        /// 마우스가 버튼 영역 밖으로 벗어난 경우에만
        /// 이동 정지 명령을 실행한다.
        /// 
        /// 단순 Hover / MouseLeave 상황에서는
        /// STOP 명령이 실행되지 않도록 한다.
        /// </summary>
        private void MoveStop_MouseLeave(
            object sender,
            MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            _viewModel
                .StopContinuousMove();
        }
        #endregion
    }

}
