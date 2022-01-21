using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JamKit
{
    public static class Util
    {
        public static Vector3 WithX(this Vector3 v, float x)
        {
            return new Vector3(x, v.y, v.z);
        }

        public static Vector3 WithY(this Vector3 v, float y)
        {
            return new Vector3(v.x, y, v.z);
        }

        public static Vector3 WithZ(this Vector3 v, float z)
        {
            return new Vector3(v.x, v.y, z);
        }

        public static Vector3 ToHorizontal(this Vector3 v)
        {
            return Vector3.ProjectOnPlane(v, Vector3.down);
        }

        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        }

        public static float VerticalComponent(this Vector3 v)
        {
            return Vector3.Dot(v, Vector3.up);
        }

        public static Vector3 TransformDirectionHorizontal(this Transform t, Vector3 v)
        {
            return t.TransformDirection(v).ToHorizontal().normalized;
        }

        public static Vector3 InverseTransformDirectionHorizontal(this Transform t, Vector3 v)
        {
            return t.InverseTransformDirection(v).ToHorizontal().normalized;
        }

        public static bool InBetween(this float val, float a, float b)
        {
            return (val < b && val > a) || (val < a && val > b);
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static bool Approx(float a, float b)
        {
            return Mathf.Abs(a - b) < 0.0001f;
        }

        public static bool IsAxisAligned(Transform bender1, Transform bender2)
        {
            var v1 = bender1.position;
            var v2 = bender2.position;

            var result = new Vector3(Mathf.FloorToInt(v1.x) ^ Mathf.FloorToInt(v2.x),
                Mathf.FloorToInt(v1.y) ^ Mathf.FloorToInt(v2.y),
                Mathf.FloorToInt(v1.z) ^ Mathf.FloorToInt(v2.z));

            if (!Approx(result.x, 0f)) result.x = 1f;
            if (!Approx(result.y, 0f)) result.y = 1f;
            if (!Approx(result.z, 0f)) result.z = 1f;

            return Approx(result.sqrMagnitude, 1f);
        }

        public static Vector3 AlignedDir(Transform tile1, Transform tile2)
        {
            var p1 = tile1.position;
            var p2 = tile2.position;

            if (Approx(p1.y, p2.y) && Approx(p1.z, p2.z))
            {
                return Vector3.right;
            }

            if (Approx(p1.x, p2.x) && Approx(p1.z, p2.z))
            {
                return Vector3.up;
            }

            return Vector3.forward;
        }

        public static void SetAlpha(this Material m, float a)
        {
            Color c = m.color;
            c.a = a;
            m.color = c;
        }

        public static void SetAlpha(this Image m, float a)
        {
            Color c = m.color;
            c.a = a;
            m.color = c;
        }

        public static bool Intersects(this Bounds b, Plane p)
        {
            Vector3 boundPoint1 = b.min;
            Vector3 boundPoint2 = b.max;
            Vector3 boundPoint3 = new Vector3(boundPoint1.x, boundPoint1.y, boundPoint2.z);
            Vector3 boundPoint4 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint1.z);
            Vector3 boundPoint5 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint1.z);
            Vector3 boundPoint6 = new Vector3(boundPoint1.x, boundPoint2.y, boundPoint2.z);
            Vector3 boundPoint7 = new Vector3(boundPoint2.x, boundPoint1.y, boundPoint2.z);
            Vector3 boundPoint8 = new Vector3(boundPoint2.x, boundPoint2.y, boundPoint1.z);

            Vector3[] vertices = new[]
            {
                boundPoint1,
                boundPoint2,
                boundPoint3,
                boundPoint4,
                boundPoint5,
                boundPoint6,
                boundPoint7,
                boundPoint8,
            };

            // Check if all vertices are on the same side of the plane
            List<bool> sides = new List<bool>(8);
            foreach (Vector3 v in vertices)
            {
                sides.Add(p.GetSide(v));
            }

            return !sides.TrueForAll(x => x) && !sides.TrueForAll(x => !x); // TODO PERF EDITOR: Convert these to regular loops
        }

        public static Vector4 PlaneEquation(Vector3 pos, Vector3 norm)
        {
            float d = norm.x * pos.x + norm.y * pos.y + norm.z * pos.z;
            d = -d;
            return new Vector4(norm.x, norm.y, norm.z, d);
        }

        public static Color Opaque(this Color c)
        {
            return new Color(c.r, c.g, c.b, 1f);
        }

        public static Color Transparent(this Color c)
        {
            return new Color(c.r, c.g, c.b, 0f);
        }

        public static Color WithAlpha(this Color c, float a)
        {
            return new Color(c.r, c.g, c.b, a);
        }

        public static bool TryGetComponent<T>(this Component c, out T result) where T : Component
        {
#if UNITY_EDITOR
            Debug.Assert(c != null, "This has never been a valid case");
#endif
            result = c.GetComponent<T>();
            return result != null;
        }

        public static bool TryGetComponentInChildren<T>(this Component c, out T result) where T : Component
        {
#if UNITY_EDITOR
            Debug.Assert(c != null, "This has never been a valid case");
#endif
            result = c.GetComponentInChildren<T>(true);
            return result != null;
        }

        public static bool TryGetEquals<K, V>(this Dictionary<K, V> dict, K key, V value)
        {
            return dict.TryGetValue(key, out V v) && v.Equals(value);
        }

        public static bool TryGetGreaterThan<K, V>(this Dictionary<K, V> dict, K key, V value) where V : IComparable<V>
        {
            return dict.TryGetValue(key, out V v) && v.CompareTo(value) > 0;
        }

        public static bool TryGetChild(this Transform t, int childIndex, out Transform child)
        {
            if (childIndex >= t.childCount)
            {
                child = null;
                return false;
            }

            child = t.GetChild(childIndex);
            return true;
        }

        public static T GetRandom<T>(this IList<T> list)
        {
            if (list.Count == 0)
            {
                return default;
            }

            if (list.Count == 1)
            {
                return list[0];
            }

            return list[UnityEngine.Random.Range(0, list.Count)];
        }
    }
}