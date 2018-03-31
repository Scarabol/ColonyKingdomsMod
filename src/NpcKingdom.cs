using System;
using System.Threading;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;
using Pipliz.Threading;
using Steamworks;
using BlockTypes.Builtin;

namespace ScarabolMods
{
  public abstract class NpcKingdom
  {
    public readonly string KingdomType;
    protected int RangeInChunks;
    protected int HeightInChunks;
    protected int PrimaryRange;
    protected int PrimaryMinY;
    protected int PrimaryMaxY;
    public uint NpcID;
    public string Name;
    public Vector3Int Origin;
    public NetworkID NetworkID;
    protected bool Dead;
    protected LootSpawner LootSpawner;

    public NpcKingdom (string kingdomType)
    {
      KingdomType = kingdomType;
      LootSpawner = new LootSpawner (kingdomType);
    }

    public virtual JSONNode GetJson ()
    {
      var kingdomNode = new JSONNode ();
      kingdomNode.SetAs ("KingdomType", KingdomType);
      kingdomNode.SetAs ("NpcID", NpcID);
      kingdomNode.SetAs ("Name", Name);
      kingdomNode.SetAs ("Origin", (JSONNode)Origin);
      kingdomNode.SetAs ("NetworkID", NetworkID.ToString ());
      return kingdomNode;
    }

    public virtual void InitFromJson (JSONNode jsonNode)
    {
      if (!jsonNode.TryGetAs ("NpcID", out NpcID)) {
        NpcID = KingdomsTracker.GetNextNpcID ();
      }
      string name;
      if (jsonNode.TryGetAs ("Name", out name)) {
        Name = name;
      }
      JSONNode jsonOrigin;
      if (jsonNode.TryGetAs ("Origin", out jsonOrigin)) {
        Origin = (Vector3Int)jsonOrigin;
      }
      string networkID;
      if (jsonNode.TryGetAs ("NetworkID", out networkID)) {
        NetworkID = NetworkID.Parse (networkID);
      } else {
        NetworkID = CreateFakeNetworkID (NpcID);
      }
      FinishInitialization ();
    }

    public virtual void InitNew ()
    {
      NpcID = KingdomsTracker.GetNextNpcID ();
      NetworkID = CreateFakeNetworkID (NpcID);
      FinishInitialization ();
    }

    public virtual void FinishInitialization ()
    {
      KingdomsTracker.RegisterKingdom (this);
      ThreadManager.InvokeOnMainThread (delegate {
        var player = Players.GetPlayer (NetworkID);
        player.Name = Name;
      });
      LootSpawner.Start (NetworkID);
    }

    static NetworkID CreateFakeNetworkID (uint npcID)
    {
      return new NetworkID (new CSteamID (new AccountID_t (npcID), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeAnonGameServer));
    }

    public void StartThread ()
    {
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        while (!Dead) {
          ThreadManager.InvokeOnMainThread (delegate {
            try {
              Update (Players.GetPlayer (NetworkID));
            } catch (Exception exception) {
              Log.WriteError ($"Exception in kingdom update thread; {exception.Message}");
            }
          });
          Thread.Sleep (5000);
        }
        AfterDeath ();
      }).Start ();
    }

    void AfterDeath ()
    {
      LootSpawner.Kill ();
      KingdomsTracker.UnregisterKingdom (this);
    }

    protected abstract void Update (Players.Player player);

    public int Range {
      get {
        return (System.Math.Max (RangeInChunks, HeightInChunks) + KingdomsModEntries.SAFETY_RANGE_CHUNKS) * KingdomsModEntries.CHUNK_SIZE;
      }
    }

    public HashSet<Vector3Int> GetPrimaryChunkPositions ()
    {
      return GetChunks (Origin, PrimaryRange, PrimaryMinY, PrimaryMaxY);
    }

    public bool IsAreaClear ()
    {
      for (int x = -PrimaryRange; x < PrimaryRange; x++) {
        for (int y = PrimaryMinY; y < PrimaryMaxY; y++) {
          for (int z = -PrimaryRange; z < PrimaryRange; z++) {
            Vector3Int checkPosition = Origin.Add (x, y, z);
            ushort spotType;
            if (!World.TryGetTypeAt (checkPosition, out spotType) || spotType != BuiltinBlocks.Air) {
              return false;
            }
          }
        }
      }
      return true;
    }

    public HashSet<Vector3Int> GetTotalChunkPositions ()
    {
      return GetChunks (Origin, Range, -Range, Range);
    }

    static HashSet<Vector3Int> GetChunks (Vector3Int center, int range, int yMin, int yMax)
    {
      HashSet<Vector3Int> result = new HashSet<Vector3Int> ();
      for (int x = -range; x < range; x++) {
        for (int y = yMin; y < yMax; y++) {
          for (int z = -range; z < range; z++) {
            Vector3Int chunkPosition = center.Add (x, y, z).ToChunk ();
            result.Add (chunkPosition);
          }
        }
      }
      return result;
    }
  }
}
