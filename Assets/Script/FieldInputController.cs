using JL.Tactics;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
#nullable enable

public class FieldInputController : MonoBehaviour
{
    // アクセス用にstatic変数に取っておく。
    public static FieldInputController Instance { get; private set; } = default!;

    [Tooltip("カーソルオブジェクト")]
    [SerializeField] GameObject CursorObject = default!;

    [Tooltip("カーソル位置表示用テキスト")]
    [SerializeField] TextMeshProUGUI? CursorPositionText;

    // Hex3?型を受け取るイベントハンドラ
    internal event Action<Hex3?>? OnClick;

    // カーソル位置
    private Hex3? mCursorPos = null;

    // ロック中カウンタ
    private int mLockingCounter = 0;

    /// <summary>ロック中かどうか</summary>
    public bool Lock
    {
        get => mLockingCounter != 0;
        set
        {
            if (value)
            {
                mLockingCounter++;
            }
            else if (mLockingCounter > 0)
            {
                mLockingCounter--;
            }
            else
            {
                Debug.LogWarning("FieldInputController: Lock is already false.");
            }
        }
    }

    /// <summary>マウスのボタン種類</summary>
    private enum MouseButton
    {
        Invalid,
        Left,
        Right,
    }

    // 現在押下中のボタン
    private MouseButton mButton;

    // 押下時の位置
    private Vector3 mMouseDownPosition;

    // ドラッグ中かどうか
    private bool mIsDragging = false;

    // Awake is called when the script instance is being loaded
    private void Awake()
    {
        // 唯一インスタンスのはずなので覚えておく。
        Instance = this;

        // 各種エラー出力
        Debug.Assert(CursorObject != null, this.GetType().Name + ".Cursor not found.");
        if (CursorPositionText == null) Debug.LogWarning(this.GetType().Name + ".CursorPositionText is null");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // ロック中
        if (Lock)
        {
            // 必要な初期化があれば行って抜ける。
            switch (mButton)
            {
                case MouseButton.Left:
                    CameraController.Instance.StopMove(Input.mousePosition);
                    break;
                case MouseButton.Right:
                    CameraController.Instance.StopRotation(Input.mousePosition);
                    break;
            }
            mButton = MouseButton.Invalid;
            mIsDragging = false;           
            return;
        }

        // マウス位置を取得
        Vector3 mousePosition = Input.mousePosition;

        // UI上でのクリック判定
        bool isPointerOverGameObject = EventSystem.current != null ? EventSystem.current.IsPointerOverGameObject() : false;

        // カーソル位置が画面外か

        // 先にカーソルの状態を決める。

        // Move中でかつドラッグを開始していない場合はカーソル更新停止
        if (mButton != MouseButton.Invalid && mIsDragging == false)
        {
            ;
        }
        // UI上にカーソルがあるか、ボタンが押されている場合、カーソルが画面外にある場合または位置が取得できない場合はカーソル無効
        else if (isPointerOverGameObject || mButton != MouseButton.Invalid || IsCursorOutOfScreen(mousePosition) || GetCursorPos(out Hex3 hex) == false)
        {
            // カーソルを無効化する
            mCursorPos = null;

            if (CursorObject.activeSelf)
            {
                CursorObject.SetActive(false);
            }

            if (CursorPositionText != null)
            {
                CursorPositionText.text = "-";
            }
        }
        else
        {
            // カーソルを有効化する
            mCursorPos = hex;

            hex.ToPointFloat(out float x, out float y);

            CursorObject.transform.position = new Vector3(x * Field.SIZE, hex.H * Field.THICKNESS, y * Field.SIZE);

            if (CursorObject.activeSelf == false)
            {
                CursorObject.SetActive(true);
            }

            if (CursorPositionText != null)
            {
                CursorPositionText.text = $"(q:{hex.Q}, r:{hex.R}, h:{hex.H})";
            }
        }

        // マウスボタンが押されたら

        // 左ボタンダウン
        if (!isPointerOverGameObject && mButton == MouseButton.Invalid && Input.GetMouseButtonDown(0))
        {
            mButton = MouseButton.Left;
            mIsDragging = false;
            mMouseDownPosition = mousePosition;
            CameraController.Instance.PrepareMove(mousePosition);
        }

        // 右ボタンダウン
        if (!isPointerOverGameObject && mButton == MouseButton.Invalid && Input.GetMouseButtonDown(1))
        {
            mButton = MouseButton.Right;
            mIsDragging = false;
            mMouseDownPosition = mousePosition;
            CameraController.Instance.PrepareRotation(mousePosition);
        }

        // ドラッグ中
        if (mButton != MouseButton.Invalid)
        {
            // ドラッグかクリックか悩んでいる場合
            if (mIsDragging == false)
            {
                // ある程度動いたらドラッグ開始
                if ((mousePosition - mMouseDownPosition).magnitude > 10.0f)
                {
                    // ドラッグで決定（クリックイベントを配信しない）
                    mIsDragging = true;

                    // ドラッグ開始通知
                    switch (mButton)
                    {
                        case MouseButton.Left:
                            CameraController.Instance.StartMove(mousePosition);
                            break;
                        case MouseButton.Right:
                            CameraController.Instance.StartRotation(mousePosition);
                            break;
                    }
                }
            }

            switch (mButton)
            {
                case MouseButton.Left:

                    // 左ボタンが離されたら
                    if (Input.GetMouseButton(0) == false)
                    {
                        // ドラッグ中ではない場合。
                        if (mIsDragging == false)
                        {
                            // ハンドラが登録されていればイベント発行
                            OnClick?.Invoke(mCursorPos);
                        }

                        CameraController.Instance.StopMove(mousePosition);
                        mButton = MouseButton.Invalid;
                        mIsDragging = false;
                        return;
                    }

                    // ドラッグ中の場合は移動
                    if (mIsDragging)
                    {
                        CameraController.Instance.Move(mousePosition);
                    }

                    break;

                case MouseButton.Right:

                    // 右ボタンが離されたら
                    if (Input.GetMouseButton(1) == false)
                    {
                        // ドラッグ中ではない場合。
                        if (mIsDragging == false)
                        {
                            // ハンドラが登録されていればイベント発行
                            OnClick?.Invoke(null);
                        }

                        CameraController.Instance.StopRotation(mousePosition);
                        mButton = MouseButton.Invalid;
                        mIsDragging = false;
                        return;
                    }

                    // ドラッグ中の場合は回転
                    if (mIsDragging)
                    {
                        CameraController.Instance.Rotation(mousePosition);
                    }

                    break;
            }
        }

        // ズーム
        if (!isPointerOverGameObject)
        {
            // マウスカーソルの移動量を取得
            float mouseWheelScroll = Input.GetAxis("Mouse ScrollWheel");

            if (mouseWheelScroll != 0)
            {
                CameraController.Instance.Wheel(mouseWheelScroll);
            }
        }
    }

    /// <summary>
    /// カーソル位置の取得
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    internal bool GetCursorPos(out Hex3 hex)
    {
        // レイを飛ばす
        Ray ray = CameraController.Instance.MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            // thicknessの半分だけずらす。
            Vector3 pos = hit.point + Vector3.up * Field.THICKNESS / 2;

            Hex2 tmpHex = JL.Tactics.Hex2.ToHex(pos.x / Field.SIZE, pos.z / Field.SIZE);
            int tmpHeight = (int)Math.Floor(pos.y / Field.THICKNESS);

            hex = new Hex3(tmpHex.Q, tmpHex.R, tmpHeight);
            return true;
        }

        hex = new Hex3();
        return false;
    }

    /// <summary>
    /// カーソル位置が画面外かどうか判定する
    /// </summary>
    private bool IsCursorOutOfScreen(Vector3 mousePosition)
    {
        // 画面サイズ取得
        int screenWidth = Screen.width;
        int screenHeight = Screen.height;

        // 画面外判定
        if (mousePosition.x < 0 || mousePosition.x > screenWidth || mousePosition.y < 0 || mousePosition.y > screenHeight)
        {
            return true;
        }

        return false;
    }
}
