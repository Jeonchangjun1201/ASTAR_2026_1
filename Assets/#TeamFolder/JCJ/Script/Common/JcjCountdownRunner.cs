using System;
using System.Collections;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
  public static class JcjCountdownRunner
  {
    public static IEnumerator RunSeconds(
      int seconds,
      Action<int> onTick,
      Action onGo,
      float stepSeconds = 1f,
      float goHoldSeconds = 0.6f)
    {
      seconds = Mathf.Max(0, seconds);
      for (int i = seconds; i > 0; i--)
      {
        onTick?.Invoke(i);
        yield return new WaitForSeconds(Mathf.Max(0.05f, stepSeconds));
      }

      onGo?.Invoke();
      if (goHoldSeconds > 0f)
        yield return new WaitForSeconds(goHoldSeconds);
    }
  }
}
