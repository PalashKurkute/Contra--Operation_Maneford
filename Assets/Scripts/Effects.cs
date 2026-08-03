using UnityEngine;

// Spawns the one-shot visual effects (muzzle flashes, sparks,
// explosions, dust). Call the static methods from anywhere;
// they do nothing if the scene has no Effects object.
public class Effects : MonoBehaviour
{
    public static Effects Instance { get; private set; }

    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;
    public GameObject explosionPrefab;
    public GameObject dustPrefab;

    [Header("Screen Shake")]
    public float explosionShake = 0.18f;
    public float explosionShakeTime = 0.25f;

    private CameraFollow cameraFollow;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cameraFollow = FindAnyObjectByType<CameraFollow>();
    }

    public static void MuzzleFlash(
        Vector3 position,
        Vector2 direction,
        Transform follow
    )
    {
        if (Instance == null)
            return;

        GameObject flash = Spawn(
            Instance.muzzleFlashPrefab,
            position,
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
        );

        if (flash != null && follow != null)
        {
            flash.GetComponent<OneShotEffect>().Follow(follow);
        }
    }

    public static void Impact(Vector3 position)
    {
        if (Instance == null)
            return;

        Spawn(Instance.impactPrefab, position, 0f);
    }

    public static void Explosion(Vector3 position)
    {
        if (Instance == null)
            return;

        Spawn(Instance.explosionPrefab, position, 0f);

        if (Instance.cameraFollow != null)
        {
            Instance.cameraFollow.Shake(
                Instance.explosionShake,
                Instance.explosionShakeTime
            );
        }
    }

    public static void Dust(Vector3 position)
    {
        if (Instance == null)
            return;

        Spawn(Instance.dustPrefab, position, 0f);
    }

    static GameObject Spawn(
        GameObject prefab,
        Vector3 position,
        float angle
    )
    {
        if (prefab == null)
            return null;

        return Instantiate(
            prefab,
            position,
            Quaternion.Euler(0f, 0f, angle)
        );
    }
}
