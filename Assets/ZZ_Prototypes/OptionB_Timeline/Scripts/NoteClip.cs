using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PilloRhythm.Core;

namespace PilloRhythm.ProtoB
{
    public class PilloNoteBehaviour : PlayableBehaviour { }

    [Serializable]
    public class NoteClip : PlayableAsset, ITimelineClipAsset
    {
        [Header("Pillo Note Settings")]
        // Type of note that should be played.
        public PilloNoteType noteType = PilloNoteType.SoftTap;

        // Pressure needed when the note starts.
        [Range(0, 100)]
        public int targetPressureStart = 35;

        // Pressure needed when the note ends.
        [Range(0, 100)]
        public int targetPressureEnd = 35;

        // This clip has no special Timeline features.
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            // Create the playable used by the Timeline clip.
            return ScriptPlayable<PilloNoteBehaviour>.Create(graph);
        }
    }
}