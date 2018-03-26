using Pipliz;

namespace ScarabolMods
{
  public static class BlockPlacementHelper
  {
    public static void Circle (Vector3Int center, int diameter, ushort type, Players.Player player)
    {
      int radius = diameter / 2;
      for (int c = 0; c < diameter; c++) {
        PlaceBlock (center.Add (-radius, 0, -radius + c), type, player);
        PlaceBlock (center.Add (radius, 0, -radius + c), type, player);
      }
      for (int c = 0; c < diameter - 2; c++) {
        PlaceBlock (center.Add (-radius + 1 + c, 0, -radius), type, player);
        PlaceBlock (center.Add (-radius + 1 + c, 0, radius), type, player);
      }
    }

    public static void PlaceBlock (Vector3Int position, ushort type, Players.Player player)
    {
      if (!ServerManager.TryChangeBlock (position, type, player)) {
        Pipliz.Log.WriteError ($"Could not place block at {position}");
      }
    }

    public static void FillPlane (Vector3Int start, int width, int depth, ushort type, Players.Player player)
    {
      FillArea (start, width, 1, depth, type, player);
    }

    public static void FillArea (Vector3Int start, int width, int height, int depth, ushort type, Players.Player player)
    {
      for (int z = 0; z < height; z++) {
        for (int y = 0; y < depth; y++) {
          for (int x = 0; x < width; x++) {
            PlaceBlock (start.Add (x, 0, y), type, player);
          }
        }
      }
    }
  }
}
