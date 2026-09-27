using UnityEngine;
using NoLightBelow.Core;

namespace NoLightBelow.Player.States
{
    /// <summary>
    /// Estado de Ataque do Jogador (AttackState).
    /// Controla sequência de combo corpo a corpo de até 3 ataques com espada bastarda,
    /// avanço no golpe (lunging step), ativação de hitboxes e Screen Shake via Cinemachine Impulse.
    /// </summary>
    public class PlayerAttackState : PlayerStateBase
    {
        public override string StateName => "Attack";

        private int _currentAttackIndex = 1;
        private bool _canBufferNextAttack = false;
        private bool _nextAttackBuffered = false;
        private float _attackTimer = 0f;
        private float _attackDuration = 0.42f;
        private Vector3 _lungeDirection = Vector3.forward;

        public PlayerAttackState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);
            Context.NotifyBlockChanged(false);

            _currentAttackIndex = 1;
            _nextAttackBuffered = false;
            ExecuteAttackStrike(_currentAttackIndex);
        }

        private void ExecuteAttackStrike(int index)
        {
            _currentAttackIndex = index;
            _attackTimer = 0f;
            _canBufferNextAttack = false;
            _nextAttackBuffered = false;

            // Determina duração baseada no índice do combo (terceiro golpe é mais pesado)
            _attackDuration = index == 3 ? 0.58f : 0.42f;

            // Rotação em direção à câmera para mirar o golpe
            Vector3 camForward = Context.MainCamera != null ? Context.MainCamera.transform.forward : Context.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude > 0.01f)
            {
                _lungeDirection = camForward.normalized;
                Context.transform.rotation = Quaternion.LookRotation(_lungeDirection, Vector3.up);
            }
            else
            {
                _lungeDirection = Context.transform.forward;
            }

            // Notifica o Animator com o índice de ataque (1, 2, 3)
            Context.NotifyAttackTriggered(index);

            // Dispara Cinemachine Impulse / Screen Shake de impacto
            float shakeIntensity = index == 3 ? 0.35f : 0.18f;
            Context.GenerateImpulseShake(shakeIntensity);

            // Executa animação física e ativação de hitbox via PlayerCombat
            Context.Combat?.ExecuteComboAttackIndex(index, () =>
            {
                // Callback chamado pelo hitbox ao acertar um inimigo
                Context.GenerateImpulseShake(0.28f);
            });
        }

        public override void Update()
        {
            Context.ApplyGravityAndVerticalMovement();

            if (Context.Health != null && Context.Health.IsDead)
            {
                StateMachine.ChangeState(Context.DeadState);
                return;
            }

            _attackTimer += Time.deltaTime;

            // Passo à frente no início do golpe (Lunging Step de impacto)
            if (_attackTimer < 0.18f)
            {
                float stepSpeed = _currentAttackIndex == 3 ? 6.5f : 5.0f;
                Context.Controller.Move(_lungeDirection * (stepSpeed * Time.deltaTime));
            }

            // Janela de buffer do próximo combo (a partir de 45% da duração do golpe)
            if (_attackTimer >= _attackDuration * 0.45f)
            {
                _canBufferNextAttack = true;
            }

            if (_canBufferNextAttack && Context.IsAttackPressed())
            {
                _nextAttackBuffered = true;
            }

            // Cancelamento emergencial com esquiva (Dodge)
            if (Context.IsDodgePressed() && Context.CanDodge())
            {
                StateMachine.ChangeState(Context.DodgeState);
                return;
            }

            // Golpe finalizado: encadeia próximo ataque ou retorna ao GroundedState
            if (_attackTimer >= _attackDuration)
            {
                if (_nextAttackBuffered && _currentAttackIndex < 3)
                {
                    ExecuteAttackStrike(_currentAttackIndex + 1);
                }
                else
                {
                    StateMachine.ChangeState(Context.GroundedState);
                }
            }
        }

        public override void Exit()
        {
            _nextAttackBuffered = false;
            Context.NotifyAttackTriggered(0);
        }
    }

    /// <summary>
    /// Estado de Defesa com Escudo (BlockState).
    /// Levanta o broquel/escudo, reduzindo dano frontal recebido em 85% e permitindo movimentação tática.
    /// </summary>
    public class PlayerBlockState : PlayerStateBase
    {
        public override string StateName => "Block";

        public PlayerBlockState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            Context.IsBlocking = true;
            Context.Combat?.SetShieldGuard(true);
            Context.NotifyBlockChanged(true);
        }

        public override void Exit()
        {
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);
            Context.NotifyBlockChanged(false);
        }

        public override void Update()
        {
            Context.ApplyGravityAndVerticalMovement();

            if (Context.Health != null && Context.Health.IsDead)
            {
                StateMachine.ChangeState(Context.DeadState);
                return;
            }

            // Soltou o botão de defesa (RMB): volta para GroundedState
            if (!Context.IsBlockHeld())
            {
                StateMachine.ChangeState(Context.GroundedState);
                return;
            }

            // Ataque rápido a partir da guarda
            if (Context.IsAttackPressed())
            {
                StateMachine.ChangeState(Context.AttackState);
                return;
            }

            // Esquiva a partir da guarda
            if (Context.IsDodgePressed() && Context.CanDodge())
            {
                StateMachine.ChangeState(Context.DodgeState);
                return;
            }

            // Movimentação defensiva (strafing mais lento, cerca de 45% da velocidade)
            Vector2 moveInput = Context.GetMoveInput();
            Context.ExecuteGuardStrafing(moveInput);
        }
    }

    /// <summary>
    /// Estado de Dano / Impacto (HurtState).
    /// Executa recuo e micro-stagger quando o cavaleiro recebe um golpe na carne.
    /// </summary>
    public class PlayerHurtState : PlayerStateBase
    {
        public override string StateName => "Hurt";
        private float _staggerTimer = 0f;
        private readonly float _staggerDuration = 0.24f;

        public PlayerHurtState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            _staggerTimer = _staggerDuration;
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);

            // Dispara trigger de Hit no Animator
            Context.NotifyHurtTriggered();

            // Screen Shake pelo dano recebido
            Context.GenerateImpulseShake(0.32f);
        }

        public override void Update()
        {
            Context.ApplyGravityAndVerticalMovement();

            if (Context.Health != null && Context.Health.IsDead)
            {
                StateMachine.ChangeState(Context.DeadState);
                return;
            }

            _staggerTimer -= Time.deltaTime;
            if (_staggerTimer <= 0f)
            {
                // Se o jogador ainda estiver segurando o botão de defesa, volta bloqueando
                if (Context.IsBlockHeld())
                {
                    StateMachine.ChangeState(Context.BlockState);
                }
                else
                {
                    StateMachine.ChangeState(Context.GroundedState);
                }
            }
        }
    }

    /// <summary>
    /// Estado de Morte (DeadState).
    /// Desativa os controles e colisores e dispara a animação de morte.
    /// </summary>
    public class PlayerDeadState : PlayerStateBase
    {
        public override string StateName => "Dead";

        public PlayerDeadState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);
            Context.NotifyBlockChanged(false);

            if (Context.Controller != null)
            {
                Context.Controller.enabled = false;
            }

            Context.NotifyDeathTriggered();
            Context.OnDeathEvent?.Invoke();
        }
    }

    /// <summary>
    /// Estado de Esquiva / Rolamento (DodgeState).
    /// Movimentação rápida com i-frames.
    /// </summary>
    public class PlayerDodgeState : PlayerStateBase
    {
        public override string StateName => "Dodge";
        private float _elapsed;
        private Vector3 _dodgeDir;

        public PlayerDodgeState(PlayerController context, PlayerStateMachine stateMachine) 
            : base(context, stateMachine) { }

        public override void Enter()
        {
            _elapsed = 0f;
            Context.IsBlocking = false;
            Context.Combat?.SetShieldGuard(false);

            Vector2 move = Context.GetMoveInput();
            _dodgeDir = Context.CalculateDodgeDirection(move);
            Context.FaceDirection(_dodgeDir);

            if (Context.Stats != null)
            {
                Context.Stats.IsInvulnerable = true;
                Context.Stats.ConsumeStamina(Context.DodgeStaminaCost);
                Context.TriggerCardDodgeEffects();
            }

            Context.OnDodgeExecutedEvent?.Invoke();
            Context.GenerateImpulseShake(0.12f);
        }

        public override void Update()
        {
            Context.ApplyGravityAndVerticalMovement();

            _elapsed += Time.deltaTime;
            float duration = Context.DodgeDuration;

            if (_elapsed < duration)
            {
                float t = _elapsed / duration;
                float speedMult = Mathf.Lerp(1.35f, 0.65f, t);
                Context.Controller.Move(_dodgeDir * (Context.DodgeSpeed * speedMult * Time.deltaTime));
            }
            else
            {
                StateMachine.ChangeState(Context.GroundedState);
            }
        }

        public override void Exit()
        {
            if (Context.Stats != null)
            {
                Context.Stats.IsInvulnerable = false;
            }
            Context.SetLastDodgeTime(Time.time);
        }
    }
}
