using UnityEngine;

namespace InkLine
{
    // 挂在敌人身上的血条。三层：底槽、延迟红条、实心墨条。
    //
    // 中间那条延迟红条是关键 —— 血直接掉下去，玩家只看得到「现在剩多少」，
    // 看不到「刚才这一下打掉多少」。让红条慢半拍追下来，每一发的份量才有得读。
    public sealed class InkBar : MonoBehaviour
    {
        Transform _rig;
        SpriteRenderer _edge;
        SpriteRenderer _track;
        SpriteRenderer _ghost;
        SpriteRenderer _fill;
        float _shown = 1f;
        float _hold;

        public void Sync(EnemyActor e, int order)
        {
            float ratio = e.MaxHp > 0.01f ? Mathf.Clamp01(e.Hp / e.MaxHp) : 0f;
            // 满血的杂兵不挂条。割草关一屏二十只，全挂上去满屏都是横杠，
            // 反而把「谁快死了」这条唯一有用的信息淹掉了。关底永远挂。
            bool show = e.IsBoss || ratio < 0.999f;
            if (!show)
            {
                _shown = 1f;
                Quiet();
                return;
            }
            Ensure(order);

            // 本体每帧在做呼吸缩放，条要反着缩回去才不跟着抖。
            Vector3 p = transform.localScale;
            _rig.localScale = new Vector3(
                p.x > 0.001f ? 1f / p.x : 1f,
                p.y > 0.001f ? 1f / p.y : 1f, 1f);

            float dt = Time.unscaledDeltaTime;
            if (ratio < _shown - 0.0001f)
            {
                // 挨打先僵一下再追，那一截红条就是这一发的伤害量。
                if (_hold <= 0f) _hold = 0.18f;
                _hold -= dt;
                if (_hold <= 0f) _shown = Mathf.Max(ratio, _shown - dt * 1.5f);
            }
            else
            {
                _shown = ratio;
                _hold = 0f;
            }

            float w = Mathf.Clamp(e.Radius * 2.7f, 0.52f, 1.9f);
            float h = e.IsBoss ? 0.15f : 0.085f;
            float y = e.Radius + (e.IsBoss ? 0.34f : 0.24f);
            float rim = h * 0.4f;

            // 空槽必须比实心条浅得多。整条都是深色的话，条永远看着是满的 ——
            // 玩家读的是「深浅的边界在哪」，不是「条有多长」。
            Lay(_edge, -w * 0.5f - rim, y, w + rim * 2f, h + rim * 2f, InkTheme.Ink, 0.88f);
            Lay(_track, -w * 0.5f, y, w, h, InkTheme.Bone, 1f);
            Lay(_ghost, -w * 0.5f, y, w * _shown, h, InkTheme.Heart, 0.95f);
            Lay(_fill, -w * 0.5f, y, w * ratio, h, FillColor(e, ratio), 1f);
        }

        static Color FillColor(EnemyActor e, float ratio)
        {
            if (e.IsBoss) return InkTheme.Word;
            // 狂化那几只过半血会加速，条同时转成警示色 —— 染红的身体和条说的是同一件事。
            if (e.Colored) return InkTheme.Explode;
            return ratio < 0.34f ? InkTheme.Fire : InkTheme.Ink;
        }

        public void Quiet()
        {
            if (_rig != null && _rig.gameObject.activeSelf) _rig.gameObject.SetActive(false);
        }

        void Lay(SpriteRenderer sr, float x, float y, float w, float h, Color c, float a)
        {
            if (sr == null) return;
            bool on = w > 0.004f;
            sr.enabled = on;
            if (!on) return;
            sr.transform.localPosition = new Vector3(x, y, 0f);
            sr.transform.localScale = new Vector3(w, h / InkFx.PillH, 1f);
            c.a = a;
            sr.color = c;
        }

        void Ensure(int order)
        {
            if (_rig == null)
            {
                var go = new GameObject("bar");
                go.transform.SetParent(transform, false);
                _rig = go.transform;
                _edge = Spawn("edge", order);
                _track = Spawn("track", order + 1);
                _ghost = Spawn("ghost", order + 2);
                _fill = Spawn("fill", order + 3);
            }
            if (!_rig.gameObject.activeSelf) _rig.gameObject.SetActive(true);
        }

        SpriteRenderer Spawn(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rig, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = InkFx.Pill();
            sr.sortingOrder = order;
            InkFx.PaintSprite(sr, Color.white);
            return sr;
        }
    }
}
