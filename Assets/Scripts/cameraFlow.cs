using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;

    public float smoothSpeed = 5f;

    // Keep camera at this height
    public float fixedY = 0f;

    // Smoothed follow position (shake is added on top)
    private Vector3 followPosition;

    private float shakeAmount;
    private float shakeDuration;
    private float shakeTimeLeft;

    void Start()
    {
        followPosition = transform.position;
    }

    // Shake the camera; a stronger shake overrides a weaker one
    public void Shake(float amount, float duration)
    {
        float current = shakeDuration > 0f ?
            shakeAmount * shakeTimeLeft / shakeDuration : 0f;

        if (amount < current)
            return;

        shakeAmount = amount;
        shakeDuration = duration;
        shakeTimeLeft = duration;
    }

    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 targetPosition = new Vector3(
                player.position.x,
                fixedY,
                transform.position.z
            );

            followPosition = Vector3.Lerp(
                followPosition,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
        }

        Vector3 shake = Vector3.zero;

        if (shakeTimeLeft > 0f)
        {
            shakeTimeLeft -= Time.deltaTime;

            shake = (Vector3)Random.insideUnitCircle *
                shakeAmount * (shakeTimeLeft / shakeDuration);
        }

        transform.position = followPosition + shake;
    }
}
