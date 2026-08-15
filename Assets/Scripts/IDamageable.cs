// Anything the player's bullets can hurt: lions, soldiers, the boss
public interface IDamageable
{
    // False once it's been defeated; bullets then fly past it
    bool IsAlive { get; }

    void TakeDamage(int damage);
}
