using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    public sealed class HomeSpellCard : MonoBehaviour
    {
        public Image Card;
        public Image Icon;
        public Text Name;
        public Text Desc;
        public Text Cost;
        public Image PriceBack;
        public Text State;
        public Button Button;
        [System.NonSerialized] public HomeSpellRow Row;
    }
}
