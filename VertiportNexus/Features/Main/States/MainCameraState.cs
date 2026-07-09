using System;
using VertiportNexus.Models.ADS1000;
using VertiportNexus.Services.Command;

namespace VertiportNexus.ViewModels.Main.States
{
    /// <summary>
    /// [Main] Camera 위치 / 입력 / UI Zero 상태
    ///
    /// MainViewModel에서 사용하던 Camera 관련 상태값과
    /// Pan / Tilt UI 기준값 변환 처리를 담당한다.
    /// </summary>
    internal sealed class MainCameraState
    {
        #region [Constants]

        /// <summary>
        /// 실수형 값 비교 허용 오차
        /// </summary>
        private const double VALUE_EPSILON =
            0.001;

        #endregion

        #region [Current Camera State Fields]

        /// <summary>
        /// 현재 [Pan] 위치값
        /// </summary>
        private double _currentPan;

        /// <summary>
        /// 장비 기준 [Pan] 누적 위치값
        /// </summary>
        private double _currentPanAccumulated;

        /// <summary>
        /// [Pan] 누적 위치값 수신 여부
        /// </summary>
        private bool _hasPanAccumulatedStatus;

        /// <summary>
        /// 마지막 [Pan] 표시용 위치값
        /// </summary>
        private double _lastPanDisplayStatus;

        /// <summary>
        /// 현재 [Tilt] 위치값
        /// </summary>
        private double _currentTilt;

        /// <summary>
        /// 현재 [Zoom] 위치값
        /// </summary>
        private double _currentZoom;

        /// <summary>
        /// 현재 [Zoom] 배율값
        /// </summary>
        private double _currentZoomRatio =
            1.0;

        /// <summary>
        /// 현재 [Focus] 위치값
        /// </summary>
        private double _currentFocus;

        #endregion

        #region [Camera Control Input Fields]

        /// <summary>
        /// [Pan] 절대 위치 입력값
        /// </summary>
        private double? _panAbsoluteValue;

        /// <summary>
        /// [Tilt] 절대 위치 입력값
        /// </summary>
        private double? _tiltAbsoluteValue;

        /// <summary>
        /// [Pan] 상대 이동 입력값
        /// </summary>
        private double? _panRelativeValue;

        /// <summary>
        /// [Tilt] 상대 이동 입력값
        /// </summary>
        private double? _tiltRelativeValue;

        /// <summary>
        /// [Zoom] 위치 입력값
        /// </summary>
        private int? _zoomPositionValue;

        /// <summary>
        /// [Zoom] 배율 입력값
        /// </summary>
        private double? _zoomRatioValue;

        /// <summary>
        /// [Focus] 위치 입력값
        /// </summary>
        private int? _focusPositionValue;

        #endregion

        #region [Current Camera State Properties]

        /// <summary>
        /// 현재 [Pan] 위치값
        /// </summary>
        internal double CurrentPan =>
            _currentPan;

        /// <summary>
        /// 현재 [Tilt] 위치값
        /// </summary>
        internal double CurrentTilt =>
            _currentTilt;

        /// <summary>
        /// 현재 [Zoom] 위치값
        /// </summary>
        internal double CurrentZoom =>
            _currentZoom;

        /// <summary>
        /// 현재 [Zoom] 배율값
        /// </summary>
        internal double CurrentZoomRatio =>
            _currentZoomRatio;

        /// <summary>
        /// 현재 [Focus] 위치값
        /// </summary>
        internal double CurrentFocus =>
            _currentFocus;

        /// <summary>
        /// [Pan] 선회 모드
        /// </summary>
        internal Ads1000PanTurnMode PanTurnMode { get; set; } =
            Ads1000PanTurnMode.Short;

        #endregion

        #region [Camera Control Input Properties]

        /// <summary>
        /// [Pan] 절대 위치 입력값
        /// </summary>
        internal double? PanAbsoluteValue =>
            _panAbsoluteValue;

        /// <summary>
        /// [Tilt] 절대 위치 입력값
        /// </summary>
        internal double? TiltAbsoluteValue =>
            _tiltAbsoluteValue;

        /// <summary>
        /// [Pan] 상대 이동 입력값
        /// </summary>
        internal double? PanRelativeValue =>
            _panRelativeValue;

        /// <summary>
        /// [Tilt] 상대 이동 입력값
        /// </summary>
        internal double? TiltRelativeValue =>
            _tiltRelativeValue;

        /// <summary>
        /// [Zoom] 위치 입력값
        /// </summary>
        internal int? ZoomPositionValue =>
            _zoomPositionValue;

        /// <summary>
        /// [Zoom] 배율 입력값
        /// </summary>
        internal double? ZoomRatioValue =>
            _zoomRatioValue;

        /// <summary>
        /// [Focus] 위치 입력값
        /// </summary>
        internal int? FocusPositionValue =>
            _focusPositionValue;

        #endregion

        #region [UI Zero Properties]

        /// <summary>
        /// [Pan] UI Zero 보정값
        /// </summary>
        internal double PanUiZeroOffset { get; set; }

        /// <summary>
        /// [Tilt] UI Zero 보정값
        /// </summary>
        internal double TiltUiZeroOffset { get; set; }

        #endregion

        #region [Current Camera State Set Methods]

        /// <summary>
        /// 현재 [Pan] 위치값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Pan] 위치값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetCurrentPan(
            double value)
        {
            if (Math.Abs(_currentPan - value) <= VALUE_EPSILON)
            {
                return false;
            }

            _currentPan =
                value;

            return true;
        }

        /// <summary>
        /// 현재 [Tilt] 위치값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Tilt] 위치값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetCurrentTilt(
            double value)
        {
            if (Math.Abs(_currentTilt - value) <= VALUE_EPSILON)
            {
                return false;
            }

            _currentTilt =
                value;

            return true;
        }

        /// <summary>
        /// 현재 [Zoom] 위치값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Zoom] 위치값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetCurrentZoom(
            double value)
        {
            if (Math.Abs(_currentZoom - value) <= VALUE_EPSILON)
            {
                return false;
            }

            _currentZoom =
                value;

            return true;
        }

        /// <summary>
        /// 현재 [Zoom] 배율값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Zoom] 배율값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetCurrentZoomRatio(
            double value)
        {
            if (Math.Abs(_currentZoomRatio - value) <= VALUE_EPSILON)
            {
                return false;
            }

            _currentZoomRatio =
                value;

            return true;
        }

        /// <summary>
        /// 현재 [Focus] 위치값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Focus] 위치값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetCurrentFocus(
            double value)
        {
            if (Math.Abs(_currentFocus - value) <= VALUE_EPSILON)
            {
                return false;
            }

            _currentFocus =
                value;

            return true;
        }

        #endregion

        #region [Camera Control Input Set Methods]

        /// <summary>
        /// [Pan] 절대 위치 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Pan] 절대 위치 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetPanAbsoluteValue(
            double? value)
        {
            double? roundedValue =
                RoundNullableAngle(
                    value);

            return SetNullableValue(
                ref _panAbsoluteValue,
                roundedValue);
        }

        /// <summary>
        /// [Tilt] 절대 위치 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Tilt] 절대 위치 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetTiltAbsoluteValue(
            double? value)
        {
            double? roundedValue =
                RoundNullableAngle(
                    value);

            return SetNullableValue(
                ref _tiltAbsoluteValue,
                roundedValue);
        }

        /// <summary>
        /// [Pan] 상대 이동 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Pan] 상대 이동 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetPanRelativeValue(
            double? value)
        {
            double? roundedValue =
                RoundNullableAngle(
                    value);

            return SetNullableValue(
                ref _panRelativeValue,
                roundedValue);
        }

        /// <summary>
        /// [Tilt] 상대 이동 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Tilt] 상대 이동 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetTiltRelativeValue(
            double? value)
        {
            double? roundedValue =
                RoundNullableAngle(
                    value);

            return SetNullableValue(
                ref _tiltRelativeValue,
                roundedValue);
        }

        /// <summary>
        /// [Zoom] 위치 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Zoom] 위치 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetZoomPositionValue(
            int? value)
        {
            return SetNullableValue(
                ref _zoomPositionValue,
                value);
        }

        /// <summary>
        /// [Zoom] 배율 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Zoom] 배율 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetZoomRatioValue(
            double? value)
        {
            return SetNullableValue(
                ref _zoomRatioValue,
                value);
        }

        /// <summary>
        /// [Focus] 위치 입력값 저장
        /// </summary>
        /// <param name="value">
        /// 변경할 [Focus] 위치 입력값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        internal bool SetFocusPositionValue(
            int? value)
        {
            return SetNullableValue(
                ref _focusPositionValue,
                value);
        }

        #endregion

        #region [Pan Accumulated State Methods]

        /// <summary>
        /// [Pan] 누적 상태값 갱신
        ///
        /// 장비에서 수신한 원본 [Pan] 각도값을 기준으로
        /// 제어용 누적 위치값과 표시용 위치값을 함께 갱신한다.
        /// </summary>
        /// <param name="panStatus">
        /// 장비에서 수신한 [Pan] 원본 각도값
        /// </param>
        internal void UpdatePanAccumulatedStatus(
            double panStatus)
        {
            _currentPanAccumulated =
                panStatus;

            _lastPanDisplayStatus =
                CameraCommandService.NormalizePanStatus(
                    panStatus);

            _hasPanAccumulatedStatus =
                true;
        }

        /// <summary>
        /// [Pan] 제어 기준 위치값 조회
        /// </summary>
        /// <param name="fallbackPan">
        /// 누적 상태값이 없을 때 사용할 현재 [Pan] 값
        /// </param>
        /// <returns>
        /// [Pan] 제어 기준 위치값
        /// </returns>
        internal double GetCurrentPanCommandAngle(
            double fallbackPan)
        {
            return _hasPanAccumulatedStatus
                ? _currentPanAccumulated
                : fallbackPan;
        }

        /// <summary>
        /// [Pan] 누적 상태값 초기화
        /// </summary>
        internal void ResetPanAccumulatedStatus()
        {
            _currentPanAccumulated =
                0.0;

            _lastPanDisplayStatus =
                0.0;

            _hasPanAccumulatedStatus =
                true;
        }

        #endregion

        #region [UI Zero Convert Methods]

        /// <summary>
        /// UI Zero 기준 [Pan] 현재 위치 계산
        /// </summary>
        /// <returns>
        /// UI Zero 기준 [Pan] 위치값
        /// </returns>
        internal double GetUiCurrentPan()
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.NormalizePanStatus(
                    CurrentPan
                    - PanUiZeroOffset));
        }

        /// <summary>
        /// UI Zero 기준 [Tilt] 현재 위치 계산
        /// </summary>
        /// <returns>
        /// UI Zero 기준 [Tilt] 위치값
        /// </returns>
        internal double GetUiCurrentTilt()
        {
            return RoundAngleToProtocolScale(
                CurrentTilt
                - TiltUiZeroOffset);
        }

        /// <summary>
        /// UI 기준 [Pan] Target을 장비 기준 Target으로 변환
        /// </summary>
        /// <param name="uiTargetPan">
        /// UI 기준 [Pan] Target
        /// </param>
        /// <returns>
        /// 장비 기준 [Pan] Target
        /// </returns>
        internal double ConvertUiPanTargetToDeviceTarget(
            double uiTargetPan)
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.NormalizePanStatus(
                    uiTargetPan
                    + PanUiZeroOffset));
        }

        /// <summary>
        /// UI 기준 [Tilt] Target을 장비 기준 Target으로 변환
        /// </summary>
        /// <param name="uiTargetTilt">
        /// UI 기준 [Tilt] Target
        /// </param>
        /// <returns>
        /// 장비 기준 [Tilt] Target
        /// </returns>
        internal double ConvertUiTiltTargetToDeviceTarget(
            double uiTargetTilt)
        {
            return RoundAngleToProtocolScale(
                CameraCommandService.Clamp(
                    uiTargetTilt
                    + TiltUiZeroOffset,
                    -90,
                    90));
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 각도값을 프로토콜 기준 소수점 둘째 자리로 보정
        /// </summary>
        /// <param name="angle">
        /// 보정할 각도값
        /// </param>
        /// <returns>
        /// 소수점 둘째 자리로 반올림된 각도값
        /// </returns>
        private static double RoundAngleToProtocolScale(
            double angle)
        {
            return Math.Round(
                angle,
                2,
                MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Nullable 각도값을 프로토콜 기준 소수점 둘째 자리로 보정
        /// </summary>
        /// <param name="value">
        /// 보정할 Nullable 각도값
        /// </param>
        /// <returns>
        /// 보정된 Nullable 각도값
        /// </returns>
        private static double? RoundNullableAngle(
            double? value)
        {
            return value.HasValue
                ? RoundAngleToProtocolScale(
                    value.Value)
                : value;
        }

        /// <summary>
        /// Nullable 값 변경 처리
        /// </summary>
        /// <typeparam name="T">
        /// Nullable 값 형식
        /// </typeparam>
        /// <param name="field">
        /// 내부 저장 필드
        /// </param>
        /// <param name="value">
        /// 변경 요청 값
        /// </param>
        /// <returns>
        /// 값 변경 여부
        /// </returns>
        private static bool SetNullableValue<T>(
            ref T? field,
            T? value)
            where T : struct
        {
            if (Equals(
                    field,
                    value))
            {
                return false;
            }

            field =
                value;

            return true;
        }
        #endregion
    }

}
