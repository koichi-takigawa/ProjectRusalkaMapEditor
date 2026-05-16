using JL.Tactics;
using System.Collections.Generic;
using System.Diagnostics;

internal class JMap3
{
    // タイルの種類を変換するための辞書
    static Dictionary<int, FieldView.Tile.TileKind> TileKindConvertDictionary = new Dictionary<int, FieldView.Tile.TileKind>()
    {
        { 00, FieldView.Tile.TileKind.無し },
        { 1 << 00, FieldView.Tile.TileKind.水路 },
        { 1 << 01, FieldView.Tile.TileKind.湿地 },
        { 1 << 10, FieldView.Tile.TileKind.土肌 },
        { 1 << 11, FieldView.Tile.TileKind.草 },
        { 1 << 12, FieldView.Tile.TileKind.砂利 },
        { 1 << 13, FieldView.Tile.TileKind.岩場 },
        { 1 << 14, FieldView.Tile.TileKind.煉瓦 },
        { 1 << 15, FieldView.Tile.TileKind.木床 },
        { 1 << 16, FieldView.Tile.TileKind.砂地 },
        { 1 << 17, FieldView.Tile.TileKind.雪 },
        { 1 << 18, FieldView.Tile.TileKind.屋根 },
        { 1 << 19, FieldView.Tile.TileKind.溶岩 },
        { 1 << 20, FieldView.Tile.TileKind.塩 },
        { 1 << 21, FieldView.Tile.TileKind.氷 },
        { 1 << 22, FieldView.Tile.TileKind.煙突 },
    };

    /// <summary>
    /// メインのロード処理
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <returns>FieldView</returns>
    public static FieldView Load(string fileName)
    {
        int maxXZ = 32;

        Dictionary<Hex2, FieldView.Grid> grids = LoadMain(fileName, ref maxXZ);

        // Gridが取れなければ失敗
        if (grids == null)
        {
            return null;
        }

        // 生成
        FieldView fieldView = new FieldView();

        // Gridの適用
        foreach (var grid in grids)
        {
            fieldView.Grids[(grid.Key.Q, grid.Key.R)] = grid.Value;
        }

        for (int i = 0; i < 100; i++)
        {
            string troopFileName = System.IO.Path.GetDirectoryName(fileName) + "\\" +
                                   System.IO.Path.GetFileNameWithoutExtension(fileName) + i.ToString("00") + ".trps";

            if (System.IO.File.Exists(troopFileName))
            {
                Field.TroopInfos troops = LoadTroops(troopFileName, maxXZ);

                if (troops != null)
                {
                    fieldView.Troops.Add(i, troops);
                }
            }
        }

        return fieldView;
    }

    /// <summary>
    /// ファイルからGrid情報の読み込み
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <param name="maxXZ">最大XZ</param>
    /// <returns>FieldView.Grid群</returns>
    private static Dictionary<Hex2, FieldView.Grid> LoadMain(string fileName, ref int maxXZ)
    {
        using (System.IO.FileStream ifs = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
        using (System.IO.Compression.DeflateStream deflateStream = new System.IO.Compression.DeflateStream(ifs, System.IO.Compression.CompressionMode.Decompress))
        using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                //Deflateで圧縮されたファイルからデータを読み込む
                int readBytes = deflateStream.Read(buffer, 0, buffer.Length);
                if (readBytes == 0)
                {
                    break;
                }

                //解凍されたデータを書き込む
                mem.Write(buffer, 0, readBytes);
            }

            // 始点位置に戻す
            mem.Position = 0;
            using (System.IO.BinaryReader br = new System.IO.BinaryReader(mem))
            {
                ushort version = br.ReadUInt16();

                // バージョン３以外は開かない。
                if (version != 3)
                    return null;

                Dictionary<Hex2, FieldView.Grid> retDict = new Dictionary<Hex2, FieldView.Grid>();

                maxXZ = br.ReadInt32();
                int maxY = br.ReadInt32();

                for (int cx = 0; cx < maxXZ; cx++)
                {
                    for (int cz = 0; cz < maxXZ; cz++)
                    {
                        int r = maxXZ / 2 - cz;
                        int q = cx - (maxXZ + r) / 2;

                        // 進入不可位置のリスト
                        HashSet<int> noEntries = new HashSet<int>();

                        // グリッドの生成
                        FieldView.Grid grid = new FieldView.Grid();

                        // タイルの読み込み
                        for (int cy = 0; cy < maxY; cy++)
                        {
                            int kindInt = br.ReadInt32();
                            int flagInt = br.ReadInt32();

                            // 変換してタイルの種類を取得
                            if (TileKindConvertDictionary.TryGetValue(kindInt, out FieldView.Tile.TileKind kind) == false)
                            {
                                // 変換できない値だった場合は無しとする
                                kind = FieldView.Tile.TileKind.無し;
                                Debug.WriteLine($"Unknown tile kind: {kindInt} at position ({q}, {r}, {cy})");
                            }

                            // タイルの生成
                            grid.Tiles[cy] = new FieldView.Tile() { Kind = kind };

                            // 進入不可を記録
                            if ((flagInt & 0x01) != 0)
                            {
                                noEntries.Add(cy);
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

                        // 位置情報をキーにして、グリッドを保存
                        retDict.Add(new Hex2(q, r), grid);
                    }
                }

                return retDict;
            }
        }
    }

    /// <summary>
    /// ファイルからTroopInfosの読み込み
    /// </summary>
    /// <param name="fileName">対象ファイル名</param>
    /// <param name="maxXZ">最大XZ</param>
    /// <returns>TroopInfos</returns>
    private static Field.TroopInfos LoadTroops(string fileName, int maxXZ)
    {
        if (System.IO.File.Exists(fileName) == false)
        {
            return null;
        }

        using (System.IO.FileStream ifs = new System.IO.FileStream(fileName, System.IO.FileMode.Open))
        using (System.IO.Compression.DeflateStream deflateStream = new System.IO.Compression.DeflateStream(ifs, System.IO.Compression.CompressionMode.Decompress))
        using (System.IO.MemoryStream mem = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[1024];
            while (true)
            {
                //Deflateで圧縮されたファイルからデータを読み込む
                int readBytes = deflateStream.Read(buffer, 0, buffer.Length);
                if (readBytes == 0)
                {
                    break;
                }

                //解凍されたデータを書き込む
                mem.Write(buffer, 0, readBytes);
            }

            // 始点位置に戻す
            mem.Position = 0;
            using (System.IO.BinaryReader br = new System.IO.BinaryReader(mem))
            {
                ushort version = br.ReadUInt16();

                int dataCount = br.ReadInt32();
                if (dataCount == 0)
                    return null;

                Field.TroopInfos troops = new Field.TroopInfos();

                // 保持されているデータ個数まで回す。
                for (int i = 0; i < dataCount; i++)
                {
                    Field.TroopInfos.PointInfo pointInfo = new Field.TroopInfos.PointInfo();

                    // Friend:0, Enemy:1
                    var whom = br.ReadByte();

                    // バージョン３以降は、MinJumpの情報が入っているが正しく設定されていないため飛ばす。
                    if (version >= 3)
                        br.ReadByte();

                    // 位置の読み込み
                    int cx = br.ReadInt32();
                    int cz = br.ReadInt32();
                    int cy = br.ReadInt32();

                    int r = maxXZ / 2 - cz;
                    int q = cx - (maxXZ + r) / 2;

                    // 位置情報の設定
                    pointInfo.Position = new Hex3(q, r, cy);

                    // whomの値に応じて、友軍か敵軍のリストに追加する。
                    if (whom == 0)
                    {
                        troops.Friends.Add(pointInfo);
                    }
                    else
                    {
                        troops.Enemies.Add(pointInfo);
                    }
                }

                return troops;
            }
        }
    }
}
