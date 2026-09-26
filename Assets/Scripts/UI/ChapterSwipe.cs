using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InkLine
{
    // 手指往左滑，进入下一章。往右滑回到上一章。
    public sealed class ChapterSwipe : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        public Action<int> Moved;
        Vector2 _start;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _start = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (Moved == null) return;
            float dx = eventData.position.x - _start.x;
            if (dx <= -56f) Moved(1);
            else if (dx >= 56f) Moved(-1);
        }
    }
}
