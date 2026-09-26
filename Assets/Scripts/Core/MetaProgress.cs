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
        public int[] Stars = new int[GameConstants.StageCount];
        public int[] Forge = new int[ForgeCatalog.LineCount];
        public int Skin;
        public bool[] SkinOwned = new bool[SkinCatalog.Count];
        public bool[] SpellOwned = new bool[SpellCatalog.Count];
        public int[] SpellLevel = new int[SpellCatalog.Count];
        public int[] SpellShards = new int[SpellCatalog.Count];
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
            Ink += ClearedCount() * 12;
        }

        void Normalize()
        {
            Stars = Fit(Stars, GameConstants.StageCount);
            Forge = Fit(Forge, ForgeCatalog.LineCount);
            SkinOwned = Fit(SkinOwned, SkinCatalog.Count);
            SpellOwned = Fit(SpellOwned, SpellCatalog.Count);
            SpellLevel = Fit(SpellLevel, SpellCatalog.Count);
            SpellShards = Fit(SpellShards, SpellCatalog.Count);
            Equipped = Fit(Equipped, GameConstants.SpellSlots);
            for (int i = 0; i < Forge.Length; i++)
                Forge[i] = Mathf.Clamp(Forge[i], 0, ForgeCatalog.MaxLevel(i));
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = Mathf.Clamp(Stars[i], 0, GameConstants.MaxStar);
            SkinOwned[0] = true;
            for (int i = 0; i < SpellShards.Length; i++)
            {
                // 老存档只有「已解锁」布尔。升到 1 级，碎片进度按下一阶重新算。
                if (SpellOwned[i] && SpellLevel[i] <= 0) SpellLevel[i] = 1;
                SpellLevel[i] = Mathf.Clamp(SpellLevel[i], 0, SpellCatalog.MaxLevel);
                SpellOwned[i] = SpellLevel[i] > 0;
                int cap = SpellCatalog.NextShards(SpellCatalog.Get(i), SpellLevel[i]);
                SpellShards[i] = Mathf.Clamp(SpellShards[i], 0, Mathf.Max(0, cap));
            }
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

        public static void Wipe()
        {
            PlayerPrefs.DeleteKey(KeyV2);
            PlayerPrefs.DeleteKey(KeyV1);
            PlayerPrefs.Save();
        }

        public void FillStamina()
        {
            Stamina = GameConstants.StaminaMax;
            StaminaTick = DateTime.UtcNow.Ticks;
            Save();
        }

        public void GrantShards(int n)
        {
            if (n <= 0 || SpellShards == null) return;
            for (int i = 0; i < SpellShards.Length && i < SpellCatalog.Count; i++)
            {
                int cap = SpellCatalog.NextShards(SpellCatalog.Get(i), SpellRank(i));
                if (cap <= 0) continue;
                SpellShards[i] = Mathf.Clamp(SpellShards[i] + n, 0, cap);
            }
            Save();
        }

        public void UnlockStages()
        {
            for (int i = 0; i < Stars.Length; i++)
                Stars[i] = GameConstants.MaxStar;
            Save();
        }

        public void UnlockSkins()
        {
            for (int i = 0; i < SkinOwned.Length; i++)
                SkinOwned[i] = true;
            Save();
        }

        public void UnlockSpells()
        {
            for (int i = 0; i < SpellLevel.Length && i < SpellCatalog.Count; i++)
            {
                if (SpellLevel[i] > 0) continue;
                SpellLevel[i] = 1;
                SpellOwned[i] = true;
            }
            Save();
        }

        public void MaxForge()
        {
            for (int i = 0; i < Forge.Length && i < ForgeCatalog.LineCount; i++)
                Forge[i] = ForgeCatalog.MaxLevel(i);
            Save();
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

        // 进关就收，打过没打过一样。失败、中途回首页都不退。
        public int StageCost(int stage)
        {
            if (stage < 0 || stage >= Stars.Length) return 0;
            return StageCatalog.Get(stage).StaminaCost;
        }

        public bool CanEnter(int stage) => Stamina >= StageCost(stage);

        public void SpendStamina(int n)
        {
            if (n <= 0) return;
            if (Stamina >= GameConstants.StaminaMax) StaminaTick = DateTime.UtcNow.Ticks;
            Stamina = Mathf.Max(0, Stamina - n);
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

        // 已通关几关。锻造、皮肤的门槛都按这个数。
        public int ClearedCount()
        {
            int n = 0;
            for (int i = 0; i < Stars.Length; i++) if (Stars[i] > 0) n++;
            return n;
        }

        // chapter 从 0 数。
        public bool ChapterCleared(int chapter)
        {
            int last = (chapter + 1) * GameConstants.ChapterSize - 1;
            return last >= 0 && last < Stars.Length && Stars[last] > 0;
        }

        // 皮肤、技能上的「通关解锁」指通关第三章。
        public const int ClearGateChapter = 2;

        public ForgeStats Forged
        {
            get
            {
                return ForgeCatalog.Stats(Forge);
            }
        }
        public int StartEmitters => Forged.Emitters;
        public int StartGold => Forged.StartGold;
        public Color SkinTint => SkinCatalog.Get(Skin).Tint;

        // 章底首通的额外奖励，结算页拿去显示。ApplyResult 每次先清掉。
        [NonSerialized] public int FinaleStamina;
        [NonSerialized] public int FinaleShard = -1;

        // 结算的墨就是这一局亲手拾到的墨，关卡不再另发一笔。返回入账数，
        // GameFlow 拿去显示，也拿去算广告双倍要补多少。
        public int ApplyResult(int stage, int collected)
        {
            FinaleStamina = 0;
            FinaleShard = -1;
            if (stage < 0 || stage >= Stars.Length) return 0;
            StageDef def = StageCatalog.Get(stage);
            bool first = Stars[stage] <= 0;
            if (first && def.Finale)
            {
                FinaleStamina = GameConstants.FinaleStamina;
                Stamina = Mathf.Min(GameConstants.StaminaMax, Stamina + FinaleStamina);
                FinaleShard = GrantOneShard();
            }
            // Stars 只剩「这关过没过」一层意思，旧存档里的 2、3 照样算通关。
            if (first) Stars[stage] = 1;
            int ink = Mathf.Max(0, collected);
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

        // 章底保底：随机挑一个还没攒满的技能给一枚碎片。全满了返回 -1。
        int GrantOneShard()
        {
            SpellShards = Fit(SpellShards, SpellCatalog.Count);
            SpellLevel = Fit(SpellLevel, SpellCatalog.Count);
            var room = new System.Collections.Generic.List<int>();
            for (int i = 0; i < SpellCatalog.Count; i++)
            {
                if (SpellLevel[i] >= SpellCatalog.MaxLevel) continue;
                int cap = SpellCatalog.NextShards(SpellCatalog.Get(i), SpellLevel[i]);
                if (SpellShards[i] < cap) room.Add(i);
            }
            if (room.Count == 0) return -1;
            int pick = room[UnityEngine.Random.Range(0, room.Count)];
            SpellShards[pick]++;
            return pick;
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
            if (gate > GameConstants.StageCount)
            {
                why = string.IsNullOrEmpty(d.LockNote) ? "暂未开放" : d.LockNote;
                return false;
            }
            if (ClearedCount() < gate) { why = $"通关 {gate} 关"; return false; }
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
            if (d.NeedClear && !ChapterCleared(ClearGateChapter)) { why = "通关三章解锁"; return false; }
            if (ClearedCount() < d.Gate) { why = $"通关 {d.Gate} 关"; return false; }
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

        public int SpellShardCount(int i)
        {
            if (SpellShards == null || i < 0 || i >= SpellShards.Length) return 0;
            return SpellShards[i];
        }

        public int SpellRank(int i)
        {
            if (SpellLevel == null || i < 0 || i >= SpellLevel.Length) return 0;
            return SpellLevel[i];
        }

        // 通关后把这一局 boss 掉的碎片记进存档。满级的不再收，没满级的继续攒下一阶。
        public void AddShards(int[] got)
        {
            if (got == null) return;
            SpellShards = Fit(SpellShards, SpellCatalog.Count);
            SpellLevel = Fit(SpellLevel, SpellCatalog.Count);
            bool any = false;
            for (int i = 0; i < got.Length && i < SpellShards.Length; i++)
            {
                if (got[i] <= 0 || SpellLevel[i] >= SpellCatalog.MaxLevel) continue;
                int cap = SpellCatalog.NextShards(SpellCatalog.Get(i), SpellLevel[i]);
                int next = Mathf.Min(cap, SpellShards[i] + got[i]);
                if (next == SpellShards[i]) continue;
                SpellShards[i] = next;
                any = true;
            }
            if (any) Save();
        }

        public bool CanBuySpell(int i, out string why)
        {
            if (i < 0 || i >= SpellCatalog.Count) { why = ""; return false; }
            int rank = SpellRank(i);
            if (rank >= SpellCatalog.MaxLevel) { why = "已满级"; return false; }
            SpellDef d = SpellCatalog.Get(i);
            int need = SpellCatalog.NextShards(d, rank);
            int have = SpellShardCount(i);
            if (have < need) { why = have + "/" + need; return false; }
            int price = SpellCatalog.NextPrice(d, rank);
            if (Ink < price) { why = $"差 {price - Ink} 墨"; return false; }
            why = "";
            return true;
        }

        public bool BuySpell(int i)
        {
            if (!CanBuySpell(i, out _)) return false;
            int rank = SpellRank(i);
            SpellDef d = SpellCatalog.Get(i);
            Ink -= SpellCatalog.NextPrice(d, rank);
            SpellShards[i] = 0;
            SpellLevel[i] = rank + 1;
            SpellOwned[i] = true;
            if (rank <= 0) Equip(i);
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
