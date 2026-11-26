using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Hulan.PilloSDK.DeviceManager;
using PilloRhythm.Core;

namespace PilloRhythm.ProtoB
{
    [RequireComponent(typeof(PlayableDirector))]
    public class ProtoB_TimelineConductor : MonoBehaviour
    {
        [Header("Prefab & Track Positions")]
        public NoteVisual notePrefab;
        public Transform spawnPoint;
        public Transform hitLinePoint;

        [Header("Timing & Quick Jump")]
        [Tooltip("How many seconds before the clip start a note spawns at SpawnPos.")]
        public double approachTimeSeconds = 2.0;

        [Tooltip("Start playback directly at this timestamp (useful for testing mid-song sections).")]
        public double startAtSeconds = 0.0;

        public bool logSyncAccuracy = true;

        [Header("Live Readout (Read Only)")]
        public double currentSongTime;
        public int latestPilloPressure;
        public int loadedNoteCount;

        private PlayableDirector director;
        private readonly List<NoteEventData> notes = new List<NoteEventData>();
        private int nextNoteIndex;
        private bool isPlaying;

        private class ActiveNote
        {
            public NoteVisual visual;
            public NoteEventData data;
            public bool loggedArrival;
        }

        private readonly List<ActiveNote> activeNotes = new List<ActiveNote>();

        private void OnEnable()
        {
            // Listen for pressure changes
            PilloDeviceManager.onPeripheralPressureDidChange += HandlePilloPressure;
        }

        private void OnDisable()
        {
            // Stop listening when this object is deleted.
            PilloDeviceManager.onPeripheralPressureDidChange -= HandlePilloPressure;
        }

        private void Start()
        {
            // Get the Timeline director and prepare the notes.
            director = GetComponent<PlayableDirector>();
            LoadNotesFromTimeline();
            StartLevel();
        }

        private void LoadNotesFromTimeline()
        {
            // Remove notes loaded from a previous Timeline.
            notes.Clear();

            // Check that the director has a Timeline asset.
            if (director.playableAsset is not TimelineAsset timeline)
            {
                Debug.LogError("[OptionB] Assign a TimelineAsset to PlayableDirector first.");
                return;
            }

            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                // Only read tracks that contain notes.
                if (track is not PilloNoteTrack) continue;

                foreach (TimelineClip clip in track.GetClips())
                {
                    // Ignore clips that are not NoteClip objects.
                    if (clip.asset is not NoteClip noteClip) continue;

                    bool isTap = noteClip.noteType == PilloNoteType.SoftTap
                              || noteClip.noteType == PilloNoteType.HardTap;

                    notes.Add(new NoteEventData
                    {
                        hitTimeSeconds = clip.start,
                        durationSeconds = isTap ? 0.0 : clip.duration,
                        noteType = noteClip.noteType,
                        targetPressureStart = noteClip.targetPressureStart,
                        targetPressureEnd = noteClip.targetPressureEnd
                    });
                }
            }

            // Keep notes sorted by their hit time.
            notes.Sort((a, b) => a.hitTimeSeconds.CompareTo(b.hitTimeSeconds));
            loadedNoteCount = notes.Count;
            Debug.Log($"[OptionB] Loaded {loadedNoteCount} notes from Timeline '{timeline.name}'.");
        }

        public void StartLevel()
        {
            // Stop if no Timeline asset is assigned.
            if (director.playableAsset == null) return;

            // Lock Timeline playback to the audio DSP
            director.timeUpdateMode = DirectorUpdateMode.DSPClock;
            director.time = startAtSeconds;

            // Skip notes that happen before the selected start time.
            nextNoteIndex = 0;
            while (nextNoteIndex < notes.Count && notes[nextNoteIndex].hitTimeSeconds < startAtSeconds)
            {
                nextNoteIndex++;
            }

            director.Play();
            isPlaying = true;
        }

        private void Update()
        {
            // Do nothing if Timeline is not playing.
            if (!isPlaying || director.state != PlayState.Playing) return;

            // Read the current time from Timeline.
            currentSongTime = director.time;

            // Spawn upcoming notes ahead of time
            while (nextNoteIndex < notes.Count)
            {
                NoteEventData nextNote = notes[nextNoteIndex];
                if (currentSongTime >= nextNote.hitTimeSeconds - approachTimeSeconds)
                {
                    NoteVisual visual = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
                    visual.Initialize(nextNote);
                    activeNotes.Add(new ActiveNote { visual = visual, data = nextNote });
                    nextNoteIndex++;
                }
                else break;
            }

            // Position every note via DSP Lerp
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                ActiveNote item = activeNotes[i];
                double spawnTime = item.data.hitTimeSeconds - approachTimeSeconds;
                // Calculate how far the note has travelled.
                float progress = (float)((currentSongTime - spawnTime) / approachTimeSeconds);

                item.visual.SetNormalizedPosition(spawnPoint.position, hitLinePoint.position, progress);

                if (logSyncAccuracy && !item.loggedArrival && currentSongTime >= item.data.hitTimeSeconds)
                {
                    item.loggedArrival = true;
                    double driftMs = (currentSongTime - item.data.hitTimeSeconds) * 1000.0;
                    Debug.Log($"[OptionB Sync] {item.data.noteType} | Target: {item.data.hitTimeSeconds:F4}s | Actual: {currentSongTime:F4}s | Frame Offset: {driftMs:F2} ms");
                }

                if (currentSongTime > item.data.hitTimeSeconds + item.data.durationSeconds + 0.6)
                {
                    Destroy(item.visual.gameObject);
                    activeNotes.RemoveAt(i);
                }
            }
        }

        private void HandlePilloPressure(string identifier, int pressure)
        {
            // Store the latest pressure for the live readout.
            latestPilloPressure = pressure;
        }
    }
}