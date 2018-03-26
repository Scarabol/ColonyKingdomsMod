using System.IO;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsModEntries
  {
    public static readonly string MOD_PREFIX = "mods.scarabol.kingdoms.";
    public static string ModDirectory;

    public static readonly int CHUNK_SIZE = 16;
    public static readonly int SAFETY_RANGE_CHUNKS = 2;

    [ModLoader.ModCallback (ModLoader.EModCallbackType.OnAssemblyLoaded, "scarabol.kingdoms.assemblyload")]
    public static void OnAssemblyLoaded (string path)
    {
      ModDirectory = Path.GetDirectoryName (path);
    }

    [ModLoader.ModCallback (ModLoader.EModCallbackType.AfterStartup, "scarabol.kingdoms.registercallbacks")]
    public static void AfterStartup ()
    {
      Pipliz.Log.Write ("Loaded Kingdoms Mod 6.0.3 by Scarabol");
    }
  }
}
