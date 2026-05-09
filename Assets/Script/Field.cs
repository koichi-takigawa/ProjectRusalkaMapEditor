using System;
using System.Collections.Generic;
using System.IO;

namespace JL.Tactics
{
    internal static class Field
    {
        // タイルのサイズ
        public const float THICKNESS = 0.5f;
        public const float SIZE = 1.0f;

        // √3
        public const float SQRT_3 = 1.7320508f;


        public class StandPoint
        {
            /// <summary>
            /// 立てる高さ
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            /// 上部スペース
            /// </summary>
            public int UpperSpace { get; set; }

            /// <summary>
            /// 水深
            /// </summary>
            public int WaterDepth { get; set; }

            /// <summary>
            /// 進入可否(true:進入可能, false:進入不可)
            /// </summary>
            public bool IsEnterable { get; set; }

            // TODO:種別が必要

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public StandPoint(int height, int upperSpace, int waterDepth, UInt32 kind, bool isEnterable)
            {
                Height = height;
                UpperSpace = upperSpace;
                WaterDepth = waterDepth;
                IsEnterable = isEnterable;

                // kind(地点種別は今後用)
                _ = kind;
            }
        }

        /// <summary>
        /// Grid
        /// </summary>
        public class Grid
        {
            /// <summary>
            /// StandPoint配列。Heightが小さい順に並ぶ。
            /// </summary>
            public StandPoint[] StandPoints { get; set; }
        }

        /// <summary>
        /// 軍勢配置位置情報
        /// </summary>
        public class TroopInfos
        {
            // 位置情報
            public class PointInfo
            {
                public Hex3 Position { get; set; }
                public Direction Direction { get; set; }
            }

            public List<PointInfo> Friends { get; set; }
            public List<PointInfo> Enemies { get; set; }

            public TroopInfos()
            {
                Friends = new List<PointInfo>();
                Enemies = new List<PointInfo>();
            }
        }

        /// <summary>
        /// Grid
        /// </summary>
        static public Dictionary<Hex2, Grid> Grids { get; set; }

        /// <summary>
        /// 軍勢配置位置情報
        /// </summary>
        static public Dictionary<int, TroopInfos> Troops { get; set; }

        /// <summary>
        /// データロード
        /// </summary>
        public static bool Load(byte[] bytes, Action<string> exportLog = null, Action<string> exportError = null)
        {
            // 引数チェック
            if (bytes == null || bytes.Length == 0)
            {
                exportError?.Invoke("Field Info Data is null");
                return false;
            }

            // データ初期化
            Grids = new Dictionary<Hex2, Grid>();
            Troops = new Dictionary<int, TroopInfos>();

            // 一時的にStandPointを格納するための変数
            Dictionary<Hex2, List<StandPoint>> grids = new Dictionary<Hex2, List<StandPoint>>();

            using (MemoryStream stream = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                if (reader.ReadChar() == 'b'
                 && reader.ReadChar() == 'd'
                 && reader.ReadChar() == 'f'
                 && reader.ReadChar() == 0)
                {
                    while (reader.PeekChar() != -1)
                    {
                        var code = reader.ReadChar();
                        switch (code)
                        {
                            case 'v':

                                // バージョン情報
                                byte major = reader.ReadByte();
                                byte minor = reader.ReadByte();
                                byte patch = reader.ReadByte();

                                if (major == 0 && minor == 0 && patch == 0)
                                {
                                    // バージョン0.0.0は正常
                                    break;
                                }

                                exportError?.Invoke($"Unknown Field Info Version: {major}.{minor}.{patch}");
                                return false;

                            case 'f':

                                string fieldName = reader.ReadString();
                                exportLog?.Invoke($"Field Name: {fieldName}");
                                break;

                            case 's':
                                {
                                    // バージョン
                                    byte version = reader.ReadByte();

                                    // データ数
                                    ushort dataCount = reader.ReadUInt16();

                                    for (int i = 0; i < dataCount; i++)
                                    {
                                        // データ読み込み
                                        sbyte q = reader.ReadSByte();
                                        sbyte r = reader.ReadSByte();
                                        sbyte height = reader.ReadSByte();
                                        byte space = reader.ReadByte();
                                        byte kindByte = reader.ReadByte();
                                        byte flags = reader.ReadByte();

                                        // 格納用の変数に変換
                                        Hex2 hex = new Hex2(q, r);
                                        UInt32 kind = kindByte < 32 ? (UInt32)(1 << kindByte) : 0;
                                        bool isEnterable = (flags & 0x1) != 0;

                                        // StandPoint生成
                                        StandPoint standPoint = new StandPoint(height, space, 0, kind, isEnterable);

                                        // すでにある場合
                                        if (grids.ContainsKey(hex))
                                        {
                                            var standPoints = grids[hex];

                                            // すでに同じHeightのStandPointがある場合は登録しない
                                            if (!standPoints.Exists(standPoint => standPoint.Height == height))
                                            {
                                                // 追加して並び替え
                                                standPoints.Add(standPoint);
                                                standPoints.Sort((a, b) => a.Height - b.Height);

                                                // 書き戻す
                                                grids[hex] = standPoints;
                                            }
                                        }
                                        else
                                        {
                                            // 新規
                                            grids[hex] = new List<StandPoint>() { standPoint };
                                        }
                                    }
                                }
                                break;

                            case 't':
                                {
                                    TroopInfos troopInfos = new TroopInfos();

                                    // バージョン
                                    byte version = reader.ReadByte();

                                    // キー
                                    int key = reader.ReadInt32();

                                    // 友軍データ数
                                    ushort friendDataCount = reader.ReadUInt16();

                                    for (int i = 0; i < friendDataCount; i++)
                                    {
                                        // データ読み込み
                                        sbyte q = reader.ReadSByte();
                                        sbyte r = reader.ReadSByte();
                                        byte h = reader.ReadByte();

                                        // 方向
                                        Direction direction = Direction.Direction_01;
                                        if (version >= 1)
                                        {
                                            byte directionByte = reader.ReadByte();
                                            direction = directionByte switch
                                            {
                                                1 => Direction.Direction_01,
                                                3 => Direction.Direction_03,
                                                5 => Direction.Direction_05,
                                                7 => Direction.Direction_07,
                                                9 => Direction.Direction_09,
                                                11 => Direction.Direction_11,
                                                _ => Direction.Direction_01,
                                            };
                                        }

                                        troopInfos.Friends.Add(new TroopInfos.PointInfo() { Position = new Hex3(q, r, h), Direction = direction });
                                    }

                                    // 敵軍データ数
                                    ushort enemyDataCount = reader.ReadUInt16();

                                    for (int i = 0; i < enemyDataCount; i++)
                                    {
                                        // データ読み込み
                                        sbyte q = reader.ReadSByte();
                                        sbyte r = reader.ReadSByte();
                                        byte h = reader.ReadByte();

                                        // 方向
                                        Direction direction = Direction.Direction_01;
                                        if (version >= 1)
                                        {
                                            byte directionByte = reader.ReadByte();
                                            direction = directionByte switch
                                            {
                                                1 => Direction.Direction_01,
                                                3 => Direction.Direction_03,
                                                5 => Direction.Direction_05,
                                                7 => Direction.Direction_07,
                                                9 => Direction.Direction_09,
                                                11 => Direction.Direction_11,
                                                _ => Direction.Direction_01,
                                            };
                                        }

                                        troopInfos.Enemies.Add(new TroopInfos.PointInfo() { Position = new Hex3(q, r, h), Direction = direction });
                                    }

                                    // 登録
                                    Troops[key] = troopInfos;
                                }
                                break;

                            case 'e':

                                // 正常終了
                                break;

                            default:

                                exportError?.Invoke($"Unknown Field Info Version Header: {code}");
                                return false;
                        }
                    }
                }

                // グリッド情報を変換
                foreach (var kv in grids)
                {
                    Grids[kv.Key] = new Grid() { StandPoints = kv.Value.ToArray() };
                }

                return true;
            }
        }
    }
}
