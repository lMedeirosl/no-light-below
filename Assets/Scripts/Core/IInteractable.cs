using UnityEngine;

namespace NoLightBelow.Core
{
    public interface IInteractable
    {
        string PromptMessage { get; }
        bool CanInteract { get; }
        void Interact(GameObject user);
        Transform Transform { get; }
    }
}
