using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 道具栏的一格：木托盘、品质光晕、道具图标、等级牌；锁着时换灰托盘加锁和章节说明。
    public sealed class HomeItemSlot : MonoBehaviour
    {
        public Image Glow;
        public Image Frame;
        public Image Halo;
        public Image Icon;
        public Image Lock;
        public Text Plus;
        public Image Badge;
        public Text Lv;
        public Text Note;
        public Button Button;
    }
}
