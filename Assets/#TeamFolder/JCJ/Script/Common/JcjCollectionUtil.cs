using System.Collections.Generic;
using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
  public static class JcjCollectionUtil
  {
    public static void Shuffle<T>(IList<T> list)
    {
      for (int i = list.Count - 1; i > 0; i--)
      {
        int j = Random.Range(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
      }
    }

    public static void Shuffle<T>(T[] arr) => Shuffle((IList<T>)arr);
  }
}
