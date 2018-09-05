﻿using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Game.Map
{
    public sealed class FacetChunk
    {
        public FacetChunk(ushort x,  ushort y)
        {
            X = x;
            Y = y;

            Tiles = new Tile[8][];
            for (int i = 0; i < 8; i++)
            {
                Tiles[i] = new Tile[8];
                for (int j = 0; j < 8; j++)
                {
                    Tiles[i][j] = new Tile();
                }
            }
        }

        public ushort X { get; private set; }
        public ushort Y { get; private set; }
        public Tile[][] Tiles { get; private set; }


        public void Load(int map)
        {
            IndexMap im = GetIndex(map);
            if (im.MapAddress == 0)
            {
                throw new Exception();
            }

            //unsafe
            {
                MapBlock block = Marshal.PtrToStructure<MapBlock>((IntPtr)im.MapAddress);

                int bx = X * 8;
                int by = Y * 8;

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        int pos = y * 8 + x;

                        ushort tileID = (ushort)(block.Cells[pos].TileID & 0x3FFF);
                        sbyte z = block.Cells[pos].Z;

                        Tiles[x][y].Graphic = tileID;
                        Tiles[x][y].Position = new Position((ushort)(bx + x), (ushort)(by + y), z);
                    }
                }

                if (im.StaticAddress != 0 )
                {
                    int size = Marshal.SizeOf<StaticsBlock>();
                    for (int i = 0; i < im.StaticCount; i++)
                    

                    //StaticsBlock* sb = (StaticsBlock*)im.StaticAddress;
                    //if (sb != null)
                    {

                        var sb = Marshal.PtrToStructure<StaticsBlock>((IntPtr)im.StaticAddress + (i * size));

                        int count = (int)im.StaticCount;

                        //for (int i = 0; i < count; i++, sb++)
                        {
                            if (sb.Color > 0 && sb.Color != 0xFFFF)
                            {
                                ushort x = sb.X;
                                ushort y = sb.Y;

                                int pos = y * 8 + x;
                                if (pos >= 64)
                                {
                                    continue;
                                }

                                sbyte z = sb.Z;

                                Static staticObject = new Static(sb.Color, sb.Hue, pos) { Position = new Position((ushort)(bx + x), (ushort)(by + y), z) };

                                Tiles[x][y].AddGameObject(staticObject);
                            }
                        }
                    }
                }
            }
        }

        private IndexMap GetIndex(int map)
        {
            return GetIndex(map, X, Y);
        }

        private IndexMap GetIndex(int map,  int x,  int y)
        {
            int block = x * IO.Resources.Map.MapBlocksSize[map][1] + y;
            return IO.Resources.Map.BlockData[map][block];
        }

        // we wants to avoid reallocation, so use a reset method
        public void SetTo(ushort x,  ushort y)
        {
            X = x;
            Y = y;
        }

        public void Unload()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Tiles[i][j].Clear();

                    //Tiles[i][j].Dispose();
                    //Tiles[i][j] = null;
                }
            }

            //Tiles = null;
        }
    }
}