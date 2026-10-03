using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具页的壳：上面道具栏木架三格，下面图鉴九张卡。ItemPageBuilder 烘进 Home.prefab。
    public sealed class HomeItemView : MonoBehaviour
    {
        public Image Shelf;
        public Text Title;
        public HomeItemSlot[] Slots;
        public RectTransform Head;
        public Text HeadTitle;
        public Text HeadCount;
        public Text SwapHint;
        public RectTransform SwapCancel;
        public ScrollRect Scroll;
        public RectTransform List;
        public HomeItemCard[] Cards;
    }
}
