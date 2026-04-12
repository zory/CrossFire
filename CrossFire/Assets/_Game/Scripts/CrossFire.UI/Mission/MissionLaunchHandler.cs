using Core.Utilities;
using CrossFire.App;
using CrossFire.HexMap;
using UnityEngine;

namespace CrossFire.App.UI
{
    // Listens for clicks on mission hex tiles and loads the gameplay scene.
    // UI mediator: receives an interaction event, sets the scene request, then loads.
    public class MissionLaunchHandler : MonoBehaviour, IInteractionListener
    {
        [SerializeField]
        private string _gameplaySceneName = "Gameplay";

        private void OnEnable()
        {
            InteractionBus.Register(this);
        }

        private void OnDisable()
        {
            InteractionBus.Unregister(this);
        }

        public void OnInteraction(InteractionEvent interactionEvent)
        {
            if (interactionEvent.Type != InteractionEventType.Click)
            {
                return;
            }

            if (interactionEvent.Context is not HexTileInteractionContext ctx)
            {
                return;
            }

            if (ctx.MissionId < 0)
            {
                return;
            }

            GameplaySceneRequest.Set(new GameplaySceneState { MissionId = ctx.MissionId });
            LevelLoader.Instance.LoadLevel(_gameplaySceneName);
        }
    }
}
