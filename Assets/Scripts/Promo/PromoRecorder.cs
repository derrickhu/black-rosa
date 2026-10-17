#if PROMO_CAPTURE
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace InkLine
{
    // 离线录屏：固定帧率逐帧截图存 JPG，声音走 AudioRenderer 逐帧取样，最后写一份 WAV。
    // captureFramerate 让游戏时间按帧推进，编码慢也不会掉帧，画面和声音天然对齐。
    public sealed class PromoRecorder : MonoBehaviour
    {
        public const int Fps = 30;

        string _dir;
        int _frame;
        bool _on;
        int _channels = 2;
        readonly List<float> _audio = new List<float>(1 << 20);

        public int Frames => _frame;

        public static PromoRecorder Begin(string dir)
        {
            Directory.CreateDirectory(dir);
            var go = new GameObject("PromoRecorder");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<PromoRecorder>();
            r._dir = dir;
            r._channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            Time.captureFramerate = Fps;
            AudioRenderer.Start();
            r._on = true;
            r.StartCoroutine(r.Loop());
            return r;
        }

        IEnumerator Loop()
        {
            var eof = new WaitForEndOfFrame();
            while (_on)
            {
                yield return eof;
                if (!_on) break;
                Grab();
            }
        }

        void Grab()
        {
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (_frame == 0)
                File.WriteAllText(Path.Combine(_dir, "size.txt"), tex.width + "x" + tex.height);
            File.WriteAllBytes(Path.Combine(_dir, $"f{_frame:D5}.jpg"), tex.EncodeToJPG(93));
            Destroy(tex);
            _frame++;

            int n = AudioRenderer.GetSampleCountForCaptureFrame();
            if (n <= 0) return;
            var buf = new NativeArray<float>(n * _channels, Allocator.Temp);
            if (AudioRenderer.Render(buf))
                for (int i = 0; i < buf.Length; i++) _audio.Add(buf[i]);
            buf.Dispose();
        }

        public void End()
        {
            if (!_on) return;
            _on = false;
            AudioRenderer.Stop();
            WriteWav(Path.Combine(_dir, "audio.wav"), _audio, _channels, AudioSettings.outputSampleRate);
            File.WriteAllText(Path.Combine(_dir, "done.txt"), _frame + " frames @" + Fps);
        }

        static void WriteWav(string path, List<float> samples, int channels, int rate)
        {
            using (var fs = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(fs))
            {
                int bytes = samples.Count * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);
                w.Write((short)1);
                w.Write((short)channels);
                w.Write(rate);
                w.Write(rate * channels * 2);
                w.Write((short)(channels * 2));
                w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                w.Write(bytes);
                for (int i = 0; i < samples.Count; i++)
                    w.Write((short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), -32768, 32767));
            }
        }
    }
}
#endif
