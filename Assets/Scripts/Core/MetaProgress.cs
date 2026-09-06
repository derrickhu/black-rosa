using UnityEngine;

namespace InkLine
{
    public sealed class MetaProgress
    {
        const string Key = "inkline.meta.v1";

        public int[] Stars = new int[GameConstants.ChapterStageCount];
        public bool ChapterCleared;
        public bool StartGoldBonus;

        public static MetaProgress Load()
        {
            var m = new MetaProgress();
            string raw = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(raw)) return m;
            string[] parts = raw.Split('|');
            if (parts.Length < 2) return m;
            m.ChapterCleared = parts[0] == "1";
            m.StartGoldBonus = parts.Length > 1 && parts[1] == "1";
            if (parts.Length > 2)
            {
                string[] stars = parts[2].Split(',');
                for (int i = 0; i < stars.Length && i < m.Stars.Length; i++)
                    int.TryParse(stars[i], out m.Stars[i]);
            }
            return m;
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, $"{(ChapterCleared ? 1 : 0)}|{(StartGoldBonus ? 1 : 0)}|{string.Join(",", Stars)}");
            PlayerPrefs.Save();
        }

        public bool Unlocked(int stage) => stage <= 0 || Stars[stage - 1] > 0;

        public int TotalStars()
        {
            int n = 0;
            for (int i = 0; i < Stars.Length; i++) n += Stars[i];
            return n;
        }

        public void ApplyResult(int stage, int earned)
        {
            if (earned > Stars[stage]) Stars[stage] = earned;
            if (stage == GameConstants.ChapterStageCount - 1 && earned > 0) ChapterCleared = true;
            if (TotalStars() >= 12) StartGoldBonus = true;
            Save();
        }

        public int StartEmitters => ChapterCleared ? 3 : 2;
        public int StartGold => StartGoldBonus ? 8 : 6;
    }
}
