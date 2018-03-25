using System.IO;

namespace ScarabolMods
{
  [ModLoader.ModManager]
  public static class KingdomsModEntries
  {
    public static string MOD_PREFIX = "mods.scarabol.kingdoms.";
    public static string ModDirectory;

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
