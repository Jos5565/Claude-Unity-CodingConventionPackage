using System;
using System.Text;
using UnityConv.Network;
using UnityEngine;

namespace UnityConv.Samples.Network
{
    /// <summary>
    /// 파일 응답 바이트로 확장자 판별 (샘플), 임포트하면 APIService.ExtensionDetector에 자동 등록
    /// </summary>
    public static class FileExtensionDecoder
    {
        // * 프로젝트에 맞게 판별 규칙과 기본값을 고쳐 쓴다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            APIService.ExtensionDetector = DetectExtension;
        }

        public static string DetectExtension(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 4) return ".ply";

            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bytes.Length >= 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 &&
                bytes[2] == 0x4E && bytes[3] == 0x47 &&
                bytes[4] == 0x0D && bytes[5] == 0x0A &&
                bytes[6] == 0x1A && bytes[7] == 0x0A)
            {
                return ".png";
            }

            // JPG와 JPEG는 동일한 파일 형식이므로 대표 확장자인 .jpg를 반환한다.
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return ".jpg";
            }

            // WAV: RIFF .... WAVE
            if (bytes.Length >= 12 &&
                bytes[0] == (byte)'R' && bytes[1] == (byte)'I' &&
                bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'A' &&
                bytes[10] == (byte)'V' && bytes[11] == (byte)'E')
            {
                return ".wav";
            }

            // MP3: ID3 태그 또는 MPEG Audio Frame Sync
            if ((bytes[0] == (byte)'I' && bytes[1] == (byte)'D' && bytes[2] == (byte)'3') ||
                (bytes[0] == 0xFF && (bytes[1] & 0xE0) == 0xE0))
            {
                return ".mp3";
            }

            string head = Encoding.ASCII.GetString(bytes, 0, Math.Min(128, bytes.Length));
            if (head.StartsWith("ply")) return ".ply";
            if (head.Contains(".PCD") || head.StartsWith("VERSION") || head.Contains("FIELDS")) return ".pcd";
            return ".xyz";
        }
    }
}
