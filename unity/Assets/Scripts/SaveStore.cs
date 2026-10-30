using System;
using System.IO;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>Keeps the save in the app's private storage (persistentDataPath) on the phone.</summary>
    public static class SaveStore
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "career.sav");

        /// <summary>Saves the club and the career in progress (null between careers).</summary>
        public static void Save(Club club, CareerState career)
        {
            try
            {
                // Write a temp file first, so a crash mid-write never leaves a half-written save.
                string temp = FilePath + ".tmp";
                File.WriteAllBytes(temp, SaveGame.ToBytes(club, career));
                File.Copy(temp, FilePath, true);
                File.Delete(temp);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save failed: {e.Message}");
            }
        }

        /// <summary>False when there's no save yet. A damaged save is set aside as .bad and treated as none.</summary>
        public static bool TryLoad(IRandom random, out Club club, out CareerState career)
        {
            club = null;
            career = null;
            if (!File.Exists(FilePath)) return false;
            try
            {
                (club, career) = SaveGame.FromBytes(File.ReadAllBytes(FilePath), random);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save could not be read, starting fresh: {e.Message}");
                File.Copy(FilePath, FilePath + ".bad", true);
                File.Delete(FilePath);
                return false;
            }
        }
    }
}
