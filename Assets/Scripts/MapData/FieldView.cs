using JL.Tactics;
using System;
using System.Collections.Generic;
#nullable enable

internal class FieldView
{
    /// <summary>タイル</summary>
    public class Tile
    {
        /// <summary>タイルの種類</summary>
        public enum TileKind : byte
        {
            無し,
            水路,
            湿地,
            陸系タイル開始 = 10,
            土肌,
            草,
            砂利,
            岩場,
            煉瓦,
            木床,
            砂地,
            雪,
            屋根,
            溶岩,
            塩,
            氷,
            煙突,
            陸系タイル終了,
        }
        
        /// <summary>タイルの種類</summary>
        public TileKind Kind { get; set; }

        /// <summary>オブジェクトの方向。タイルの場合は存在しない</summary>
        public Direction Direction { get; set; }

        /// <summary>複数のタイルをまたぐオブジェクトの場合の基準オブジェクトへのオフセット値 Q</summary>
        public sbyte OffsetQ { get; set; }

        /// <summary>複数のタイルをまたぐオブジェクトの場合の基準オブジェクトへのオフセット値 R</summary>
        public sbyte OffsetR { get; set; }

        /// <summary>複数のタイルをまたぐオブジェクトの場合の基準オブジェクトへのオフセット値 H</summary>
        public sbyte OffsetH { get; set; }
    }

    /// <summary>グリッド情報</summary>
    public class Grid : Field.Grid
    {
        /// <summary>最大高さ</summary>
        public const int MAX_H = 64;

        /// <summary>タイルの配列。高さ方向に並んでいる。配列の長さは高さ方向の最大値。</summary>
        public Tile[] Tiles { get; set; }

        /// <summary>コンストラクタ</summary>
        public Grid()
        {
            Tiles = new Tile[MAX_H];
            for (int i = 0; i < Tiles.Length; i++)
                Tiles[i] = new Tile();
        }

        /// <summary>StandPointの更新</summary>
        public void CreateStandPoint()
        {
            // StandPointの生成

        }
    }

    public Tile? this[int q, int r, int h]
    {
        get => Grids.TryGetValue((q, r), out Grid grid) && h >= 0 && h < Grid.MAX_H ? grid.Tiles[h] : null;
    } 
    public Tile? this[Hex2 hex, int h]
    {
        get => Grids.TryGetValue((hex.Q, hex.R), out Grid grid) && h >= 0 && h < Grid.MAX_H ? grid.Tiles[h] : null;
    }

    /// <summary>Grid</summary>
    public Dictionary<(int q, int r), Grid> Grids { get; set; }

    /// <summary>軍勢配置位置情報</summary>
    public Dictionary<int, Field.TroopInfos> Troops { get; set; }

    /// <summary>コンストラクタ</summary>
    public FieldView()
    {
        Grids = new Dictionary<(int q, int r), Grid>();
        Troops = new Dictionary<int, Field.TroopInfos>();
    }
}
