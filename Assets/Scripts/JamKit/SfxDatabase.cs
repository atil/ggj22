using System.Collections.Generic;
using UnityEngine;

namespace JamKit
{
    [CreateAssetMenu(fileName = "SfxDatabase", menuName = "Torreng/SfxDatabase", order = 0)]
    public class SfxDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioClip> _clips;
        public IReadOnlyList<AudioClip> Clips => _clips;
    }
}