using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UniversalCharacterAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip hitSound;   // <-- Just 1 slot now!
    public AudioClip deathSound; // <-- Just 1 slot now!

    [Header("Settings")]
    [Range(0.0f, 0.3f)] 
    public float pitchVariation = 0.15f; // Randomizes pitch so it never sounds repetitive

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Ensure the sound is 3D so it gets quieter as you walk away
        audioSource.spatialBlend = 1f; 
        audioSource.minDistance = 2f;
        audioSource.maxDistance = 20f;
    }

    public void PlayHitSound()
    {
        if (hitSound == null) return;

        // Slightly randomize the pitch (e.g., between 0.85x and 1.15x speed)
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(hitSound);
    }

    public void PlayDeathSound()
    {
        if (deathSound == null) return;

        // Slightly randomize the pitch 
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(deathSound);
    }
}