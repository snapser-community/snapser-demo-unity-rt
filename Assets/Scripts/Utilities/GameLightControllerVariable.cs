using UnityEngine;

namespace Utilities
{
    [CreateAssetMenu(menuName = "Assets/ScriptableObjectVariable")]
    public class GameLightControllerVariable : ScriptableObject
    {
        private GameLightController value;

        public GameLightController Value
        {
            get => value;
            set => this.value = value;
        }
    }
}