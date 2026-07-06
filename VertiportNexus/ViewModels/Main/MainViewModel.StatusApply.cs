using System;
using VertiportNexus.Models.ADS1000;

namespace VertiportNexus.ViewModels.Main
{
    /// <summary>
    /// [Main] 화면 [ViewModel] - Status Apply
    /// ADS1000 상태 Packet에서 파싱된 Pan / Tilt / Zoom / Focus 값을 화면 상태에 반영한다.
    /// </summary>
    public partial class MainViewModel
    {
        #region [Status Apply Methods]

        /// <summary>
        /// [ADS1000] 파싱 상태값 화면 반영
        /// 
        /// 수신 [Packet]에서 추출된
        /// [Pan] / [Tilt] / [Zoom] / [Focus] 값을
        /// 화면 표시용 속성과 [CameraStateProvider]에 반영한다.
        /// </summary>
        /// <param name="parsedPacket">
        /// [ADS1000] 파싱 [Packet]
        /// </param>
        private void ApplyParsedStatusValue(
            Ads1000ParsedPacket parsedPacket)
        {
            double? updatedPan =
                null;

            double? updatedTilt =
                null;

            double? updatedZoom =
                null;

            double? updatedFocus =
                null;

            if (parsedPacket.HasPanValue)
            {
                // [Pan] 누적 상태값 갱신
                //
                // 화면 표시용 [Pan] 값은 [0 ~ 360] 범위로 정규화하지만,
                // 장비 제어용 [Pan] 값은 한 바퀴 이상 회전한 위치를 유지해야 하므로
                // 상태 Packet 수신 시 누적 위치값을 별도로 갱신한다.
                UpdatePanAccumulatedStatus(
                    parsedPacket.PanValue);

                // [Pan] 상태값 갱신
                //
                // Pan은 최종 ICD 기준 [0 ~ 360] 범위로 표시한다.
                // 장비 Encoder 오차로 인해 [0] / [360] 또는 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentPan =
                    NormalizePanStatus(
                        parsedPacket.PanValue);

#if DEBUG

                // [Pan] Raw / Display 상태값 로그
                //
                // 장비에서 수신한 원본 Pan 값과
                // 화면 표시 기준으로 보정된 Pan 값을 비교하여,
                // 장비 상태값과 UI 표시값의 보정 차이를 확인한다.
                //Console.WriteLine(
                //    "[ADS1000][PAN] Raw Pan : "
                //    + parsedPacket.PanValue.ToString("F2"));

                //Console.WriteLine(
                //    "[ADS1000][PAN] Display Pan : "
                //    + CurrentPan.ToString("F2"));

#endif

                updatedPan =
                    CurrentPan;
            }

            if (parsedPacket.HasTiltValue)
            {
                // [Tilt] 상태값 갱신
                //
                // Tilt는 장비 물리 제한 기준 [-90 ~ 90] 범위로 표시한다.
                // 장비 Encoder 오차로 인해 [0] 근처 미세 오차가 발생하면
                // 화면 표시 및 상태 응답 기준에서는 [0]으로 보정한다.
                CurrentTilt =
                    NormalizeTiltStatus(
                        parsedPacket.TiltValue);

#if DEBUG

                // [Tilt] Raw / Display 상태값 로그
                //
                // 장비에서 수신한 원본 Tilt 값과
                // 화면 표시 기준으로 보정된 Tilt 값을 비교하여,
                // 장비 Limit 문제인지 표시 Clamp 문제인지 확인한다.
                //Console.WriteLine(
                //    "[ADS1000][TILT] Raw Tilt : "
                //    + parsedPacket.TiltValue.ToString("F2"));

                //Console.WriteLine(
                //    "[ADS1000][TILT] Display Tilt : "
                //    + CurrentTilt.ToString("F2"));

#endif
                updatedTilt =
                    CurrentTilt;
            }

            if (parsedPacket.HasZoomValue)
            {
                // [Zoom] 상태값 갱신
                //
                // Zoom Position은 장비 제어 기준 [0 ~ 1000] 범위로 표시한다.
                // 화면의 현재 상태 표시에서는 Position 값과 함께
                // 실제 배율 기준 [x1.0 ~ x66.0] 값을 소수점 첫째 자리까지 표시한다.
                //
                // 장비 응답값이 범위를 벗어나거나 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentZoom =
                    NormalizeRangePosition(
                        parsedPacket.ZoomValue,
                        0,
                        1000);

                CurrentZoomRatio =
                    ConvertZoomPositionToRatio(
                        CurrentZoom);

                updatedZoom =
                    CurrentZoom;
            }

            if (parsedPacket.HasFocusValue)
            {
                // [Focus] 상태값 갱신
                //
                // Focus Position은 장비 제어 기준 [0 ~ 1000] 범위로 표시한다.
                // 장비 응답값이 범위를 벗어나거나 정수 위치 근처
                // 미세 오차가 발생하면 화면 표시 및 상태 응답 기준에서는 보정한다.
                CurrentFocus =
                    NormalizeRangePosition(
                        parsedPacket.FocusValue,
                        0,
                        1000);

                updatedFocus =
                    CurrentFocus;
            }

            // [Camera] 상태 저장소 갱신
            //
            // [CSE] 상태 조회 응답에서 사용할 수 있도록
            // 수신 [Packet]에 포함된 상태값만 저장한다.
            _cameraStateProvider.UpdateState(
                updatedPan,
                updatedTilt,
                updatedZoom,
                updatedFocus);
        }
        #endregion
    }

}
