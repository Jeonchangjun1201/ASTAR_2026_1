using UnityEngine;
using UnityEngine.UI;

namespace _TeamFolder.JCJ.Script
{
  public static class JcjUiFactory
  {
    public static Canvas FindFirstOverlayCanvas() =>
      Object.FindFirstObjectByType<Canvas>();

    public static Canvas FindOrCreateOverlayCanvas(string name = "Canvas (auto)", int sortOrder = 100)
    {
      var existing = FindFirstOverlayCanvas();
      if (existing != null)
      {
        ConfigureOverlayScaler(existing);
        return existing;
      }

      var go = new GameObject(name);
      var canvas = go.AddComponent<Canvas>();
      canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      canvas.sortingOrder = sortOrder;
      go.AddComponent<GraphicRaycaster>();
      ConfigureOverlayScaler(canvas);
      return canvas;
    }

    public static void ConfigureOverlayScaler(Canvas canvas, float matchWidthOrHeight = 0.5f)
    {
      if (canvas == null) return;
      var scaler = canvas.GetComponent<CanvasScaler>();
      if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
      scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
      scaler.referenceResolution = new Vector2(1920f, 1080f);
      scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
      scaler.matchWidthOrHeight = matchWidthOrHeight;
      scaler.referencePixelsPerUnit = 100f;
    }
  }
}
