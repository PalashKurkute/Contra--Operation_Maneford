using UnityEngine;
using System.Collections;

// Turns the sprite solid white for a moment when it gets hit
public class HitFlash : MonoBehaviour
{
    public Material flashMaterial;
    public float duration = 0.08f;

    private SpriteRenderer spriteRenderer;
    private Material normalMaterial;
    private Coroutine flashing;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        normalMaterial = spriteRenderer.sharedMaterial;
    }

    public void Flash()
    {
        if (flashMaterial == null || !isActiveAndEnabled)
            return;

        if (flashing != null)
        {
            StopCoroutine(flashing);
        }

        flashing = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        spriteRenderer.sharedMaterial = flashMaterial;

        yield return new WaitForSeconds(duration);

        spriteRenderer.sharedMaterial = normalMaterial;
        flashing = null;
    }
}
