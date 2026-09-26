using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InkLine
{
    // 挂在按钮上。能点就响纸，不能点就闷一下。预制体和后来生成的按钮都靠 AudioBus.Sweep 补上。
    public sealed class AudioTap : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            Button btn = GetComponent<Button>();
            if (btn != null && !btn.interactable) AudioBus.Deny();
            else AudioBus.Tap();
        }
    }
}
