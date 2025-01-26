using UnityEngine;

public class Parallelepiped : MonoBehaviour
{
    void Start()
    {
        // Create a new mesh
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Define vertices
        Vector3[] vertices = new Vector3[]
        {
            // Bottom face
            new Vector3(0, 0, 0), // 0
            new Vector3(1, 0, 0), // 1
            new Vector3(1.5f, 0, 1), // 2
            new Vector3(0.5f, 0, 1), // 3

            // Top face
            new Vector3(0, 1, 0), // 4
            new Vector3(1, 1, 0), // 5
            new Vector3(1.5f, 1, 1), // 6
            new Vector3(0.5f, 1, 1)  // 7
        };

        // Define triangles (2 per face)
        int[] triangles = new int[]
        {
            // Bottom face
            0, 1, 3,
            1, 2, 3,

            // Top face
            4, 7, 5,
            5, 7, 6,

            // Front face
            0, 4, 1,
            1, 4, 5,

            // Back face
            3, 2, 7,
            2, 6, 7,

            // Left face
            0, 3, 4,
            4, 3, 7,

            // Right face
            1, 5, 2,
            2, 5, 6
        };

        // Assign vertices and triangles to the mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Recalculate normals for lighting
        mesh.RecalculateNormals();
    }
}