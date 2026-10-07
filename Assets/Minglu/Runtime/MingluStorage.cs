using System.IO;
using UnityEngine;

namespace Minglu
{
    public static class MingluStorage
    {
        // Keep the established save identity when the game's display name changes.
        // Company remains DefaultCompany; no user files are moved or overwritten.
        public static string Directory
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.persistentDataPath), "Operator911"); }
        }
    }
}
