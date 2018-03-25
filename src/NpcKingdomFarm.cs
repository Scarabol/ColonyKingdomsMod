using Pipliz;
using Pipliz.JSON;
using Pipliz.Threading;
using BlockTypes.Builtin;
using NPC;

namespace ScarabolMods
{
  public class NpcKingdomFarm : NpcKingdom
  {
    int Size;

    public static void CreateFarm (Vector3Int farmPosition, int size)
    {
      var npcKingdom = new NpcKingdomFarm ();
      npcKingdom.InitPlayer (KingdomsTracker.GetNextID ());
      npcKingdom.SetName ("NPC-Farmer");
      npcKingdom.Size = size;
      npcKingdom.SetOrigin (farmPosition);
      new NpcFarmBuilder (npcKingdom.Player, farmPosition, npcKingdom.Size).Build ();
    }

    public NpcKingdomFarm () : base ("farm")
    {
    }

    public override JSONNode GetJson ()
    {
      var kingdomNode = base.GetJson ();
      kingdomNode.SetAs ("Size", Size);
      return kingdomNode;
    }

    public override void InitFromJson (JSONNode jsonNode)
    {
      base.InitFromJson (jsonNode);
      jsonNode.TryGetAs ("Size", out Size);
    }

    protected override void Update ()
    {
      ThreadManager.InvokeOnMainThread (delegate {
        CheckFood ();
        CheckFollower ();
        CheckSeeds ();
      });
    }

    void CheckFood ()
    {
      var missing = 5000 - Stockpile.AmountContained (BuiltinBlocks.Bread);
      Stockpile.Add (BuiltinBlocks.Bread, missing);
    }

    void CheckFollower ()
    {
      int maxFollower = Math.Min (Size, BedBlockTracker.GetCount (Player));
      for (int c = Colony.FollowerCount; c < maxFollower; c++) {
        SpawnNpc ();
      }
    }

    void SpawnNpc ()
    {
      NPCBase npc = new NPCBase (Server.NPCs.NPCType.GetByKeyNameOrDefault ("pipliz.laborer"), Origin.Vector, Colony);
      ModLoader.TriggerCallbacks (ModLoader.EModCallbackType.OnNPCRecruited, npc);
      Colony.SendUpdate ();
    }

    void CheckSeeds ()
    {
      var missing = 100 - Stockpile.AmountContained (BuiltinBlocks.WheatStage1);
      Stockpile.Add (BuiltinBlocks.WheatStage1, missing);
    }
  }
}
