using UnityEngine;
using NoLightBelow.Dungeon;

namespace NoLightBelow.Dungeon.Procedural
{
    [SelectionBase]
    public class RoomConnector : MonoBehaviour
    {
        [Header("Connector Configuration")]
        [Tooltip("Define se este conector é a entrada principal da sala.")]
        [SerializeField] private bool isEntrance = false;

        [Tooltip("Referência opcional para o portão físico associado a este conector.")]
        [SerializeField] private DungeonGate associatedGate;

        private bool _isConnected = false;
        private RoomConnector _connectedNeighbor;

        public bool IsEntrance => isEntrance;
        public bool IsConnected => _isConnected;
        public RoomConnector ConnectedNeighbor => _connectedNeighbor;
        public DungeonGate AssociatedGate => associatedGate;

        public Vector3 WorldPosition => transform.position;
        public Vector3 WorldForward => transform.forward;

        public void Connect(RoomConnector neighbor)
        {
            _isConnected = true;
            _connectedNeighbor = neighbor;

            if (associatedGate == null && neighbor != null && neighbor.AssociatedGate != null)
            {
                associatedGate = neighbor.AssociatedGate;
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _connectedNeighbor = null;
        }

        public void SetAssociatedGate(DungeonGate gate)
        {
            associatedGate = gate;
        }

        public void SetAsEntrance(bool entrance)
        {
            isEntrance = entrance;
        }

        private void OnDrawGizmos()
        {
            // Visual feedback in Scene View
            Gizmos.color = isEntrance ? Color.cyan : (_isConnected ? Color.green : Color.red);
            Gizmos.DrawSphere(transform.position, 0.35f);

            // Forward direction arrow
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}
