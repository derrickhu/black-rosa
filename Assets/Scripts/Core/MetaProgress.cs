using System;
using UnityEngine;

namespace InkLine
{
    [Serializable]
    public sealed class MetaProgress
    {
        const string KeyV2 = "inkline.meta.v2";
        const string KeyV1 = "inkline.meta.v1";

        public int Ink;
        public int Stamina = GameConstants.StaminaMax;
        public long StaminaTick;
        public int AdStaminaToday;
        public int LastDay;
        public bool DailyWinDone;
        public bool StarterGranted;
        public int[] Stars = new int[GameConstants.ChapterStageCount];
        public int[] Forge = new int[ForgeCatalog.LineCount];
        public int Skin;
        public bool[] SkinOwned = new bool[SkinCatalog.Count];
        public bool[] SpellOwned = new bool[SpellCatalog.Count];
        public int[] Equipped = { 0, -1 };

        public static MetaProgress Load()
        {
            var m = new MetaProgress();
            string raw = PlayerPrefs.GetString(KeyV2, "");
            if (!string.IsNullOrEmpty(raw))
            {
                try { JsonUtility.FromJsonOverwrite(raw, m); }
                catch (Exception) { m = new MetaProgress(); }
            }
            else m.MigrateV1();
            m.Normalize();
            m.GrantStarter();
            m.Refresh();
            return m;
        }

        // v1 是管道字符串，只有章通关和满星两个布尔。折算成对应的升级等级，
        // 再按已有星数补一笔墨，免得老存档进来面对空的升级树。
        void MigrateV1()
        {
            string raw = PlayerPrefs.GetString(KeyV1, "");
            if (string.IsNullOrEmpty(raw)) return;
            string[] parts = raw.Split('|');
            if (parts.Length < 2) return;
            bool chapterCleared = parts[0] == "1";
            bool goldBonus = parts[1] == "1";
            if (parts.Length > 2)
            {
                string[] stars = parts[2].Split(',');
                for (int i = 0; i < stars.Length && i < Stars.Length; i++)
                    int.TryParse(stars[i], out Stars[i]);
            }
            if (chapterCleared) Forge[(int)ForgeLine.Emitters] = 1;
            if (goldBonus) Forge[(int)ForgeLine.StartGold] = 1;
            Ink += TotalStars() * 4;
        }

        void Normalize()
        {
            Stars = Fit(Stars, GameConstants.ChapterStageCount);
            Forge = Fit(Forge, ForgeCatalog.LineCount);
            SkinOwned = Fit(SkinOwned, SkinCatalog.Count);
            SpellOwned = Fit(SpellOwned, SpellCatalog.Count);
            Equipped = Fit(Equipped, GameConstants.SpellSlots);
            for (int i = 0; i < Forge.Length; i++)
                Forge[i] = Mathf.Clamp(Forge[i], 0, ForgeCatalog.MaxLevel(i));
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = Mathf.Clamp(Stars[i], 0, GameConstants.MaxStar);
            SkinOwned[0] = true;
            SpellOwned[0] = true;
            Skin = SkinOwned[Mathf.Clamp(Skin, 0, SkinCatalog.Count - 1)] ? Mathf.Clamp(Skin, 0, SkinCatalog.Count - 1) : 0;
            for (int s = 0; s < Equipped.Length; s++)
            {
                int id = Equipped[s];
                if (id < 0 || id >= SpellCatalog.Count || !SpellOwned[id]) Equipped[s] = -1;
            }
            if (Equipped[0] < 0 && Equipped[1] >= 0)
            {
                Equipped[0] = Equipped[1];
                Equipped[1] = -1;
            }
            if (Equipped[0] < 0) Equipped[0] = 0;
            if (Equipped[0] == Equipped[1]) Equipped[1] = -1;
            Ink = Mathf.Max(0, Ink);
            Stamina = Mathf.Clamp(Stamina, 0, GameConstants.StaminaMax);
        }

        static int[] Fit(int[] src, int len)
        {
            if (src != null && src.Length == len) return src;
            var dst = new int[len];
            if (src != null)
                for (int i = 0; i < src.Length && i < len; i++) dst[i] = src[i];
            return dst;
        }

        static bool[] Fit(bool[] src, int len)
        {
            if (src != null && src.Length == len) return src;
            var dst = new bool[len];
            if (src != null)
                for (int i = 0; i < src.Length && i < len; i++) dst[i] = src[i];
            return dst;
        }

        void GrantStarter()
        {
            if (StarterGranted) return;
            StarterGranted = true;
            Ink += GameConstants.StarterInk;
            Stamina = GameConstants.StaminaMax;
            StaminaTick = DateTime.UtcNow.Ticks;
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetString(KeyV2, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        // 大厅每帧调。体力恢复和日重置都是纯读时间的，放一起省心。
        public void Refresh()
        {
            bool dirty = RollDay();
            if (RegenStamina()) dirty = true;
            if (dirty) Save();
        }

        bool RollDay()
        {
            DateTime n = DateTime.Now;
            int today = n.Year * 10000 + n.Month * 100 + n.Day;
            if (LastDay == today) return false;
            LastDay = today;
            AdStaminaToday = 0;
            DailyWinDone = false;
            return true;
        }

        static long RegenTicks => TimeSpan.TicksPerMinute * GameConstants.StaminaRegenMinutes;

        bool RegenStamina()
        {
            long now = DateTime.UtcNow.Ticks;
            if (StaminaTick <= 0L || Stamina >= GameConstants.StaminaMax)
            {
                // 满体力时只把锚点贴到当下，不报脏 —— 否则大厅每帧写一次 PlayerPrefs。
                // 锚点不落盘也没事：加载时若仍是满的会再贴一次，离开满格由 SpendStamina 落盘。
                StaminaTick = now;
                return false;
            }
            // 把系统时间往回调不给恢复，只把锚点拉到当下。往前调挡不住，
            // 要彻底堵得靠云存档时间戳，见 docs/局外成长.md。
            if (now < StaminaTick)
            {
                StaminaTick = now;
                return true;
            }
            long gain = (now - StaminaTick) / RegenTicks;
            if (gain <= 0L) return false;
            int add = (int)Math.Min(gain, GameConstants.StaminaMax);
            Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + add);
            StaminaTick = Stamina >= GameConstants.StaminaMax ? now : StaminaTick + add * RegenTicks;
            return true;
        }

        public int SecondsToNextStamina()
        {
            if (Stamina >= GameConstants.StaminaMax) return 0;
            long due = StaminaTick + RegenTicks - DateTime.UtcNow.Ticks;
            return due <= 0L ? 0 : (int)(due / TimeSpan.TicksPerSecond);
        }

        // 没打过的关不收体力 —— 推主线全程免费，只有回头重刷才耗。
        public int StageCost(int stage)
        {
            if (stage < 0 || stage >= Stars.Length) return 0;
            return Stars[stage] > 0 ? GameConstants.StaminaPerStage : 0;
        }

        public bool CanEnter(int stage) => Stamina >= StageCost(stage);

        public void SpendStamina(int n)
        {
            if (n <= 0) return;
            if (Stamina >= GameConstants.StaminaMax) StaminaTick = DateTime.UtcNow.Ticks;
            Stamina = Mathf.Max(0, Stamina - n);
            Save();
        }

        // 没赢就全额退，等价于只有通关才真的扣。
        public void RefundStamina(int n)
        {
            if (n <= 0) return;
            Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + n);
            Save();
        }

        public bool CanAdStamina => AdStaminaToday < GameConstants.AdStaminaPerDay
                                    && Stamina < GameConstants.StaminaMax;

        public void GrantAdStamina()
        {
            AdStaminaToday++;
            Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + GameConstants.AdStaminaGain);
            Save();
        }

        public bool Unlocked(int stage) => stage <= 0 || Stars[stage - 1] > 0;

        public int TotalStars()
        {
            int n = 0;
            for (int i = 0; i < Stars.Length; i++) n += Stars[i];
            return n;
        }

        public bool ChapterCleared => Stars[GameConstants.ChapterStageCount - 1] > 0;

        public ForgeStats Forged => ForgeCatalog.Stats(Forge);
        public int StartEmitters => Forged.Emitters;
        public int StartGold => Forged.StartGold;
        public Color SkinTint => SkinCatalog.Get(Skin).Tint;

        // 返回这一局赚到的墨，GameFlow 拿去显示，也拿去算广告双倍要补多少。
        public int ApplyResult(int stage, int earned)
        {
            if (stage < 0 || stage >= Stars.Length || earned <= 0) return 0;
            int old = Stars[stage];
            int full = 4 + stage * 2 + earned * 4;
            int ink;
            if (old <= 0) ink = full;                         // 首通
            else if (earned > old) ink = (earned - old) * 4;  // 提星
            else ink = Mathf.Max(1, full / 2);                // 重刷
            if (earned > old) Stars[stage] = earned;
            if (!DailyWinDone)
            {
                DailyWinDone = true;
                ink *= 2;
                Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + GameConstants.DailyWinStamina);
            }
            Ink += ink;
            Save();
            return ink;
        }

        public void AddInk(int n)
        {
            if (n <= 0) return;
            Ink += n;
            Save();
        }

        public int ForgeLevel(int line) => Forge[Mathf.Clamp(line, 0, ForgeCatalog.LineCount - 1)];

        // 买不起 / 没到门槛时 why 里放要显示给玩家的那句话。
        public bool CanBuyForge(int line, out string why)
        {
            int lv = ForgeLevel(line);
            ForgeDef d = ForgeCatalog.Get(line);
            if (lv >= d.MaxLevel) { why = "已满级"; return false; }
            int gate = ForgeCatalog.Gate(line, lv);
            if (gate > GameConstants.ChapterStageCount * GameConstants.MaxStar)
            {
                why = string.IsNullOrEmpty(d.LockNote) ? "暂未开放" : d.LockNote;
                return false;
            }
            if (TotalStars() < gate) { why = $"需 {gate} 星"; return false; }
            int cost = ForgeCatalog.Cost(line, lv);
            if (Ink < cost) { why = $"差 {cost - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        public bool BuyForge(int line)
        {
            if (!CanBuyForge(line, out _)) return false;
            Ink -= ForgeCatalog.Cost(line, ForgeLevel(line));
            Forge[line]++;
            Save();
            return true;
        }

        public bool CanBuySkin(int i, out string why)
        {
            SkinDef d = SkinCatalog.Get(i);
            if (SkinOwned[i]) { why = ""; return false; }
            if (d.NeedClear && !ChapterCleared) { why = "通关解锁"; return false; }
            if (TotalStars() < d.Gate) { why = $"需 {d.Gate} 星"; return false; }
            if (Ink < d.Price) { why = $"差 {d.Price - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        public bool BuySkin(int i)
        {
            if (!CanBuySkin(i, out _)) return false;
            Ink -= SkinCatalog.Get(i).Price;
            SkinOwned[i] = true;
            Skin = i;
            Save();
            return true;
        }

        public void EquipSkin(int i)
        {
            if (i < 0 || i >= SkinCatalog.Count || !SkinOwned[i]) return;
            Skin = i;
            Save();
        }

        public bool CanBuySpell(int i, out string why)
        {
            SpellDef d = SpellCatalog.Get(i);
            if (SpellOwned[i]) { why = ""; return false; }
            if (d.NeedClear && !ChapterCleared) { why = "通关解锁"; return false; }
            if (TotalStars() < d.Gate) { why = $"需 {d.Gate} 星"; return false; }
            if (Ink < d.Price) { why = $"差 {d.Price - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        public bool BuySpell(int i)
        {
            if (!CanBuySpell(i, out _)) return false;
            Ink -= SpellCatalog.Get(i).Price;
            SpellOwned[i] = true;
            Equip(i);
            Save();
            return true;
        }

        public int EquippedSlot(int spell)
        {
            for (int s = 0; s < Equipped.Length; s++)
                if (Equipped[s] == spell) return s;
            return -1;
        }

        // 点一下就上/下阵：已装备的取下，没装备的填第一个空槽，满了就顶掉第二格。
        public void Equip(int spell)
        {
            if (spell < 0 || spell >= SpellCatalog.Count || !SpellOwned[spell]) return;
            int at = EquippedSlot(spell);
            if (at >= 0)
            {
                Equipped[at] = -1;
                if (at == 0 && Equipped[1] >= 0)
                {
                    Equipped[0] = Equipped[1];
                    Equipped[1] = -1;
                }
                Save();
                return;
            }
            if (Equipped[0] < 0) Equipped[0] = spell;
            else if (Equipped[1] < 0) Equipped[1] = spell;
            else Equipped[1] = spell;
            Save();
        }
    }
}
