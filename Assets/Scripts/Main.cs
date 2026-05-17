using JL.Tactics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#nullable enable

internal class Main : MonoBehaviour
{
    /// <summary>新規作成ボタン</summary>
    [SerializeField] Button ButtonNewMap = default!;

    /// <summary>読み込みボタン</summary>
    [SerializeField] Button ButtonLoad = default!;

    /// <summary>上書きボタン</summary>
    [SerializeField] Button ButtonSave = default!;

    /// <summary>名前をつけて保存ボタン</summary>
    [SerializeField] Button ButtonSaveWithNamed = default!;

    /// <summary>マテリアル</summary>
    [SerializeField] Material[] Materials = default!;

    /// <summary>マップデータのルート</summary>
    [SerializeField] Transform MapRoot = default!;

    /// <summary>ツールのトグルグループ</summary>
    [SerializeField] ToggleGroup ToolToggleGroup = default!;

    /// <summary>MapのGameObject</summary>
    private Dictionary<(int q, int r), GameObject> EdittingGameObjects = new Dictionary<(int q, int r), GameObject>();

    /// <summary>編集中のMapFile名</summary>
    [System.NonSerialized] public static string? EdittingMapFilePath = null;

    /// <summary>FieldView</summary>
    [System.NonSerialized] public static FieldView EdittingFieldView = new FieldView();

    /// <summary>現在選択中のツール</summary>
    ToolItem.ToolKind CurrentPen
    {
        get
        {
            var activeToggle = ToolToggleGroup?.ActiveToggles()?.FirstOrDefault();
            return activeToggle?.GetComponent<ToolItem>()?.Tool ?? ToolItem.ToolKind.Pen;
        }
    }

    /// <summary>
    /// 変更有無(true:変更あり、false:変更なし)。変更がある場合は、マップを閉じる前に保存するか確認する。
    /// </summary>
    private bool _hasChanges = false;

    /// <summary>
    /// 変更有無(true:変更あり、false:変更なし)。変更がある場合は、マップを閉じる前に保存するか確認する。
    /// </summary>
    public bool HasChanges
    {
        get => _hasChanges;
        set
        {
            // 変更有無を更新
            _hasChanges = value;

            // 保存ボタンの有効化/無効化を更新
            // 何らかのファイルを編集中でかつ変更がある場合のみ保存可能
            ButtonSave.interactable = value && EdittingMapFilePath != null;

            // 名前を付けて保存ボタンの有効化/無効化を更新
            // 何らかのファイルを編集中か、変更がある場合のみ名前を付けて保存可能
            ButtonSaveWithNamed.interactable = value || EdittingMapFilePath != null;
        }
    }

    void Awake()
    {
        // ボタン群の初期化
        EdittingMapFilePath = null;
        HasChanges = false;

        // エラー表示
        Debug.Assert(ButtonNewMap != null, $"{this.name}.ButtonNewMap is null.");
        Debug.Assert(ButtonLoad != null, $"{this.name}.ButtonLoad is null.");
        Debug.Assert(ButtonSave != null, $"{this.name}.ButtonSave is null.");
        Debug.Assert(ButtonSaveWithNamed != null, $"{this.name}.ButtonSaveWithNamed is null.");

        Debug.Assert(Materials != null && Materials.Length > 0, $"{this.name}.Materials is null or empty.");
        Debug.Assert(MapRoot != null, $"{this.name}.MapRoot is null.");

        Debug.Assert(ToolToggleGroup != null, $"{this.name}.ToolToggleGroup is null.");
    }

    /// <summary>
    /// 新規作成ボタン
    /// </summary>
    private void OnNew()
    {
        // 新しいマップを作成する前に、現在のマップに変更があるか確認する
        if (HasChanges)
        {
            var dialogResult = Dialog.ShowMessageBox(
                "新規作成により現在までに編集した情報は失われます。継続しますか？",
                "確認",
                Dialog.MessageBoxButtons.OKCancel,
                Dialog.MessageBoxIcon.Question
            );

            // キャンセルされた場合は、新規作成を中止する
            if (dialogResult != Dialog.MessageBoxResult.IDOK)
                return;

            // 変更を破棄して新規作成
            Debug.Log("Discarding changes and creating a new map...");
        }

        // 既存のマップオブジェクトを削除
        foreach (var go in EdittingGameObjects.Values)
        {
            Destroy(go);
        }

        // 編集中のマップファイルパスをリセット
        EdittingMapFilePath = null;

        // 変更なしにする
        HasChanges = false;
    }

    /// <summary>
    /// 開くボタン
    /// </summary>
    private void OnOpen()
    {
        // マップを開く前に、現在のマップに変更があるか確認する
        if (HasChanges)
        {
            var dialogResult = Dialog.ShowMessageBox(
                "ファイルを開くことで現在までに編集した情報は失われます。継続しますか？",
                "確認",
                Dialog.MessageBoxButtons.OKCancel,
                Dialog.MessageBoxIcon.Question
            );

            // キャンセルされた場合は、ファイルを開く処理を中止する
            if (dialogResult != Dialog.MessageBoxResult.IDOK)
                return;
        }

        OpenFileName ofn = new OpenFileName();
        ofn.title = "マップデータを開く";
        ofn.filter = "マップファイル(*.jmap3, *.jmap4)\0*.jmap3;*.jmap4\0すべてのファイル(*.*)\0*.*\0\0";

        // フラグ設定
        // 0x00080000: OFN_EXPLORER (新しいスタイルのダイアログを使用)
        // 0x00000008: OFN_NOCHANGEDIR (ダイアログを閉じた後もカレントディレクトリを変更しない)
        // 0x00000800: OFN_PATHMUSTEXIST (存在するパスのみ選択可能)
        // 0x00001000: OFN_FILEMUSTEXIST (存在するファイルのみ選択可能)
        ofn.flags = 0x00080000 | 0x00000008 | 0x00000800 | 0x00001000;

        // 開くダイアログを表示
        if (Dialog.ShowOpenFileDialog(ofn) == false)
        {
            // 失敗した場合、エラーコードを確認（0以外なら何らかの設定ミス）
            // [DllImport("comdlg32.dll")] public static extern int CommDlgExtendedError();
            Debug.Log("Dialog cancelled or failed.");
            return;
        }

        // 選択されたファイルパスを取得
        var path = Path.GetFullPath(ofn.file.Trim());
        FieldView? fieldView = null;

        switch (System.IO.Path.GetExtension(path).ToLower())
        {
            case ".jmap3":

                fieldView = JMap3.Load(path);
                break;

            case ".jmap4":

                fieldView = JMap4.Load(path);
                break;

            default:
                Dialog.ShowMessageBox(
                    "サポートされていないファイル形式です。",
                    "メッセージ",
                    Dialog.MessageBoxButtons.OK,
                    Dialog.MessageBoxIcon.Information
                );
                return;
        }

        // ファイルの読み込みに失敗した場合はエラーメッセージを表示して処理を中止する
        if (fieldView == null)
        {
            Dialog.ShowMessageBox(
                   "ファイルの読み込みに失敗しました。",
                   "エラー",
                   Dialog.MessageBoxButtons.OK,
                   Dialog.MessageBoxIcon.Error
               );
            return;
        }

        // 読み込んだフィールドビューを編集中のフィールドビューに設定
        EdittingFieldView = fieldView;

        // 既存のマップオブジェクトを削除
        if (EdittingGameObjects != null)
        {
            foreach (var go in EdittingGameObjects.Values)
            {
                Destroy(go);
            }
        }

        // マップを表示
        EdittingGameObjects = MapCreator.CreateMesh(EdittingFieldView, MapRoot, Materials);

        // 編集中のファイルパスを設定
        EdittingMapFilePath = path;

        // 変更なしにする
        HasChanges = false;
    }

    /// <summary>
    /// 上書き保存ボタン押下時
    /// </summary>
    private void OnSave()
    {
        // 変更なしにする
        HasChanges = false;
    }

    /// <summary>
    /// 名前を付けて保存押下時
    /// </summary>
    private void OnSaveWithNamed()
    {
        OpenFileName sfn = new OpenFileName();
        sfn.title = "マップデータを名前を付けて保存";
        sfn.filter = "Map Files (*.map)\0*.map\0All Files (*.*)\0*.*\0\0";

        // フラグ設定
        // 0x00080000: OFN_EXPLORER (新しいスタイルのダイアログを使用)
        // 0x00000008: OFN_NOCHANGEDIR (ダイアログを閉じた後もカレントディレクトリを変更しない)
        // 0x00000002: OFN_OVERWRITEPROMPT (既存ファイルがある場合に警告を出す)
        // 0x00000004: OFN_HIDEREADONLY (読み取り専用チェックボックスを隠す)
        sfn.flags = 0x00080000 | 0x00000008 | 0x00000002 | 0x00000004;

        if (Dialog.ShowSaveFileDialog(sfn) == false)
        {
            // 失敗した場合、エラーコードを確認（0以外なら何らかの設定ミス）
            // [DllImport("comdlg32.dll")] public static extern int CommDlgExtendedError();
            Debug.Log("Dialog cancelled or failed.");
            return;
        }

        // 選択されたファイルパスを取得して更新
        EdittingMapFilePath = Path.GetFullPath(sfn.file.Trim());

        // 変更なしにする
        HasChanges = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 新規作成ボタン
        ButtonNewMap?.onClick.AddListener(OnNew);

        // 開くボタン
        ButtonLoad?.onClick.AddListener(OnOpen);

        // 上書き保存ボタン
        ButtonSave?.onClick.AddListener(OnSave);

        // 名前を付けて保存ボタン
        ButtonSaveWithNamed?.onClick.AddListener(OnSaveWithNamed);

        // クリックイベントのテスト
        FieldInputController.Instance.OnClick += (button, fromPos, toPos) =>
        {
            // 有効なボタンと位置が提供されていることを確認
            if (button != null && fromPos != null && toPos != null)
            {
                switch (CurrentPen)
                {
                    case ToolItem.ToolKind.Pen:

                        // ペンツールが選択されている場合は、クリックイベントに応じてブロックの追加/削除を行う
                        switch (button)
                        {
                            case FieldInputController.Button.Left:
                                // 左クリックでブロックを追加
                                HasChanges |= AddBlock(toPos);
                                break;
                            case FieldInputController.Button.Right:
                                // 右クリックでブロックを削除
                                HasChanges |= RemoveBlock(fromPos);
                                break;
                        }
                        break;

                    case ToolItem.ToolKind.Paint:

                        // ペイントツールが選択されている場合はペイント
                        HasChanges |= PaintBlocks(fromPos);
                        break;

                    default:

                        // その他のツールが選択されている場合は、クリックイベントを無視する
                        return;
                }
            }
        };
    }

    // 指定された位置にブロックを追加する
    private bool AddBlock(Hex3 pos)
    {
        // 指定範囲内
        if (pos.H >= 0 && pos.H < FieldView.Grid.MAX_H)
        {
            // 編集中フィールドから対象のグリッドを取得し、指定された位置にブロックが存在する場合は削除する
            if (EdittingFieldView.Grids.TryGetValue((pos.Q, pos.R), out FieldView.Grid grid) == false)
            {
                // 対象のグリッドが存在しない場合は新規作成して追加する
                grid = new FieldView.Grid();
                EdittingFieldView.Grids.Add((pos.Q, pos.R), grid);
            }

            // 指定の位置に草原を仮設定する
            grid.Tiles[pos.H].Kind = FieldView.Tile.TileKind.草;

            HashSet<(int q, int r)> affectedPositions = new HashSet<(int q, int r)>();
            MapCreator.MarkAround(affectedPositions, pos.Q, pos.R);

            // 更新
            MapCreator.Update(EdittingFieldView, MapRoot, Materials, EdittingGameObjects, affectedPositions);

            return true;
        }

        return false;
    }

    // 指定された位置のブロックを削除する
    private bool RemoveBlock(Hex3 pos)
    {
        // 編集中フィールドから対象のグリッドを取得し、指定された位置にブロックが存在する場合は削除する
        if (EdittingFieldView.Grids.TryGetValue((pos.Q, pos.R), out FieldView.Grid grid) && pos.H >= 0 && pos.H < FieldView.Grid.MAX_H)
        {
            if (grid.Tiles[pos.H].Kind != FieldView.Tile.TileKind.無し)
            {
                grid.Tiles[pos.H].Kind = FieldView.Tile.TileKind.無し;

                // データがなくなったら排除する。
                if (grid.HasData == false)
                {
                    EdittingFieldView.Grids.Remove((pos.Q, pos.R));
                }

                HashSet<(int q, int r)> affectedPositions = new HashSet<(int q, int r)>();
                MapCreator.MarkAround(affectedPositions, pos.Q, pos.R);

                // 更新
                MapCreator.Update(EdittingFieldView, MapRoot, Materials, EdittingGameObjects, affectedPositions);

                return true;
            }
        }

        return false;
    }

    // 塗りつぶし
    private bool PaintBlocks(Hex3 pos)
    {
        // 編集中フィールドから対象のグリッドを取得し、指定された位置にブロックが存在する場合は削除する
        if (EdittingFieldView.Grids.TryGetValue((pos.Q, pos.R), out FieldView.Grid grid) == false ||
            pos.H < 0 || FieldView.Grid.MAX_H <= pos.H)
        {
            return false;
        }

        // その場所に何もない場合は塗りつぶしできない
        if (grid.Tiles[pos.H].Kind == FieldView.Tile.TileKind.無し)
        {
            return false;
        }

        // 塗りつぶしの対象の色を取得
        var targetKind = grid.Tiles[pos.H].Kind;

        // 上下に空白でないタイルが続く範囲を取得
        int minH = pos.H;
        int maxH = pos.H;

        // 上方向の範囲を取得（境界チェックを条件式に含める）
        while (maxH + 1 < FieldView.Grid.MAX_H && grid.Tiles[maxH + 1].Kind == targetKind)
        {
            maxH++;
        }

        // 下方向の範囲を取得
        while (minH - 1 >= 0 && grid.Tiles[minH - 1].Kind == targetKind)
        {
            minH--;
        }

        // 範囲内のタイルを塗りつぶし
        for (int h = minH; h <= maxH; h++)
        {
            grid.Tiles[h].Kind = FieldView.Tile.TileKind.草;
        }

        // ブロックは増減しないので、現在のグリッドのみを更新する。
        HashSet<(int q, int r)> affectedPositions = new HashSet<(int q, int r)>();
        MapCreator.Mark(affectedPositions, pos.Q, pos.R);

        // 更新
        MapCreator.Update(EdittingFieldView, MapRoot, Materials, EdittingGameObjects, affectedPositions);

        return true;
    }
}
