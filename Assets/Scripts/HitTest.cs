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

    // Start is called before the first frame update
    void Start()
    {

    }

    void Update()
    {
        hitTest();
    }

    void hitTest()
    {
        Vector3 _dir = ray_dir.normalized;
        Vector2 aabb_x = new Vector2(aabb_pos.x - aabb_scale * 0.5f, aabb_pos.x + aabb_scale * 0.5f);
        Vector2 aabb_y = new Vector2(aabb_pos.y - aabb_scale * 0.5f, aabb_pos.y + aabb_scale * 0.5f);
        Vector2 aabb_z = new Vector2(aabb_pos.z - aabb_scale * 0.5f, aabb_pos.z + aabb_scale * 0.5f);

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
    }
}
