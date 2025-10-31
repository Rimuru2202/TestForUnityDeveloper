using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Resources.Scripts
{
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public Slider musicSlider;

        public AudioSource musicSource;

        public List<AudioClip> musicPlaylist = new List<AudioClip>();

        public bool playOnStart = true;

        private const string MusicKey = "music_volume";
        private const float DefaultMusic = 0.7f;

        private Coroutine _musicLoopCoroutine;
        private int _playlistIndex;
        private bool _audioUnlocked;

        private void Awake()
        {
            var m = PlayerPrefs.HasKey(MusicKey) ? PlayerPrefs.GetFloat(MusicKey) : DefaultMusic;

            if (musicSlider != null) musicSlider.value = m;
            if (musicSource != null) musicSource.volume = m;

            if (musicSource != null)
                musicSource.loop = false;
        }

        private void OnEnable()
        {
            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }

        private void OnDisable()
        {
            if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);

            StopMusicLoop();
        }

        private void Start()
        {
#if UNITY_WEBGL
            _audioUnlocked = false;
            if (!playOnStart && musicSource != null)
                musicSource.Stop();
#else
            if (playOnStart && musicPlaylist != null && musicPlaylist.Count > 0)
            {
                StartMusicLoop();
            }
#endif
        }

        private void Update()
        {
#if UNITY_WEBGL
            if (_audioUnlocked || (!Input.GetMouseButtonDown(0) && Input.touchCount <= 0 && !Input.anyKeyDown)) return;
            _audioUnlocked = true;
            if (playOnStart && musicPlaylist is { Count: > 0 })
            {
                StartMusicLoop();
            }
#endif
        }

        private void OnMusicChanged(float value)
        {
            if (musicSource != null) musicSource.volume = value;
            PlayerPrefs.SetFloat(MusicKey, value);
            PlayerPrefs.Save();
        }
        
        public void ApplyMusicVolumeFromSlider()
        {
            if (musicSlider == null) return;
            var v = musicSlider.value;
            if (musicSource != null) musicSource.volume = v;
            PlayerPrefs.SetFloat(MusicKey, v);
            PlayerPrefs.Save();
        }

        private void StartMusicLoop()
        {
            if (musicPlaylist == null || musicPlaylist.Count == 0 || musicSource == null)
                return;

            StopMusicLoop();
            _musicLoopCoroutine = StartCoroutine(MusicLoopCoroutine());
        }

        private void StopMusicLoop()
        {
            if (_musicLoopCoroutine != null)
            {
                StopCoroutine(_musicLoopCoroutine);
                _musicLoopCoroutine = null;
            }

            if (musicSource != null)
                musicSource.Stop();
        }

        public void PlayNextTrack()
        {
            if (musicPlaylist == null || musicPlaylist.Count == 0 || musicSource == null) return;

            _playlistIndex = (_playlistIndex + 1) % musicPlaylist.Count;
            musicSource.clip = musicPlaylist[_playlistIndex];
            musicSource.Play();
        }

        public void PlayPreviousTrack()
        {
            if (musicPlaylist == null || musicPlaylist.Count == 0 || musicSource == null) return;

            _playlistIndex = (_playlistIndex - 1 + musicPlaylist.Count) % musicPlaylist.Count;
            musicSource.clip = musicPlaylist[_playlistIndex];
            musicSource.Play();
        }

        private IEnumerator MusicLoopCoroutine()
        {
            if (musicSource == null || musicPlaylist == null || musicPlaylist.Count == 0)
                yield break;

            _playlistIndex = Mathf.Clamp(_playlistIndex, 0, Mathf.Max(0, musicPlaylist.Count - 1));

            while (true)
            {
                var clip = musicPlaylist[_playlistIndex];
                if (clip == null)
                {
                    _playlistIndex = (_playlistIndex + 1) % musicPlaylist.Count;
                    yield return null;
                    continue;
                }

                musicSource.clip = clip;
                musicSource.Play();

                yield return new WaitWhile(() => musicSource.isPlaying);

                _playlistIndex = (_playlistIndex + 1) % musicPlaylist.Count;

                yield return null;
            }
        }
    }
}
