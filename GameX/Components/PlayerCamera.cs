using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Nez;
using GameX.Entities;

namespace GameX.Components
{
    class PlayerCamera: FollowCamera
    {
        public PlayerCamera(Entity targetEntity, CameraStyle cameraStyle = CameraStyle.LockOn) 
            : base(targetEntity, cameraStyle)
        {
            FollowLerp = 1.0f;
            MapLockEnabled = true;
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.SetCenteredDeadzone(0, 0);
            Debug.Log(Camera.Bounds);
            Debug.Log(Camera.Position);
        }

        public void UpdateMapSize(Vector2 mapSize)
        {
            MapSize = mapSize;
        }
    }
}
