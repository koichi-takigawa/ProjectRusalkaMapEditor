using UnityEngine;

public class ToolItem : MonoBehaviour
{
    /// <summary>ツール</summary>
    public enum ToolKind
    {
        Pen,
        Paint,
    }

    // ツール
    [SerializeField] private ToolKind tool;

    public ToolKind Tool => tool;
}
