using System.Collections.Generic;
using UnityEngine;
using Hulan.PilloSDK.DeviceManager;

public class PilloRhythmGame : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Prefab of the falling note (e.g., a 2D Circle or Square).")]
    public GameObject notePrefab;

    [Tooltip("Position at the top of the screen where notes spawn.")]
    public Transform spawnPoint;

    [Tooltip("The target circle at the bottom where the player must press.")]
    public Transform targetCircle;

    [Header("Gameplay Settings")]
    [Tooltip("Falling speed of the notes in Unity units per second.")]
    public float noteSpeed = 5f;

    [Tooltip("Minimum and maximum random time (in seconds) between spawned notes.")]
    public float minSpawnInterval = 0.8f;
    public float maxSpawnInterval = 2.0f;

    [Tooltip("Maximum vertical distance from the target circle to count as a valid hit.")]
    public float hitRadius = 0.8f;

    [Tooltip("How far below the target circle a note can fall before counting as a miss.")]
    public float missOffset = 1.5f;

    [Header("Pillo Input Settings")]
    [Tooltip("Minimum pressure value required to register a button press.")]
    public int pressureThreshold = 50;

    [Header("Feedback & Stats (Read Only)")]
    public int score = 0;
    public int misses = 0;

    // Internal list to keep track of all notes currently falling on screen
    private readonly List<GameObject> activeNotes = new List<GameObject>();

    // Timer used to trigger the next random note spawn
    private float spawnTimer;

    // Edge-detection flags to turn continuous pressure readings into a single "click"
    private bool isPilloPressed = false;
    private bool pendingClick = false;

    // Visual feedback variables for flashing the target circle color
    private SpriteRenderer targetRenderer;
    private Color originalTargetColor = Color.white;
    private float flashTimer = 0f;

    private void OnEnable()
    {
        // Subscribe to the Pillo SDK pressure sensor event when this object becomes active
        PilloDeviceManager.onPeripheralPressureDidChange += HandlePressureChanged;
    }

    private void OnDisable()
    {
        // Always unsubscribe when disabled/destroyed to prevent memory leaks or null callbacks
        PilloDeviceManager.onPeripheralPressureDidChange -= HandlePressureChanged;
    }

    private void Start()
    {
        // Cache the target circle's SpriteRenderer so we can flash it green (hit) or red (miss)
        if (targetCircle != null)
        {
            targetRenderer = targetCircle.GetComponent<SpriteRenderer>();
            if (targetRenderer != null)
            {
                originalTargetColor = targetRenderer.color;
            }
        }

        // Schedule the first note spawn
        ResetSpawnTimer();
    }

    private void Update()
    {
        // 1. Spawn new notes at random intervals
        HandleSpawning();

        // 2. Move existing notes downward and remove any that were missed
        UpdateNotes();

        // 3. Process Pillo click safely on Unity's main thread
        if (pendingClick)
        {
            pendingClick = false;
            CheckHit();
        }

        // 4. Restore the target circle's original color after a brief flash
        UpdateTargetFlash();
    }

    /// <summary>
    /// Callback invoked by PilloDeviceManager whenever the sensor's pressure value changes.
    /// </summary>
    private void HandlePressureChanged(string identifier, int pressure)
    {
        // Trigger a single click only on the rising edge (when crossing above the threshold)
        if (pressure >= pressureThreshold && !isPilloPressed)
        {
            isPilloPressed = true;
            pendingClick = true;
        }
        // Reset the press state once the user releases pressure below the threshold
        else if (pressure < pressureThreshold && isPilloPressed)
        {
            isPilloPressed = false;
        }
    }

    /// <summary>
    /// Counts down the spawn timer and instantiates a new note when it reaches zero.
    /// </summary>
    private void HandleSpawning()
    {
        if (notePrefab == null || spawnPoint == null) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            GameObject newNote = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
            activeNotes.Add(newNote);
            ResetSpawnTimer();
        }
    }

    /// <summary>
    /// Picks a new random delay between minSpawnInterval and maxSpawnInterval.
    /// </summary>
    private void ResetSpawnTimer()
    {
        spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
    }

    /// <summary>
    /// Moves all active notes down and destroys notes that pass too far below the target circle.
    /// </summary>
    private void UpdateNotes()
    {
        if (targetCircle == null) return;

        float destroyY = targetCircle.position.y - missOffset;

        // Iterate backwards so removing items from the list is safe
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            GameObject note = activeNotes[i];

            if (note == null)
            {
                activeNotes.RemoveAt(i);
                continue;
            }

            // Move the note straight down
            note.transform.position += Vector3.down * (noteSpeed * Time.deltaTime);

            // Check if the player missed the note completely
            if (note.transform.position.y < destroyY)
            {
                activeNotes.RemoveAt(i);
                Destroy(note);
                RegisterMiss("Note missed!");
            }
        }
    }

    /// <summary>
    /// Finds the closest falling note to the target circle and evaluates if it's within hitRadius.
    /// </summary>
    private void CheckHit()
    {
        if (targetCircle == null) return;

        GameObject closestNote = null;
        float closestDistance = float.MaxValue;

        // Search for the note nearest to the target circle's Y position
        foreach (GameObject note in activeNotes)
        {
            if (note == null) continue;

            float distance = Mathf.Abs(note.transform.position.y - targetCircle.position.y);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestNote = note;
            }
        }

        // If the closest note is inside the allowed hit radius, award a point and destroy it
        if (closestNote != null && closestDistance <= hitRadius)
        {
            activeNotes.Remove(closestNote);
            Destroy(closestNote);

            score++;
            FlashTarget(Color.green);
            Debug.Log($"HIT! Score: {score}");
        }
        else
        {
            // Player clicked when no note was inside the target circle
            RegisterMiss("Clicked too early or with no note in range!");
        }
    }

    private void RegisterMiss(string reason)
    {
        misses++;
        FlashTarget(Color.red);
        Debug.Log($"MISS ({reason}) | Total Misses: {misses}");
    }

    private void FlashTarget(Color flashColor)
    {
        if (targetRenderer == null) return;

        targetRenderer.color = flashColor;
        flashTimer = 0.15f;
    }

    private void UpdateTargetFlash()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && targetRenderer != null)
            {
                targetRenderer.color = originalTargetColor;
            }
        }
    }
}