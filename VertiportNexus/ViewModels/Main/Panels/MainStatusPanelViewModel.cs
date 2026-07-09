namespace VertiportNexus.ViewModels.Main.Panels
{
    /// <summary>
    /// [Main] 화면 상태 표시 문자열 상태
    /// </summary>
    internal sealed class MainStatusPanelViewModel
    {
        #region [Fields]

        /// <summary>
        /// [RabbitMQ] 상태 표시 문자열
        /// </summary>
        private string _mqStatusText =
            "RabbitMQ Ready";

        /// <summary>
        /// 마지막 [RabbitMQ] 수신 메시지
        /// </summary>
        private string _lastMqMessageText =
            string.Empty;

        /// <summary>
        /// Main 상태 표시 문자열
        /// </summary>
        private string _mainStatusText;

        /// <summary>
        /// 운용 모드 표시 문자열
        /// </summary>
        private string _operationModeText;

        /// <summary>
        /// [PTZ] 제어 모드 표시 문자열
        /// </summary>
        private string _ptzControlModeText;

        #endregion

        #region [Properties]

        /// <summary>
        /// [RabbitMQ] 상태 표시 문자열
        /// </summary>
        internal string MqStatusText =>
            _mqStatusText;

        /// <summary>
        /// 마지막 [RabbitMQ] 수신 메시지
        /// </summary>
        internal string LastMqMessageText =>
            _lastMqMessageText;

        /// <summary>
        /// Main 상태 표시 문자열
        /// </summary>
        internal string MainStatusText =>
            _mainStatusText;

        /// <summary>
        /// 운용 모드 표시 문자열
        /// </summary>
        internal string OperationModeText =>
            _operationModeText;

        /// <summary>
        /// [PTZ] 제어 모드 표시 문자열
        /// </summary>
        internal string PtzControlModeText =>
            _ptzControlModeText;

        #endregion

        #region [Set Methods]

        /// <summary>
        /// [RabbitMQ] 상태 표시 문자열 저장
        /// </summary>
        internal bool SetMqStatusText(
            string value)
        {
            return SetValue(
                ref _mqStatusText,
                value);
        }

        /// <summary>
        /// 마지막 [RabbitMQ] 수신 메시지 저장
        /// </summary>
        internal bool SetLastMqMessageText(
            string value)
        {
            return SetValue(
                ref _lastMqMessageText,
                value);
        }

        /// <summary>
        /// Main 상태 표시 문자열 저장
        /// </summary>
        internal bool SetMainStatusText(
            string value)
        {
            return SetValue(
                ref _mainStatusText,
                value);
        }

        /// <summary>
        /// 운용 모드 표시 문자열 저장
        /// </summary>
        internal bool SetOperationModeText(
            string value)
        {
            return SetValue(
                ref _operationModeText,
                value);
        }

        /// <summary>
        /// [PTZ] 제어 모드 표시 문자열 저장
        /// </summary>
        internal bool SetPtzControlModeText(
            string value)
        {
            return SetValue(
                ref _ptzControlModeText,
                value);
        }

        #endregion

        #region [Utility Methods]

        /// <summary>
        /// 값 변경 처리
        /// </summary>
        private static bool SetValue<T>(
            ref T field,
            T value)
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
