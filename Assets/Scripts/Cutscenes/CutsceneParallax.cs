using UnityEngine;
using UnityEngine.Rendering;

// Parallax for cutscene sets. Slides one layer by a fraction of
// the camera's movement away from an anchor point, so far
// layers drift slower than near ones.
//
// Unlike ParallaxLayer it also runs in the Editor, so scrubbing
// the Timeline shows the depth. It positions the layer just
// before the game camera renders, after Cinemachine has moved it.
[ExecuteAlways]
public class CutsceneParallax : MonoBehaviour
{
    // 1 = stuck to the camera (sky), 0 = fixed in the world,
    // below 0 = foreground that passes faster than the world
    [Range(-1f, 1f)]
    public float parallax = 0.5f;

    // Camera position at which the layer sits where you placed it
    public Vector2 anchor;

    // The child that slides; you move this object, it moves that
    public Transform layer;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += Apply;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= Apply;
    }

    void Apply(ScriptableRenderContext context, Camera cam)
    {
        // Only the game camera; the Scene view keeps its last offset
        if (layer == null ||
            cam.cameraType != CameraType.Game ||
            !cam.CompareTag("MainCamera"))
            return;

        Vector2 moved = (Vector2)cam.transform.position - anchor;

        layer.localPosition = new Vector3(
            moved.x * parallax, moved.y * parallax,
            layer.localPosition.z
        );
    }
}
