using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR;

public class MeshController : MonoBehaviour
{
    [SerializeField]
    ARMeshManager arMeshManager;

    [SerializeField]
    GpuVoxelRenderer gpuVoxelRenderer;

    [SerializeField]
    GameObject obj;

    bool isFire = false;

    [ContextMenu("Create Combine Mesh")]
    public void createCombineMesh()
    {
        if (isFire)
            return;

        isFire = true;

        var meshes = arMeshManager.meshes;
        Debug.Log(meshes.Count);
        Debug.Log(meshes[0].mesh.colors.Length);
        Debug.Log(meshes[0].mesh.normals.Length);

        CombineInstance[] combine = new CombineInstance[meshes.Count];
        for (int i = 0; i < meshes.Count; i++)
        {
            combine[i].mesh = meshes[i].sharedMesh;
            combine[i].transform = meshes[i].transform.localToWorldMatrix;
            // meshFilterList[i].gameObject.SetActive(false);
        }

        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combine);

        // obj.GetComponent<MeshFilter>()
        obj.GetComponent<MeshFilter>().mesh = combinedMesh;

        // arMeshManager.
        // gpuVoxelRenderer.createVoxel();
        gameObject.GetComponent<ARMeshManager>().enabled = false;
        Debug.Log("press step1");
    }
}
