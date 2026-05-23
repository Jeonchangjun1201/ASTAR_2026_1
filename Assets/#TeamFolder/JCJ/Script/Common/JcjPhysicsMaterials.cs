using UnityEngine;

namespace _TeamFolder.JCJ.Script
{
  public static class JcjPhysicsMaterials
  {
    private static PhysicsMaterial _playerLowFriction;
    private static PhysicsMaterial _wallLowFriction;

    public static PhysicsMaterial PlayerLowFriction =>
      _playerLowFriction ??= Create("JcjPlayerLowFriction");

    public static PhysicsMaterial WallLowFriction =>
      _wallLowFriction ??= Create("JcjWallLowFriction");

    public static void ApplyPlayerLowFriction(Collider collider)
    {
      if (collider == null) return;
      collider.sharedMaterial = PlayerLowFriction;
    }

    public static void ApplyWallLowFriction(Collider collider)
    {
      if (collider == null) return;
      collider.sharedMaterial = WallLowFriction;
    }

    private static PhysicsMaterial Create(string name) => new(name)
    {
      dynamicFriction = 0f,
      staticFriction = 0f,
      bounciness = 0f,
      frictionCombine = PhysicsMaterialCombine.Minimum,
      bounceCombine = PhysicsMaterialCombine.Minimum
    };
  }
}
