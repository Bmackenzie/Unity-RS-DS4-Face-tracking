/********************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION This software is supplied under the 
terms of a license agreement or nondisclosure agreement with Intel Corporation 
and may not be copied or disclosed except in accordance with the terms of that 
agreement.
Copyright(c) 2011-2015 Intel Corporation. All Rights Reserved.

*********************************************************************************/
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class DynamicMesh : MonoBehaviour
{
    /// <summary>
    /// Mesh object associated with the game object.
    /// </summary>
    private Mesh m_mesh;

    /// <summary>
    /// Associated mesh filter.
    /// </summary>
    private MeshFilter m_mf;

    /// <summary>
    /// Thickness of the mesh grid lines.
    /// </summary>
    private const float MESH_GRIDLINE_SIZE = 0.5f;

    void Awake()
    {
        m_mf = GetComponent<MeshFilter>();

        if (m_mesh == null)
            m_mesh = new Mesh();

        SetMeshVisibility();
    }

    void Update()
    {
        SetMeshVisibility();
    }

    void OnApplicationQuit()
    {
        m_mesh.Clear();
    }

    /// <summary>
    /// Sets the id, vertices, and face information for the mesh.
    /// </summary>
    /// <param name="meshID">ID number of the mesh.</param>
    /// <param name="vertices">Array of vertex coordinates in the mesh.</param>
    /// <param name="triangles">Array of the triangles in the mesh (also known as mesh faces).</param>
    public void SetData(int meshID, Vector3[] vertices, int[] triangles)
    {
        m_mesh.name = "Mesh ID: " + meshID;
        m_mesh.vertices = vertices;
        m_mesh.triangles = triangles;

        //m_mesh.RecalculateNormals();  // <= May not be needed

        if (m_mf != null)
            m_mf.sharedMesh = m_mesh;
    }

    /// <summary>
    /// Hides or shows the mesh grid by changing the gridline thickness depending on the value of 
    /// the static bool (DFusion_Behavior.m_meshIsVisible).
    /// </summary>
    private void SetMeshVisibility()
    {
        if (DepthFusion.m_meshIsVisible)
        {
            GetComponent<Renderer>().material.SetFloat("Thickness", MESH_GRIDLINE_SIZE);
        }
        else
        {
            GetComponent<Renderer>().material.SetFloat("Thickness", 0);
        }
    }
}