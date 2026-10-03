using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 图鉴里的一张道具卡：品质卡框、品质名、图标、等级牌、已装标、名字、卡数进度条。
    public sealed class HomeItemCard : MonoBehaviour
    {
        public Image Frame;
        public Text Quality;
        public Image Halo;
        public Image Icon;
        public Image Badge;
        public Text Lv;
        public Image Worn;
        public Text WornText;
        public Text Name;
        public Image Track;
        public Image Fill;
        public Text BarText;
        public Image CardIcon;
        public Image CardItem;
        public Button Button;
    }
}
