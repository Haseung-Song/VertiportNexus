using System;
using Serilog;
using Serilog.Events;

namespace VertiportNexus.Common
{
    /// <summary>
    /// [Console] / [Serilog] 로그 출력 공통 Helper
    /// 
    /// UI 버튼 방어 코드처럼 현장 확인이 필요한 메시지는
    /// Console 창과 Serilog 파일 로그에 동시에 남기고,
    /// 일반 내부 처리 로그는 메시지 성격에 맞는 Log Level로 분류한다.
    /// </summary>
    public static class ConsoleLogHelper
    {
        /// <summary>
        /// [Console] 로그 출력 동기화 객체
        ///
        /// 여러 비동기 수신 Task에서 동시에 Console 로그를 출력할 경우,
        /// 로그 내용이 서로 섞이지 않도록 출력 단위를 보호한다.
        /// </summary>
        private static readonly object _consoleLock =
            new object();

        /// <summary>
        /// [Console] 로그 구분선
        /// </summary>
        public const string LogLine =
            "=======================================================================================================================";

        /// <summary>
        /// [Console] 로그 구분선 출력
        /// </summary>
        public static void PrintLine()
        {
            lock (_consoleLock)
            {
                Console.WriteLine(LogLine);
            }

            WriteSerilog(
                LogEventLevel.Debug,
                LogLine,
                null);
        }

        /// <summary>
        /// [Console] / [Serilog] 로그 메시지 출력
        /// 
        /// 기존 Console.WriteLine 대체용 기본 메서드이며,
        /// 메시지 내용에 따라 Warning / Error / Debug / Information을 자동 분류한다.
        /// </summary>
        public static void WriteLine(
            string message)
        {
            WriteLine(
                message,
                (LogEventLevel?)null);
        }

        /// <summary>
        /// [Console] / [Serilog] 빈 줄 출력
        /// </summary>
        public static void WriteLine()
        {
            lock (_consoleLock)
            {
                Console.WriteLine();
            }

        }

        /// <summary>
        /// [Console] / [Serilog] 로그 메시지 출력
        /// 
        /// 기존 Console.WriteLine(format, args) 대체용 기본 메서드이다.
        /// </summary>
        public static void WriteLine(
            string format,
            params object[] args)
        {
            string message =
                args == null || args.Length == 0
                    ? format
                    : string.Format(
                        format,
                        args);

            WriteLine(
                message,
                (LogEventLevel?)null);
        }

        /// <summary>
        /// [Console] / [Serilog] Debug 로그 출력
        /// </summary>
        public static void Debug(
            string message)
        {
            WriteLine(
                message,
                LogEventLevel.Debug);
        }

        /// <summary>
        /// [Console] / [Serilog] Information 로그 출력
        /// </summary>
        public static void Information(
            string message)
        {
            WriteLine(
                message,
                LogEventLevel.Information);
        }

        /// <summary>
        /// [Console] / [Serilog] Warning 로그 출력
        /// </summary>
        public static void Warning(
            string message)
        {
            WriteLine(
                message,
                LogEventLevel.Warning);
        }

        /// <summary>
        /// [Console] / [Serilog] Error 로그 출력
        /// </summary>
        public static void Error(
            string message)
        {
            WriteLine(
                message,
                LogEventLevel.Error);
        }

        /// <summary>
        /// [Console] 로그 메시지 출력
        /// </summary>
        public static void PrintLine(
            string message)
        {
            WriteLine(
                message);
        }

        /// <summary>
        /// [Console] 로그 블록 출력
        ///
        /// 구분선 / 제목 / 내용 출력이 서로 섞이지 않도록
        /// 하나의 출력 단위로 묶어 출력한다.
        /// </summary>
        public static void PrintBlock(
            string title,
            string content = null)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(LogLine);
                Console.WriteLine(title);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine(content);
                }

            }

            WriteSerilog(
                LogEventLevel.Debug,
                LogLine,
                null);

            WriteSerilog(
                ResolveLogLevel(title),
                title,
                null);

            if (!string.IsNullOrWhiteSpace(content))
            {
                WriteSerilog(
                    ResolveLogLevel(content),
                    content,
                    null);
            }

        }

        /// <summary>
        /// [Console] 출력과 [Serilog] 파일 로그를 동시에 수행한다.
        /// </summary>
        private static void WriteLine(
            string message,
            LogEventLevel? level)
        {
            string safeMessage =
                message ?? string.Empty;

            lock (_consoleLock)
            {
                Console.WriteLine(
                    safeMessage);
            }

            WriteSerilog(
                level ?? ResolveLogLevel(safeMessage),
                safeMessage,
                null);
        }

        /// <summary>
        /// 메시지 내용 기준으로 기본 Log Level을 분류한다.
        /// </summary>
        private static LogEventLevel ResolveLogLevel(
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return LogEventLevel.Debug;
            }

            string upperMessage =
                message.ToUpperInvariant();

            if (upperMessage.Contains("ERROR") ||
                upperMessage.Contains("EXCEPTION") ||
                upperMessage.Contains("CRITICAL"))
            {
                return LogEventLevel.Error;
            }

            if (upperMessage.Contains("FAILED") ||
                upperMessage.Contains("FAIL") ||
                upperMessage.Contains("IGNORED") ||
                upperMessage.Contains("INVALID") ||
                upperMessage.Contains("UNSUPPORTED") ||
                upperMessage.Contains("MISMATCH") ||
                upperMessage.Contains("SKIP") ||
                upperMessage.Contains("EMPTY") ||
                upperMessage.Contains("ALREADY") ||
                upperMessage.Contains("NOT CONNECTED") ||
                upperMessage.Contains("TIMEOUT"))
            {
                return LogEventLevel.Warning;
            }

            if (upperMessage.Contains("RAW") ||
                upperMessage.Contains("PARSE RESULT") ||
                upperMessage.Contains("CHECKSUM") ||
                upperMessage.Contains("LENGTH OK") ||
                upperMessage.Contains("TAIL CHECK") ||
                upperMessage.Contains("ENCODER") ||
                upperMessage.Contains("RECEIVE") ||
                upperMessage.Contains("RX") ||
                upperMessage.Contains("SEND") ||
                upperMessage.Contains("TX"))
            {
                return LogEventLevel.Debug;
            }
            return LogEventLevel.Information;
        }

        /// <summary>
        /// Serilog 출력 수행
        /// </summary>
        private static void WriteSerilog(
            LogEventLevel level,
            string message,
            Exception exception)
        {
            try
            {
                Log.Write(
                    level,
                    exception,
                    "{Message}",
                    message);
            }
            catch
            {
                // Logger 초기화 전 / 종료 중 로그 실패는 프로그램 동작에 영향을 주지 않는다.
            }

        }

    }

}
