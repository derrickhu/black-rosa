using System;
using UnityEngine;

namespace InkLine
{
    public static class DraftPanel
    {
        public static RectTransform Show(RectTransform layer, string title, CardId[] offer, bool rerolled, Action<CardId> pick, Action reroll)
        {
            DraftView view = null;
            GameObject prefab = Resources.Load<GameObject>("UI/DraftPanel");
            if (prefab != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, layer, false);
                go.name = "DraftPanel";
                view = go.GetComponent<DraftView>();
            }
            if (view == null)
                view = DraftView.BuildTemplate(layer);
            UiKit.ApplyTo(view.transform);
            view.Bind(title, offer, rerolled, pick, reroll);
            return view.GetComponent<RectTransform>();
        }
    }
}
