using UnityEngine;
using UnityEngine.UI;

// The boss's health bar at the top of the screen. The red part drops
// at once; a pale trail follows it down, so big hits are easy to see.
public class BossHealthBar : MonoBehaviour
{
    public BossHealth boss;
    public CanvasGroup group;
    public Image fill;
    public Image trail;

    public float trailSpeed = 0.6f;

    private float target = 1f;

    void Start()
    {
        group.alpha = 0f;
        fill.fillAmount = 1f;
        trail.fillAmount = 1f;

        boss.OnHealthChanged += OnHealthChanged;
        boss.OnDeath += OnDeath;
    }

    void OnDestroy()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= OnHealthChanged;
            boss.OnDeath -= OnDeath;
        }
    }

    public void Show()
    {
        group.alpha = 1f;
    }

    void OnHealthChanged(float fraction)
    {
        target = fraction;
        fill.fillAmount = fraction;
    }

    void OnDeath()
    {
        target = 0f;
    }

    void Update()
    {
        trail.fillAmount = Mathf.MoveTowards(
            trail.fillAmount,
            target,
            trailSpeed * Time.unscaledDeltaTime
        );
    }
}
