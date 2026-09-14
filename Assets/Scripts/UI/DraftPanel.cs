using System;
using UnityEngine;

namespace InkLine
{
    public static class DraftPanel
    {
        public static RectTransform Show(RectTransform layer, string title, CardId[] offer, bool rerolled, Action<CardId> pick, Action reroll)
        {
            // 一律运行时搭，不再走 Resources/UI/DraftPanel 预制体。
            // UiKit 的圆角/描边/投影都是 UiSprites 在运行时烘的 Texture2D，不是工程资源，
            // 预制体序列化时这类引用一律变 null（烘出来 25 个 m_Sprite 全是 fileID: 0），
            // 结果整屏退化成没有圆角的裸矩形。其余各屏本来也都是运行时搭的。
            DraftView view = DraftView.BuildTemplate(layer);
            UiKit.ApplyTo(view.transform);
            view.Bind(title, offer, rerolled, pick, reroll);
            return view.GetComponent<RectTransform>();
        }
    }
}
