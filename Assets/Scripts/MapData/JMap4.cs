using JL.Tactics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
#nullable enable

public class JMap4
{
    /*

             [Flags]
        public enum TileKind
        {
            無し = 0,
            水路 = 1 << 0,
            湿地 = 1 << 1,
            土肌 = 1 << 10,
            草   = 1 << 11,
            砂利 = 1 << 12,
            岩場 = 1 << 13,
            煉瓦 = 1 << 14,
            木床 = 1 << 15,
            砂地 = 1 << 16,
            雪   = 1 << 17,
            屋根 = 1 << 18,
            溶岩 = 1 << 19,
            塩   = 1 << 20,
            氷   = 1 << 21,
            煙突 = 1 << 22,

            水系 = 0x3ff,
            陸系 = 0x7ffffc00,
        }
     
     */
    // タイルの種類を変換するための辞書
    static Dictionary<string, FieldView.Tile.TileKind> TileKindConvertDictionary = new Dictionary<string, FieldView.Tile.TileKind>()
    {
        { "無し", FieldView.Tile.TileKind.無し },
        { "水路", FieldView.Tile.TileKind.水路 },
        { "湿地", FieldView.Tile.TileKind.湿地 },
        { "土肌", FieldView.Tile.TileKind.土肌 },
        { "草",   FieldView.Tile.TileKind.草 },
        { "砂利", FieldView.Tile.TileKind.砂利 },
        { "岩場", FieldView.Tile.TileKind.岩場 },
        { "煉瓦", FieldView.Tile.TileKind.煉瓦 },
        { "木床", FieldView.Tile.TileKind.木床 },
        { "砂地", FieldView.Tile.TileKind.砂地 },
        { "雪",   FieldView.Tile.TileKind.雪 },
        { "屋根", FieldView.Tile.TileKind.屋根 },
        { "溶岩", FieldView.Tile.TileKind.溶岩 },
        { "塩",   FieldView.Tile.TileKind.塩 },
        { "氷",   FieldView.Tile.TileKind.氷 },
        { "煙突", FieldView.Tile.TileKind.煙突 },
    };

    /// <summary>
    /// メインのロード処理
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <returns>FieldView</returns>
    internal static FieldView Load(string path)
    {
        // FieldViewのインスタンスを作成
        FieldView fieldView = new FieldView();

        try
        {
            // XMLファイルをロード
            XElement xml = XElement.Load(path);

            // クエリ式を用いて Dictionary<Vector2Int, List<TileData>> の形式に変換
            var gridDictionary = xml.Element("Grids")?.Elements("PairOfHexGridPosGrid")
                .Select(pair => new
                {
                    // Keyタグから座標を取得 (q, r)
                    Pos = new Hex2(
                        (int)pair.Element("Key").Attribute("q"),
                        (int)pair.Element("Key").Attribute("r")
                    ),
                    // Value/Tiles/Tileタグからリストを取得
                    Tiles = pair.Element("Value")?.Element("Tiles")?.Elements("Tile")
                        .Select(t => new
                        {
                            Kind = (string)t.Attribute("Kind"),
                            NoEntry = (bool)t.Attribute("NoEntry")
                        })
                        .ToList()
                })
                .ToList();

            // TroopInfosセクションを取得
            var troopDictionary = xml.Element("TroopInfos")?.Elements("PairOfInt32ArrayOfTroopInfo")
                .Select(pair => new
                {
                    // Key (int) を取得
                    GroupId = (int?)pair.Element("Key") ?? -1,

                    // Value/TroopInfo のリストを取得
                    Troops = pair.Element("Value")?.Elements("TroopInfo")
                        .Select(t => new
                        {
                            Whom = (string)t.Attribute("Whom") ?? "Unknown",

                            Pos = new Hex3(
                                (int)t.Element("Pos").Element("grid").Attribute("q"),
                                (int)t.Element("Pos").Element("grid").Attribute("r"),
                                (int)t.Element("Pos").Attribute("h"))
                        }).ToList()
                })
                .ToList();

            // グリッドの生成
            if (gridDictionary != null)
            {
                foreach (var kvp in gridDictionary)
                {
                    // 進入不可位置のリスト
                    HashSet<int> noEntries = new HashSet<int>();

                    // グリッドの生成
                    FieldView.Grid grid = new FieldView.Grid();

                    if (kvp.Tiles != null)
                    {
                        for (int i = 0; i < kvp.Tiles.Count; i++)
                        {
                            // タイルの種類を変換
                            if (TileKindConvertDictionary.TryGetValue(kvp.Tiles[i].Kind, out FieldView.Tile.TileKind kind) == false)
                            {
                                // 変換できない場合は無しとする
                                kind = FieldView.Tile.TileKind.無し;
                                Debug.LogWarning($"Unknown tile kind: {kvp.Tiles[i].Kind} at position ({kvp.Pos.Q}, {kvp.Pos.R}, {i})");
                            }

                            // タイルの生成
                            grid.Tiles[i] = new FieldView.Tile() { Kind = kind };

                            // 進入不可を記録
                            if (kvp.Tiles[i].NoEntry)
                            {
                                noEntries.Add(i);
                            }
                        }

                        // StandPointの生成
                        grid.CreateStandPoint();

                        // 進入不可の反映
                        if (grid.StandPoints != null)
                        {
                            foreach (var standPoint in grid.StandPoints)
                            {
                                if (noEntries.Contains(standPoint.Height))
                                {
                                    standPoint.IsEnterable = false;
                                }
                            }
                        }
                    }

                    // グリッドをフィールドに追加
                    fieldView.Grids[(kvp.Pos.Q, kvp.Pos.R)] = grid;
                }
            }

            // 配置情報の生成
            if (troopDictionary != null)
            {
                foreach (var kvp in troopDictionary)
                {
                    // TroopInfosの生成
                    Field.TroopInfos troopInfos = new Field.TroopInfos();

                    // データが存在する場合は配置情報を追加
                    if (kvp.Troops != null)
                    {
                        foreach (var troop in kvp.Troops)
                        {
                            Field.TroopInfos.PointInfo pointInfo = new Field.TroopInfos.PointInfo();
                            pointInfo.Position = troop.Pos;

                            // whomの値に応じて、友軍か敵軍のリストに追加する。
                            if (troop.Whom == "Friend")
                            {
                                troopInfos.Friends.Add(pointInfo);
                            }
                            else if (troop.Whom == "Enemy")
                            {
                                troopInfos.Enemies.Add(pointInfo);
                            }
                            else
                            {
                                Debug.LogWarning($"Unknown troop type: {troop.Whom} in group {kvp.GroupId}");
                            }
                        }
                    }

                    fieldView.Troops[kvp.GroupId] = troopInfos;
                }
            }
        }
        catch (System.Exception ex)
        {
            // エラー処理
            Debug.LogError($"Error loading map data: {ex.Message}");
        }

        return fieldView;
    }
}
