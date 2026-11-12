using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页的一张卡：玩法底板、左边立绘、右边奖励和进入键。排版在 Prefabs/Home 的 page_dungeon 里改。
    public sealed class DungeonCard : MonoBehaviour
    {
        public Image Frame;
        public Image Art;
        public Button ArtButton;
        public Image Lock;
        public Image Chip;
        public Text ChipLabel;
        public Text Title;
        public Image PlaysBar;
        public Text Plays;
        public Button Plus;
        public Button Go;
        public Text GoLabel;
        public Image Dot;
    }
}
