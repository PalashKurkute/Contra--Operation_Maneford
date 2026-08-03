using UnityEngine;

// Scrolls a background layer slower than the camera and
// leap-frogs its tiles so the layer never runs out.
// Runs after CameraFollow so it uses this frame's camera position.
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    // 0 = moves with the world, 1 = stuck to the camera
    [Range(0f, 1f)]
    public float parallax = 0.5f;

    // Width of one repeat of this layer (world units)
    public float tileWidth = 16f;

    private Transform cam;
    private float startX;
    private float startY;
    private float camStartY;

    void Start()
    {
        cam = Camera.main.transform;

        startX = transform.position.x;
        startY = transform.position.y;
        camStartY = cam.position.y;
    }

    void LateUpdate()
    {
        float camX = cam.position.x;

        transform.position = new Vector3(
            startX + camX * parallax,
            startY + (cam.position.y - camStartY) * parallax,
            transform.position.z
        );

        // How far the camera has moved past this layer's centre
        float drift = camX * (1f - parallax) - startX;

        if (drift > tileWidth)
        {
            startX += tileWidth;
        }
        else if (drift < -tileWidth)
        {
            startX -= tileWidth;
        }
    }
}
