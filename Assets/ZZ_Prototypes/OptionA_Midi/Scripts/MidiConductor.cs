using System.Collections.Generic;
using UnityEngine;
using Hulan.PilloSDK.DeviceManager;
using PilloRhythm.Core;

namespace PilloRhythm.ProtoA
{
    [RequireComponent(typeof(AudioSource))]
    public class MidiConductor : MonoBehaviour
    {
        [Header("Level & Prefab")]
        public MidiLevelDataAsset levelData;
        public NoteVisual notePrefab;

        [Header("Track Positions")]
        public Transform spawnPoint;
        public Transform hitLinePoint;

        [Header("Startup & Debug")]
        [Tooltip("Buffer before AudioSource.PlayScheduled starts (seconds).")]
        public double scheduleDelay = 0.5;
        public bool logSyncAccuracy = true;

        [Header("Live Readout (Read Only)")]
        public double currentSongTime;
        public int latestPilloPressure;

        private AudioSource audioSource;
        private double dspSongStartTime;
        private bool isPlaying;
        private int nextNoteIndex;

        private class ActiveNote
        {
            public NoteVisual visual;
            public NoteEventData data;
            public bool loggedArrival;
        }

        private readonly List<ActiveNote> activeNotes = new List<ActiveNote>();

        private void OnEnable()
        {
            // Listen for pressure changes.
            PilloDeviceManager.onPeripheralPressureDidChange += HandlePilloPressure;
        }

        private void OnDisable()
        {
            // Stop listening when this object is deleted.
            PilloDeviceManager.onPeripheralPressureDidChange -= HandlePilloPressure;
        }

        private void Start()
        {
            // Get the audio source and start the level.
            audioSource = GetComponent<AudioSource>();
            StartLevel();
        }

        public void StartLevel()
        {
            // Stop if the level has no audio.
            if (levelData == null || levelData.songClip == null)
            {
                Debug.LogError("[OptionA] Assign a MidiLevelDataSO with an AudioClip.");
                return;
            }

            audioSource.clip = levelData.songClip;
            // Schedule the music using DSP time for accurate timing.
            dspSongStartTime = AudioSettings.dspTime + scheduleDelay;
            audioSource.PlayScheduled(dspSongStartTime);

            // Reset the level state.
            nextNoteIndex = 0;
            isPlaying = true;
        }

        private void Update()
        {
            // Do nothing until the level starts.
            if (!isPlaying) return;

            // Calculate the current song position.
            currentSongTime = (AudioSettings.dspTime - dspSongStartTime) - levelData.firstBeatOffset;
            double approach = levelData.approachTimeSeconds;

            // Spawn upcoming notes ahead of time
            while (nextNoteIndex < levelData.notes.Count)
            {
                NoteEventData nextNote = levelData.notes[nextNoteIndex];
                if (currentSongTime >= nextNote.hitTimeSeconds - approach)
                {
                    NoteVisual visual = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
                    visual.Initialize(nextNote);
                    activeNotes.Add(new ActiveNote { visual = visual, data = nextNote });
                    nextNoteIndex++;
                }
                else break;
            }

            // Position every note 
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                ActiveNote item = activeNotes[i];
                double spawnTime = item.data.hitTimeSeconds - approach;
                // Calculate how far the note has travelled.
                float progress = (float)((currentSongTime - spawnTime) / approach);

                item.visual.SetNormalizedPosition(spawnPoint.position, hitLinePoint.position, progress);

                if (logSyncAccuracy && !item.loggedArrival && currentSongTime >= item.data.hitTimeSeconds)
                {
                    item.loggedArrival = true;
                    double driftMs = (currentSongTime - item.data.hitTimeSeconds) * 1000.0;
                    Debug.Log($"[OptionA Sync] {item.data.noteType} | Target: {item.data.hitTimeSeconds:F4}s | Actual: {currentSongTime:F4}s | Frame Offset: {driftMs:F2} ms");
                }

                // Despawn
                if (currentSongTime > item.data.hitTimeSeconds + item.data.durationSeconds + 0.6)
                {
                    Destroy(item.visual.gameObject);
                    activeNotes.RemoveAt(i);
                }
            }
        }

        private void HandlePilloPressure(string identifier, int pressure)
        {
            // Store the latest pressure
            latestPilloPressure = pressure;
        }
    }
}