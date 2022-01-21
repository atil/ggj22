using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace JamKit
{
    public class Sfx : SingletonBehaviour<Sfx>
    {
        private AudioSource _commonAudioSource;
        private AudioSource _musicAudioSource;

        private const float SfxVolume = 0.5f;
        private const float MusicVolume = 0.5f;

        private SfxDatabase _database;
        private bool _isMusicPaused;

        private void Awake()
        {
            _commonAudioSource = gameObject.AddComponent<AudioSource>();
            _commonAudioSource.volume = SfxVolume;
            _musicAudioSource = gameObject.AddComponent<AudioSource>();
            _musicAudioSource.volume = MusicVolume;
            _database = Resources.Load<SfxDatabase>("Sfx/SfxDatabase");
        }

        public void FollowTransform(Transform t)
        {
            transform.SetParent(t);
        }

        [CanBeNull]
        private AudioClip GetClip(string clipName)
        {
            foreach (AudioClip audioClip in _database.Clips)
            {
                if (audioClip == null)
                {
                    Debug.LogWarning("There's a null clip in the sfx database");
                    continue;
                }

                if (audioClip.name == clipName)
                {
                    return audioClip;
                }
            }

            Debug.LogError($"Audioclip not found: {clipName}");
            return null;
        }

        public void StartMusic(string clipName, bool isLooped)
        {
            AudioClip clip = GetClip(clipName);
            if (clip != null)
            {
                _musicAudioSource.loop = isLooped;
                _musicAudioSource.clip = clip;
                _musicAudioSource.volume = _isMusicPaused ? 0f : MusicVolume;
                _musicAudioSource.Play();
            }
        }

        public void PlayRandom(string clipPrefix)
        {
            List<AudioClip> clips = _database.Clips.Where(x => x.name.StartsWith(clipPrefix)).ToList();

            if (clips.Count > 0)
            {
                _commonAudioSource.PlayOneShot(clips.GetRandom());
            }
            else
            {
                Debug.LogError($"Couldn't find any clips with prefix {clipPrefix}");
            }
        }

        public void Play(string clipName)
        {
            AudioClip clip = GetClip(clipName);
            if (clip != null)
            {
                _commonAudioSource.PlayOneShot(clip);
            }
        }

        public void ChangeMusicTrack(string clipName)
        {
            _musicAudioSource.clip = GetClip(clipName);
            _musicAudioSource.volume = MusicVolume;
            _musicAudioSource.Play();
        }

        public void FadeOutMusic(float duration)
        {
            Curve.Tween(AnimationCurve.Linear(0f, 0f, 1f, 1f),
                duration,
                t => { _musicAudioSource.volume = Mathf.Lerp(MusicVolume, 0f, t); },
                () => { _musicAudioSource.volume = 0f; });
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (_musicAudioSource.volume > 0.01f)
                {
                    _musicAudioSource.volume = 0f;
                    _isMusicPaused = true;
                }
                else
                {
                    _musicAudioSource.volume = MusicVolume;
                    _isMusicPaused = false;
                }
            }
        }
    }
}