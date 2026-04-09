using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class UniversalCharacterAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] hitSounds;   // You can add 1 or 5 different grunts here!
    public AudioClip[] deathSounds; // Same for death sounds

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
        PlayRandomClip(hitSounds);
    }

    public void PlayDeathSound()
    {
        PlayRandomClip(deathSounds);
    }

    private void PlayRandomClip(AudioClip[] clips)
    {
        // If we forgot to assign sounds, just ignore it and don't crash
        if (clips == null || clips.Length == 0) return;

        // Pick a random clip from the list
        AudioClip clipToPlay = clips[Random.Range(0, clips.Length)];

        // Slightly randomize the pitch (e.g., between 0.85x and 1.15x speed)
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);

        // Play it! (PlayOneShot allows multiple hits to overlap naturally)
        audioSource.PlayOneShot(clipToPlay);
    }
}