using System;
using System.Collections.Generic;
using System.Text;
using Nez.Sprites;
using Nez;
using Microsoft.Xna.Framework;
using GameX.Utils;
using GameX.Constants;

namespace GameX.Entities
{
    class Projectile : Entity
    {
        SpriteAnimator _animator;
        BoxCollider _collider;
        ProjectileMover _projectileMover;

        public SpriteAnimator.LoopMode LiveLoopMode = SpriteAnimator.LoopMode.Loop; // animation loop mode for when projectile is "live" (not start or hit anim)
        public Vector2 Velocity = new Vector2(0, 0);
        public float Damage = 0f;
        public float LifeSpan = 1000f; // how long the projectile lives before self destructing
        public bool HasStartAnim = false;
        public bool HasHitAnim = false;
        public bool ContinuesAfterKill = false; // if true the projectile will continue on if hit target is destroyed

        private PhysicsLayers _physicsLayer;
        private PhysicsLayers _collidesWithLayer;
        private Dictionary<string, int> _fpsData;
        private Vector2 _colliderDimensions;
        private string _atlasPath;
        private bool _destroySelf;
        
        public Projectile(Scene scene,
            Vector2 spawnPosition,
            PhysicsLayers physicsLayer,
            PhysicsLayers collidesWithLayer,
            Vector2 colliderDimensions,
            string atlasPath,
            Dictionary<string, int> fpsData = null)
        {
            _colliderDimensions = colliderDimensions;
            _atlasPath = atlasPath;
            _physicsLayer = physicsLayer;
            _fpsData = fpsData;
            _collidesWithLayer = collidesWithLayer;
            this.AttachToScene(scene);
            this.Position = spawnPosition;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            SetAnimator(_atlasPath, _fpsData);
            SetCollider(_colliderDimensions);
            SetProjectileMover();
        }

        private void SetAnimator(string atlasPath, Dictionary<string, int> fpsData)
        {
            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, atlasPath, fpsData);
            this.AddComponent<SpriteAnimator>(_animator);
            _animator.RenderLayer = (int)RenderLayers.PROJECTILES;
            _animator.OnAnimationCompletedEvent += new Action<string>(HandleAnimationCompleted);
            if (Velocity.X < 0)
            {
                _animator.FlipX = true;
            }
            if(HasStartAnim)
            {
                _animator.Play("start", SpriteAnimator.LoopMode.ClampForever);
            } else
            {
                _animator.Play("live", LiveLoopMode);
            }
        }

        private void SetCollider(Vector2 colliderDimensions)
        {
            BoxCollider boxCollider = new BoxCollider(colliderDimensions.X, colliderDimensions.Y);
            _collider = this.AddComponent(boxCollider);
            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)_physicsLayer);
        }

        private void SetProjectileMover()
        {
            _projectileMover = this.AddComponent(new ProjectileMover());
        }

        public override void Update()
        {
            base.Update();

            _projectileMover.Move(Velocity * Time.DeltaTime);

            if (!_destroySelf)
            {
                CheckForCollisions();
            }
        }

        private void CheckForCollisions()
        {
            HashSet<Collider> neighborColliders = Physics.BoxcastBroadphaseExcludingSelf(_collider, 1 << (int)_collidesWithLayer);

            foreach (Collider neighborCollider in neighborColliders)
            {
                if (_collider.Overlaps(neighborCollider))
                {
                    if (neighborCollider.Entity is Enemy enemyEntity)
                    {
                        DamageEnemy(enemyEntity);
                    }
                }
            }
        }

        private void OnHitTarget()
        {
            if(_destroySelf)
            {
                Velocity = new Vector2(0, 0);

                if (!HasHitAnim)
                {
                    this.Destroy();
                } else
                {
                    _animator.Play("hit", SpriteAnimator.LoopMode.ClampForever);
                }
            }
        }

        private void HandleAnimationCompleted(string animName)
        {
            if (animName == "hit") this.Destroy(); // destroy projectile after hit anim completes
            else if(animName == "start") _animator.Play("live", LiveLoopMode); // switch to live anim after start finished
        }

        private void DamageEnemy(Enemy enemy)
        {
            enemy.TakeDamage(Damage);
            if (!ContinuesAfterKill || enemy.Health > 0)
            {
                _destroySelf = true;
            }
            OnHitTarget();
        }

    }
}
