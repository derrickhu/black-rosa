using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 副本页的一张卡：横幅插画、飘带标题、左下三行字、右下进入键。排版在 Prefabs/Home 的 page_dungeon 里改。
    public sealed class DungeonCard : MonoBehaviour
    {
        public Image Frame;
        public Image Art;
        public Button ArtButton;
        public Image Lock;
        public Image Chip;
        public Text ChipLabel;
        public Image Ribbon;
        public Text Title;
        public Text Rule;
        public Text Best;
        public Text Plays;
        public Button Go;
        public Text GoLabel;
        public Image Dot;
    }
}
