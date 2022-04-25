using System;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Linq;

public class GpuVoxelRenderer : MonoBehaviour
{
    [SerializeField] ComputeShader voxelCalculator;
    [SerializeField] Mesh cloneMesh;
    [SerializeField] GameObject sampleObject;
    [SerializeField] Material voxelMaterial;

    ComputeBuffer voxelBuffer;
    ComputeBuffer argBuffer;
    ComputeBuffer collisionDetectionBuffer;
    const int voxelSizeOneLine = 10;
    const int voxelCountOneLine = 128; //16 or 32 or 64 or 128 or 256
    float voxelScale;
    MeshFilter meshFilter;
    SkinnedMeshRenderer skinnedMeshRenderer;

    Vector4[] voxelVertices = new Vector4[8];
    Vector3[] meshVertices;
    int[] meshIndices;

    uint[] args = new uint[5] { 0, 0, 0, 0, 0, };

    bool isReadyCreateVoxel = false;
    bool isReadyUpdateMeshColor = false;
    ComputeBuffer vertexBuffer;
    ComputeBuffer indexBuffer;
    Matrix4x4 matrix;
    int meshIndicesLength;
    int meshVerticesLength;
    Texture2D RGB_Texture;

    [SerializeField] ARCameraManager arCameraManager;
    [SerializeField] ARCameraBackground arCameraBackground;

    [SerializeField]
    GameObject test;

    Ray _ray;

    public struct VoxelData
    {
        public Vector3 position;
        public Color color;
        public int isRendering;
    }

    public struct VertexData
    {
        public Vector3 position;
        public Color color;
    }

    public struct CollisionData
    {
        public int voxelIndex;
        public float distance;
        public Vector3 pos;
    }

    public struct DebugData
    {
        public Vector3 index;
    }

    void Start()
    {
        arCameraManager.frameReceived += OnARCameraFrameReceived;
    }

    unsafe void OnARCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);
        image.Convert(conversionParams, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);
        image.Dispose();

        if (RGB_Texture == null)
        {
            var x = conversionParams.outputDimensions.x;
            var y = conversionParams.outputDimensions.y;
            RGB_Texture = new Texture2D(x, y, conversionParams.outputFormat, false);
        }

        RGB_Texture.LoadRawTextureData(buffer);
        RGB_Texture.Apply();

        buffer.Dispose();
    }

    void Update()
    {
        drawVoxels();
        updateMeshColor();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = Camera.main.ScreenPointToRay(touch.position);
                hitTest(ray.direction, ray.origin);
                Debug.Log(ray.direction);
                _ray = ray;
            }
        }
    }

    public void CreateVoxel()
    {
        isReadyUpdateMeshColor = false;
        // clearBuffer();
        // initBuffer();

        voxelCalculator.SetInt("gridSize", voxelSizeOneLine);
        voxelCalculator.SetBuffer(1, "VoxelBuffer", voxelBuffer);
        voxelCalculator.SetMatrix("LocalToWorldMatrix", matrix);
        voxelCalculator.SetBuffer(1, "VertexBuffer", vertexBuffer);
        voxelCalculator.SetBuffer(1, "IndexBuffer", indexBuffer);
        voxelCalculator.SetVectorArray("VoxelVertices", voxelVertices);
        voxelCalculator.Dispatch(1, meshIndicesLength / 24, 1, 1);

        vertexBuffer.Release();
        indexBuffer.Release();

        isReadyCreateVoxel = true;
        arCameraBackground.enabled = false;
    }

    public void InitBuffer()
    {
        StartCoroutine(initBuffer());
    }

    IEnumerator initBuffer()
    {
        //for delay to create combineMesh
        yield return new WaitForSeconds(2f);

        voxelScale = (float)voxelSizeOneLine / (float)voxelCountOneLine;

        meshFilter = sampleObject.GetComponent<MeshFilter>();
        meshIndices = meshFilter.mesh.GetIndices(0);
        meshVertices = meshFilter.mesh.vertices;

        voxelBuffer = new ComputeBuffer(voxelCountOneLine * voxelCountOneLine * voxelCountOneLine, Marshal.SizeOf(typeof(VoxelData)));

        VoxelData[] voxelDatas = new VoxelData[voxelCountOneLine * voxelCountOneLine * voxelCountOneLine];
        voxelBuffer.SetData(voxelDatas);

        args[0] = cloneMesh.GetIndexCount(0);
        args[1] = (uint)(voxelCountOneLine * voxelCountOneLine * voxelCountOneLine);
        args[2] = cloneMesh.GetIndexStart(0);
        args[3] = cloneMesh.GetBaseVertex(0);

        argBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        argBuffer.SetData(args);

        voxelCalculator.SetBuffer(0, "VoxelBuffer", voxelBuffer);
        voxelCalculator.SetInt("voxelCountOneLine", voxelCountOneLine);
        voxelCalculator.SetInt("voxelSizeOneLine", voxelSizeOneLine);
        voxelCalculator.SetFloat("voxelScale", voxelScale);
        // voxelUpdater.Dispatch(0, voxelCountOneLine / 4, voxelCountOneLine / 4, voxelCountOneLine / 4);
        clearBuffer();

        voxelMaterial.SetBuffer("VoxelBuffer", voxelBuffer);
        voxelMaterial.SetFloat("voxelScale", voxelScale);

        //voxelの頂点を定義
        voxelVertices[0] = new Vector4(0, 0, 0, 0);
        voxelVertices[1] = new Vector4(voxelScale, 0, 0, 0);
        voxelVertices[2] = new Vector4(voxelScale, voxelScale, 0, 0);
        voxelVertices[3] = new Vector4(0, voxelScale, 0, 0);
        voxelVertices[4] = new Vector4(0, 0, voxelScale, 0);
        voxelVertices[5] = new Vector4(voxelScale, 0, voxelScale, 0);
        voxelVertices[6] = new Vector4(voxelScale, voxelScale, voxelScale, 0);
        voxelVertices[7] = new Vector4(0, voxelScale, voxelScale, 0);

        //vertex bufferの作成
        var mesh = meshFilter.sharedMesh;
        var indices = mesh.GetIndices(0);
        var vertices = mesh.vertices;
        meshIndicesLength = meshIndices.Length;
        meshVerticesLength = meshVertices.Length;
        matrix = sampleObject.transform.localToWorldMatrix;

        vertexBuffer = new ComputeBuffer(vertices.Length, Marshal.SizeOf(typeof(VertexData)));
        indexBuffer = new ComputeBuffer(indices.Length, Marshal.SizeOf(typeof(int)));

        VertexData[] vertexDatas = new VertexData[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexDatas[i].position = vertices[i];
            vertexDatas[i].color = Color.white;
        }

        vertexBuffer.SetData(vertexDatas);
        indexBuffer.SetData(indices);

        isReadyUpdateMeshColor = true;

        //衝突判定用のappend bufferを定義
        collisionDetectionBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(CollisionData)), ComputeBufferType.Append);
    }

    void clearBuffer()
    {
        voxelCalculator.Dispatch(0, voxelCountOneLine / 2, voxelCountOneLine / 2, voxelCountOneLine / 2);
    }

    void updateMeshColor()
    {
        if (!isReadyUpdateMeshColor)
            return;

        var viewMatrix = Camera.main.worldToCameraMatrix;
        var projectionMatrix = GL.GetGPUProjectionMatrix(Camera.main.projectionMatrix, true);

        voxelCalculator.SetVector("TextureSize", new Vector4(RGB_Texture.width, RGB_Texture.height, 0, 0));
        voxelCalculator.SetBuffer(2, "VertexBuffer", vertexBuffer);
        voxelCalculator.SetBuffer(2, "VoxelBuffer", voxelBuffer);
        voxelCalculator.SetMatrix("LocalToWorldMatrix", matrix);
        voxelCalculator.SetMatrix("MatrixV", viewMatrix);
        voxelCalculator.SetMatrix("MatrixP", projectionMatrix);
        voxelCalculator.SetTexture(2, "CameraImage", RGB_Texture);
        voxelCalculator.Dispatch(2, meshVerticesLength / 8, 1, 1);
    }

    void drawVoxels()
    {
        if (!isReadyCreateVoxel)
            return;

        Graphics.DrawMeshInstancedIndirect(cloneMesh, 0, voxelMaterial, new Bounds(Vector3.zero, Vector3.one * 100), argBuffer);
    }

    public void hitTest(Vector3 rayDir, Vector3 rayOrigin)
    {
        if (!isReadyCreateVoxel)
            return;

        collisionDetectionBuffer = new ComputeBuffer(10, Marshal.SizeOf(typeof(CollisionData)), ComputeBufferType.Append);
        var _ray = rayDir.normalized;
        voxelCalculator.SetVector("RayDir", new Vector4(_ray.x, _ray.y, _ray.z, 0));
        voxelCalculator.SetVector("RayOrigin", new Vector4(rayOrigin.x, rayOrigin.y, rayOrigin.z, 0));
        voxelCalculator.SetBuffer(3, "VoxelBuffer", voxelBuffer);
        voxelCalculator.SetBuffer(3, "CollisionDetectionBuffer", collisionDetectionBuffer);
        voxelCalculator.SetFloat("voxelScale", voxelScale);
        voxelCalculator.Dispatch(3, voxelCountOneLine / 2, voxelCountOneLine / 2, voxelCountOneLine / 2);

        CollisionData[] result = new CollisionData[10];
        collisionDetectionBuffer.GetData(result);
        var v = result
                   .OrderByDescending(d => d.distance).ToArray<CollisionData>();

        // GameObject.Instantiate(test, v[0].pos, quaternion.identity);
    }

    void OnDisable()
    {
        voxelBuffer.Release();
        argBuffer.Release();
    }

    void OnDrawGizmos()
    {
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f));

        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));

        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f), new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * -0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f));
        Debug.DrawLine(new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f), new Vector3(voxelSizeOneLine * 0.5f, voxelSizeOneLine * 0.5f, voxelSizeOneLine * -0.5f));

        Debug.DrawLine(_ray.origin, _ray.origin + _ray.direction * 3f, Color.red);
    }
}
