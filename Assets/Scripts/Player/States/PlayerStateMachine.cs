namespace NoLightBelow.Player.States
{
    public interface IPlayerState
    {
        string StateName { get; }
        void Enter();
        void Update();
        void FixedUpdate();
        void Exit();
    }

    public class PlayerStateMachine
    {
        public IPlayerState CurrentState { get; private set; }
        public event System.Action<IPlayerState, IPlayerState> OnStateChanged;

        public void Initialize(IPlayerState startingState)
        {
            CurrentState = startingState;
            CurrentState.Enter();
        }

        public void ChangeState(IPlayerState newState)
        {
            if (CurrentState == newState || newState == null) return;

            IPlayerState previousState = CurrentState;
            CurrentState.Exit();
            CurrentState = newState;
            CurrentState.Enter();

            OnStateChanged?.Invoke(previousState, newState);
        }

        public void Update()
        {
            CurrentState?.Update();
        }

        public void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }
    }
}
