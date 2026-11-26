using System.IO;
using UnityEditor;
using UnityEngine;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using PilloRhythm.Core;

namespace PilloRhythm.ProtoA.Editor
{
    [CustomEditor(typeof(MidiLevelDataAsset))]
    public class MidiLevelDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Show the normal asset fields.    
            DrawDefaultInspector();

            MidiLevelDataAsset levelData = (MidiLevelDataAsset)target;

            GUILayout.Space(10);
            // Add a button to import notes from the selected MIDI file.
            if (GUILayout.Button("Import Notes from MIDI (Auto Tempo Map)", GUILayout.Height(32)))
            {
                ImportMidi(levelData);
            }
        }

        private void ImportMidi(MidiLevelDataAsset levelData)
        {
            // Stop if no MIDI file was selected.
            if (levelData.midiFileAsset == null)
            {
                Debug.LogWarning("[OptionA] Assign a .mid file to 'Midi File Asset' first.");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(levelData.midiFileAsset);
            string fullPath = Path.GetFullPath(assetPath);

            // Check that the MIDI file exists before reading it.
            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[OptionA] MIDI file not found at: {fullPath}");
                return;
            }

            MidiFile midiFile = MidiFile.Read(fullPath);
            TempoMap tempoMap = midiFile.GetTempoMap();

            // Save the change and remove the old notes.
            Undo.RecordObject(levelData, "Import MIDI Notes");
            levelData.notes.Clear();

            foreach (var midiNote in midiFile.GetNotes())
            {
                // Convert MIDI time values to seconds.
                MetricTimeSpan startMetric = midiNote.TimeAs<MetricTimeSpan>(tempoMap);
                MetricTimeSpan lengthMetric = midiNote.LengthAs<MetricTimeSpan>(tempoMap);

                double startSec = startMetric.TotalMicroseconds / 1_000_000.0;
                double durSec = lengthMetric.TotalMicroseconds / 1_000_000.0;

                PilloNoteType mappedType = MapPitchToPilloType(midiNote.NoteNumber);
                (int pStart, int pEnd) = GetDefaultPressureRange(mappedType);

                // Tap notes keep duration 0 so they render as short notes
                if (mappedType == PilloNoteType.SoftTap || mappedType == PilloNoteType.HardTap)
                    durSec = 0.0;

                levelData.notes.Add(new NoteEventData
                {
                    hitTimeSeconds = startSec,
                    durationSeconds = durSec,
                    noteType = mappedType,
                    targetPressureStart = pStart,
                    targetPressureEnd = pEnd
                });
            }

            // Keep notes in the order they should be played.
            levelData.notes.Sort((a, b) => a.hitTimeSeconds.CompareTo(b.hitTimeSeconds));
            EditorUtility.SetDirty(levelData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[OptionA] Imported {levelData.notes.Count} notes cleanly with Tempo Map from '{levelData.midiFileAsset.name}'.");
        }

        private static PilloNoteType MapPitchToPilloType(int midiPitch)
        {
            // Map each MIDI pitch to a type of Pillo note.
            return midiPitch switch
            {
                60 => PilloNoteType.SoftTap,        // C4
                62 => PilloNoteType.HardTap,        // D4
                64 => PilloNoteType.SoftHold,       // E4
                65 => PilloNoteType.HardHold,       // F4
                67 => PilloNoteType.GradualPress,   // G4
                69 => PilloNoteType.GradualRelease, // A4
                _ => PilloNoteType.SoftTap
            };
        }

        private static (int start, int end) GetDefaultPressureRange(PilloNoteType type)
        {
            // Set the default pressure for each note type.
            return type switch
            {
                PilloNoteType.SoftTap => (35, 35),
                PilloNoteType.HardTap => (80, 80),
                PilloNoteType.SoftHold => (35, 35),
                PilloNoteType.HardHold => (80, 80),
                PilloNoteType.GradualPress => (15, 100),
                PilloNoteType.GradualRelease => (100, 15),
                _ => (35, 35)
            };
        }
    }
}