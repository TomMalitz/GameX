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
    class Projectile : Entity, ITriggerListener
    {
        SpriteAnimator _animator;
        BoxCollider _collider;
        ProjectileMover _projectileMover;

        public float Damage = 0;
        public Vector2 Speed = new Vector2(0, 0);

        private PhysicsLayers _physicsLayer;
        private Vector2 _colliderDimensions;
        private string _atlasPath;
        private string _spawnAnimationName = "";
        private string _destroyAnimationName = "";
        private SpriteAnimator.LoopMode _spawnAnimationLoopMode = SpriteAnimator.LoopMode.ClampForever;
        private SpriteAnimator.LoopMode _destoryAnimationLoopMode = SpriteAnimator.LoopMode.ClampForever;
        private Dictionary<string, int> _fpsData;

        public Projectile(Scene scene,
            Vector2 spawnPosition,
            Vector2 speed,
            PhysicsLayers physicsLayer,
            Vector2 colliderDimensions,
            string atlasPath,
            string spawnAnimationName,
            SpriteAnimator.LoopMode spawnloopMode,
            Dictionary<string, int> fpsData = null)
        {
            if (spawnAnimationName.Length == 0)
            {
                throw new InvalidOperationException("spawn animation for projectile must be set.");
            }
            _colliderDimensions = colliderDimensions;
            _atlasPath = atlasPath;
            _physicsLayer = physicsLayer;
            _fpsData = fpsData;
            Speed = speed;
            SetSpawnAnimation(spawnAnimationName, spawnloopMode);
            this.AttachToScene(scene);
            this.Position = spawnPosition;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            SetAnimator(_atlasPath);
            SetCollider(_colliderDimensions);
            SetProjectileMover();
        }

        private void SetAnimator(string atlasPath)
        {
            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, atlasPath, _fpsData);
            this.AddComponent<SpriteAnimator>(_animator);
            _animator.RenderLayer = (int)RenderLayers.PROJECTILES;
            if(Speed.X < 0)
            {
                _animator.FlipX = true;
            }
            _animator.Play(_spawnAnimationName, _spawnAnimationLoopMode);
        }

        private void SetCollider(Vector2 colliderDimensions)
        {
            BoxCollider boxCollider = new BoxCollider(colliderDimensions.X, colliderDimensions.Y);
            _collider = this.AddComponent(boxCollider);
            //_collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.CollidesWithLayers, (int)PhysicsLayers.ENEMIES);
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)_physicsLayer);
        }

        private void SetProjectileMover()
        {
            _projectileMover = this.AddComponent(new ProjectileMover());
        }

        private void SetSpawnAnimation(string animationName, SpriteAnimator.LoopMode loopMode = SpriteAnimator.LoopMode.ClampForever)
        {
            _spawnAnimationName = animationName;
            _spawnAnimationLoopMode = loopMode;
        }

        public void SetDestroyAnimation(string animationName, SpriteAnimator.LoopMode loopMode = SpriteAnimator.LoopMode.ClampForever)
        {
            _destroyAnimationName = animationName;
            _destoryAnimationLoopMode = loopMode;
        }

        public override void Update()
        {
            base.Update();

            _projectileMover.Move(Speed * Time.DeltaTime);
        }


        void ITriggerListener.OnTriggerEnter(Collider other, Collider self)
        {
            //if(other.PhysicsLayer)
            Debug.Log("PROJECTILE TRIGGER ENTER");
        }

        void ITriggerListener.OnTriggerExit(Collider other, Collider self)
        {
            Debug.Log("PROJECTILE TRIGGER EXIT");
        }

    }
}
