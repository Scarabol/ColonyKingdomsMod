using System;
using System.Threading;
using Pipliz;
using Pipliz.JSON;
using Steamworks;

namespace ScarabolMods
{
  public abstract class NpcKingdom
  {
    public readonly string KingdomType;
    public uint NpcID;
    public Players.Player Player;
    public Vector3Int Origin;

    public NpcKingdom (string kingdomType)
    {
      KingdomType = kingdomType;
      KingdomsTracker.RegisterKingdom (this);
    }

    public Stockpile Stockpile {
      get {
        return Stockpile.GetStockPile (Player);
      }
    }

    public Colony Colony {
      get {
        return Colony.Get (Player);
      }
    }

    public virtual void InitPlayer (uint NpcID)
    {
      this.NpcID = NpcID;
      var fakeSteamID = new CSteamID (new AccountID_t (NpcID), EUniverse.k_EUniversePublic, EAccountType.k_EAccountTypeAnonGameServer);
      Player = Players.GetPlayer (new NetworkID (fakeSteamID));
    }

    public void SetName (string name)
    {
      Player.Name = name;
    }

    public void SetOrigin (Vector3Int origin)
    {
      Origin = origin;
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
      InitPlayer (NpcID);
      string name;
      if (jsonNode.TryGetAs ("Name", out name)) {
        Player.Name = name;
      }
      JSONNode jsonOrigin;
      if (jsonNode.TryGetAs ("Origin", out jsonOrigin)) {
        Origin = (Vector3Int)jsonOrigin;
      }
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
  }
}
