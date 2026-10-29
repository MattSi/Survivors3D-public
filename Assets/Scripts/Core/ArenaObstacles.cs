using System.Collections.Generic;
using UnityEngine;

/// <summary>One layout per run, with a shared visibility graph for enemy navigation.</summary>
public sealed class ArenaObstacles : MonoBehaviour
{
    [SerializeField, Range(8, 16)] private int obstacleCount = 10;
    private readonly List<Bounds> blocks = new List<Bounds>();
    private readonly List<Vector3> corners = new List<Vector3>();
    private Transform generated;
    private float[,] edges;
    private float[] costs;
    private float nextRouteUpdate;
    private const float Clearance = 0.7f;
    public IReadOnlyList<Bounds> Blocks => blocks;

    public void PrepareBossArena()
    {
        // Remove only this run's generated cover; the surrounding forest remains intact.
        if(generated!=null) generated.gameObject.SetActive(false);
        blocks.Clear(); corners.Clear(); costs=new float[0]; edges=new float[0,0];
        Physics.SyncTransforms();
    }

    public void Generate()
    {
        if (generated != null) { generated.gameObject.SetActive(false); Destroy(generated.gameObject); }
        blocks.Clear(); corners.Clear();
        var templates = new List<Transform>();
        foreach (Transform child in transform)
            if (child.name == "Rock_3" || child.name == "Box" || child.name == "Barrel" || child.name == "Stump1") templates.Add(child);
        generated = new GameObject("Forest Camp Combat Layout").transform;
        generated.SetParent(transform, false);
        if (templates.Count == 0) return;
        // The eight anchors define two distinct, walk-around groups. Both cross-field
        // shortcuts and the outer retreat lane remain clear for every run seed.
        foreach(int side in new[] {-1,1})
            foreach(int end in new[] {-1,1})
                for(int row=0;row<2;row++)
                {
                    string kind=side<0?(row==0?"Stump1":"Box"):"Rock_3";
                    Transform template=templates.Find(t=>t.name==kind) ?? templates[0];
                    PlaceObstacle(template,new Vector3(side*(6.5f+row*4),0,end*4.7f),
                        2.4f,side<0?0:end*18,side<0?"West Timber":"East Rocks");
                }
        // Random props occupy optional bays, never either shortcut or the outer lane.
        var bays=new List<Vector3>();
        foreach(int side in new[] {-1,1}) foreach(int end in new[] {-1,1})
            foreach(float x in new[] {6.5f,10.5f}) bays.Add(new Vector3(side*x,0,end*10.5f));
        int extras=Mathf.Clamp(obstacleCount,8,16)-8;
        for(int i=0;i<extras;i++)
        {
            int chosen=Random.Range(0,bays.Count); Vector3 spot=bays[chosen]; bays.RemoveAt(chosen);
            spot+=new Vector3(Random.Range(-.4f,.4f),0,Random.Range(-.4f,.4f));
            string kind=Random.value<.5f?"Box":"Barrel";
            PlaceObstacle(templates.Find(t=>t.name==kind) ?? templates[0],spot,
                Random.Range(1.4f,1.9f),Random.Range(-25f,25f),"Supply Bay");
        }
        foreach (Bounds b in blocks)
        {
            Vector3 e = b.extents + Vector3.one * (Clearance + 0.08f);
            foreach (int x in new[] {-1, 1}) foreach (int z in new[] {-1, 1})
                corners.Add(new Vector3(b.center.x + x * e.x, 0, b.center.z + z * e.z));
        }
        edges = new float[corners.Count, corners.Count]; costs = new float[corners.Count];
        for (int i = 0; i < corners.Count; i++) for (int j = 0; j < corners.Count; j++)
            edges[i,j] = Clear(corners[i], corners[j], Clearance) ? Vector3.Distance(corners[i], corners[j]) : float.PositiveInfinity;
        nextRouteUpdate = 0;
        Physics.SyncTransforms();
    }

    private void PlaceObstacle(Transform template,Vector3 position,float width,float yaw,string group)
    {
        Transform visual=Instantiate(template,generated);
        visual.name=group+" "+(blocks.Count+1);
        visual.rotation=Quaternion.Euler(0,yaw,0)*visual.rotation;
        Bounds bounds=VisualBounds(visual);
        visual.localScale*=width/Mathf.Max(bounds.size.x,bounds.size.z);
        bounds=VisualBounds(visual);
        visual.position+=position-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        bounds=VisualBounds(visual);
        foreach(Collider old in visual.GetComponentsInChildren<Collider>()) {old.enabled=false; Destroy(old);}
        var body=new GameObject(group+" Collision"); body.transform.SetParent(generated,false);
        body.transform.position=new Vector3(bounds.center.x,1,bounds.center.z);
        body.AddComponent<BoxCollider>().size=new Vector3(bounds.size.x,2,bounds.size.z);
        var cameraObstacle=body.AddComponent<CameraObstacleBounds>();
        cameraObstacle.VisualBounds=bounds;
        cameraObstacle.VisualRenderers=visual.GetComponentsInChildren<Renderer>();
        bounds.center=new Vector3(bounds.center.x,0,bounds.center.z);
        bounds.size=new Vector3(bounds.size.x,20,bounds.size.z);
        blocks.Add(bounds);
    }

    private static Bounds VisualBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    public float HitDistance(Vector3 origin, Vector3 direction, float radius)
    {
        origin.y = 0; direction.y = 0;
        float nearest = float.PositiveInfinity;
        foreach (Bounds original in blocks)
        {
            Bounds b = original; b.Expand(new Vector3(radius * 2, 0, radius * 2));
            if (b.Contains(origin)) return 0;
            if (b.IntersectRay(new Ray(origin, direction), out float d)
                && !float.IsNaN(d) && !float.IsInfinity(d) && d >= 0f) nearest = Mathf.Min(nearest, d);
        }
        return nearest;
    }

    public bool Clear(Vector3 from, Vector3 to, float radius = 0)
    {
        Vector3 delta = to - from; delta.y = 0;
        return delta.sqrMagnitude < 0.000001f || HitDistance(from, delta.normalized, radius) > delta.magnitude;
    }

    public void UpdateRoutes(Vector3 target)
    {
        if (costs == null || Time.time < nextRouteUpdate) return;
        nextRouteUpdate = Time.time + 0.2f; target.y = 0;
        var visited = new bool[corners.Count];
        for (int i = 0; i < costs.Length; i++) costs[i] = Clear(corners[i], target, 0.4f) ? Vector3.Distance(corners[i], target) : float.PositiveInfinity;
        for (int step = 0; step < costs.Length; step++)
        {
            int best = -1;
            for (int i = 0; i < costs.Length; i++) if (!visited[i] && (best < 0 || costs[i] < costs[best])) best = i;
            if (best < 0 || float.IsPositiveInfinity(costs[best])) break;
            visited[best] = true;
            for (int i = 0; i < costs.Length; i++) costs[i] = Mathf.Min(costs[i], costs[best] + edges[best,i]);
        }
    }

    public Vector3 Move(Vector3 origin, Vector3 target, float travel, float radius=0.4f)
    {
        float y = origin.y; origin.y = 0; target.y = 0;
        Vector3 waypoint = target;
        if (!Clear(origin, target, radius))
        {
            float best = float.PositiveInfinity;
            waypoint = origin;
            for (int i = 0; i < corners.Count; i++)
            {
                // Reaching a corner must advance the path, not keep selecting that same node.
                if((origin-corners[i]).sqrMagnitude<.0025f) continue;
                float score = Vector3.Distance(origin, corners[i]) + costs[i];
                if (score >= best || !Clear(origin, corners[i], Clearance)) continue;
                best = score; waypoint = corners[i];
            }
        }
        Vector3 delta = waypoint - origin;
        travel = Mathf.Min(travel, delta.magnitude, Mathf.Max(0, HitDistance(origin, delta.normalized, radius) - 0.01f));
        Vector3 result = origin + delta.normalized * travel; result.y = y;
        return result;
    }
}
