using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
  public static class JcjAudioAmbience
  {
    public static void EnsureAudioListener()
    {
      if (Object.FindFirstObjectByType<AudioListener>() != null) return;
      var cam = Camera.main;
      if (cam != null) cam.gameObject.AddComponent<AudioListener>();
    }

  /// <summary>8초 루프 앰비언트 드론 — Maze/Tile BGM 베드 공용.</summary>
    public static AudioClip CreateAmbientBed(string clipName = "JcjAmbientBed")
    {
      const int sr = 44100;
      const float duration = 8f;
      int total = Mathf.CeilToInt(duration * sr);
      var buffer = new float[total];

      const float f1 = 110f;
      const float f2 = 164.81f;
      const float f3 = 220f;

      float prev = 0f;
      float rc = 1f / (Mathf.PI * 2f * 800f);
      float dt = 1f / sr;
      float alpha = dt / (rc + dt);

      for (int i = 0; i < total; i++)
      {
        float t = (float)i / sr;
        float lfo = 0.5f + 0.5f * Mathf.Sin(Mathf.PI * 2f * 0.1f * t);
        float tone = 0.18f * Mathf.Sin(Mathf.PI * 2f * f1 * t)
                   + 0.12f * Mathf.Sin(Mathf.PI * 2f * f2 * t)
                   + 0.09f * Mathf.Sin(Mathf.PI * 2f * f3 * t);
        float noise = (Random.value * 2f - 1f) * 0.04f;
        prev = prev + alpha * (noise - prev);

        float fade = 1f;
        if (t < 0.5f) fade = t / 0.5f;
        else if (t > duration - 0.5f) fade = (duration - t) / 0.5f;

        buffer[i] = (tone * lfo + prev) * fade * 0.6f;
      }

      var clip = AudioClip.Create(clipName, total, 1, sr, false);
      clip.SetData(buffer, 0);
      return clip;
    }
  }
}
