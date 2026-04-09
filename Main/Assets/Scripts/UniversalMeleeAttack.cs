using UnityEngine;
using System.Collections;

public class UniversalMeleeAttack : MonoBehaviour
{
    private Animator anim;
    private Transform playerTarget;

    [Header("Universal Attack Settings")]
    public AudioClip attackSound;
    public int animatorLayer = 1; // Layer 1 is your ActionLayer!
    public float timeBeforeEnd = 0.2f; // Deal damage 0.2 seconds before the animation finishes
    public float hitTrackingRange = 3f; // If the player dashes further than this during the swing, the punch misses!

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

        // Wait a tiny fraction of a second for Unity to transition to the Attack animation state
        yield return new WaitForSeconds(0.05f); 

        // 2. Figure out exactly how long the punch animation is
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(animatorLayer);
        
        // Calculate how long to wait (Total length minus the 0.2 seconds you requested)
        float waitTime = stateInfo.length - timeBeforeEnd - 0.05f; 
        
        // Just in case the math is weird, make sure we don't wait a negative amount of time
        if (waitTime < 0) waitTime = 0.1f;

        // 3. Wait for the punch to swing forward!
        yield return new WaitForSeconds(waitTime);

        // 4. THE DODGE CHECK: Is the player still close enough to get hit?
        if (playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) <= hitTrackingRange)
        {
            PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
            if (stats != null) stats.TakeDamage(damageAmount, transform);
        }
    }
}