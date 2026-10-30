// ─────────────────────────────────────────────────────────────────────────────
// 【链路】UnityRuneWorld.Hit / PlayerInjured ──> HitFeedback（本文件）
//        ──> 伤害数字 + 震屏 + 顿帧 + 受击红屏
// 【职责】只做"打中了"的即时反馈，不影响伤害结算。
// 【顿帧】短促 0.03~0.05s；如果当前已经被暂停（升级/结算），不覆盖暂停。
// ─────────────────────────────────────────────────────────────────────────────
using RuneAshes.Framework;
using RuneAshes.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RuneAshes.UI
{
    public sealed class HitFeedback : MonoBehaviour
    {
        [SerializeField] private float _hitStopDuration = 0.035f;
        [SerializeField] private float _injuryHitStopDuration = 0.05f;
        [SerializeField] private float _hitStopScale = 0.05f;
        [SerializeField] private float _hitStopThrottle = 0.08f;

        private UnityRuneWorld _world;
        private PlayerHealth _player;
        private TopDownCamera _camera;
        private Image _vignette;

        private float _lastHitStopTime = -1f;
        private float _vignetteAlpha;

        private void Awake()
        {
            _world = GetComponent<UnityRuneWorld>();
            _player = GetComponent<PlayerHealth>();
            _camera = FindFirstObjectByType<TopDownCamera>();

            var canvas = UiTheme.CreateCanvas("HitFeedbackCanvas", 50);
            _vignette = UiTheme.CreateImage(canvas.transform, "DamageVignette",
                new Color(0.75f, 0.08f, 0.06f, 0f), new Vector2(1920f, 1080f), Vector2.zero);
            _vignette.raycastTarget = false;
        }

        private void OnEnable()
        {
            if (_world != null)
            {
                _world.Hit += OnHit;
                _world.PlayerInjured += OnPlayerInjured;
            }
        }

        private void OnDisable()
        {
            if (_world != null)
            {
                _world.Hit -= OnHit;
                _world.PlayerInjured -= OnPlayerInjured;
            }
        }

        private void OnHit(DamageEvent damage, IDamageable target)
        {
            if (target == null)
            {
                return;
            }

            var color = damage.Type == DamageType.Explosion ? UiTheme.Amber : UiTheme.TextColor;
            FloatingDamageText.Create(target.Position + Vector3.up * 1.8f,
                Mathf.Max(1, Mathf.RoundToInt(damage.Amount)).ToString(), color);

            if (_camera != null)
            {
                _camera.AddTrauma(damage.Type == DamageType.Explosion ? 0.22f : 0.12f);
            }

            StartHitStop(_hitStopDuration);
        }

        private void OnPlayerInjured(DamageEvent damage)
        {
            var position = _player != null ? _player.Position : transform.position;
            FloatingDamageText.Create(position + Vector3.up * 2.0f,
                Mathf.Max(1, Mathf.RoundToInt(damage.Amount)).ToString(), UiTheme.Danger);

            _vignetteAlpha = 0.30f;

            if (_camera != null)
            {
                _camera.AddTrauma(0.45f);
            }

            StartHitStop(_injuryHitStopDuration);
        }

        private void StartHitStop(float duration)
        {
            var time = GameTime.Current;
            if (time == null || time.IsPaused)
            {
                return;
            }

            if (Time.unscaledTime - _lastHitStopTime < _hitStopThrottle)
            {
                return;
            }

            _lastHitStopTime = Time.unscaledTime;
            time.RequestHitStop(duration, _hitStopScale);
        }

        private void Update()
        {
            if (_vignetteAlpha > 0f)
            {
                _vignetteAlpha = Mathf.MoveTowards(_vignetteAlpha, 0f, Time.unscaledDeltaTime * 1.8f);
                var color = _vignette.color;
                color.a = _vignetteAlpha;
                _vignette.color = color;
            }
        }
    }

    /// <summary>世界空间伤害数字：上浮 + 淡出 + 轻微缩放。</summary>
    public sealed class FloatingDamageText : MonoBehaviour
    {
        private const float Lifetime = 0.8f;

        private float _age;
        private Color _color;
        private TextMesh _text;
        private Vector3 _startScale;

        public static void Create(Vector3 worldPosition, string content, Color color)
        {
            var go = new GameObject("DamageText");
            go.transform.position = worldPosition + new Vector3(
                Random.Range(-0.25f, 0.25f), 0f, Random.Range(-0.25f, 0.25f));

            var camera = Camera.main;
            go.transform.rotation = camera != null
                ? camera.transform.rotation
                : Quaternion.Euler(55f, 0f, 0f);

            var text = go.AddComponent<TextMesh>();
            text.text = content;
            text.font = UiTheme.Font;
            text.fontSize = 64;
            text.characterSize = 0.075f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = color;

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = go.AddComponent<MeshRenderer>();
            }

            renderer.sharedMaterial = text.font.material;
            renderer.sortingOrder = 20;

            var floating = go.AddComponent<FloatingDamageText>();
            floating._text = text;
            floating._color = color;
            floating._startScale = go.transform.localScale;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            _age += dt;

            transform.position += Vector3.up * (1.1f * dt);
            transform.localScale = _startScale * Mathf.Lerp(1.1f, 0.85f, _age / Lifetime);

            var color = _color;
            color.a = Mathf.Clamp01(1f - _age / Lifetime);
            _text.color = color;

            if (_age >= Lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
