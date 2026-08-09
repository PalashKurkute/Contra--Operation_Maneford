using UnityEngine;

// Swings an object back and forth around its Z axis,
// e.g. a searchlight scanning the sky
public class Sweep : MonoBehaviour
{
    // Degrees either side of the starting angle
    public float angle = 25f;

    // Seconds for one full swing there and back
    public float period = 6f;

    private float startAngle;
    private float phase;

    void Start()
    {
        startAngle = transform.localEulerAngles.z;
        phase = Random.value * period;
    }

    void Update()
    {
        float swing = Mathf.Sin((Time.time + phase) * 2f * Mathf.PI / period);

        transform.localRotation = Quaternion.Euler(
            0f,
            0f,
            startAngle + swing * angle
        );
    }
}
