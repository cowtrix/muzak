using Common;
using System.Collections.Generic;
using UnityEngine;

namespace Muzak
{
    public static class MuzakClipDisplayUtility
    {
        private static Dictionary<AudioClip, Texture2D> m_textureLookup = new Dictionary<AudioClip, Texture2D>();

        public static Texture GetTexture(AudioClip clip)
        {
            if (!clip)
            {
                return null;
            }
            if (m_textureLookup.TryGetValue(clip, out Texture2D tex) && tex)
            {
                return tex;
            }
            var width = Mathf.RoundToInt(clip.length * 64);
            var height = 64;
            tex = new Texture2D(width, height);

            float[] samples = new float[clip.samples];
            float[] waveform = new float[width];
            clip.GetData(samples, 0);
            int packSize = (clip.samples / width) + 1;
            int s = 0;
            for (int i = 0; i < clip.samples; i += packSize)
            {
                waveform[s] = Mathf.Abs(samples[i]);
                s++;
            }

            var foreground = Color.white.WithAlpha(.5f);
            var background = Color.clear;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tex.SetPixel(x, y, background);
                }
            }

            for (int x = 0; x < waveform.Length; x++)
            {
                for (int y = 0; y <= waveform[x] * ((float)height * .75f); y++)
                {
                    tex.SetPixel(x, (height / 2) + y, foreground);
                    tex.SetPixel(x, (height / 2) - y, foreground);
                }
            }
            tex.Apply();
            m_textureLookup[clip] = tex;
            return tex;
        }
    }
}