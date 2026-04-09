using UnityEngine;

// An interface is just a rule. Anything that uses this interface MUST have a TakeDamage function!
public interface IDamageable
{
    void TakeDamage(float amount);
}