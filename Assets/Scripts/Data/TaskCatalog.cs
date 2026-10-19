using System.Collections.Generic;

namespace InkLine
{
    public enum TaskKind
    {
        WinStage,
        Kills,
        KillsWith,
        Words,
        Pairs,
        TopStars,
        Items,
        Combo,
        EndlessWaves,
        Daily,
        Hard,
    }

    public readonly struct TaskDef
    {
        public readonly TaskKind Kind;
        public readonly int Need;
        public readonly string Text;

        public TaskDef(TaskKind kind, int need, string text)
        {
            Kind = kind;
            Need = need;
            Text = text;
        }
    }

    // 一局打完的局内计数，交给 MetaProgress.NoteBattle 记功课。
    public sealed class BattleTally
    {
        public bool Win;
        public bool Main;
        public bool Hard;
        public bool Daily;
        public bool Endless;
        public int Waves;
        public int Kills;
        public int[] KillsWith;
        public int Words;
        public int Pairs;
        public int TopStars;
        public int Items;
        public int Combo;
    }

    // 每日功课：每天三条，按日期和玩家进度挑。条目只推玩家去试没用过的打法，奖励只有墨和钻石。
    // 条目编码：低 8 位是 TaskKind，往上是「用某字打死」的那个字。
    public static class TaskCatalog
    {
        static readonly TaskDef[] Defs =
        {
            new TaskDef(TaskKind.WinStage, 2, "打赢 {0} 场主线关卡"),
            new TaskDef(TaskKind.Kills, 250, "消灭 {0} 只怪"),
            new TaskDef(TaskKind.KillsWith, 40, "用「{1}」字打死 {0} 只怪"),
            new TaskDef(TaskKind.Words, 2, "让词醒过来 {0} 次"),
            new TaskDef(TaskKind.Pairs, 1, "触发 {0} 种秘卷组合"),
            new TaskDef(TaskKind.TopStars, 2, "把字叠到三星 {0} 次"),
            new TaskDef(TaskKind.Items, 4, "放 {0} 次道具"),
            new TaskDef(TaskKind.Combo, 25, "一局里打出 {0} 连杀"),
            new TaskDef(TaskKind.EndlessWaves, 12, "无尽模式累计撑过 {0} 波"),
            new TaskDef(TaskKind.Daily, 1, "打 {0} 局每日残局"),
            new TaskDef(TaskKind.Hard, 1, "打赢 {0} 场精英"),
        };

        public static TaskDef Def(TaskKind k) => Defs[(int)k];

        public static TaskKind KindOf(int code) => (TaskKind)(code & 0xff);

        public static CardId GlyphOf(int code) => (CardId)(code >> 8);

        public static int Need(int code) => Def(KindOf(code)).Need;

        public static string Text(int code)
        {
            TaskDef d = Def(KindOf(code));
            string glyph = KindOf(code) == TaskKind.KillsWith ? CardCatalog.Get(GlyphOf(code)).Name : "";
            return string.Format(d.Text, d.Need, glyph);
        }

        // 每条给的墨跟着推到第几章走，前期一条够点一两级，后期不至于没感觉。
        public static int Ink(int chapter) => 40 + 10 * UnityEngine.Mathf.Clamp(chapter, 0, GameConstants.Chapters);

        public static int[] Roll(int day, MetaProgress meta)
        {
            var rng = new System.Random(DayClock.Hash(day, 4099));
            var pool = new List<TaskKind> { TaskKind.WinStage, TaskKind.Kills, TaskKind.TopStars, TaskKind.Combo };
            var glyphs = new List<CardId>();
            bool words = false;
            foreach (CardId id in meta.EventPool())
            {
                CardDef def = CardCatalog.Get(id);
                if (def.Wake == CardWake.WordPart) words = true;
                else glyphs.Add(id);
            }
            if (glyphs.Count > 0) pool.Add(TaskKind.KillsWith);
            if (words) pool.Add(TaskKind.Words);
            if (meta.ClearedCount() >= GameConstants.ChapterSize * 2) pool.Add(TaskKind.Pairs);
            if (meta.AnyItemEquipped) pool.Add(TaskKind.Items);
            if (meta.EndlessOpen) pool.Add(TaskKind.EndlessWaves);
            if (meta.DailyOpen) pool.Add(TaskKind.Daily);
            if (meta.AnyHardOpen) pool.Add(TaskKind.Hard);

            var codes = new int[GameConstants.TaskCount];
            for (int i = 0; i < codes.Length; i++)
            {
                TaskKind k = pool[rng.Next(pool.Count)];
                pool.Remove(k);
                int code = (int)k;
                if (k == TaskKind.KillsWith) code |= (int)glyphs[rng.Next(glyphs.Count)] << 8;
                codes[i] = code;
            }
            return codes;
        }

        // 这一局给这条功课添多少。连杀记的是单局最高，其余是累加。
        public static int Gain(int code, BattleTally t)
        {
            switch (KindOf(code))
            {
                case TaskKind.WinStage: return t.Win && t.Main ? 1 : 0;
                case TaskKind.Kills: return t.Kills;
                case TaskKind.KillsWith:
                    int g = (int)GlyphOf(code);
                    return t.KillsWith != null && g > 0 && g < t.KillsWith.Length ? t.KillsWith[g] : 0;
                case TaskKind.Words: return t.Words;
                case TaskKind.Pairs: return t.Pairs;
                case TaskKind.TopStars: return t.TopStars;
                case TaskKind.Items: return t.Items;
                case TaskKind.Combo: return t.Combo;
                case TaskKind.EndlessWaves: return t.Endless && !t.Daily ? t.Waves : 0;
                case TaskKind.Daily: return t.Daily ? 1 : 0;
                case TaskKind.Hard: return t.Win && t.Hard ? 1 : 0;
                default: return 0;
            }
        }

        public static bool Peak(int code) => KindOf(code) == TaskKind.Combo;
    }
}
