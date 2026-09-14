using System.Collections.Generic;
using UnityEngine;

namespace InkLine
{
    // 死亡表现。一只怪没了，屏幕上要留下三样东西：
    //   1. 白色剪影炸开一下 —— 「它没了」这句话必须在 0.2 秒内说完；
    //   2. 地上一摊墨 —— 战场要记得这里死过人，割草才有痕迹；
    //   3. 几滴甩出去的墨点 —— 摊子本身太规整，有碎点才像溅开的。
    public static class InkSpill
    {
        const int Pool = 18;
        static readonly List<InkStain> _pool = new List<InkStain>();
        static Transform _root;

        public static void BindRoot(Transform root)
        {
            _root = root;
            _pool.Clear();
        }

        public static void Play(DeathFx fx)
        {
            if (_root == null) return;
            Rent().Play(fx);
        }

        static InkStain Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null && !_pool[i].Busy) return _pool[i];
            if (_pool.Count >= Pool)
            {
                // 满了就抢最老的那一摊。墨迹本来就是会盖掉的东西。
                InkStain oldest = _pool[0];
                _pool.RemoveAt(0);
                _pool.Add(oldest);
                return oldest;
            }
            var go = new GameObject("stain");
            go.transform.SetParent(_root, false);
            var stain = go.AddComponent<InkStain>();
            _pool.Add(stain);
            return stain;
        }
    }

    public sealed class InkStain : MonoBehaviour
    {
        const int Flecks = 6;
        const float FlashLife = 0.22f;
        const float PoolLife = 3.2f;

        SpriteRenderer _flash;
        SpriteRenderer _pool;
        SpriteRenderer[] _fleck;
        Vector2[] _vel;
        Vector2[] _at;
        float _t;
        float _size = 1f;
        bool _on;

        public bool Busy => _on;

        public void Play(DeathFx fx)
        {
            Ensure();
            transform.position = new Vector3(fx.Pos.x, fx.Pos.y, 0f);
            _t = 0f;
            _size = Mathf.Max(0.34f, fx.Radius) * (fx.Boss ? 3.4f : 2.4f);

            // 剪影用敌人自己那张图的纯白版，形状对得上才读得出是「谁」没了。
            _flash.sprite = InkSprites.Flash(fx.Type);
            _flash.enabled = _flash.sprite != null;

            for (int i = 0; i < Flecks; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float speed = Random.Range(0.9f, 2.6f) * (fx.Boss ? 1.6f : 1f);
                _vel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.7f) * speed;
                _at[i] = Vector2.zero;
                _fleck[i].transform.localScale = Vector3.one * (_size * Random.Range(0.08f, 0.2f));
            }

            _on = true;
            enabled = true;
            gameObject.SetActive(true);
        }

        void LateUpdate()
        {
            if (!_on || _pool == null || _fleck == null)
            {
                _on = false;
                enabled = false;
                return;
            }
            float dt = Time.unscaledDeltaTime;
            _t += dt;

            // 剪影：撑大一圈、一瞬间白掉
            float fu = Mathf.Clamp01(_t / FlashLife);
            if (_flash.sprite != null)
            {
                _flash.enabled = fu < 1f;
                _flash.transform.localScale = Vector3.one * (_size * Mathf.Lerp(0.42f, 0.72f, fu));
                _flash.color = new Color(1f, 1f, 1f, (1f - fu) * 0.95f);
            }

            // 墨摊：先泼开，再慢慢被纸吃掉
            float pu = Mathf.Clamp01(_t / PoolLife);
            float grow = Mathf.Min(1f, _t / 0.2f);
            float spread = _size * Mathf.Lerp(0.35f, 1f, grow * grow);
            _pool.enabled = pu < 1f;
            _pool.transform.localPosition = new Vector3(0f, -_size * 0.16f, 0f);
            // 压扁成椭圆，看着才是躺在地上而不是浮在半空
            _pool.transform.localScale = new Vector3(spread, spread * 0.42f, 1f);
            Color ink = InkTheme.Ink;
            ink.a = 0.46f * Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(0.45f, 1f, pu));
            _pool.color = ink;

            // 碎点：甩出去，落回地面，一起淡掉
            float flu = Mathf.Clamp01(_t / 0.85f);
            for (int i = 0; i < Flecks; i++)
            {
                _vel[i].y -= 7f * dt;
                _at[i] += _vel[i] * dt;
                if (_at[i].y < -_size * 0.2f) { _at[i].y = -_size * 0.2f; _vel[i] = Vector2.zero; }
                _fleck[i].enabled = flu < 1f;
                _fleck[i].transform.localPosition = new Vector3(_at[i].x, _at[i].y, 0f);
                Color c = InkTheme.Ink;
                c.a = 0.75f * (1f - flu);
                _fleck[i].color = c;
            }

            if (pu < 1f) return;
            _flash.enabled = false;
            _pool.enabled = false;
            for (int i = 0; i < Flecks; i++) _fleck[i].enabled = false;
            _on = false;
            enabled = false;
        }

        void Ensure()
        {
            if (_pool != null && _fleck != null) return;
            // 域重载会把引用清空但子节点还挂着，先清一遍再建，免得越积越多。
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            // 墨摊压在格子底纹之上、走怪之下 —— 盖住网格会让人以为格子锁了。
            _pool = Make("pool", InkFx.Splat(), 1);
            _fleck = new SpriteRenderer[Flecks];
            _vel = new Vector2[Flecks];
            _at = new Vector2[Flecks];
            for (int i = 0; i < Flecks; i++) _fleck[i] = Make("fleck" + i, InkFx.Splat(), 2);
            _flash = Make("flash", null, 8);
        }

        SpriteRenderer Make(string name, Sprite sprite, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            InkFx.PaintSprite(sr, Color.white);
            sr.enabled = false;
            return sr;
        }
    }
}
