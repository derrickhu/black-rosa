using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 首页三页的壳。排版在 Prefabs/Home 里改，代码只填数、挂钩子。
    public sealed class HomeView : MonoBehaviour
    {
        public Text Stamina;
        public Text Ink;
        public Text StamTip;
        public Button[] Tabs;
        public Image TabDock;
        public GameObject ForgePage;
        public GameObject SortiePage;
        public GameObject SpellPage;

        public Image Board;
        public Text GunSummary;
        public HomeSkinCell[] Skins;
        public HomeBoostRow[] Boosts;

        public Image Logo;
        public Button GoButton;
        public Text GoLabel;
        public Button AdButton;
        public Text AdLabel;
        public Text Help;
        public HomeSealCell[] Seals;

        public HomeSpellSlot[] Equipped;
        public Text SpellTip;
        public HomeSpellCard[] Spells;
    }
}
