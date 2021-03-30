using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Nez;
using Nez.Textures;
using Nez.Sprites;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace GameX.Utils
{
    /* "frames": {
        "filename": "anim_dash 0.aseprite",
        "frame": { "x": 0, "y": 0, "w": 48, "h": 48 },
        "rotated": false,
        "trimmed": false,
        "spriteSourceSize": { "x": 0, "y": 0, "w": 48, "h": 48 },
        "sourceSize": { "w": 48, "h": 48 },
        "duration": 100
    }*/

    /*"meta": {
        "app": "http://www.aseprite.org/",
        "version": "1.2.25-x64",
        "image": "atlas.png",
        "format": "RGBA8888",
        "size": { "w": 336, "h": 288 },
        "scale": "1"
    }*/

    class AsepriteAtlasFrameData 
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    class AsepriteSize
    {
        public int w;
        public int h;
    }

    class AsepriteAtlasFrame
    {
        public string filename;
        public AsepriteAtlasFrameData frame;
        public bool rotated;
        public bool trimmed;
        public AsepriteAtlasFrameData spriteSourceSize;
        public AsepriteSize sourceSize;
        public int duration;
    }

    class AsepriteMetaData
    {
        public string app;
        public string version;
        public string image;
        public string format;
        public AsepriteSize size;
        public string scale;
    }

    class AsepriteAtlasData
    {
        public List<AsepriteAtlasFrame> frames;
        public AsepriteMetaData meta;
    }

    class SpriteUtil
    {
        public SpriteUtil()
        {

        }

        public static SpriteAnimator CreateSpriteAnimatorFromAtlas(
            ref Scene scene, string atlasPath, 
            Dictionary<string, int> fpsDictionary = null)
        {
            AsepriteAtlasData asepriteAtlasData = AsepriteAtlasDataFromJson(atlasPath + ".json");

            if(asepriteAtlasData.frames.Count <= 0)
            {
                throw new Exception("There is no frame data in the provided atlas path");
            }

            // Get atlas cell dimensions from first frame's data
            Vector2 atlasCellDimensions = new Vector2(asepriteAtlasData.frames[0].frame.w, asepriteAtlasData.frames[0].frame.h);

            var atlas = scene.Content.LoadTexture(atlasPath + ".png");
            List<Sprite> sprites = Sprite.SpritesFromAtlas(atlas, (int)atlasCellDimensions.X, (int)atlasCellDimensions.Y);
            SpriteAnimator animator = new SpriteAnimator();

            int currentAnimFrameCount = 1;
            int currentAnimStartIndex = 0;
            string currentAnimName;
            Debug.Log("Sprite Count: {0}", sprites.Count);
            Debug.Log("Frame Data Count: {0}", asepriteAtlasData.frames.Count);
            for (int i=0; i< asepriteAtlasData.frames.Count; i++)
            {
                AsepriteAtlasFrame currentFrame = asepriteAtlasData.frames[i];
                currentAnimName = GetFrameNameFromFileName(currentFrame.filename);

                // Add the final animation
                if (i == asepriteAtlasData.frames.Count - 1)
                {
                    Debug.Log("{0}, {1}, {2}", currentAnimName, currentAnimStartIndex, currentAnimFrameCount);
                    AddAnimationWithFPSLookup(ref animator, currentAnimName, currentAnimStartIndex, currentAnimFrameCount, sprites, fpsDictionary);
                    break;
                }

                AsepriteAtlasFrame nextFrame = asepriteAtlasData.frames[i+1];
                if (GetFrameNameFromFileName(currentFrame.filename) == GetFrameNameFromFileName(nextFrame.filename))
                {
                    currentAnimFrameCount++;
                } else
                {
                    Debug.Log("{0}, {1}, {2}", currentAnimName, currentAnimStartIndex, currentAnimFrameCount);
                    AddAnimationWithFPSLookup(ref animator, currentAnimName, currentAnimStartIndex, currentAnimFrameCount, sprites, fpsDictionary);

                    currentAnimFrameCount = 1; // reset frame count for next animation
                    currentAnimStartIndex = i+1; // set the start index for the next animation
                }
            }

            return animator;
        }
        public static Sprite[] GetSpritesForRange(List<Sprite> sprites, int start, int length)
        {
            return sprites.GetRange(start, length).ToArray();
        }

        private static string GetFrameNameFromFileName(string filename)
        {
            /** examples 
             * anim_run 0.aseprite => run
             * anim_run 10.aseprite => run
             * anim_run.asprite => run
             */
            string rawName = filename.Split(".aseprite")[0];
            string finalName = rawName.Split("anim_")[1];
            if (finalName.Contains(" "))
            {
                finalName = finalName.Split(" ")[0];
            }

            return finalName;
        }

        private static void AddAnimationWithFPSLookup(
            ref SpriteAnimator animator, 
            string animName, 
            int animStartIndex, 
            int animFrameCount, 
            List<Sprite> sprites, 
            Dictionary<string, int> fpsDictionary)
        {
            int animFps;
            if (fpsDictionary != null && fpsDictionary.TryGetValue(animName, out animFps))
            {
                animator.AddAnimation(
                    animName,
                    GetSpritesForRange(sprites, animStartIndex, animFrameCount), animFps
                );
            }
            else
            {
                animator.AddAnimation(
                    animName,
                    GetSpritesForRange(sprites, animStartIndex, animFrameCount)
                );
            }
        }

        private static AsepriteAtlasData AsepriteAtlasDataFromJson(string jsonPath)
        {
            AsepriteAtlasData asepriteAtlasData;
            using (StreamReader reader = new StreamReader(jsonPath))
            {
                string json = reader.ReadToEnd();
                asepriteAtlasData = JsonConvert.DeserializeObject<AsepriteAtlasData>(json);
            }
            return asepriteAtlasData;
        }
    }

}
