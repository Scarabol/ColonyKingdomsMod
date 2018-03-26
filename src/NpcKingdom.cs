using System;
using System.Threading;
using System.Collections.Generic;
using Pipliz;
using Pipliz.JSON;
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
    public Players.Player Player;
    public Vector3Int Origin;
    public Stockpile Stockpile { get { return Stockpile.GetStockPile (Player); } }
    public Colony Colony { get { return Colony.Get (Player); } }

    public NpcKingdom (string kingdomType)
    {
      KingdomType = kingdomType;
    }

    public string Name {
      get { return Player.Name; }
      set { Player.Name = value; }
    }

    public virtual JSONNode GetJson ()
    {
      var kingdomNode = new JSONNode ();
      kingdomNode.SetAs ("KingdomType", KingdomType);
      kingdomNode.SetAs ("NpcID", NpcID);
      kingdomNode.SetAs ("Name", Player.Name);
      kingdomNode.SetAs ("Origin", (JSONNode)Origin);
      return kingdomNode;
    }

    public virtual void InitFromJson (JSONNode jsonNode)
    {
      if (!jsonNode.TryGetAs ("NpcID", out NpcID)) {
        NpcID = KingdomsTracker.GetNextID ();
      }
      InitPlayer ();
      string name;
      if (jsonNode.TryGetAs ("Name", out name)) {
        Player.Name = name;
      }
      JSONNode jsonOrigin;
      if (jsonNode.TryGetAs ("Origin", out jsonOrigin)) {
        Origin = (Vector3Int)jsonOrigin;
      }
      KingdomsTracker.RegisterKingdom (this);
    }

    public virtual void InitNew ()
    {
      NpcID = KingdomsTracker.GetNextID ();
      InitPlayer ();
      KingdomsTracker.RegisterKingdom (this);
    }

    void InitPlayer ()
    {
      var fakeSteamID = new CSteamID (new AccountID_t (NpcID), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeAnonGameServer);
      Player = Players.GetPlayer (new NetworkID (fakeSteamID));
    }

    public void StartThread ()
    {
      new Thread (() => {
        Thread.CurrentThread.IsBackground = true;
        Log.Write ($"Started AI thread");
        while (true) {
          try {
            Update ();
          } catch (Exception exception) {
            Log.WriteError ($"Exception in kingdom update thread; {exception.Message}");
          }
          Thread.Sleep (5000);
        }
      }).Start ();
    }

    protected abstract void Update ();

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
            if (!World.TryGetTypeAt (checkPosition, out ushort spotType) || spotType != BuiltinBlocks.Air) {
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
