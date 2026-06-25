using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placeholder for the boss's first appearance — no movement/animation yet,
/// that comes with the real boss AI later (the spawn point below is exactly
/// where that will eventually happen). 20 seconds after the stopwatch starts,
/// plays a laugh FROM the boss's fixed spawn point — a real 3D AudioSource
/// positioned there, so it pans/attenuates correctly based on where the
/// player actually is relative to the boss — then hands a forced two-line
/// story beat to IntroDialogueSequencer, which pauses everything. Once the
/// player clicks through it, gameplay resumes and the noise meter becomes
/// active immediately (rather than waiting on its own timer).
/// </summary>
public class BossLaughEvent : MonoBehaviour
{
    public float triggerAfterSeconds = 20f;
    public AudioClip laughClip;
    [Range(0f, 1f)] public float laughVolume = 0.85f;

    [Header("Spatial Audio")]
    [Tooltip("The boss's fixed spawn point. The laugh plays from here in 3D so it sounds like it's coming from wherever the boss actually is relative to the player.")]
    public Transform bossSpawnPoint;
    public float minAudibleDistance = 3f;
    public float maxAudibleDistance = 45f;

    public List<IntroDialogueSequencer.IntroLine> lines = new List<IntroDialogueSequencer.IntroLine>
    {
        new IntroDialogueSequencer.IntroLine
        {
            kind = IntroDialogueSequencer.IntroLine.Kind.Monologue,
            text = "WHAT, thats Paul talking. I can't be caught here. It will be too loud to leave now, the door is so creaky and I think he has cameras there. My only way is to leave after he leaves. He always does an office check right after shift ends, that is my only way"
        },
        new IntroDialogueSequencer.IntroLine
        {
            kind = IntroDialogueSequencer.IntroLine.Kind.Tip,
            text = "Your mission now: Survive until shift ends, your only way of escape is to wait for your boss to leave..."
        },
    };

    [Header("References (auto-found if left empty)")]
    public GameTimer gameTimer;
    public PlayerNoiseMeter noiseMeter;
    public IntroDialogueSequencer dialogueSequencer;
    [Tooltip("The boss GameObject's BossAI component. Wakes up (Activate()) the instant the laugh plays — not on its own timer, so it can never drift out of sync with this trigger.")]
    public BossAI bossAI;

    AudioSource source;
    bool triggered;

    void Awake()
    {
        if (gameTimer == null) gameTimer = Object.FindAnyObjectByType<GameTimer>();
        if (noiseMeter == null) noiseMeter = Object.FindAnyObjectByType<PlayerNoiseMeter>();
        if (dialogueSequencer == null) dialogueSequencer = Object.FindAnyObjectByType<IntroDialogueSequencer>();
        if (bossAI == null) bossAI = Object.FindAnyObjectByType<BossAI>();

        // The source lives ON the boss spawn point (not the player), so the
        // sound is genuinely emitted from the boss's position in 3D space.
        GameObject sourceHost = bossSpawnPoint != null ? bossSpawnPoint.gameObject : gameObject;
        source = sourceHost.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = bossSpawnPoint != null ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minAudibleDistance;
        source.maxDistance = maxAudibleDistance;

        if (bossSpawnPoint == null)
        {
            Debug.LogWarning("[BossLaughEvent] No bossSpawnPoint assigned — the laugh will play as a flat 2D sound instead of coming from the boss's position.");
        }
    }

    void Update()
    {
        if (triggered || gameTimer == null || !gameTimer.IsRunning)
        {
            return;
        }

        if (gameTimer.ElapsedSeconds >= triggerAfterSeconds)
        {
            Trigger();
        }
    }

    void Trigger()
    {
        triggered = true;

        if (laughClip != null)
        {
            source.PlayOneShot(laughClip, laughVolume);
        }

        if (bossAI != null)
        {
            bossAI.Activate();
        }

        if (dialogueSequencer != null)
        {
            dialogueSequencer.BeginChapter(lines, OnDialogueComplete);
        }
    }

    void OnDialogueComplete()
    {
        if (gameTimer != null)
        {
            gameTimer.ResumeTimer();
        }

        if (noiseMeter != null)
        {
            noiseMeter.ForceReveal();
        }
    }
}
