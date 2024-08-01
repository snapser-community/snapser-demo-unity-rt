using UnityEngine;

namespace Utilities
{
    public static class CanvasGroupExtensions
    {
        public static void Show(this CanvasGroup canvasGroup, bool makeInteractable = true)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = canvasGroup.blocksRaycasts = makeInteractable;
        }

        public static void Hide(this CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = canvasGroup.blocksRaycasts = false;
        }
        
    }
}