using System.Windows.Input;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Keyboard PTZ Control
    /// 키보드 방향키 입력을 Pan / Tilt 연속 이동 및 대각선 제어로 변환한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Keyboard Control Methods]

        /// <summary>
        /// [Keyboard] 방향키 입력 처리
        /// 
        /// 운용 제어 화면에서 방향키 입력을
        /// Pan / Tilt 연속 이동 명령으로 변환한다.
        /// 
        /// 두 방향키가 동시에 눌린 경우
        /// Pan / Tilt 축을 각각 제어하여 대각선 이동으로 처리한다.
        /// </summary>
        /// <param name="key">
        /// 입력된 키
        /// </param>
        public void HandlePanTiltKeyDown(
            Key key)
        {
            switch (key)
            {
                case Key.Left:
                    _isKeyboardPanLeftPressed =
                        true;
                    break;

                case Key.Right:
                    _isKeyboardPanRightPressed =
                        true;
                    break;

                case Key.Up:
                    _isKeyboardTiltUpPressed =
                        true;
                    break;

                case Key.Down:
                    _isKeyboardTiltDownPressed =
                        true;
                    break;

                default:
                    return;
            }
            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// [Keyboard] 방향키 해제 처리
        /// 
        /// 해제된 방향키에 해당하는 축만 정지하고,
        /// 다른 방향키가 계속 눌려 있는 경우 해당 축 이동은 유지한다.
        /// </summary>
        /// <param name="key">
        /// 해제된 키
        /// </param>
        public void HandlePanTiltKeyUp(
            Key key)
        {
            switch (key)
            {
                case Key.Left:
                    _isKeyboardPanLeftPressed =
                        false;
                    break;

                case Key.Right:
                    _isKeyboardPanRightPressed =
                        false;
                    break;

                case Key.Up:
                    _isKeyboardTiltUpPressed =
                        false;
                    break;

                case Key.Down:
                    _isKeyboardTiltDownPressed =
                        false;
                    break;

                default:
                    return;
            }
            UpdateKeyboardPanTiltMove();
        }

        /// <summary>
        /// [Keyboard] Pan / Tilt 이동 상태 갱신
        /// 
        /// 현재 눌려 있는 방향키 상태를 기준으로
        /// Pan 축과 Tilt 축의 연속 이동 / 정지 명령을 각각 처리한다.
        /// </summary>
        private void UpdateKeyboardPanTiltMove()
        {
            if (_mcbConnectionState != ConnectionState.Connected ||
                _isHomePositionMoving)
            {
                return;
            }

            // [Pan] 이동 방향 결정
            //
            // Left / Right가 동시에 눌린 경우에는
            // 상쇄 입력으로 판단하여 Pan 축을 정지한다.
            if (_isKeyboardPanLeftPressed &&
                !_isKeyboardPanRightPressed)
            {
                StartPanLeftMove();
            }
            else if (_isKeyboardPanRightPressed &&
                     !_isKeyboardPanLeftPressed)
            {
                StartPanRightMove();
            }
            else
            {
                StopPanMove();
            }

            // [Tilt] 이동 방향 결정
            //
            // Up / Down이 동시에 눌린 경우에는
            // 상쇄 입력으로 판단하여 Tilt 축을 정지한다.
            if (_isKeyboardTiltUpPressed &&
                !_isKeyboardTiltDownPressed)
            {
                StartTiltUpMove();
            }
            else if (_isKeyboardTiltDownPressed &&
                     !_isKeyboardTiltUpPressed)
            {
                StartTiltDownMove();
            }
            else
            {
                StopTiltMove();
            }

        }
        #endregion
    }

}
