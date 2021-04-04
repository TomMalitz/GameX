using System;
using System.Collections.Generic;
using System.Text;
using Nez;
using Nez.Sprites;
using Nez.Tiled;
using GameX.Utils;
using GameX.Constants;
using Microsoft.Xna.Framework;

namespace GameX.Entities
{
    class Enemy : Entity
    {
        SpriteAnimator _animator;
        BoxCollider _collider;
        TiledMapMover _mover;

        private TmxMap _sceneTileMap;
        TiledMapMover.CollisionState _collisionState = new TiledMapMover.CollisionState();
        Vector2 _velocity = new Vector2(0,0);

        public Enemy(TmxMap sceneTileMap)
        {
            _sceneTileMap = sceneTileMap;
        }

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();

            Debug.Log("ENEMY ADDED TO SCENE");

            _mover = this.AddComponent(new TiledMapMover(_sceneTileMap.GetLayer<TmxLayer>("main")));

            _collider = this.AddComponent(new BoxCollider(32, 32));
            _collider.IsTrigger = true;
            Flags.SetFlagExclusive(ref _collider.PhysicsLayer, (int)PhysicsLayers.ENEMIES);

            _animator = SpriteUtil.CreateSpriteAnimatorFromAtlas(ref Scene, "Assets/Enemies/Test/atlas");
            _animator.RenderLayer = (int)RenderLayers.ENEMIES;
            this.AddComponent<SpriteAnimator>(_animator);
            _animator.Play("idle", SpriteAnimator.LoopMode.ClampForever);
        }

        public override void Update()
        {
            base.Update();

            _velocity.Y += 1000f * Time.DeltaTime;
            _velocity.Y = Mathf.Clamp(_velocity.Y, _velocity.Y, 300f);

            _mover.Move(_velocity * Time.DeltaTime, _collider, _collisionState);

            if (_collisionState.Below)
            {
                _velocity.Y = 0;
            }

        }
    }
}
