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
    [SerializeField] public ToolKind Tool;
}
