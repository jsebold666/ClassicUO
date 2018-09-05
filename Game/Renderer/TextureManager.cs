﻿using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicUO.Game.Renderer
{
    public class SpriteTexture : Texture2D
    {
        public SpriteTexture(int width,  int height,  bool is32bit = true) : base(TextureManager.Device, width, height, false, is32bit ? SurfaceFormat.Color : SurfaceFormat.Bgra5551)
        {
            //ID = TextureManager.NextID;
        }

        public long Ticks { get; set; }
        //public int ID { get; }
    }

    public static class TextureManager
    {
        private const long TEXTURE_TIME_LIFE = 3000;

        private static int _updateIndex;

        private static readonly Dictionary<ushort, SpriteTexture> _staticTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _landTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _gumpTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _textmapTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<ushort, SpriteTexture> _lightTextureCache = new Dictionary<ushort, SpriteTexture>();
        private static readonly Dictionary<TextureAnimationFrame, SpriteTexture> _animations = new Dictionary<TextureAnimationFrame, SpriteTexture>();
        private static readonly Dictionary<GameText, SpriteTexture> _textTextureCache = new Dictionary<GameText, SpriteTexture>();


        private static int _first;


        public static GraphicsDevice Device { get; set; }
        public static int NextID => _first++;


        public static void Update()
        {
            if (_updateIndex == 0)
            {
                //var list = _animations.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();

                //foreach (var t in list)
                //{
                //    t.Value.Dispose();

                //    //Array.Clear(t.Key.Pixels, 0, t.Key.Pixels.Length);
                //    //t.Key.Pixels = null;
                //    //t.Key.Width = 0;
                //    //t.Key.Height = 0;

                //    _animations.Remove(t.Key);

                //}

                IO.Resources.Animations.ClearUnusedTextures();

                _updateIndex++;
            }
            else if (_updateIndex == 1)
            {
                var list = _textTextureCache.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();

                foreach (var t in list)
                {
                    t.Value.Dispose();
                    _textTextureCache.Remove(t.Key);
                }
                _updateIndex++;
            }
            else
            {
               

                if (_updateIndex == 2)
                {
                    CheckSpriteTexture(_staticTextureCache);
                }
                else if (_updateIndex == 3)
                {
                    CheckSpriteTexture(_landTextureCache);
                }
                else if (_updateIndex == 4)
                {
                    CheckSpriteTexture(_gumpTextureCache);
                }
                else if (_updateIndex == 5)
                {
                    CheckSpriteTexture(_textmapTextureCache);
                }
                else if (_updateIndex == 6)
                {
                    CheckSpriteTexture(_lightTextureCache);
                }
                else
                {
                    _updateIndex = 0;
                }
            }
        }

        private static void CheckSpriteTexture(Dictionary<ushort, SpriteTexture> dict)
        {
            var toremove = dict.Where(s => World.Ticks - s.Value.Ticks >= TEXTURE_TIME_LIFE).ToList();
            foreach (var t in toremove)
            {
                dict[t.Key].Dispose();
                dict.Remove(t.Key);
            }

            _updateIndex++;
        }

        public static SpriteTexture GetOrCreateAnimTexture(TextureAnimationFrame frame)
        {
            if (!_animations.TryGetValue(frame, out var sprite))
            {
                //sprite = new SpriteTexture(frame.Width, frame.Height, false) { Ticks = World.Ticks };
                //sprite.SetData(frame.Pixels);
                _animations[frame] = sprite;
            }
            else
            {
                sprite.Ticks = World.Ticks;
            }

            return sprite;
        }


        public static SpriteTexture GetOrCreateStaticTexture(ushort g)
        {
            if (!_staticTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                ushort[] pixels = Art.ReadStaticArt(g, out short w, out short h);

                texture = new SpriteTexture(w, h, false) { Ticks = World.Ticks };

                texture.SetData(pixels);
                _staticTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateLandTexture(ushort g)
        {
            if (!_landTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                ushort[] pixels = Art.ReadLandArt(g);
                texture = new SpriteTexture(44, 44, false) { Ticks = World.Ticks };
                texture.SetData(pixels);
                _landTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateGumpTexture(ushort g)
        {
            if (!_gumpTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                ushort[] pixels = IO.Resources.Gumps.GetGump(g, out int w, out int h);
                texture = new SpriteTexture(w, h, false) { Ticks = World.Ticks };
                texture.SetData(pixels);
                _gumpTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateTexmapTexture(ushort g)
        {
            if (!_textmapTextureCache.TryGetValue(g, out var texture) || texture.IsDisposed)
            {
                ushort[] pixels = TextmapTextures.GetTextmapTexture(g, out int size);
                texture = new SpriteTexture(size, size, false) { Ticks = World.Ticks };
                texture.SetData(pixels);
                _textmapTextureCache[g] = texture;
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }

        public static SpriteTexture GetOrCreateStringTextTexture(GameText gt)
        {
            if (!_textTextureCache.TryGetValue(gt, out var texture) || texture.IsDisposed)
            {
                uint[] data;
                int linesCount;

                if (gt.IsHTML)
                    Fonts.SetUseHTML(true);

                if (gt.IsUnicode)
                {
                    (data, gt.Width, gt.Height, linesCount, gt.Links) = Fonts.GenerateUnicode(gt.Font, gt.Text, gt.Hue, gt.Cell, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
                }
                else
                {
                    (data, gt.Width, gt.Height, linesCount, gt.IsPartialHue) = Fonts.GenerateASCII(gt.Font, gt.Text, gt.Hue, gt.MaxWidth, gt.Align, (ushort)gt.FontStyle);
                }

                texture = new SpriteTexture(gt.Width, gt.Height);
                texture.SetData(data);
                _textTextureCache[gt] = texture;


                if (gt.IsHTML)
                    Fonts.SetUseHTML(false);
            }
            else
            {
                texture.Ticks = World.Ticks;
            }

            return texture;
        }
    }
}