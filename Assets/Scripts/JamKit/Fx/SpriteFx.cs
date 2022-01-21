using System;
using System.Collections;
using UnityEngine;

namespace JamKit
{
    public enum SpriteFxType
    {
        OneShot,
        Infinite,
        Duration,
    }
    
    [CreateAssetMenu(fileName = "SpriteList", menuName = "Torreng/SpriteFx", order = 0)]
    public class SpriteFx : ScriptableObject
    {
        [SerializeField] private SpriteFxType _type = SpriteFxType.OneShot;
        public SpriteFxType Type => _type;
        
        [SerializeField] private Vector2 _offset = Vector3.zero;
        public Vector2 Offset => _offset;
        
        [SerializeField] private float _scale = 1;
        public float Scale => _scale;
        
        [SerializeField] private int _framesPerSecond = 8;
        public int FramesPerSecond => _framesPerSecond;

        [SerializeField, DrawIf("_type", SpriteFxType.Duration)] private float _duration = -1f;
        public float Duration => _duration;
        
        [SerializeField] private Sprite[] _sprites;
        public Sprite[] Sprites => _sprites;
        
        public static IEnumerator Play(SpriteFx fx, SpriteRenderer spriteRenderer)
        {
            spriteRenderer.transform.localScale *= fx.Scale;
            switch (fx.Type)
            {
                case SpriteFxType.OneShot:
                    foreach (Sprite sprite in fx.Sprites)
                    {
                        spriteRenderer.sprite = sprite;
                        yield return new WaitForSeconds(1f / fx.FramesPerSecond);
                    }

                    Destroy(spriteRenderer.gameObject);
                    break;
                case SpriteFxType.Infinite:
                    for (int i = 0; spriteRenderer != null && spriteRenderer.gameObject != null; i++)
                    {
                        spriteRenderer.sprite = fx.Sprites[i % fx.Sprites.Length];
                        yield return new WaitForSeconds(1f / fx.FramesPerSecond);
                    }

                    break;
                case SpriteFxType.Duration:
                    Debug.Assert(fx.Duration > 0f);
                    float secondsPerFrame = 1f / fx.FramesPerSecond;
                    int spriteIndex = 0;
                    float frameTimer = 0;
                    for (float f = 0f;
                        f < fx.Duration && spriteRenderer != null && spriteRenderer.gameObject != null;
                        f += Time.deltaTime, frameTimer += Time.deltaTime)
                    {
                        if (frameTimer > secondsPerFrame)
                        {
                            spriteRenderer.sprite = fx.Sprites[++spriteIndex % fx.Sprites.Length];
                            frameTimer = 0;
                        }

                        yield return null;
                    }

                    Destroy(spriteRenderer.gameObject);

                    break;
                default: throw new ArgumentOutOfRangeException($"{fx.Type}");
            }
        }
    }
}