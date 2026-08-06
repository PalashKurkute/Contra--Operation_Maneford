using UnityEngine;

// Slowly pans the camera sideways, so the parallax forest
// scrolls behind the title screen
public class MenuCameraPan : MonoBehaviour
{
    public float speed = 0.6f;

    void Update()
    {
        transform.position +=
            Vector3.right * speed * Time.unscaledDeltaTime;
    }
}
