using System;
using System.IO;
using BaseballSim;
using BaseballSim.Career;
using UnityEngine;
using CareerState = BaseballSim.Career.Career;

namespace BaseballGame
{
    /// <summary>What the title screen shows for a save slot.</summary>
    public class SlotSummary
    {
        public int Slot;
        /// <summary>Null when the slot is empty (or between careers).</summary>
        public CareerState Career;
        public bool IsEmpty => Career == null;
    }

    /// <summary>
    /// Keeps the saves in the app's private storage (persistentDataPath) on the phone: one club
    /// shared by every slot (my team, legacy points, collection), and one career per slot.
    /// </summary>
    public static class SaveStore
    {
        public const int BaseSlots = 3;

        /// <summary>Slots this device can use; the extra-slots purchase adds two.</summary>
        public static int SlotCount => BaseSlots + (Premium.Owns(Premium.ExtraSlots) ? Premium.ExtraSlotCount : 0);

        /// <summary>The slot being played, 1-based. The title screen sets it.</summary>
        public static int CurrentSlot = 1;

        static string Dir => Application.persistentDataPath;
        static string ClubPath => Path.Combine(Dir, "club.sav");
        static string SlotPath(int slot) => Path.Combine(Dir, $"career_slot{slot}.sav");
        /// <summary>The single save from before slots; it becomes the club and slot 1.</summary>
        static string LegacyPath => Path.Combine(Dir, "career.sav");

        /// <summary>Saves the club and the current slot's career (null between careers empties the slot).</summary>
        public static void Save(Club club, CareerState career)
        {
            SaveClub(club);
            if (career == null) DeleteFile(SlotPath(CurrentSlot));
            else Write(SlotPath(CurrentSlot), SaveGame.CareerToBytes(career, club));
        }

        public static void SaveClub(Club club) => Write(ClubPath, SaveGame.ClubToBytes(club));

        /// <summary>The shared club, or a new one. Moves an old single save over first.</summary>
        public static Club LoadClub(IRandom random)
        {
            MigrateLegacy(random);
            if (!File.Exists(ClubPath)) return new Club();
            try
            {
                return SaveGame.ClubFromBytes(File.ReadAllBytes(ClubPath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Club save could not be read, starting fresh: {e.Message}");
                SetAside(ClubPath);
                return new Club();
            }
        }

        /// <summary>False when the slot is empty. A damaged save is set aside as .bad and treated as empty.</summary>
        public static bool TryLoadCareer(int slot, Club club, IRandom random, out CareerState career)
        {
            career = null;
            string path = SlotPath(slot);
            if (!File.Exists(path)) return false;
            try
            {
                career = SaveGame.CareerFromBytes(File.ReadAllBytes(path), club, random);
                return career != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Slot {slot} could not be read, treating it as empty: {e.Message}");
                SetAside(path);
                return false;
            }
        }

        public static SlotSummary Peek(int slot, Club club, IRandom random)
        {
            TryLoadCareer(slot, club, random, out var career);
            return new SlotSummary { Slot = slot, Career = career };
        }

        /// <summary>Empties one slot. The club (my team) is kept.</summary>
        public static void DeleteSlot(int slot) => DeleteFile(SlotPath(slot));

        /// <summary>Removes every save, for a fresh start from the settings screen.</summary>
        public static void DeleteAll()
        {
            DeleteFile(ClubPath);
            DeleteFile(LegacyPath);
            for (int slot = 1; slot <= BaseSlots + Premium.ExtraSlotCount; slot++) DeleteFile(SlotPath(slot));
        }

        static void MigrateLegacy(IRandom random)
        {
            if (!File.Exists(LegacyPath) || File.Exists(ClubPath)) return;
            try
            {
                var (club, career) = SaveGame.FromBytes(File.ReadAllBytes(LegacyPath), random);
                Write(ClubPath, SaveGame.ClubToBytes(club));
                if (career != null) Write(SlotPath(1), SaveGame.CareerToBytes(career, club));
                File.Delete(LegacyPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Old save could not be moved to slots: {e.Message}");
                SetAside(LegacyPath);
            }
        }

        static void Write(string path, byte[] bytes)
        {
            try
            {
                // Write a temp file first, so a crash mid-write never leaves a half-written save.
                string temp = path + ".tmp";
                File.WriteAllBytes(temp, bytes);
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save failed: {e.Message}");
            }
        }

        static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save could not be deleted: {e.Message}");
            }
        }

        static void SetAside(string path)
        {
            try
            {
                File.Copy(path, path + ".bad", true);
                File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Damaged save could not be set aside: {e.Message}");
            }
        }
    }
}
