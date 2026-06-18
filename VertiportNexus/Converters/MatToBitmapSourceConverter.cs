using OpenCvSharp;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VertiportNexus.Converters
{
    public static class MatToBitmapSourceConverter
    {
        /// <summary>
        /// [OpenCV] [Mat] → [WPF] [BitmapSource] 변환
        /// </summary>
        public static BitmapSource Convert(Mat frame)
        {
            if (frame == null ||
                frame.Empty())
            {
                return null;
            }

            /// <summary>
            /// [OpenCV] 기본 영상 포맷
            /// 
            /// 일반적인 [BGR 3채널] 영상을 기준으로 설정한다.
            /// </summary>
            PixelFormat pixelFormat =
                PixelFormats.Bgr24;

            // [채널 수]에 따른 [PixelFormat] 선택
            if (frame.Channels() == 1)
            {
                pixelFormat =
                    PixelFormats.Gray8;
            }
            else if (frame.Channels() == 4)
            {
                pixelFormat =
                    PixelFormats.Bgra32;
            }

            /// <summary>
            /// [OpenCV] [Mat] 데이터를
            /// [WPF]에서 표시 가능한 [BitmapSource]로 변환
            ///
            /// [Width] / [Height]
            /// : 영상 해상도
            ///
            /// [PixelFormat]
            /// : 영상 색상 포맷
            ///
            /// [Data]
            /// : [Mat] 원본 영상 버퍼
            ///
            /// [Stride]
            /// : 한 줄당 메모리 크기(Byte)
            /// </summary>
            return BitmapSource.Create(
                frame.Width,
                frame.Height,
                96,
                96,
                pixelFormat,
                null,
                frame.Data,
                (int)(
                    frame.Step()
                    * frame.Height),
                (int)
                    frame.Step());

        }

    }

}
