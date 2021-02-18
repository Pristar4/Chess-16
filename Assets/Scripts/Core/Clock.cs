using TMPro;
using UnityEngine;

namespace Chess.Game
{
    public class Clock : MonoBehaviour
    {
        public TMP_Text timerUI;
        public bool isTurnToMove;
        public int startSeconds;
        public int lowTimeThreshold = 10;

        [Range(0, 1)] public float inactiveAlpha = 0.75f;

        [Range(0, 1)] public float decimalFontSizeMultiplier = 0.75f;

        public Color lowTimeCol;
        private float secondsRemaining;

        private void Start()
        {
            secondsRemaining = startSeconds;
        }

        private void Update()
        {
            if (isTurnToMove)
            {
                secondsRemaining -= Time.deltaTime;
                secondsRemaining = Mathf.Max(0, secondsRemaining);
            }

            var numMinutes = (int) (secondsRemaining / 60);
            var numSeconds = (int) (secondsRemaining - numMinutes * 60);

            timerUI.text = $"{numMinutes:00}:{numSeconds:00}";
            if (secondsRemaining <= lowTimeThreshold)
            {
                var dec = (int) ((secondsRemaining - numSeconds) * 10);
                var size = timerUI.fontSize * decimalFontSizeMultiplier;
                timerUI.text += $"<size={size}>.{dec}</size>";
            }

            var col = Color.white;
            if ((int) secondsRemaining <= lowTimeThreshold) col = lowTimeCol;
            if (!isTurnToMove) col = new Color(col.r, col.g, col.b, inactiveAlpha);
            timerUI.color = col;
        }
    }
}