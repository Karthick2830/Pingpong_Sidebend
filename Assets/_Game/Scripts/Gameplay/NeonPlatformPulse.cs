using BallCatch.Core;
using UnityEngine;

namespace BallCatch.Gameplay
{
    /// <summary>
    /// Visual-only life for the neon player platform: the halo breathes slowly and flares when the ball is caught.
    /// It never touches physics, the collider or the player's position.
    /// </summary>
    public class NeonPlatformPulse : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private SpriteRenderer glow;
        [SerializeField] private SpriteRenderer bar;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;
        [SerializeField, Range(0f, 0.5f)] private float pulseDepth = 0.15f;
        [SerializeField, Min(0.05f)] private float flashSeconds = 0.35f;

        private Vector3 glowBaseScale;
        private float flash;

        private void Awake()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            glowBaseScale = glow.transform.localScale;
        }

        private void OnEnable() => gameManager.AttemptResolved += OnAttemptResolved;

        private void OnDisable() => gameManager.AttemptResolved -= OnAttemptResolved;

        private void OnAttemptResolved(AttemptResult result)
        {
            if (result == AttemptResult.Catch)
                flash = 1f;
        }

        private void Update()
        {
            // Unscaled time keeps the glow alive on the (frozen) pause and results screens.
            float dt = Time.unscaledDeltaTime;
            flash = Mathf.MoveTowards(flash, 0f, dt / flashSeconds);

            float breathe = 1f - pulseDepth + pulseDepth * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            glow.color = new Color(1f, 1f, 1f, Mathf.Clamp01(breathe + flash * 0.3f));
            glow.transform.localScale = glowBaseScale * (1f + flash * 0.25f);
            bar.color = Color.Lerp(Color.white, new Color(0.85f, 1f, 1f, 1f), flash);
        }
    }
}
