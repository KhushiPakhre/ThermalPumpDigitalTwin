using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThermalPumpDT.UI
{
    /// <summary>
    /// Real-time scrolling line graph drawn on a RawImage via Texture2D.
    /// Plots up to 3 series (RPM, Temperature, Vibration) normalised 0-1.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class TimeSeriesGraph : MonoBehaviour
    {
        [Header("Graph Settings")]
        public int   Width     = 512;
        public int   Height    = 128;
        public int   MaxSamples = 120;

        [Header("Series Colors")]
        public Color Series1Color = Color.cyan;
        public Color Series2Color = new Color(1f, 0.5f, 0f);
        public Color Series3Color = Color.magenta;
        public Color BackgroundColor = new Color(0.05f, 0.05f, 0.12f, 1f);
        public Color GridColor       = new Color(0.2f, 0.2f, 0.3f, 1f);

        private Texture2D _texture;
        private RawImage  _image;

        private readonly Queue<float> _s1 = new();
        private readonly Queue<float> _s2 = new();
        private readonly Queue<float> _s3 = new();

        private void Awake()
        {
            _image   = GetComponent<RawImage>();
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
                       { filterMode = FilterMode.Bilinear };
            _image.texture = _texture;
            Clear();
        }

        public void AddSample(float v1, float v2, float v3)
        {
            Enqueue(_s1, v1);
            Enqueue(_s2, v2);
            Enqueue(_s3, v3);
            Redraw();
        }

        private void Enqueue(Queue<float> q, float v)
        {
            q.Enqueue(Mathf.Clamp01(v));
            if (q.Count > MaxSamples) q.Dequeue();
        }

        private void Clear()
        {
            Color32[] pix = new Color32[Width * Height];
            for (int i = 0; i < pix.Length; i++) pix[i] = BackgroundColor;
            _texture.SetPixels32(pix);
            _texture.Apply();
        }

        private void Redraw()
        {
            // Background
            Color32[] pix = new Color32[Width * Height];
            for (int i = 0; i < pix.Length; i++) pix[i] = BackgroundColor;

            // Grid lines
            for (int g = 1; g < 4; g++)
            {
                int y = Mathf.RoundToInt(Height * g / 4f);
                for (int x = 0; x < Width; x++) pix[y * Width + x] = GridColor;
            }

            // Series
            DrawSeries(_s1, Series1Color, pix);
            DrawSeries(_s2, Series2Color, pix);
            DrawSeries(_s3, Series3Color, pix);

            _texture.SetPixels32(pix);
            _texture.Apply();
        }

        private void DrawSeries(Queue<float> series, Color col, Color32[] pix)
        {
            var arr   = series.ToArray();
            if (arr.Length < 2) return;

            float xStep = (float)Width / MaxSamples;
            int xOff = MaxSamples - arr.Length;

            for (int i = 1; i < arr.Length; i++)
            {
                int x0 = Mathf.RoundToInt((xOff + i - 1) * xStep);
                int x1 = Mathf.RoundToInt((xOff + i)     * xStep);
                int y0 = Mathf.RoundToInt(arr[i-1] * (Height - 2)) + 1;
                int y1 = Mathf.RoundToInt(arr[i]   * (Height - 2)) + 1;

                DrawLine(pix, x0, y0, x1, y1, col);
            }
        }

        private void DrawLine(Color32[] pix, int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(pix, x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
            }
        }

        private void SetPixel(Color32[] pix, int x, int y, Color c)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            pix[y * Width + x] = c;
        }
    }
}
