using System.Collections.Generic;
using UnityEngine;
using PilloRhythm.Core;

namespace PilloRhythm.ProtoA
{
    [CreateAssetMenu(fileName = "NewMidiLevel", menuName = "Prototypes/Option A - MIDI Level Data")]
    public class MidiLevelDataAsset : ScriptableObject
    {
        [Header("Audio & Timing")]
        public AudioClip songClip;
        [Tooltip("Manual offset in seconds if the audio has silence in the start")]
        public double firstBeatOffset = 0.0;
        [Tooltip("How many seconds before the hit time and a note Spawns.")]
        public double approachTimeSeconds = 2.0;

#if UNITY_EDITOR
        [Header("MIDI Source (Editor Only)")]
        [Tooltip("Drag a .mid file exported from FL Studio here, then click 'Import Notes from MIDI'.")]
        public Object midiFileAsset;
#endif

        [Header("Analyzed Notes")]
        public List<NoteEventData> notes = new List<NoteEventData>();
    }
}