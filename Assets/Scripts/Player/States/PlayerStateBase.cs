using UnityEngine;
using NoLightBelow.Core;

namespace NoLightBelow.Player.States
{
    public abstract class PlayerStateBase : IPlayerState
    {
        protected readonly PlayerController Context;
        protected readonly PlayerStateMachine StateMachine;

        public abstract string StateName { get; }

        protected PlayerStateBase(PlayerController context, PlayerStateMachine stateMachine)
        {
            Context = context;
            StateMachine = stateMachine;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }

        protected void CheckCommonTransitions()
        {
            if (Context.Stats.IsDead)
            {
                StateMachine.ChangeState(Context.DeadState);
                return;
            }

            // Attack (LMB)
            if (InputBridge.IsAttackDown())
            {
                StateMachine.ChangeState(Context.AttackState);
                return;
            }

            // Block (RMB hold)
            if (InputBridge.IsBlockHeld())
            {
                StateMachine.ChangeState(Context.BlockState);
                return;
            }

            // Dodge (Alt / Ctrl / F)
            if (InputBridge.IsDodgeDown() && Context.CanDodge())
            {
                StateMachine.ChangeState(Context.DodgeState);
                return;
            }

            // Jump (Space)
            if (Context.IsGrounded && InputBridge.IsJumpDown())
            {
                Context.ExecuteJump();
            }
        }
    }
}
