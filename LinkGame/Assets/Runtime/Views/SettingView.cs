using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kyrnel.LinkGame
{
    /// <summary>
    /// 设置面板视图（Views 层）：控制音效/音乐音量与面板的打开、关闭。
    /// 音量通过 PlayerPrefs 记忆，音量值直接作用于场景中配置的 AudioSource。
    ///
    /// 使用说明：本组件既可以挂在"常驻激活"的对象上（推荐，_panel 指向子面板），
    /// 也可以直接挂在面板自身上（此时 _panel 留空即代表自身 GameObject）。
    /// 两种情况都通过幂等的 <see cref="EnsureInitialized"/> 完成初始化：
    /// 面板即使在场景中默认处于失活状态，按钮回调与音量滑块依然会正确绑定。
    /// </summary>
    public class SettingView : MonoBehaviour
    {
        [Header("面板")]
        [SerializeField] private GameObject _panel;

        [Header("音量")]
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private AudioSource _musicSource;

        [Header("按钮")]
        [SerializeField] private Button _openButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private AudioClip _buttonClickSound;

        private const string VolumePrefsKey = "LinkGame_Volume";
        private const string MusicPrefsKey = "LinkGame_Music";

        private bool _initialized;

        /// <summary>
        /// 面板对象。未在 Inspector 指定时代表本组件所在的 GameObject，
        /// 这样把组件直接挂在面板上也能正常工作。
        /// </summary>
        private GameObject Panel => _panel != null ? _panel : gameObject;

        /// <summary>面板当前是否打开。</summary>
        public bool IsOpen => Panel.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            // 对象初始处于失活状态时 Awake 不会被调用，这里兜底一次
            EnsureInitialized();
        }

        /// <summary>
        /// 幂等初始化：绑定音量滑块与开/关按钮，并把面板置为关闭状态。
        /// 可安全地重复调用，用于兼容"组件挂在面板自身、面板默认隐藏"的挂法。
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            if (_panel == null)
            {
                this.Warning("未指定 _panel，将把自身 GameObject 当作设置面板（若挂在不该隐藏的对象上请手动指定 _panel）。");
            }

            // 读取上次保存的音效音量
            float savedVolume = PlayerPrefs.GetFloat(VolumePrefsKey, 1f);
            ApplyVolume(savedVolume);

            if (_volumeSlider != null)
            {
                _volumeSlider.value = savedVolume;
                _volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
                _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            // 读取上次保存的音乐音量
            float savedMusic = PlayerPrefs.GetFloat(MusicPrefsKey, 1f);
            ApplyMusicVolume(savedMusic);

            if (_musicSlider != null)
            {
                _musicSlider.value = savedMusic;
                _musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
                _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (_openButton != null)
            {
                _openButton.onClick.RemoveListener(Open);
                _openButton.onClick.AddListener(Open);
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }

            // 默认关闭面板（启动时不播放按钮音效）
            Panel.SetActive(false);
        }

        /// <summary>
        /// 场景加载后统一补一次初始化：
        /// 面板在场景里默认隐藏时组件收不到 Awake / OnEnable，
        /// 会导致"打开设置"按钮的回调根本没被注册。
        /// 这里用 Resources.FindObjectsOfTypeAll 以便连失活对象一起找到（并过滤掉预制体资源）。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapPanels()
        {
            SettingView[] views = Resources.FindObjectsOfTypeAll<SettingView>();
            for (int i = 0; i < views.Length; i++)
            {
                SettingView view = views[i];
                if (view == null || !view.gameObject.scene.IsValid())
                {
                    continue;
                }

                view.EnsureInitialized();
            }
        }

        /// <summary>打开设置面板。</summary>
        public void Open()
        {
            EnsureInitialized();
            PlayButtonClickSound();
            Panel.SetActive(true);
        }

        /// <summary>关闭设置面板。</summary>
        public void Close()
        {
            EnsureInitialized();
            PlayButtonClickSound();
            Panel.SetActive(false);
        }

        /// <summary>切换设置面板的显示状态。</summary>
        public void Toggle()
        {
            EnsureInitialized();
            Panel.SetActive(!Panel.activeSelf);
        }

        private void OnVolumeChanged(float value)
        {
            ApplyVolume(value);
            PlayerPrefs.SetFloat(VolumePrefsKey, value);
            PlayerPrefs.Save();
        }

        private void ApplyVolume(float value)
        {
            if (_audioSource != null)
            {
                _audioSource.volume = Mathf.Clamp01(value);
            }
        }

        private void OnMusicVolumeChanged(float value)
        {
            ApplyMusicVolume(value);
            PlayerPrefs.SetFloat(MusicPrefsKey, value);
            PlayerPrefs.Save();
        }

        private void ApplyMusicVolume(float value)
        {
            if (_musicSource != null)
            {
                _musicSource.volume = Mathf.Clamp01(value);
            }
        }

        /// <summary>播放按钮点击音效。</summary>
        private void PlayButtonClickSound()
        {
            if (_buttonClickSound == null || _audioSource == null)
            {
                return;
            }

            _audioSource.PlayOneShot(_buttonClickSound);
        }
    }
}
