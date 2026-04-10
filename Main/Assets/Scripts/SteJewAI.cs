using UnityEngine;

public class SteJewAI : MonoBehaviour
{
    [Header("Flee & Combat Settings")]
    public float moveSpeed = 6f;
    public float fleeDistance = 15f; 
    public float attackRange = 3f; 
    public float attackDamage = 5f;
    
    [Tooltip("How fast he spams his magic attack. Lower is faster!")]
    public float attackCooldown = 0.4f; 

    private Transform playerTarget;
    private float lastAttackTime = 0f;

    private Animator anim;
    private UniversalHealth healthScript;

    void Start()
    {
        anim = GetComponent<Animator>();
        healthScript = GetComponent<UniversalHealth>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTarget = p.transform;
    }

    void Update()
    {
        if (healthScript != null && healthScript.isDead) return;
        if (playerTarget == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);

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

            // If player is close, rapid-fire magic!
            if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
            {
                AttackPlayer();
            }
        }
        else
        {
            if (anim != null) anim.SetBool("isChasing", false);
        }
    }

    void AttackPlayer()
    {
        lastAttackTime = Time.time;
        if (anim != null) anim.SetTrigger("Attack");

        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.TakeDamage(attackDamage, transform); 
            stats.AddDizzyStack();
        }

        SimpleShoot activeGun = playerTarget.GetComponentInChildren<SimpleShoot>();
        if (activeGun != null)
        {
            float roll = Random.Range(0f, 100f);
            
            if (roll <= 10f) 
            {
                activeGun.EmptyMagazine();
                Debug.Log("SteJew used Magic to empty your magazine!");
            }
            else if (roll > 10f && roll <= 30f) 
            {
                activeGun.StealReserveAmmo(30); 
                Debug.Log("SteJew used Magic to steal 30 Reserve Ammo!");
            }
        }
    }
}