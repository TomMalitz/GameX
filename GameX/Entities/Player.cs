using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
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

        // Params
        public float MoveSpeed = 125f;
        public float DashSpeed = 350f;
        public float WallSlideSpeed = 100f;
        public float MaxGroundDashTime = 0.50f;
        public float MaxAirDashTime = 0.35f;
        public float Gravity = 1000f;
        public float TerminalVelocity = 350f;
        public float JumpHeight = 75f;
        public float halfChargeTime = 1f;
        public float fullChargeTime = 2f;

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
        bool _facingRight = true;
        bool _jumping = false;
        bool _dashJumping = false;
        bool _wallSliding = false;
        bool _groundDashing = false;
        bool _airDashing = false;
        bool _canAirDash = true;
        bool _canJump = true;
        bool _chargingShot = false;
        bool _halfChargeReady = false;
        bool _fullChargeReady = false;
        float _chargeTime = 0;
        float _groundDashTime = 0;
        float _airDashTime = 0;
        float _wallJumpDashTime = 0;

        // Input Axis
        VirtualIntegerAxis _xAxisInput;
        VirtualIntegerAxis _yAxisInput;

        // Input Buttons
        VirtualButton _jumpInput;
        VirtualButton _dashInput;
        VirtualButton _attackInput;
        VirtualButton _weaponChangeInput;

        class AnimationInstruction
        {
            public string name;
            public SpriteAnimator.LoopMode loopMode;
        }

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
            _camera.Entity.UpdateOrder = int.MaxValue;

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
            int width = 24;
            int height = 42;
            int widthOffset = 1;
            int heightOffset = 2;

            _collider = this.AddComponent(new BoxCollider(-width / 2 + widthOffset, -height / 2 + heightOffset, width, height));
        }

        private void ConfigureAnimations()
        {
            Dictionary<string, int> fpsData = new Dictionary<string, int>();
            fpsData.Add("idle", 6);
            fpsData.Add("run", 18);
            fpsData.Add("dash", 18);
            fpsData.Add("idle_shoot_weak", 18);
            fpsData.Add("idle_shoot_strong", 18);

            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, "Assets/Player/atlas", fpsData);
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

        public override void Update()
        {
            base.Update();

            HandleMovement();
            HandleWeaponInput();

            _lastAnimation = _currentAnimation;

            if (!_lastAnimation.loopMode.Equals(SpriteAnimator.LoopMode.Once) ||
                (_lastAnimation.loopMode.Equals(SpriteAnimator.LoopMode.Once)
                && _animator.AnimationState.Equals(SpriteAnimator.State.Completed)))
            {
       
                _currentAnimation = GetAnimationInstruction();

                if (_currentAnimation != null && !_animator.IsAnimationActive(_currentAnimation.name))
                    _animator.Play(_currentAnimation.name, _currentAnimation.loopMode);

            }
        }

        private AnimationInstruction GetAnimationInstruction()
        {
            AnimationInstruction animation = new AnimationInstruction();

            animation.name = "idle";
            animation.loopMode = SpriteAnimator.LoopMode.Loop;

            // movement animations
            if (_velocity.X == 0 && _collisionState.Below)
            {
                animation.name = "idle";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
            }

            if (_velocity.X != 0 && _velocity.Y == 0 && _collisionState.Below)
            {
                animation.name = "run";
                animation.loopMode = SpriteAnimator.LoopMode.Loop;
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
            if(animation.name == "idle" && _attackInput.IsPressed)
            {
                animation.name = "idle_shoot_strong";
                animation.loopMode = SpriteAnimator.LoopMode.Once;
            }

            if (animation.name == "idle" && _attackInput.IsReleased && _fullChargeReady)
            {
                animation.name = "idle_shoot_strong";
                animation.loopMode = SpriteAnimator.LoopMode.Once;
            }

            return animation;
        }

        private void HandleWeaponInput()
        {
            if(_attackInput.IsDown)
            {
                _chargeTime += Time.DeltaTime;
                _chargingShot = true;
                
                // reset charge ready flags
                _halfChargeReady = false;
                _fullChargeReady = false;
            }

            if(_attackInput.IsReleased)
            {
                _chargeTime = 0;
                _chargingShot = false;
            }

            if(_chargeTime >= halfChargeTime)
            {
                _halfChargeReady = true;
            }

            if(_chargeTime >= fullChargeTime)
            {
                _halfChargeReady = false;
                _fullChargeReady = true;
            }  
        }

        private void HandleMovement()
        {
            Vector2 moveDir = new Vector2(_xAxisInput.Value, 0);

            bool xInputPresent = Math.Abs(moveDir.X) > 0;
            // Horizontal movement input

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
                if(_dashJumping)
                {
                    _velocity.X = _facingRight ? DashSpeed : -DashSpeed;
                    _wallJumpDashTime += Time.DeltaTime;
                }

                // normal speed if no dashing or dash jumping
                if (!_airDashing && !_groundDashing && !_dashJumping)
                {
                    _velocity.X = _facingRight ? MoveSpeed : -MoveSpeed;
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
                if(_groundDashing && !_collisionState.Below)
                {
                    _velocity.X = 0;
                    _groundDashing = false;
                    _groundDashTime = 0;
                }
            }

            // Vertical Movement

            // start jumping
       
            if ((_canJump || _wallSliding) && !_jumping && _jumpInput.IsPressed)
            {
                _velocity.Y = -Mathf.Sqrt(2f * JumpHeight * Gravity);
                _jumping = true;
                _canJump = false;

                if (Math.Abs(_velocity.X) == DashSpeed)
                {
                    _dashJumping = true;
                }
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

            //_subpixelV2.Update(ref _velocity);
            _mover.Move(_velocity * Time.DeltaTime, _collider, _collisionState);

            // stop jump with collision below or above
            if (_collisionState.Below || _collisionState.Above)
            {
                _velocity.Y = 0;
               // Debug.Log("ZEROING Y VEL GROUNDED");
                _jumping = false;
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

    }
}
