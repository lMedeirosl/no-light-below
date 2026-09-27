using UnityEngine;
using NoLightBelow.Core;

namespace NoLightBelow.Player.States
{
    /// <summary>
    /// Estado terrestre padrão do jogador (GroundedState).
    /// Controla movimentação suave com aceleração/desaceleração, rotação voltada para a câmera
    /// e transições para Ataque, Defesa com Escudo, Esquiva e Pulo.
    /// </summary>
    public class PlayerGroundedState : PlayerStateBase
    {
        public override string StateName => "Grounded";

        private float _currentSpeed = 0f;
        private Vector3 _moveDirection = Vector3.zero;

        public PlayerGroundedState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);
            Context.NotifyBlockChanged(false);
        }

        public override void Update()
        {
            // Aplica gravidade e forças verticais
            Context.ApplyGravityAndVerticalMovement();

            // 1. Checagens de Transições de Maior Prioridade
            if (Context.Health != null && Context.Health.IsDead)
            {
                StateMachine.ChangeState(Context.DeadState);
                return;
            }

            // Ataque (LMB / New Input System)
            if (Context.IsAttackPressed())
            {
                StateMachine.ChangeState(Context.AttackState);
                return;
            }

            // Defesa com Escudo (RMB / New Input System)
            if (Context.IsBlockHeld())
            {
                StateMachine.ChangeState(Context.BlockState);
                return;
            }

            // Esquiva (Alt / Ctrl / F)
            if (Context.IsDodgePressed() && Context.CanDodge())
            {
                StateMachine.ChangeState(Context.DodgeState);
                return;
            }

            // Pulo (Espaço)
            if (Context.IsGrounded && Context.IsJumpPressed())
            {
                Context.ExecuteJump();
            }

            // Interação com o ambiente (E)
            if (Context.IsInteractPressed())
            {
                Context.TryInteract();
            }

            // 2. Movimentação Suave e Rotação
            Vector2 input = Context.GetMoveInput();
            float targetSpeed = input.sqrMagnitude > 0.01f ? (Context.IsSprintHeld() ? Context.RunSpeed : Context.WalkSpeed) : 0f;

            // Aceleração / Desaceleração suave
            float accelRate = targetSpeed > _currentSpeed ? Context.Acceleration : Context.Deceleration;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accelRate * Time.deltaTime);

            if (input.sqrMagnitude > 0.01f)
            {
                // Calcula direção relativa à câmera
                Vector3 camForward = Context.MainCamera != null ? Context.MainCamera.transform.forward : Vector3.forward;
                Vector3 camRight = Context.MainCamera != null ? Context.MainCamera.transform.right : Vector3.right;
                camForward.y = 0f;
                camRight.y = 0f;
                camForward.Normalize();
                camRight.Normalize();

                _moveDirection = (camForward * input.y + camRight * input.x).normalized;

                // Rotação suave na direção do movimento
                Quaternion targetRot = Quaternion.LookRotation(_moveDirection, Vector3.up);
                Context.transform.rotation = Quaternion.Slerp(Context.transform.rotation, targetRot, Context.RotationSpeed * Time.deltaTime);
            }

            // Aplica movimento horizontal no CharacterController
            Vector3 horizontalVelocity = _moveDirection * _currentSpeed;
            Context.Controller.Move(horizontalVelocity * Time.deltaTime);

            // Atualiza parâmetro Speed do Animator
            float normalizedSpeed = Context.RunSpeed > 0f ? _currentSpeed / Context.RunSpeed : 0f;
            Context.NotifySpeedChanged(normalizedSpeed);
        }

        public override void Exit()
        {
            _currentSpeed = 0f;
            Context.NotifySpeedChanged(0f);
        }
    }

    /// <summary>
    /// Classes legadas Idle e Move mantidas como wrappers para total compatibilidade regressiva.
    /// </summary>
    public class PlayerIdleState : PlayerGroundedState
    {
        public override string StateName => "Idle";
        public PlayerIdleState(PlayerController context, PlayerStateMachine stateMachine) : base(context, stateMachine) { }
    }

    public class PlayerMoveState : PlayerGroundedState
    {
        public override string StateName => "Move";
        public PlayerMoveState(PlayerController context, PlayerStateMachine stateMachine) : base(context, stateMachine) { }
    }
}
