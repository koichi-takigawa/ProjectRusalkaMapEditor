using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class Compass : MonoBehaviour
{
    // コンパスの針（または盤面）のRectTransform
    private RectTransform mCompassRect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.TryGetComponentWithError<RectTransform>(out mCompassRect);
    }

    // Update is called once per frame
    void Update()
    {
        var cameraTransform = CameraController.Instance.MainCamera.transform;
        mCompassRect.localRotation = Quaternion.Euler(90 - cameraTransform.eulerAngles.x, 0, cameraTransform.eulerAngles.y);
    }
}
