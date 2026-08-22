using UnityEngine;
using System.Collections.Generic;

// A round hitbox for the helicopter chase. Everything in the chase flies
// along with the camera, so instead of physics the chase does simple
// circle tests against this list.
public class ChaseHitbox : MonoBehaviour
{
    public static readonly List<ChaseHitbox> All = new List<ChaseHitbox>();

    public float radius = 0.5f;
    public Vector2 offset;

    // The player's bullets can hit it
    public bool shootable = true;

    // Touching it hurts the player
    public bool harmful = true;

    // Blown up when it hits the player (rockets, drones)
    public bool destroyOnContact = true;

    public Vector2 Center
    {
        get { return (Vector2)transform.position + offset; }
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    public bool Overlaps(Vector2 point, float otherRadius)
    {
        float reach = radius + otherRadius;
        return (Center - point).sqrMagnitude <= reach * reach;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = harmful ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(Center, radius);
    }
}
