using System;
using UnityEngine;

namespace PilloRhythm.Core
{
    public enum PilloNoteType
    {
        SoftTap,
        HardTap,
        SoftHold,
        HardHold,
        GradualPress,
        GradualRelease
    }

    [Serializable]
    public struct NoteEventData
    {
        [Tooltip("hit time in seconds from the start of the song")]
        public double hitTimeSeconds;

        [Tooltip("Duration in seconds")]
        public double durationSeconds;

        [Tooltip("Which type of note this is")]
        public PilloNoteType noteType;

        [Range(0, 100)]
        [Tooltip("Target pressure value (0-100).")]
        public int targetPressureStart;

        [Range(0, 100)]
        [Tooltip("Ending pressure for Gradual notes.")]
        public int targetPressureEnd;
    }
}