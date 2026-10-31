namespace InkLine
{
    // 一局无尽入账的明细。MetaProgress.ApplyEndless 算好，结算页只拿来演。
    public struct EndlessResult
    {
        public int Waves;       // 打完几波
        public int OldBest;     // 这局之前的最好成绩
        public bool NewBest;
        public int Ink;         // 已入账的墨：局内拾到的 + 每波给的
        public int Chest;       // 宝箱品阶，-1 没有
        public int ChestSlot;   // 进了哪个位；-1 位满先寄着，-3 寄不下折成墨，-2 没箱子
        public bool HasSeal;    // 每日残局必得的那一枚，已经进背包
        public SealDrop Seal;
        public SealSpin Spin;   // 结算转轮停在哪三枚，Seal 就是它给的那一枚
    }
}
