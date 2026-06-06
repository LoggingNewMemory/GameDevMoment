using UnityEngine;
using System.Collections;

public class UniversalMeleeAttack : MonoBehaviour
{
    private Animator anim;
    private Transform playerTarget;

    [Header("Universal Attack Settings")]
    public AudioClip attackSound;
    
    [Tooltip("How many seconds after the animation starts should the punch hit?")]
    public float damageDelay = 0.4f; // <-- THE FIX: Direct, foolproof timing!
    
    public float hitTrackingRange = 3f; // If the player dashes further than this, the punch misses!

    void Awake()
    {
        anim = GetComponent<Animator>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    public void TriggerAttack(float damageAmount)
    {
        StartCoroutine(AttackRoutine(damageAmount));
    }

    public void CancelAttack()
    {
        // Call this when the enemy dies so a dead body doesn't finish its punch!
        StopAllCoroutines(); 
    }

    IEnumerator AttackRoutine(float damageAmount)
    {
        // 1. Start the punch animation
        anim.SetTrigger("Attack");
        if (attackSound != null) AudioSource.PlayClipAtPoint(attackSound, transform.position);

        // 2. Wait for the exact moment the fist extends!
        yield return new WaitForSeconds(damageDelay);

        // 3. THE DODGE CHECK: Is the player still close enough to get hit?
        if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(damageAmount, transform);
        }
    }
}