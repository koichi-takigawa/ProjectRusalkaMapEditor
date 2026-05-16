using JL.Tactics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#nullable enable

public class MapCreator
{
    /// <summary>
    /// 進行方向ごとのQ,R,Hのオフセット
    /// 指定の方向にタイルがあるかどうかを判定して、必要な面を生成するために使用する
    /// 下から見上げることはないため、上方向も含めて7方向とする
    /// </summary>
    static readonly Hex3Offset[] DIRECTIONS = new Hex3Offset[7]
    {
        new Hex3Offset(+0, +0, +1),     // 上
        new Hex3Offset(+1, -1, +0),     // １時方向
        new Hex3Offset(+1, +0, +0),     // ３時方向
        new Hex3Offset(+0, +1, +0),     // ５時方向
        new Hex3Offset(-1, +1, +0),     // ７時方向
        new Hex3Offset(-1, +0, +0),     // ９時方向
        new Hex3Offset(+0, -1, +0),     // 11時方向
    };

    /// <summary>
    /// MARK用のQ,Rオフセット
    /// </summary>
    static readonly Hex2Offset[] DIRECTIONS_FOR_MARK = new Hex2Offset[7]
    {
        new Hex2Offset(+0, +0),         // 現在地
        new Hex2Offset(+1, -1),         // １時方向
        new Hex2Offset(+1, +0),         // ３時方向
        new Hex2Offset(+0, +1),         // ５時方向
        new Hex2Offset(-1, +1),         // ７時方向
        new Hex2Offset(-1, +0),         // ９時方向
        new Hex2Offset(+0, -1),         // 11時方向
    };

    // 以下のように六角形の頂点位置を定義する。Z+2が天板の上側、Z-2が天板の下側となる。
    // これにSIZE_X,Y,Zを掛けることでUnity座標系へ変換される
    // 
    //         _______ Z+2 (=Field.SIZE)
    //    　／＼　　 
    //  　／ ｜ ＼　　
    //  ／　 ｜ 　＼ _ Z+1
    // ｜＼　｜　／｜
    // ｜　＼｜／　｜
    // ｜　　＋　　｜- Z+0
    // ｜　／｜＼　｜
    // ｜／　｜　＼｜_ Z-1
    //  ＼　 ｜ 　／
    //  　＼ ｜ ／ ｜
    //  　　＼／ __｜_ Z-2
    // ｜　　｜　　｜
    // X-2   X+0　 X+2(=Field.SQRT_3 / 2.0f * Field.SIZE)

    /// <summary>六角形の頂点位置計算用の定数X</summary>
    static readonly float SIZE_X = Field.SQRT_3 / 2.0f * Field.SIZE;

    /// <summary>六角形の頂点位置計算用の定数Y</summary>
    static readonly float SIZE_Y = Field.THICKNESS;

    /// <summary>六角形の頂点位置計算用の定数Z</summary>
    static readonly float SIZE_Z = Field.SIZE / 2.0f;

    // ポイント位置
    static readonly Vector3Int[] POSITIONS_INT = new Vector3Int[13]
    {
        new Vector3Int(+0, +0, +0),     // 天板中央
        new Vector3Int(+0, +0, +2),     // 天板上
        new Vector3Int(+1, +0, +1),     // 天板右上
        new Vector3Int(+1, +0, -1),     // 天板右下
        new Vector3Int(+0, +0, -2),     // 天板下
        new Vector3Int(-1, +0, -1),     // 天板左下
        new Vector3Int(-1, +0, +1),     // 天板左上
        new Vector3Int(+0, -1, +2),     // 底面上
        new Vector3Int(+1, -1, +1),     // 底面右上
        new Vector3Int(+1, -1, -1),     // 底面右下
        new Vector3Int(+0, -1, -2),     // 底面下
        new Vector3Int(-1, -1, -1),     // 底面左下
        new Vector3Int(-1, -1, +1),     // 底面左上
    };

    /// <summary>
    /// HEXの中央位置を計算する
    /// </summary>
    /// <param name="q">Q</param>
    /// <param name="r">R</param>
    /// <param name="h">H</param>
    /// <returns>Vector3</returns>
    static Vector3Int CalcCenterPositionInt(int q, int r, int h)
    {
        return new Vector3Int()
        {
            x = (2 * q + r),
            y = h,
            z = (-3 * r),
        };
    }

    // POSITIONSのどこをつないで六角柱を作るか
    static readonly int[][] POSITION_INDICES = new int[][]
    {
        // 以下のように使用して三角形を作っていく
        //          0   1   2
        //          0       1   2
        //          0           1   2...
        new int[] { 00, 01, 02, 03, 04, 05, 06, 01 },   // 上
        new int[] { 01, 07, 08, 02 },                   // １時方向
        new int[] { 02, 08, 09, 03 },                   // ３時方向
        new int[] { 03, 09, 10, 04 },                   // ５時方向
        new int[] { 04, 10, 11, 05 },                   // ７時方向
        new int[] { 05, 11, 12, 06 },                   // ９時方向
        new int[] { 06, 12, 07, 01 },                   // 11時方向
    };

    // UnityのUV座標系は左下原点
    static readonly Vector2[] UV_POSITIONS = new Vector2[15]
    {
        new Vector2(05f, 15f) / 64f,        // 天板中央
        new Vector2(05f, 19f) / 64f,        // 天板上
        new Vector2(09f, 17f) / 64f,        // 天板右上
        new Vector2(09f, 13f) / 64f,        // 天板右下
        new Vector2(05f, 11f) / 64f,        // 天板下
        new Vector2(01f, 13f) / 64f,        // 天板左下
        new Vector2(01f, 17f) / 64f,        // 天板左上

        new Vector2(09f, 09f) / 64f,        // 側面２右上
        new Vector2(09f, 07f) / 64f,        // 側面２右下
        new Vector2(05f, 07f) / 64f,        // 側面２左下
        new Vector2(05f, 09f) / 64f,        // 側面２左上

        new Vector2(05f, 09f) / 64f,        // 側面１右上
        new Vector2(05f, 07f) / 64f,        // 側面１右下
        new Vector2(01f, 07f) / 64f,        // 側面１左下
        new Vector2(01f, 09f) / 64f,        // 側面１左上
    };

    // UV_POSITIONSのどこをつないで六角柱を作るか
    static readonly int[][] UV_POSITION_INDICES = new int[][]
    {
        // 以下のように使用して三角形を作っていく
        //          0   1   2
        //          0       1   2
        //          0           1   2...
        new int[] { 00, 01, 02, 03, 04, 05, 06, 01 },   // 上
        new int[] { 07, 08, 09, 10 },                   // １時方向
        new int[] { 11, 12, 13, 14 },                   // ３時方向
        new int[] { 07, 08, 09, 10 },                   // ５時方向
        new int[] { 11, 12, 13, 14 },                   // ７時方向
        new int[] { 07, 08, 09, 10 },                   // ９時方向
        new int[] { 11, 12, 13, 14 },                   // 11時方向
    };

    /// <summary>UVのひとつのサイズ定数X</summary>
    static readonly float UV_SIZE_X = 10f / 64f;

    /// <summary>UVのひとつのサイズ定数Y</summary>
    static readonly float UV_SIZE_Y = 20f / 64f;

    // フィールド全体のメッシュを生成する
    internal static Dictionary<(int q, int r), GameObject> CreateMesh(FieldView field, Transform root, Material[] materials)
    {
        // 8x8ごとにグリッド分割して生成する。
        Dictionary<(int q, int r), GameObject> result = new Dictionary<(int q, int r), GameObject>();

        // グリッドを走査して、8x8の区画ごとにメッシュを生成する
        foreach (var grid in field.Grids)
        {
            // グリッドのQ,Rを8で割って、どの区画に属するかを計算
            int q = (int)System.Math.Floor(grid.Key.q / 8.0) * 8;
            int r = (int)System.Math.Floor(grid.Key.r / 8.0) * 8;

            // すでにこの区画が生成されているかを確認
            if (result.ContainsKey((q, r)))
            {
                continue;
            }

            // 親要素となるGameObjectを作成
            GameObject gameObject = new GameObject($"GeneratedMeshObject_{q}_{r}");
            gameObject.transform.parent = root;

            // 該当区画のメッシュを生成
            CreateMeshPart(field, gameObject, q, r, materials);

            // コライダの生成
            CreateColliderPart(field, gameObject, q, r);

            // 生成済み区画の登録
            result.Add((q, r), gameObject);
        }

        return result;
    }

    // 1マスの内容が変更されたときに、そのマスを含む区画を更新する必要があることを記録する
    internal static void Mark(HashSet<(int q, int r)> affectedPositions, int baseQ, int baseR)
    {
        // 変更されたマスを含む区画と、その周囲の区画が影響を受けうるため、周囲も含めて記録する
        for (int i = 0; i < DIRECTIONS_FOR_MARK.Length; i++)
        {
            // グリッドのQ,Rを8で割って、どの区画に属するかを計算
            int q = (int)System.Math.Floor((baseQ + DIRECTIONS_FOR_MARK[i].Q) / 8.0) * 8;
            int r = (int)System.Math.Floor((baseR + DIRECTIONS_FOR_MARK[i].R) / 8.0) * 8;

            // すでに記録されている場合はスキップ
            if (affectedPositions.Contains((q, r)))
            {
                continue;
            }

            // 記録
            affectedPositions.Add((q, r));
        }
    }

    // 1マスの内容が変更されたときに、そのマスを含む区画のメッシュを更新する
    internal static void Update(FieldView field, Transform root, Material[] materials, Dictionary<(int q, int r), GameObject> meshes, HashSet<(int q, int r)> affectedPositions)
    {
        foreach (var pos in affectedPositions)
        {
            // すでにこの区画が生成されている場合は削除する
            if (meshes.TryGetValue((pos.q, pos.r), out GameObject oldGameObject))
            {
                GameObject.Destroy(oldGameObject);
                meshes.Remove((pos.q, pos.r));
            }

            // 親要素となるGameObjectを作成
            GameObject gameObject = new GameObject($"GeneratedMeshObject_{pos.q}_{pos.r}");
            gameObject.transform.parent = root;

            // 該当区画のメッシュを生成
            CreateMeshPart(field, gameObject, pos.q, pos.r, materials);

            // コライダの生成
            CreateColliderPart(field, gameObject, pos.q, pos.r);

            // 生成済み区画の登録
            meshes.Add((pos.q, pos.r), gameObject);
        }
    }

    // 8x8の区画ごとにメッシュを生成する
    internal static void CreateMeshPart(FieldView field, GameObject gameObject, int baseQ, int baseR, Material[] materials)
    {
        // マテリアルがない場合はスキップ
        if (materials == null || materials.Length == 0)
        {
            return;
        }

        // メイン形状の座標リスト
        List<Vector3> listVertices = new List<Vector3>();

        // UV座標リスト
        List<Vector2> listUVs = new List<Vector2>();

        foreach (var grid in field.Grids
            .Where(a => baseQ <= a.Key.q && a.Key.q < baseQ + 8 && 
                        baseR <= a.Key.r && a.Key.r < baseR + 8))
        {
            // テクスチャレベル（０：天板）
            int level = 0;

            for (int h = FieldView.Grid.MAX_H - 1; h >= 0; h--)
            {
                // 現在ブロック
                FieldView.Tile currentTile = grid.Value.Tiles[h];

                // 現在位置が空間であればSkip
                if (currentTile == null || currentTile.Kind == FieldView.Tile.TileKind.無し)
                {
                    level = 0;
                    continue;
                }

                // 陸関係のみ処理
                if ((int)currentTile.Kind <= (int)FieldView.Tile.TileKind.陸系タイル開始 ||
                    (int)FieldView.Tile.TileKind.陸系タイル終了 <= (int)currentTile.Kind)
                {
                    continue;
                }

                // 中央位置のグリッド取得
                var centerInt = CalcCenterPositionInt(grid.Key.q, grid.Key.r, h);

                // 各点構成
                Vector3[] positions = new Vector3[POSITIONS_INT.Length];
                for (int i = 0; i < POSITIONS_INT.Length; i++)
                {
                    positions[i] = new Vector3(
                        (centerInt.x + POSITIONS_INT[i].x) * SIZE_X,
                        (centerInt.y + POSITIONS_INT[i].y) * SIZE_Y,
                        (centerInt.z + POSITIONS_INT[i].z) * SIZE_Z);
                }

                // 種別によるUVのオフセット
                Vector2 uvOffset = currentTile.Kind switch
                {
                    FieldView.Tile.TileKind.土肌 => new Vector2(0 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.草   => new Vector2(1 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.砂利 => new Vector2(2 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.岩場 => new Vector2(3 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.煉瓦 => new Vector2(4 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.木床 => new Vector2(5 * UV_SIZE_X, 0 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.砂地 => new Vector2(0 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.雪   => new Vector2(1 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.屋根 => new Vector2(2 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.溶岩 => new Vector2(3 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.塩   => new Vector2(4 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.氷   => new Vector2(5 * UV_SIZE_X, 1 * UV_SIZE_Y),
                    FieldView.Tile.TileKind.煙突 => new Vector2(0 * UV_SIZE_X, 2 * UV_SIZE_Y),
                    _ => new Vector2(5 * UV_SIZE_X, 2 * UV_SIZE_Y),
                };

                // levelによるオフセット
                uvOffset += (new Vector2(0f, -2f / 64f)) * (level < 4 ? level : 2 + (level % 2));

                // 各点構成
                Vector2[] uvs = new Vector2[UV_POSITIONS.Length];
                for (int i = 0; i < UV_POSITIONS.Length; i++)
                {
                    uvs[i] = UV_POSITIONS[i] + uvOffset;
                }

                // 必要なデータを探して登録
                for (int i = 0; i < (int)DIRECTIONS.Length; i++)
                {
                    // 内外判定
                    FieldView.Tile? nextTile = field[grid.Key.q + DIRECTIONS[i].Q, grid.Key.r + DIRECTIONS[i].R, h + DIRECTIONS[i].H];

                    // 必要
                    if (nextTile == null ||
                        (int)nextTile.Kind <= (int)FieldView.Tile.TileKind.陸系タイル開始 ||
                        (int)FieldView.Tile.TileKind.陸系タイル終了 <= (int)nextTile.Kind)
                    {
                        // 登録
                        CreateTriangles(positions, POSITION_INDICES[i], ref listVertices);
                        CreateTriangles(uvs, UV_POSITION_INDICES[i], ref listUVs);
                    }
                }

                level++;
            }
        }

        // 描画に必要なコンポーネントを追加
        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        // Meshオブジェクトを作成してデータを流し込む
        Mesh mesh = new Mesh();
        mesh.name = "MyCustomMesh";

        mesh.SetVertices(listVertices);
        mesh.SetUVs(0, listUVs);

        // サブメッシュは1つ
        mesh.subMeshCount = 1;
        mesh.SetTriangles(Enumerable.Range(0, listVertices.Count).ToArray(), 0);

        // 法線と境界を計算
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // MeshFilterにメッシュをセット
        meshFilter.mesh = mesh;

        // マテリアルを設定
        meshRenderer.material = materials[0];
    }

    internal static void CreateColliderPart(FieldView field, GameObject gameObject, int baseQ, int baseR)
    {
        // コライダ用の頂点リスト
        List<Vector3> listVertices = new List<Vector3>();

        foreach (var grid in field.Grids.Where(a => baseQ <= a.Key.q && a.Key.q < baseQ + 8 &&
                                                    baseR <= a.Key.r && a.Key.r < baseR + 8))
        {
            // 必要なデータを探して登録
            for (int i = 0; i < (int)DIRECTIONS.Length; i++)
            {
                for (int h = FieldView.Grid.MAX_H - 1; h >= 0; h--)
                {
                    // 現在ブロック
                    FieldView.Tile? currentTile = grid.Value.Tiles[h];
                    FieldView.Tile? nextTile = field[grid.Key.q + DIRECTIONS[i].Q, grid.Key.r + DIRECTIONS[i].R, h + DIRECTIONS[i].H];

                    // 書き込みが必要ない
                    if (currentTile == null || currentTile.Kind == FieldView.Tile.TileKind.無し ||
                        (nextTile != null && nextTile.Kind != FieldView.Tile.TileKind.無し))
                    {
                        continue;
                    }

                    // まとめて登録できる部分はまとめて登録するため、開始位置を覚える
                    int startH = h;

                    // 中央位置のグリッド取得
                    var centerInt = CalcCenterPositionInt(grid.Key.q, grid.Key.r, h);

                    h--;
                    for (; h >= 0; h--)
                    {
                        currentTile = grid.Value.Tiles[h];
                        nextTile = field[grid.Key.q + DIRECTIONS[i].Q, grid.Key.r + DIRECTIONS[i].R, h + DIRECTIONS[i].H];

                        // 書き込みが必要なくなったら抜ける
                        if (currentTile == null || currentTile.Kind == FieldView.Tile.TileKind.無し ||
                            (nextTile != null && nextTile.Kind != FieldView.Tile.TileKind.無し))
                        {
                            break;
                        }
                    }

                    // 必要な点のみ構成
                    Vector3[] positions = new Vector3[POSITIONS_INT.Length];
                    for (int j = 0; j < POSITION_INDICES[i].Length; j++)
                    {
                        positions[POSITION_INDICES[i][j]] = new Vector3()
                        {
                            x = (centerInt.x + POSITIONS_INT[POSITION_INDICES[i][j]].x) * SIZE_X,
                            y = (centerInt.y + POSITIONS_INT[POSITION_INDICES[i][j]].y * (startH - h)) * SIZE_Y,
                            z = (centerInt.z + POSITIONS_INT[POSITION_INDICES[i][j]].z) * SIZE_Z
                        };
                    }

                    // 登録
                    CreateTriangles(positions, POSITION_INDICES[i], ref listVertices);
                }
            }
        }

        // コライダを追加して頂点をセット
        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        Mesh mesh = new Mesh();
        mesh.name = "MyCustomColliderMesh";
        mesh.SetVertices(listVertices);
        mesh.SetTriangles(Enumerable.Range(0, listVertices.Count).ToArray(), 0);
        mesh.RecalculateBounds();
        meshCollider.sharedMesh = mesh;
    }

    // パーツを追加する
    private static void CreateTriangles<T>(T[] coordinate, int[] indices, ref List<T> destList)
    {
        for (int i = 1; i < indices.Length - 1; i++)
        {
            destList.Add(coordinate[indices[0]]);
            destList.Add(coordinate[indices[i]]);
            destList.Add(coordinate[indices[i + 1]]);
        }
    }
}
