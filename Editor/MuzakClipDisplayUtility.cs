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
            if (m_textureLookup.TryGetValue(clip, out Texture2D texture))
            {
                return texture;
            }
            var w = Mathf.RoundToInt(clip.length * 64);
            var h = 64;
            texture = new Texture2D(w, h);

            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            var sampleCounter = 0;
            var xTexStep = 0;
            var step = Mathf.Max(1, (clip.samples / 2) / w);
            while (sampleCounter < samples.Length / 2)
            {
                var x = sampleCounter;
                var leftChannel = samples[sampleCounter];
                var rightChannel = samples[sampleCounter + samples.Length / 2];
                for (var y = 0; y < h / 2; y++)
                {
                    var hF = (float)h / 2;
                    var yF = 1 - (y / hF);
                    texture.SetPixel(xTexStep, y, leftChannel > yF / hF ? Color.cyan : Color.clear);
                }
                for (var y = h / 2; y < h; y++)
                {
                    var hF = (float)h / 2;
                    var yF = ((y - (h / 2)) / hF);
                    texture.SetPixel(xTexStep, y, rightChannel > yF / hF ? Color.cyan : Color.clear);
                }
                sampleCounter += step;
                xTexStep++;
            }
            texture.Apply();
            m_textureLookup.Add(clip, texture);
            return texture;
        }
    }
}