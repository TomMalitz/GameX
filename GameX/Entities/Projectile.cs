using System;
using System.Collections.Generic;
using System.Text;
using Nez.Sprites;
using Nez;
using Microsoft.Xna.Framework;
using GameX.Utils;

namespace GameX.Entities
{
    class Projectile : Entity, ITriggerListener
    {
        SpriteAnimator _animator;
        BoxCollider _collider;
        ProjectileMover _projectileMover;

        public float Damage = 0;
        public Vector2 Speed = new Vector2(0, 0);

        private Vector2 _colliderDimensions;
        private string _atlasPath;

        public Projectile(Vector2 colliderDimensions, string atlasPath)
        {
            _colliderDimensions = colliderDimensions;
            _atlasPath = atlasPath;
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
            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, atlasPath);
            this.AddComponent<SpriteAnimator>(_animator);
            _animator.Play("projectile");
        }

        private void SetCollider(Vector2 colliderDimensions)
        {
            _collider = this.AddComponent(new BoxCollider(colliderDimensions.X, colliderDimensions.Y));
        }

        private void SetProjectileMover()
        {
            _projectileMover = this.AddComponent(new ProjectileMover());
        }

        public override void Update()
        {
            base.Update();

            _projectileMover.Move(Speed * Time.DeltaTime);

        }


        void ITriggerListener.OnTriggerEnter(Collider other, Collider self)
        {
            //if(other.PhysicsLayer)
        }

        void ITriggerListener.OnTriggerExit(Collider other, Collider self)
        {

        }

    }
}
