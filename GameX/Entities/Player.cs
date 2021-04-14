using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Nez;
using Nez.Textures;
using Nez.Sprites;
using Nez.Tiled;
using GameX.Components;
using GameX.Constants;
using GameX.Utils;

namespace GameX.Entities
{
    class Player : Entity
    {

        // Damage
        public Vector2 DamageImpulseVector = new Vector2(100f, -200f);
        public float DamageLockTime = 0.5f; // The time that the player is locked from input while the damage animation plays
        public float DamageProtectionLockTime = 1f;
        public Color DamageProtectionBlinkColor = new Color(255, 255, 255);
        public float DamageProtectionBlinkSpeed = 10f;

        // Animation
        public int BlinkColorAlpha = 100; // The alpha amount the charge blink effect will "blink" to

        // Stats
        public float Health = 100f;

        // Movement
        public float MoveSpeed = 125f;
        public float DashSpeed = 300f;
        public float WallSlideSpeed = 100f;
        public float MaxGroundDashTime = 0.50f;
        public float MaxAirDashTime = 0.35f;
        public float Gravity = 1000f;
        public float TerminalVelocity = 400f;
        public float JumpHeight = 75f;
        public float WallJumpTimeWindow = 0.1f;
        public float WallJumpMultiplier = 1.5f; // multiplier to increase the distance a wall jump goes away from the wall

        // Weapons
        Dictionary<string, int> _projectileFpsData = new Dictionary<string, int>();
        public Color ChargeBlinkColor = new Color(1, 1, 1);
        public float HalfChargeTime = 0.75f;
        public float FullChargeTime = 1.50f;
        public float ProjectileSpeed = 400f;
        public float AttackInputLockTime = 0.10f;
        public float NoChargeDamage = 10f;
        public float HalfChargeDamage = 50f;
        public float FullChargeDamage = 100f;
        public float ChargeBlinkSpeed = 20f; // The rate at which the charge blink effect will display
        public float ChargeEffectDelayTime = 0.5f; // The amount of time the attack button needs to be held before player starts blinking
        public float ShootTimeWindow = 0.30f; // The time window when running that shoot will be considered "pressed" - for smooth run_shoot anim looping with fire spamming

        // Components
        SpriteAnimator _animator;
        BoxCollider _collider;
        TiledMapMover _mover;
        TmxMap _sceneTileMap;
        PlayerCamera _camera;

        // Local state
        TiledMapMover.CollisionState _collisionState = new TiledMapMover.CollisionState();
        Vector2 _velocity;
        AnimationInstruction _lastAnimation;
        AnimationInstruction _currentAnimation;
        ChargeState _chargeState;
        Color _blinkColor = new Color(1, 1, 1, 0);
        ITimer _blinkTimer = null;

        bool _facingRight = true;
        bool _jumping = false;
        bool _dashJumping = false;
        bool _wallSliding = false;
        bool _wallJumping = false;
        bool _groundDashing = false;
        bool _airDashing = false;
        bool _canAirDash = true;
        bool _canJump = false;
        bool _chargingShot = false;
        bool _canFire = false;
        bool _damageLocked = false;
        bool _damageProtected = false;
        bool _damagedThisFrame = false;
        float _chargeTime = 0;
        float _groundDashTime = 0;
        float _airDashTime = 0;
        float _wallJumpDashTime = 0;
        float _attackLockTime = 0;
        float _attackReleaseTime = 0; // amount of time the shoot button has not been pressed
        float _wallJumpTime = 0;

        // Input Axis
        VirtualIntegerAxis _xAxisInput;
        VirtualIntegerAxis _yAxisInput;

        // Input Buttons
        VirtualButton _jumpInput;
        VirtualButton _dashInput;
        VirtualButton _attackInput;
        VirtualButton _weaponChangeInput;

        enum ChargeState
        {
            NONE,
            HALF,
            FULL
        }

        class AnimationInstruction
        {
            public string name;
            public SpriteAnimator.LoopMode loopMode;
            public int startFrame = 0;
        }

        #region SETUP

        public Player(TmxMap sceneTileMap)
        {
            _sceneTileMap = sceneTileMap;
            _camera = new PlayerCamera(this);
        }

        public override void OnAddedToScene()
        {
            this.SetTag((int)EntityTags.PLAYER);

            // Attach player camera to scene
            Scene.Camera.AddComponent(_camera);

            // Setup components and inputs
            _mover = this.AddComponent(new TiledMapMover(_sceneTileMap.GetLayer<TmxLayer>("main")));
            SetCollider();
            ConfigureAnimations();
            SetupInput();

            _camera.UpdateMapSize(new Vector2(_sceneTileMap.WorldWidth, _sceneTileMap.WorldHeight));
            Debug.Log(_camera.MapSize);
        }

        private void SetCollider()
        {
            int width = 16;
            int height = 32;
            int widthOffset = 1;
            int heightOffset = 7;

            BoxCollider boxCollider = new BoxCollider(-width / 2 + widthOffset, -height / 2 + heightOffset, width, height);
            _collider = this.AddComponent(boxCollider);
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)PhysicsLayers.PLAYER);
        }

        private void ConfigureAnimations()
        {
            // projectile fps data
            _projectileFpsData.Add("hit", 32);

            Dictionary<string, int> fpsData = new Dictionary<string, int>();
            fpsData.Add("idle", 6);
            fpsData.Add("grounded", 25);
            fpsData.Add("run", 18);
            fpsData.Add("run_shoot", 18);
            fpsData.Add("dash", 18);
            fpsData.Add("idle_shoot_weak", 18);
            fpsData.Add("idle_shoot_strong", 18);

            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, "Assets/Player/atlas", fpsData);
            _animator.RenderLayer = (int)RenderLayers.PLAYER;

            SpriteBlinkEffect blinkEffect = new SpriteBlinkEffect();
            blinkEffect.BlinkColor = _blinkColor;
            _animator.Material = new Material(BlendState.NonPremultiplied, blinkEffect);

            this.AddComponent<SpriteAnimator>(_animator);

            _lastAnimation = new AnimationInstruction();
            _currentAnimation = new AnimationInstruction();
            _animator.Play("idle");
        }

        private void SetupInput()
        {
            _xAxisInput = new VirtualIntegerAxis();
            _xAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadLeftRight());
            _xAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickX());
            _xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Left, Keys.Right));
            _xAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.A, Keys.D));

            _yAxisInput = new VirtualIntegerAxis();
            _yAxisInput.Nodes.Add(new VirtualAxis.GamePadDpadUpDown());
            _yAxisInput.Nodes.Add(new VirtualAxis.GamePadLeftStickY());
            _yAxisInput.Nodes.Add(new VirtualAxis.KeyboardKeys(VirtualInput.OverlapBehavior.TakeNewer, Keys.Up, Keys.Down));

            _jumpInput = new VirtualButton();
            _jumpInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.A));
            _jumpInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Space));
            _jumpInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.W));

            _dashInput = new VirtualButton();
            _dashInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.B));
            _dashInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.LeftShift));

            _attackInput = new VirtualButton();
            _attackInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
            _attackInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.F));

            _weaponChangeInput = new VirtualButton();
            _weaponChangeInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Y));
            _weaponChangeInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
        }

        #endregion

        public override void Update()
        {
            base.Update();

            HandleMovement();
         
            HandleWeaponInput();

            HandleAnimations();

            CheckForCollisions();
        }

        #region COLLISIONS
        private void CheckForCollisions()
        {
            if (!_damageLocked && !_damageProtected)
            {
                HashSet<Collider> neighborColliders = Physics.BoxcastBroadphaseExcludingSelf(_collider, 1 << (int)PhysicsLayers.ENEMIES);

                foreach (Collider neighborCollider in neighborColliders)
                {
                    if (_collider.Overlaps(neighborCollider))
                    {
                        _damageLocked = true;
                        _damagedThisFrame = true;
                        TakeDamage(10f);
                    }
                }
            } else
            {
                _damagedThisFrame = false;
            }
        }

        private void TakeDamage(float damage)
        {
            Core.Schedule(DamageLockTime, this, EndDamageLockAction);
            Health -= damage;
            if (Health <= 0)
            {
                // TODO play death animation
                this.Destroy();
            }
        }

        private static void EndDamageLockAction(ITimer timer)
        {
            Player player = timer.GetContext<Player>();
            player._damageLocked = false;

            player.StartDamageProtectionLock();
        }

        private void StartDamageProtectionLock()
        {
            _damageProtected = true;
            StartBlinkTimer(DamageProtectionBlinkColor, DamageProtectionBlinkSpeed);
            Core.Schedule(DamageProtectionLockTime, this, EndDamageProtectionLockAction);
        }

        private static void EndDamageProtectionLockAction(ITimer timer)
        {
            Player player = timer.GetContext<Player>();
            player._damageProtected = false;
            player.EndBlinkTimer();
        }


        #endregion

        #region ANIMATIONS

        private static void BlinkAction(ITimer timer)
        {
            Player player = timer.GetContext<Player>();

            if (player._blinkColor.A == Convert.ToByte(0))
            {
                player._blinkColor.A = Convert.ToByte(player.BlinkColorAlpha);
            }
            else if (player._blinkColor.A == Convert.ToByte(player.BlinkColorAlpha))
            {
                player._blinkColor.A = Convert.ToByte(0);
            }

            player.UpdateBlinkMaterial();
        }

        private void UpdateBlinkMaterial()
        {
            SpriteBlinkEffect blinkEffect = new SpriteBlinkEffect();
            blinkEffect.BlinkColor = _blinkColor;

            _animator.Material.Effect = blinkEffect;
        }

        private void UpdateBlinkColor(int r, int g, int b)
        {
            _blinkColor.R = Convert.ToByte(r);
            _blinkColor.G = Convert.ToByte(g);
            _blinkColor.B = Convert.ToByte(b);
        }

        private void UpdateBlinkColor(Color color)
        {
            _blinkColor.R = color.R;
            _blinkColor.G = color.G;
            _blinkColor.B = color.B;
        }

        private void StartBlinkTimer(Color blinkColor, float speed)
        {
            if (_blinkTimer != null) EndBlinkTimer();
            UpdateBlinkColor(blinkColor);
            _blinkTimer = Core.Schedule(1 / speed, true, this, BlinkAction);
        }

        private void EndBlinkTimer()
        {
            if (_blinkTimer != null) { 
                _blinkTimer.Stop();
                _blinkTimer = null;
            }

            _blinkColor.A = Convert.ToByte(0);
            SpriteBlinkEffect blinkEffect = new SpriteBlinkEffect();
            blinkEffect.BlinkColor = _blinkColor;

            _animator.Material.Effect = blinkEffect;
        }

        private void HandleAnimations()
        {
            _lastAnimation = _currentAnimation;

            if (CanNextAnimationInterruptLast(GetAnimationInstruction(), _lastAnimation))
            {
                _currentAnimation = GetAnimationInstruction();

                if (_currentAnimation != null && !_animator.IsAnimationActive(_currentAnimation.name))
                {
                    if (_currentAnimation.startFrame != 0)
                    {
                        _animator.PlayAtFrame(_currentAnimation.name, _currentAnimation.startFrame, _currentAnimation.loopMode);
                    }
                    else
                    {
                        _animator.Play(_currentAnimation.name, _currentAnimation.loopMode);
                    }
                }
            }
        }

        private bool CanNextAnimationInterruptLast(AnimationInstruction nextAnimation, AnimationInstruction lastAnimation)
        {
            bool isShootingWindowActive = _attackReleaseTime < (ShootTimeWindow) && _chargeState.Equals(ChargeState.NONE);
            bool animCompleted = _animator.AnimationState.Equals(SpriteAnimator.State.Completed);

            /** Special behavior for certain anim sequences */
            if (lastAnimation.name == "damaged" && _damageLocked) return false;
            if (lastAnimation.name == "grounded" && nextAnimation.name == "idle" && !animCompleted) return false;
            if (lastAnimation.name == "idle_shoot_strong" && nextAnimation.name == "idle" && !animCompleted) return false;
            if (lastAnimation.name == "run_shoot" && nextAnimation.name == "run" && isShootingWindowActive) return false;
            if (lastAnimation.name == "jump_shoot" && nextAnimation.name == "jump" && isShootingWindowActive) return false;
            if (lastAnimation.name == "fall_shoot" && nextAnimation.name == "fall" && isShootingWindowActive) return false;

            /** Proceed to next animation if:
             * last animation was Once or ClampForever and is completed
             */
            if (
                (_lastAnimation.loopMode.Equals(SpriteAnimator.LoopMode.ClampForever)
                || _lastAnimation.loopMode.Equals(SpriteAnimator.LoopMode.Once))
                && _animator.AnimationState.Equals(SpriteAnimator.State.Completed))
            {
                return true;
            }

            /** Default rest to true */
            return true;
        }

        private AnimationInstruction GetAnimationInstruction()
        {
            AnimationInstruction animation = new AnimationInstruction();

            animation.name = "idle";
            animation.loopMode = SpriteAnimator.LoopMode.Loop;

            // movement animations
            if (!_collisionState.WasGroundedLastFrame && _collisionState.BecameGroundedThisFrame)
            {
                animation.name = "grounded";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
                return animation;
            }
            if (_velocity.X == 0 && _collisionState.Below)
            {
                animation.name = "idle";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
            }

            if (_velocity.X != 0 && _velocity.Y == 0 && _collisionState.Below)
            {
                animation.name = "run";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
                animation.startFrame = _animator.CurrentFrame;
            }

            if(_groundDashing || _airDashing)
            {
                animation.name = "dash";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
            }

            if(_wallSliding && _velocity.Y > 0)
            {
                animation.name = "wall_slide";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
            }

            if(_velocity.Y < 0)
            {
                animation.name = "jump";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
            }

            if (_velocity.Y > 0 && !_wallSliding)
            {
                animation.name = "fall";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
            }


            // action animations
            bool isShooting = _attackInput.IsPressed || (_attackInput.IsReleased && !_chargeState.Equals(ChargeState.NONE));
            bool isShootingWithWindow = isShooting || (!_attackInput.IsDown && _attackReleaseTime < ShootTimeWindow); // adding time window for shoot animations that linger/loop (i.e. run, etc.)

            if (animation.name == "idle" && isShooting)
            {
                animation.name = "idle_shoot_strong";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
            }

            if(animation.name == "run" && isShootingWithWindow)
            {
                animation.name = "run_shoot";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
                animation.startFrame = _animator.CurrentFrame;
            }

            if(animation.name == "jump" && isShootingWithWindow)
            {
                animation.name = "jump_shoot";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
                animation.startFrame = _animator.CurrentFrame;
            }

            if (animation.name == "fall" && isShootingWithWindow)
            {
                animation.name = "fall_shoot";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
                animation.startFrame = _animator.CurrentFrame;
            }

            if (_lastAnimation.name == "jump_shoot" && _velocity.Y > 0)
            {
                animation.name = "fall_shoot";
                animation.loopMode = SpriteAnimator.LoopMode.ClampForever;
                animation.startFrame = _animator.CurrentFrame;
            }

            if(_damageLocked)
            {
                animation.name = "damaged";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
            }

            return animation;
        }

        #endregion

        #region WEAPONS

        private void HandleWeaponInput()
        {

            if (_attackInput.IsDown)
            {
                if(_blinkTimer == null && _chargeTime >= ChargeEffectDelayTime && !_damageLocked && !_damageProtected)
                {
                    StartBlinkTimer(ChargeBlinkColor, ChargeBlinkSpeed);
                }
                _chargeTime += Time.DeltaTime;
                _chargingShot = true;

                // reset charge ready flags
                _chargeState = ChargeState.NONE;

                // reset shoot release time;
                _attackReleaseTime = 0;
            }

            if(_attackInput.IsReleased)
            {
                if(!_chargeState.Equals(ChargeState.NONE))
                {
                    SpawnProjectile();
                    _canFire = false;
                }
                _chargeTime = 0;
                _chargingShot = false;

                if (!_damageProtected && !_damageProtected)
                {
                    EndBlinkTimer();
                }
            }

            if (_attackInput.IsPressed && _canFire)
            {
                SpawnProjectile();
                _canFire = false;
            }

            if (!_attackInput.IsDown)
            {
                _attackLockTime += Time.DeltaTime;
                if (_attackReleaseTime < ShootTimeWindow) _attackReleaseTime += Time.DeltaTime;                
            }

            if(_attackLockTime >= AttackInputLockTime)
            {
                _attackLockTime = 0;
                _canFire = true;
            }

            if(_chargeTime >= HalfChargeTime)
            {
                _chargeState = ChargeState.HALF;
            }

            if(_chargeTime >= FullChargeTime)
            {
                _chargeState = ChargeState.FULL;
            }  
        }

        private void SpawnProjectile()
        {
            if (!_damageLocked)
            {
                Projectile projectile = new Projectile(Scene,
                    this.Position + GetProjectileOffsetForAnimation(_currentAnimation.name),
                    PhysicsLayers.PLAYER_PROJECTILE,
                    PhysicsLayers.ENEMIES,
                    GetColliderDimensionsForChargeState(_chargeState),
                    GetAtlasPathForChargeState(_chargeState),
                    _projectileFpsData);
                float xVelocity = _facingRight ? ProjectileSpeed : -ProjectileSpeed;
                projectile.Velocity = new Vector2(xVelocity, 0);
                projectile.Damage = GetDamageForChargeState(_chargeState);
                projectile.HasHitAnim = true;
                if (!_chargeState.Equals(ChargeState.NONE)) projectile.ContinuesAfterKill = true; 
            }
        }

        private string GetAtlasPathForChargeState(ChargeState chareState)
        {
            switch(chareState)
            {
                case ChargeState.NONE:
                    return "Assets/Player/Weapons/WaterCannon/Projectiles/Normal/atlas";
                case ChargeState.HALF:
                    return "Assets/Player/Weapons/WaterCannon/Projectiles/HalfCharge/atlas";
                case ChargeState.FULL:
                    return "Assets/Player/Weapons/WaterCannon/Projectiles/FullCharge/atlas";
                default:
                    return "Assets/Player/Weapons/WaterCannon/Projectiles/Normal/atlas";
            }
        }

        private float GetDamageForChargeState(ChargeState chargeState)
        {
            switch (chargeState)
            {
                case ChargeState.NONE:
                    return NoChargeDamage;
                case ChargeState.HALF:
                    return HalfChargeDamage;
                case ChargeState.FULL:
                    return FullChargeDamage;
                default:
                    return NoChargeDamage;
            }
        }

        private Vector2 GetColliderDimensionsForChargeState(ChargeState chargeState)
        {
            switch (chargeState)
            {
                case ChargeState.NONE:
                    return new Vector2(2, 6);
                case ChargeState.HALF:
                    return new Vector2(2, 9);
                case ChargeState.FULL:
                    return new Vector2(2, 9);
                default:
                    return new Vector2(2, 9);
            }
        }

        private Vector2 GetProjectileOffsetForAnimation(string animationName)
        {
            Vector2 offset = new Vector2(12, 4);
            if (animationName.Contains("jump") || animationName.Contains("fall"))
            {
                offset = new Vector2(12, -5);
            }
            if(_animator.FlipX)
            {
                offset.X *= -1;
            }
            return offset;
        }

        #endregion

        #region MOVEMENT

        private void HandleMovement()
        {

            if (!_damageLocked)
            {
                // Horizontal movement input
                Vector2 moveDir = new Vector2(_xAxisInput.Value, 0);

                bool xInputPresent = Math.Abs(moveDir.X) > 0;

                // start ground dash
                if ((_collisionState.Below || _wallSliding) && (_dashInput.IsPressed || _groundDashing))
                {
                    _velocity.X = _facingRight ? DashSpeed : -DashSpeed;
                    _groundDashing = true;
                    _groundDashTime += Time.DeltaTime;
                }

                // start air dash
                if (!_collisionState.Below && !_wallSliding && _canAirDash && (_dashInput.IsPressed || _airDashing))
                {
                    if (_dashInput.IsPressed)
                    {
                        _velocity.X = _facingRight ? DashSpeed : -DashSpeed;
                    }
                    _airDashing = true;
                    _airDashTime += Time.DeltaTime;
                    _velocity.Y = 0;
                }

                // end ground dash when grounded or ground dash time has expired
                if (_collisionState.Below && (!_dashInput.IsDown || _groundDashTime >= MaxGroundDashTime))
                {
                    _groundDashing = false;
                    _groundDashTime = 0;
                }

                // end ground dash if we are free falling
                if (!_collisionState.Left && !_collisionState.Right && !_collisionState.Below)
                {
                    _groundDashing = false;
                    _groundDashTime = 0;
                }

                // end air dash with time expiration
                if (!_collisionState.Below && _airDashTime >= MaxAirDashTime)
                {
                    _canAirDash = false;
                    _airDashing = false;
                    _airDashTime = 0;
                }

                // with x input
                if (xInputPresent)
                {
                    // dont change direction with air dodge
                    if (!_airDashing)
                    {
                        _facingRight = moveDir.X > 0;
                        _animator.FlipX = !_facingRight;
                    }

                    // end ground dash if x input sign changes when grounded
                    if (!_airDashing && _collisionState.Below && Math.Sign(_velocity.X) != Math.Sign(moveDir.X))
                    {
                        _groundDashing = false;
                        _groundDashTime = 0;
                    }

                    // allow ground dash jump to change direction mid-air
                    if (_dashJumping)
                    {
                        _velocity.X = _facingRight ? DashSpeed : -DashSpeed;
                        _wallJumpDashTime += Time.DeltaTime;
                    }

                    // normal speed if no dashing or dash jumping
                    if (!_airDashing && !_groundDashing && !_dashJumping)
                    {
                        _velocity.X = _facingRight ? MoveSpeed : -MoveSpeed;
                    }

                    // if wall jumping invert the input to jump away from wall for a brief window
                    if(_wallJumping && _wallJumpTime < WallJumpTimeWindow)
                    {
                        _velocity.X *= -1 * WallJumpMultiplier;
                    }
                }

                // no x input
                else
                {
                    // not dashing and no x input - stop moving (idle)
                    if (!_groundDashing && !_airDashing)
                    {
                        _velocity.X = 0;
                    }

                    // Stop dash jump momentarily if there is no x input
                    if (_groundDashing && !_collisionState.Below)
                    {
                        _velocity.X = 0;
                        _groundDashing = false;
                        _groundDashTime = 0;
                    }
                }

                // Vertical Movement

                // start jumping
                if (((_canJump && _collisionState.Below) || _wallSliding) && !_jumping && _jumpInput.IsPressed)
                {
                    _velocity.Y = -Mathf.Sqrt(2f * JumpHeight * Gravity);
                    _jumping = true;
                    _canJump = false;

                    if (Math.Abs(_velocity.X) == DashSpeed)
                    {
                        _dashJumping = true;
                    }

                    if(_wallSliding)
                    {
                        _wallJumping = true;
                    }
                }

                // used for wall jump away from wall window
                if(_wallJumping)
                {
                    _wallJumpTime += Time.DeltaTime;
                }

                // end jumping
                if (_jumping && _jumpInput.IsReleased)
                {
                    // cancel upward velocity if we are going up
                    if (_velocity.Y < 0)
                    {
                        _velocity.Y = 0;
                    }
                    _jumping = false;
                    _wallJumping = false;
                    _wallJumpTime = 0;
                }

                // start wall sliding 
                if (!_jumping && !_collisionState.Below && _velocity.Y > 0 && (_collisionState.Right || _collisionState.Left))
                {
                    _wallSliding = true;
                    _velocity.Y = WallSlideSpeed;
                }

                // stop wall sliding
                else
                {
                    _wallSliding = false;
                }

                // Apply gravity and clamp to terminal velocity if not air dashing or wall sliding
                if (!_airDashing && !_wallSliding)
                {
                    _velocity.Y += Gravity * Time.DeltaTime;
                    _velocity.Y = Mathf.Clamp(_velocity.Y, _velocity.Y, TerminalVelocity);
                }
            }

            // Apply damage vector when damage locked
            if(_damageLocked)
            {
                float damageXDirection = _facingRight ? -1 : 1;
                _velocity.X = damageXDirection * DamageImpulseVector.X;
                if (_damagedThisFrame)
                {
                    _velocity.Y = DamageImpulseVector.Y;
                } else
                {
                    _velocity.Y += Gravity * Time.DeltaTime;
                    _velocity.Y = Mathf.Clamp(_velocity.Y, _velocity.Y, TerminalVelocity);
                }
            }

            _mover.Move(_velocity * Time.DeltaTime, _collider, _collisionState);

            // stop jump with collision below or above
            if (_collisionState.Below || _collisionState.Above)
            {
                _velocity.Y = 0;
                _jumping = false;
                _wallJumping = false;
                _wallJumpTime = 0;
            }

            if(_collisionState.BecameGroundedThisFrame)
            {
                _canJump = true;
            }

            // grounding resets
            if (_collisionState.Below)
            {
                _canAirDash = true;
                _dashJumping = false;
                _wallJumpDashTime = 0;
            }

            // end dash jumping with left or right collision after initial jump (_wallJumpDashTime used for length of window)
            if (_dashJumping && _wallJumpDashTime > 0.20f && (_collisionState.Left || _collisionState.Right))
            {
                _dashJumping = false;
                _wallJumpDashTime = 0;
                _groundDashing = false;
                _groundDashTime = 0;
            }

        }

        #endregion

    }
}
