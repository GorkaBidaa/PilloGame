using UnityEngine;
using static PilloRhythmGame;

namespace Assets.Scripts
{
    public class ScoreManager : MonoBehaviour
    {
        private int score = 0;

        // Define states, needs to be defined later based on the preffered score system (stars, points system, etc.)
        // Define new events? UI changes, needs to be debated.

        // Updates the score on hit -> Is handled in the main file (PilloRhythmGame.cs)
        public void UpdateScore(int score)
        {
            score++;
            Debug.Log("Current score: " + score);
        }

        public int GetScore() => score;

        // Function to determine end score based on states (again, stars, etc.)
    }
}