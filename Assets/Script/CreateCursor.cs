using JL.Tactics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class CreateCursor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Mesh mesh = new Mesh();

        // 1. 頂点座標の作成 (12個: 上6個 + 下6個)
        Vector3[] vertices = new Vector3[12];

        for (int i = 0; i < 6; i++)
        {
            float angle = (30.0f + i * 60.0f) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * Field.SIZE;
            float z = Mathf.Sin(angle) * Field.SIZE;

            vertices[i] = new Vector3(x, Field.THICKNESS, z);      // 上の頂点
            vertices[i + 6] = new Vector3(x, 0, z); // 下の頂点
        }

        // 2. インデックスの作成 (18本の線 = 36個のインデックス)
        int[] indices = new int[36];
        int idx = 0;

        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;

            // 上の六角形のエッジ (6本)
            indices[idx++] = i;
            indices[idx++] = next;

            // 下の六角形のエッジ (6本)
            indices[idx++] = i + 6;
            indices[idx++] = next + 6;

            // 垂直方向のエッジ (6本)
            indices[idx++] = i;
            indices[idx++] = i + 6;
        }

        mesh.vertices = vertices;
        // 重要：ここで「これは三角形ではなく線です」と指定する
        mesh.SetIndices(indices, MeshTopology.Lines, 0);

        GetComponent<MeshFilter>().mesh = mesh;
    }
}
