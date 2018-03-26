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

    public static NpcKingdomFarm Create (Vector3Int farmPosition, int size)
    {
      var result = new NpcKingdomFarm ();
      result.Origin = farmPosition;
      result.Size = size;
      return result;
    }

    public NpcKingdomFarm () : base ("farm")
    {
      RangeInChunks = 2;
      HeightInChunks = 1;
      PrimaryRange = 8;
      PrimaryMinY = 0;
      PrimaryMaxY = 6;
    }

    public override void InitNew ()
    {
      base.InitNew ();
      Name = "NPC-Farmer";
      new NpcFarmBuilder (Player, Origin, Size).Build ();
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
