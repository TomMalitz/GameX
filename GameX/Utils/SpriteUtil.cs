using System;
using System.Collections.Generic;
using System.Text;
using Nez.Textures;

namespace GameX.Utils
{
    class SpriteUtil
    {
        public SpriteUtil()
        {

        }   

        public static Sprite[] GetSpritesForRange(List<Sprite> sprites, int start, int length)
        {
            return sprites.GetRange(start, length).ToArray();
        }
    }
}
