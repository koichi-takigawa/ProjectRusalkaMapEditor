using System;
using UnityEngine;
#nullable enable

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    // アクセス用にstatic変数に取っておく。
    public static CameraController Instance { get; private set; } = default!;

    public static float PITCH_MAX = 70.0f;
    public static float PITCH_MIN = 20.0f;

    // カメラとの距離。この値でどの程度パースペクティブに表示されるか決定する。
    const float DISTANCE = 80;

    // 回転速度。
    const float ROTATE_SPEED = 10;

    // ホイール時のズームスピード。
    const float ZOOM_SPEED = 10;

    // カメラのFieldOfViewの範囲。Distanceの影響を受ける。
    const float FIELD_OF_VIEW_RANGE_MAX = 40;
    const float FIELD_OF_VIEW_RANGE_MIN = 4;
    const float INIT_FIELD_OF_VIEW = 6;

    // カメラ。FieldOfView設定用に使用
    Camera mCamera = default!;

    // 注視点。ここを基準に動かす。
    Vector3 mTarget = Vector3.zero;

    // アイレベル
    [SerializeField]
    float m_EyeHeight = 1.0f;

    // 水平回転角度
    float mYaw = 22.5f;

    // 上下回り込み角度
    public float Pitch = 30.0f;

    // カメラの表示範囲
    float FieldOfView
    {
        get => mCamera.fieldOfView;
        set => mCamera.fieldOfView = value;
    }

    private Vector3 mLastHitPoint;
    private Plane mMovePlane;

    private void Awake()
    {
        // 唯一インスタンスのはずなので覚えておく。
        Instance = this;

        // 各種コンポーネントを取得。
        mCamera = this.GetComponentWithError<Camera>();

        // 各種エラー出力
        ;

        // FieldOfViewの適用
        FieldOfView = INIT_FIELD_OF_VIEW;

        // 再計算
        Calc();
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    /// 移動モードの準備
    /// </summary>
    /// <param name="mousePosition"></param>
    /// <returns></returns>
    internal void PrepareMove(Vector3 mousePosition)
    {
        // マウス位置からRayを作成
        Ray ray = mCamera.ScreenPointToRay(mousePosition);

        // 地面にレイキャストして、衝突点を記憶
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // クリック時の衝突位置を保存
            mLastHitPoint = hit.point;

            // 平面を作成
            mMovePlane = new Plane(ray.direction, hit.point);

            // カーソルはここでは消さない
        }
        else
        {
            // レイが地面に当たらなかった場合はワールド原点を基準にする。
            mLastHitPoint = Vector3.zero;
            mMovePlane = new Plane(ray.direction, Vector3.zero);
        }
    }

    /// <summary>
    /// 移動モードの開始
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void StartMove(Vector3 mousePosition)
    {
    }

    /// <summary>
    /// 移動モード継続
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void Move(Vector3 mousePosition)
    {
        Ray ray = mCamera.ScreenPointToRay(mousePosition);

        if (mMovePlane.Raycast(ray, out float enter))
        {
            Vector3 newHitPoint = ray.GetPoint(enter); // 現在のマウス位置に対応するワールド座標
            Vector3 move = mLastHitPoint - newHitPoint; // 差分を計算
            mTarget += move; // カメラを移動

            // 再計算
            Calc();
        }
    }

    internal void StopMove(Vector3 mousePosition)
    {
        // ドラッグ終了
    }

    /// <summary>
    /// 回転モードの準備
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void PrepareRotation(Vector3 mousePosition)
    {
    }

    /// <summary>
    /// 回転モードの開始
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void StartRotation(Vector3 mousePosition)
    {
    }

    /// <summary>
    /// 回転モードの継続
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void Rotation(Vector3 mousePosition)
    {
        // マウスの移動量を取得
        float dx = Input.GetAxis("Mouse X");
        float dy = Input.GetAxis("Mouse Y");

        // 回転
        mYaw -= dx * ROTATE_SPEED;
        Pitch = Mathf.Clamp(Pitch - dy * ROTATE_SPEED, PITCH_MIN, PITCH_MAX);

        // 再計算
        Calc();
    }

    /// <summary>
    /// 回転モードの終了
    /// </summary>
    /// <param name="mousePosition"></param>
    internal void StopRotation(Vector3 mousePosition)
    {
        // ドラッグ終了
    }

    // 現在の設定からカメラの位置・方向を設定
    void Calc()
    {
        var pos = mTarget + Vector3.up * m_EyeHeight;
        pos.x += Mathf.Sin(mYaw * Mathf.Deg2Rad) * Mathf.Cos(Pitch * Mathf.Deg2Rad) * DISTANCE;
        pos.z += -Mathf.Cos(mYaw * Mathf.Deg2Rad) * Mathf.Cos(Pitch * Mathf.Deg2Rad) * DISTANCE;
        pos.y += Mathf.Sin(Pitch * Mathf.Deg2Rad) * DISTANCE;

        this.transform.position = pos;
        this.transform.LookAt(mTarget + Vector3.up * m_EyeHeight);
    }

    // カメラの位置を設定
    internal void Set(Vector3 vector3, float yaw = 45.0f, float pitch = 30.0f, float fov = INIT_FIELD_OF_VIEW)
    {
        mTarget = vector3;
        FieldOfView = fov;
        mYaw = yaw;
        Pitch = pitch;

        Calc();
    }

    /// <summary>
    /// マウスホイール
    /// </summary>
    /// <param name="mouseWheelScroll"></param>
    internal void Wheel(float mouseWheelScroll)
    {
        FieldOfView = Math.Min(FIELD_OF_VIEW_RANGE_MAX, Math.Max(FIELD_OF_VIEW_RANGE_MIN, mCamera.fieldOfView - mouseWheelScroll * ZOOM_SPEED));
    }
}