using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace VertiportNexus.Common.Helpers
{
    /// <summary>
    /// [Application] 로그 관리 Helper
    /// 
    /// Serilog 기반으로 프로그램 실행 로그를 파일로 저장한다.
    /// 
    /// 장비 연동 시험 중 발생하는 TCP / UDP / MQ / RTSP / PTZ 제어 이력을
    /// 추후 분석할 수 있도록 날짜별 로그 파일로 관리한다.
    /// </summary>
    internal static class AppLogger
    {
        #region [Fields]

        /// <summary>
        /// [Logger] 초기화 여부
        /// </summary>
        private static bool _isInitialized;

        #endregion

        #region [Initialize Methods]

        /// <summary>
        /// [Logger] 초기화
        /// </summary>
        internal static void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            string logDirectoryPath =
                Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Logs");

            Directory.CreateDirectory(
                logDirectoryPath);

            string logFilePath =
                Path.Combine(
                    logDirectoryPath,
                    "vertiport-.log");

            Log.Logger =
                new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override(
                        "Microsoft",
                        LogEventLevel.Warning)
                    .WriteTo.Debug(
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                    .WriteTo.Async(
                        log => log.File(
                            logFilePath,
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 30,
                            shared: true,
                            outputTemplate:
                            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
                    .CreateLogger();

            _isInitialized =
                true;

            Log.Information(
                "[SYSTEM] Logger Initialize Complete");
        }

        /// <summary>
        /// [Logger] 종료
        /// </summary>
        internal static void Shutdown()
        {
            if (!_isInitialized)
            {
                return;
            }

            Log.Information(
                "[SYSTEM] Logger Shutdown");

            Log.CloseAndFlush();

            _isInitialized =
                false;
        }
        #endregion
    }

}
