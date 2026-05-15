using JL.Tactics;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

internal class Main : MonoBehaviour
{
    /// <summary>新規作成ボタン</summary>
    [SerializeField] Button ButtonNewMap;

    /// <summary>読み込みボタン</summary>
    [SerializeField] Button ButtonLoad;

    /// <summary>上書きボタン</summary>
    [SerializeField] Button ButtonSave;

    /// <summary>名前をつけて保存ボタン</summary>
    [SerializeField] Button ButtonSaveWithNamed;

    /// <summary>マテリアル</summary>
    [SerializeField] Material[] Materials;

    /// <summary>マップデータのルート</summary>
    [SerializeField] Transform MapRoot;

    /// <summary>MapのGameObject</summary>
    private Dictionary<(int q, int r), GameObject> EdittingGameObjects = new Dictionary<(int q, int r), GameObject>();

    /// <summary>編集中のMapFile名</summary>
    [System.NonSerialized] public static string EdittingMapFilePath = null;

    /// <summary>FieldView</summary>
    [System.NonSerialized] public static FieldView EdittingFieldView = new FieldView();

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

            // 変更を破棄してファイルを開く
            Debug.Log("Discarding changes and opening a map...");
        }

        OpenFileName ofn = new OpenFileName();
        ofn.title = "マップデータを開く";
        ofn.filter = "Map Files (*.map)\0*.map\0All Files (*.*)\0*.*\0\0";

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
        EdittingMapFilePath = ofn.file;

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
                // ブロック消去
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
            MapCreator.Mark(affectedPositions, pos.Q, pos.R);

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

                HashSet<(int q, int r)> affectedPositions = new HashSet<(int q, int r)>();
                MapCreator.Mark(affectedPositions, pos.Q, pos.R);

                // 更新
                MapCreator.Update(EdittingFieldView, MapRoot, Materials, EdittingGameObjects, affectedPositions);

                return true;
            }
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
