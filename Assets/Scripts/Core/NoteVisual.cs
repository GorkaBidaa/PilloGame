using UnityEngine;

namespace PilloRhythm.Core
{
    public class NoteVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public NoteEventData Data { get; set; }

        public void Initialize(NoteEventData data)
        {
            Data = data;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                spriteRenderer.color = GetColorForNoteType(data.noteType);

            // hold notes should be longer
            float lengthScale = data.durationSeconds > 0.05
                ? Mathf.Clamp(1f + (float)data.durationSeconds * 1.5f, 1.2f, 5f)
                : 0.7f;

            transform.localScale = new Vector3(lengthScale, 0.7f, 1f);
        }

        // path = 0 -> in spawn, path = 1 -> at the end (hit line)
        public void SetNormalizedPosition(Vector3 spawnPos, Vector3 hitLinePos, float path)
        {
            transform.position = Vector3.LerpUnclamped(spawnPos, hitLinePos, path);
        }

        private static Color GetColorForNoteType(PilloNoteType type)
        {
            return type switch
            {
                PilloNoteType.SoftTap => new Color(0.35f, 0.78f, 0.75f), 
                PilloNoteType.HardTap => new Color(0.12f, 0.25f, 0.45f), 
                PilloNoteType.SoftHold => new Color(0.45f, 0.85f, 0.55f),
                PilloNoteType.HardHold => new Color(0.85f, 0.30f, 0.28f), 
                PilloNoteType.GradualPress => new Color(0.95f, 0.68f, 0.20f), 
                PilloNoteType.GradualRelease => new Color(0.65f, 0.45f, 0.85f),
                _ => Color.white
            };
        }
    }
}