using UnityEngine;
using UnityEngine.UI;

namespace Game.Graphics
{
    /// <summary>
    /// 配套 UI/URP/BeautyImage Shader 使用的自适应辅助组件。
    /// 挂载在 Image 物体上，自动在尺寸变化时将宽高比 (_Aspect) 注入到材质中，确保圆角永不拉伸变形。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Graphic))]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/UI Beauty Image Fit")]
    public class UIBeautyImageFit : MonoBehaviour, IMaterialModifier
    {
        private static readonly int AspectPropId = Shader.PropertyToID("_Aspect");
        private const string TargetShaderName = "UI/URP/BeautyImage";

        private Graphic _graphic;
        private RectTransform _rectTransform;

        private Material _modifiedMaterial;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            SetDirty();
        }

        private void OnDisable()
        {
            SetDirty();
            CleanupMaterial();
        }

        private void OnDestroy()
        {
            CleanupMaterial();
        }

        private void OnRectTransformDimensionsChange()
        {
            SetDirty();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetDirty();
        }
#endif

        private void SetDirty()
        {
            if (_graphic != null)
            {
                // 通知 UGUI 重新通过 GetModifiedMaterial 请求材质，不破坏批处理与材质生命周期
                _graphic.SetMaterialDirty();
            }
        }

        /// <summary>
        /// UGUI 原生标准接口：安全注入材质属性，避免 Editor 模式产生 Material 内存泄漏
        /// </summary>
        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (baseMaterial == null || baseMaterial.shader == null || baseMaterial.shader.name != TargetShaderName)
            {
                return baseMaterial;
            }

            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            Rect rect = _rectTransform.rect;
            if (rect.height <= 0.0001f)
            {
                return baseMaterial;
            }

            float aspect = rect.width / rect.height;

            // 复用或创建材质副本
            if (_modifiedMaterial == null || _modifiedMaterial.shader != baseMaterial.shader)
            {
                CleanupMaterial();
                _modifiedMaterial = new Material(baseMaterial)
                {
                    name = baseMaterial.name + " (BeautyFit)",
                    hideFlags = HideFlags.DontSave
                };
            }
            else
            {
                _modifiedMaterial.CopyPropertiesFromMaterial(baseMaterial);
            }

            _modifiedMaterial.SetFloat(AspectPropId, aspect);
            return _modifiedMaterial;
        }

        private void CleanupMaterial()
        {
            if (_modifiedMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_modifiedMaterial);
                }
                else
                {
                    DestroyImmediate(_modifiedMaterial);
                }

                _modifiedMaterial = null;
            }
        }
    }
}