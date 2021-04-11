using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Tiled;
using Nez.Textures;
using GameX.Entities;
using GameX.Constants;

namespace GameX
{
    public class Game1 : Nez.Core
    {

        public Game1(): base(width: 1280, height: 720, isFullScreen: false, windowTitle: "Game X")
        {
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            Scene testScene = Scene.CreateWithDefaultRenderer(Color.DarkGray);
            testScene.SetDesignResolution(512, 288, Scene.SceneResolutionPolicy.NoBorderPixelPerfect);
            //testScene.SetDesignResolution(1280, 720, Scene.SceneResolutionPolicy.NoBorderPixelPerfect);

            TmxMap testLevel = testScene.Content.LoadTiledMap("Assets/Levels/Test/test.tmx");
            Entity tiledEntity = testScene.CreateEntity("tiled-level");
            TiledMapRenderer tiledMapRenderer = tiledEntity.AddComponent(new TiledMapRenderer(testLevel, "main"));
            tiledMapRenderer.RenderLayer = (int)RenderLayers.LEVEL;

            Player player = new Player(testLevel);
            player.AttachToScene(testScene);
            player.Position = new Vector2(50, 50);

            Enemy testEnemy = new Enemy(testLevel);
            testEnemy.AttachToScene(testScene);
            testEnemy.Position = new Vector2(250, 50);

            Scene = testScene;

            Window.Title = "Game X";
            Screen.SetSize(2560, 1440);

            //IsFixedTimeStep = true;

            //Core.DefaultSamplerState = SamplerState.PointClamp;
            //Core.DebugRenderEnabled = true;

        }
    }
}
