using System.Collections.Generic;
using Pipliz;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public class NpcFarmBuilder
  {
    public static readonly int MAX_SIZE = 8;

    public readonly List<Vector3Int> LootSpots = new List<Vector3Int> ();
    readonly Players.Player Owner;
    readonly Vector3Int FarmOrigin;
    int FarmSize;

    public NpcFarmBuilder (Players.Player owner, Vector3Int farmOrigin, int size)
    {
      Owner = owner;
      FarmOrigin = farmOrigin;
      FarmSize = size;
    }

    public void Build ()
    {
      CreateFields ();
      CreateFarmHouse ();
      CreatePaths ();
    }

    void CreateFarmHouse ()
    {
      Vector3Int houseOrigin = FarmOrigin.Add (-3, 0, -2);
      BlockPlacementHelper.FillPlane (houseOrigin.Add (0, -1, 0), 7, 8, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin, 1, 1, 8, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (6, 0, 0), 1, 1, 8, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (1, 0, 7), 5, 1, 1, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (1, 0, 0), 2, 1, 1, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (4, 0, 0), 2, 1, 1, BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (0, 1, 0), BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (6, 1, 0), BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (0, 1, 7), BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (6, 1, 7), BuiltinBlocks.StoneBricks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (0, 1, 1), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (0, 1, 3), 1, 1, 2, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (0, 1, 6), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (6, 1, 1), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (6, 1, 3), 1, 1, 2, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (6, 1, 6), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (1, 1, 7), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (3, 1, 7), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (5, 1, 7), BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (1, 1, 0), 2, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (1, 1, -1), BuiltinBlocks.TorchZP, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (4, 1, 0), 2, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.PlaceBlock (houseOrigin.Add (5, 1, -1), BuiltinBlocks.TorchZP, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (0, 2, 0), 1, 1, 8, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (6, 2, 0), 1, 1, 8, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (1, 2, 0), 5, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (1, 2, 7), 5, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (-1, 3, -1), 3, 1, 10, BuiltinBlocks.Straw, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (5, 3, -1), 3, 1, 10, BuiltinBlocks.Straw, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (2, 3, 0), 3, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (2, 3, 7), 3, 1, 1, BuiltinBlocks.Planks, Owner);
      BlockPlacementHelper.FillArea (houseOrigin.Add (0, 4, -1), 7, 1, 10, BuiltinBlocks.Straw, Owner);
      List<Vector3Int> possibleBedSpots = new List<Vector3Int> ();
      for (int c = 0; c < 6; c++) {
        possibleBedSpots.Add (houseOrigin.Add (1, 0, 1 + c));
        possibleBedSpots.Add (houseOrigin.Add (4, 0, 1 + c));
        LootSpots.Add (houseOrigin.Add (1, 0, 1 + c));
        LootSpots.Add (houseOrigin.Add (5, 0, 1 + c));
      }
      PlaceBeds (possibleBedSpots);
    }

    void PlaceBeds (List<Vector3Int> possibleBedSpots)
    {
      for (int c = 0; c < FarmSize && possibleBedSpots.Count > 0; c++) {
        var spot = Random.Next (possibleBedSpots.Count);
        var bedPosition = possibleBedSpots [spot];
        possibleBedSpots.RemoveAt (spot);
        var flipped = Random.NextBool ();
        if (flipped) {
          BlockPlacementHelper.PlaceBlock (bedPosition.Add (1, 0, 0), BuiltinBlocks.BedXN, Owner);
        } else {
          BlockPlacementHelper.PlaceBlock (bedPosition, BuiltinBlocks.BedXP, Owner);
        }
      }
    }

    void CreatePaths ()
    {
      BlockPlacementHelper.FillArea (FarmOrigin.Add (-1, -1, -6), 3, 1, 4, BuiltinBlocks.Dirt, Owner);
    }

    void CreateFields ()
    {
      List<Vector3Int> possibleFieldPositions = new List<Vector3Int> ();
      for (int x = -1; x <= 1; x++) {
        for (int z = -1; z <= 1; z++) {
          if (x != 0 || z != 0) {
            var rx = Random.Next (5);
            var rz = Random.Next (5);
            possibleFieldPositions.Add (FarmOrigin.Add (x * (6 + 10 / 2 + rx), 0, z * (6 + 10 / 2 + rz)));
          }
        }
      }
      int c;
      for (c = 0; c < FarmSize && possibleFieldPositions.Count > 0;) {
        var spot = Random.Next (possibleFieldPositions.Count);
        var fieldPosition = possibleFieldPositions [spot];
        possibleFieldPositions.RemoveAt (spot);
        if (CreateFieldAndJob (fieldPosition)) {
          c++;
        }
      }
      FarmSize = c;
    }

    bool CreateFieldAndJob (Vector3Int center)
    {
      if (!IsClear (center.Add (-5, 0, -5), 11, 2, 11)) {
        return false;
      }
      var crateZ = -4 + Random.Next (10);
      BlockPlacementHelper.PlaceBlock (center.Add (-5, 0, crateZ), BuiltinBlocks.Crate, Owner);
      BlockPlacementHelper.PlaceBlock (center.Add (-5, 1, crateZ), BuiltinBlocks.TorchYP, Owner);
      var min = center.Add (-4, 0, -4);
      var max = center.Add (5, 0, 5);
      for (int x = min.x; x < max.x; x++) {
        for (int z = min.z; z < max.z; z++) {
          if (!World.TryIsSolid (new Vector3Int (x, center.y - 1, z), out bool solid) || !solid) {
            return false;
          }
        }
      }
      AreaJobTracker.CreateNewAreaJob ("pipliz.wheatfarm", Owner, min, max);
      return true;
    }

    bool IsClear (Vector3Int start, int width, int height, int depth)
    {
      for (int y = 0; y < height; y++) {
        for (int x = 0; x < width; x++) {
          for (int z = 0; z < depth; z++) {
            if (!World.TryGetTypeAt (start.Add (x, y, z), out ushort type) || type != BuiltinBlocks.Air) {
              return false;
            }
          }
        }
      }
      return true;
    }
  }
}
