using UnityEngine;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    /// <summary>新規作成ボタン</summary>
    [SerializeField] Button ButtonNewMap;

    /// <summary>読み込みボタン</summary>
    [SerializeField] Button ButtonLoad;

    /// <summary>上書きボタン</summary>
    [SerializeField] Button ButtonSave;

    /// <summary>名前をつけて保存ボタン</summary>
    [SerializeField] Button ButtonSaveWithNamed;

    /// <summary>編集中のMapFile名</summary>
    public static string EdittingMapFilePath = null;

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

        // 新規作成ボタン
        ButtonNewMap?.onClick.AddListener(OnNew);

        // 開くボタン
        ButtonLoad?.onClick.AddListener(OnOpen);

        // 上書き保存ボタン
        ButtonSave?.onClick.AddListener(OnSave);

        // 名前を付けて保存ボタン
        ButtonSaveWithNamed?.onClick.AddListener(OnSaveWithNamed);
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
        EdittingMapFilePath = sfn.file;

        // 変更なしにする
        HasChanges = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
