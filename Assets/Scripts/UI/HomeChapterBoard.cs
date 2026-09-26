using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 出征页中间那张章节卡。一章 9 关，3 行，行与行之间是一条 S 弯。
    public sealed class HomeChapterBoard : MonoBehaviour
    {
        public Image Board;
        public Image Art;
        public Image Ribbon;
        public Text Title;
        public UiPath Route;
        public HomeSealCell[] Nodes;
        public Image[] Dots;
        public Button Prev;
        public Text PrevLabel;
        public Button Next;
        public Text NextLabel;
    }
}
