using UnityEngine;

// Drifts a tiled water strip sideways and bobs it gently.
// The drift wraps every tile so the strip never runs out.
public class WaterScroll : MonoBehaviour
{
    public float speed = 0.4f;
    public float tileWidth = 2f;

    public float bobAmount = 0.04f;
    public float bobSpeed = 1.3f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float x = Mathf.Repeat(Time.time * speed, tileWidth);
        float y = Mathf.Sin(Time.time * bobSpeed) * bobAmount;

        transform.localPosition =
            startPosition + new Vector3(x, y, 0f);
    }
}
