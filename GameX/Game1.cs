using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Nez;
using Nez.Tiled;
using GameX.Entities;

namespace GameX
{
    public class Game1 : Nez.Core
    {

        public Game1(): base(width: 1280, height: 720, isFullScreen: false, windowTitle: "Game X")
        {
            
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            Scene testScene = Scene.CreateWithDefaultRenderer(Color.CornflowerBlue);
            testScene.SetDesignResolution(512, 288, Scene.SceneResolutionPolicy.BestFit);

            TmxMap testLevel = testScene.Content.LoadTiledMap("Assets/Levels/Test/test.tmx");
            Entity tiledEntity = testScene.CreateEntity("tiled-level");
            tiledEntity.AddComponent(new TiledMapRenderer(testLevel, "main"));

            Player player = new Player(testLevel);
            player.AttachToScene(testScene);
            player.Position = new Vector2(100, 200);

            Scene = testScene;

            Window.Title = "Game X";
            Screen.SetSize(1280, 720);

        }
    }
}
