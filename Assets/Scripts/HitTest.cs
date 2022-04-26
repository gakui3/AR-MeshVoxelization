using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitTest : MonoBehaviour
{
    [SerializeField]
    Vector3 ray_origin;

    [SerializeField]
    Vector3 ray_dir;

    [SerializeField]
    Vector3 aabb_pos;

    [SerializeField]
    float aabb_scale;

    Vector3 normal;
    Vector3 v0, v1, v2, a0, a1;

    // Start is called before the first frame update
    void Start()
    {
        v0 = Vector3.zero;
        v1 = new Vector3(1, 0, 0);
        v2 = new Vector3(1, 0, 1);

        a0 = new Vector3(0.5f, -1, 0.25f);
        a1 = new Vector3(0.5f, 1, 0.25f);

        Debug.Log(detectIsIntersectedLineSegmentAndPolygon(a0, a1, v0, v2, v1));
    }

    void Update()
    {
        //hitTest();
    }

    void hitTest()
    {
        Vector3 _dir = ray_dir.normalized;
        Vector2 aabb_x = new Vector2(aabb_pos.x - aabb_scale * 0.5f, aabb_pos.x + aabb_scale * 0.5f);
        Vector2 aabb_y = new Vector2(aabb_pos.y - aabb_scale * 0.5f, aabb_pos.y + aabb_scale * 0.5f);
        Vector2 aabb_z = new Vector2(aabb_pos.z - aabb_scale * 0.5f, aabb_pos.z + aabb_scale * 0.5f);
        normal = Vector3.zero;


        var x01 = (aabb_x.x - ray_origin.x) / _dir.x;
        var x02 = (aabb_x.y - ray_origin.x) / _dir.x;
        var x_slab_in = Mathf.Min(x01, x02);
        var x_slab_out = Mathf.Max(x01, x02);
        if (x01 < 0 && x02 < 0)
            return;

        var y01 = (aabb_y.x - ray_origin.y) / _dir.y;
        var y02 = (aabb_y.y - ray_origin.y) / _dir.y;
        var y_slab_in = Mathf.Min(y01, y02);
        var y_slab_out = Mathf.Max(y01, y02);
        if (y01 < 0 && y02 < 0)
            return;

        var z01 = (aabb_z.x - ray_origin.z) / _dir.z;
        var z02 = (aabb_z.y - ray_origin.z) / _dir.z;
        var z_slab_in = Mathf.Min(z01, z02);
        var z_slab_out = Mathf.Max(z01, z02);
        if (z01 < 0 && z02 < 0)
            return;

        Vector3 v0 = aabb_pos + new Vector3(-aabb_scale * 0.5f, -aabb_scale * 0.5f, -aabb_scale * 0.5f);
        Vector3 v1 = aabb_pos + new Vector3(-aabb_scale * 0.5f, aabb_scale * 0.5f, -aabb_scale * 0.5f);
        Vector3 v2 = aabb_pos + new Vector3(aabb_scale * 0.5f, aabb_scale * 0.5f, aabb_scale * 0.5f);
        Debug.Log(detectIsIntersectedLineSegmentAndPolygon(ray_origin, ray_dir * 3f, v0, v1, v2));


        var maxIN = Mathf.Max(Mathf.Max(x_slab_in, y_slab_in), z_slab_in);
        var minOUT = Mathf.Min(Mathf.Min(x_slab_out, y_slab_out), z_slab_out);

        if (minOUT - maxIN > 0)
        {
            Debug.Log("hit!!!");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 direction = ray_dir * 5;
        Gizmos.DrawRay(ray_origin, direction);

        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Gizmos.DrawCube(aabb_pos, new Vector3(aabb_scale, aabb_scale, aabb_scale));

        Gizmos.color = new Color(0, 0, 1, 1);
        Gizmos.DrawRay(aabb_pos, normal * 5f);

        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(v1, v2);
        Gizmos.DrawLine(v2, v0);
        Gizmos.DrawLine(a0, a1);
    }

    // -------------------------------------------------------------
    /// <summary>
    /// 3次元座標上の線分と3角ポリゴンが交差してるかを判定
    /// </summary>
    bool detectIsIntersectedLineSegmentAndPolygon(Vector3 a, Vector3 b, Vector3 v0, Vector3 v1, Vector3 v2)
    {

        bool bCollision = detectCollisionLineSegmentAndPlane(a, b, v0, v1, v2);

        if (bCollision)
        {
            Vector3 p = calcIntersectionLineSegmentAndPlane(a, b, v0, v1, v2);
            if (detectPointIsEnclosedByPolygon(p, v0, v1, v2))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    // -------------------------------------------------------------
    /// <summary>
    /// ポリゴン上に点が含まれるかを判定
    /// </summary>
    bool detectPointIsEnclosedByPolygon(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v1));

        Vector3 n0 = Vector3.Normalize(Vector3.Cross(v1 - v0, p - v1));
        Vector3 n1 = Vector3.Normalize(Vector3.Cross(v2 - v1, p - v2));
        Vector3 n2 = Vector3.Normalize(Vector3.Cross(v0 - v2, p - v0));

        if ((1.0f - Vector3.Dot(n, n0)) > 0.001f) return false;
        if ((1.0f - Vector3.Dot(n, n1)) > 0.001f) return false;
        if ((1.0f - Vector3.Dot(n, n2)) > 0.001f) return false;

        return true;
    }

    // -------------------------------------------------------------
    /// <summary>
    /// 3次元座標上の線分と平面の交点座標を求める
    /// </summary>
    Vector3 calcIntersectionLineSegmentAndPlane(Vector3 a, Vector3 b, Vector3 v0, Vector3 v1, Vector3 v2)
    {

        float distAP = calcDistancePointAndPlane(a, v0, v1, v2);
        float distBP = calcDistancePointAndPlane(b, v0, v1, v2);

        float t = distAP / (distAP + distBP);

        return (b - a) * t + a;
    }

    // -------------------------------------------------------------
    /// <summary>
    /// ある点から平面までの距離
    /// </summary>
    float calcDistancePointAndPlane(Vector3 p, Vector3 v0, Vector3 v1, Vector3 v2)
    {
        Vector3 n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v1));
        Vector3 g = (v0 + v1 + v2) / 3.0f;
        return Mathf.Abs(Vector3.Dot(n, p - g));
    }

    // -------------------------------------------------------------
    /// <summary>
    /// 3次元座標上の線分と平面が交差してるかを判定
    /// </summary>
    bool detectCollisionLineSegmentAndPlane(Vector3 a, Vector3 b, Vector3 v0, Vector3 v1, Vector3 v2)
    {

        Vector3 n = Vector3.Normalize(Vector3.Cross(v1 - v0, v2 - v1));
        Vector3 g = (v0 + v1 + v2) / 3.0f;

        if (Vector3.Dot((a - g), n) * Vector3.Dot((b - g), n) <= 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}