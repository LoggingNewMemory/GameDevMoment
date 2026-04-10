using UnityEngine;
using System.Collections; 

public class SteJewAI : MonoBehaviour
{
    [Header("Flee Settings")]
    public float moveSpeed = 22f; 
    public float fleeDistance = 15f; 

    [Header("Global Combat Settings")]
    public float attackDamage = 5f;
    public float attackCooldown = 8f; 
    public float damageDelay = 1f; 

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip attackCastSound;    
    public AudioClip emptyMagSound;      
    public AudioClip stealReserveSound;  

    private Transform playerTarget;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalHealth healthScript;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();
        
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;

        lastAttackTime = Time.time; 
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

        // --- FLEE LOGIC ---
        if (distance < fleeDistance)
        {
            Vector3 fleeDir = (transform.position - playerTarget.position).normalized;
            fleeDir.y = 0; 
            
            transform.position += fleeDir * moveSpeed * Time.deltaTime;
            
            if (fleeDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fleeDir), Time.deltaTime * 10f);
            }

            if (anim != null) anim.SetBool("isChasing", true); 
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
        }

        // --- GLOBAL ATTACK LOGIC ---
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            AttackPlayer();
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");

        if (audioSource != null && attackCastSound != null)
        {
            audioSource.PlayOneShot(attackCastSound);
        }

        StartCoroutine(MagicHitRoutine());
    }

    IEnumerator MagicHitRoutine()
    {
        yield return new WaitForSeconds(damageDelay);

        if (healthScript != null && healthScript.isDead) yield break;

        // 1. ALWAYS apply base damage and dizziness
        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, null); 
            stats.AddDizzyStack();
        }

        // 2. ROLL FOR EXTRA EFFECTS
        float roll = Random.Range(0f, 100f);
        SimpleShoot activeGun = playerTarget.GetComponentInChildren<SimpleShoot>();

        // 60% Chance: JUST NORMAL ATTACK (Do nothing extra)
        if (roll <= 60f) 
        {
            Debug.Log("SteJew just did a normal magic attack!");
        }
        // 25% Chance: STEAL RESERVE AMMO
        else if (roll > 60f && roll <= 85f) 
        {
            if (activeGun != null)
            {
                activeGun.StealReserveAmmo(30); 
                if (stealReserveSound != null) AudioSource.PlayClipAtPoint(stealReserveSound, playerTarget.position);
                Debug.Log("SteJew used Global Magic to steal 30 Reserve Ammo!");
            }
        }
        // 15% Chance: EMPTY MAGAZINE
        else 
        {
            if (activeGun != null)
            {
                activeGun.EmptyMagazine();
                if (emptyMagSound != null) AudioSource.PlayClipAtPoint(emptyMagSound, playerTarget.position);
                Debug.Log("SteJew used Global Magic to empty your magazine!");
            }
        }
    }
}