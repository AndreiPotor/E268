using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ViewCone : MonoBehaviour
{
    public Player player;
    public LayerMask layerMask;
    

    private Vector3 origin;
    private float startingAngle;

    // Frontal cone
    public float fov = 90;
    public int frontalConeRayCount = 100;
    public float frontalViewDistance = 20f;
    private float frontalViewDistanceModifier = 1f; // for applying buffs/debuffs

    // Surround view
    public int surroundRayCount = 30;
    public float surroundViewDistance = 2f;
    private float surroundViewDistanceModifer = 1f; // for applying buffs/debuffs


    // algorithm variables
    Mesh mesh;

    Vector3[] vertices;
    Vector2[] uv;
    int[] triangles;

    float angle;
    float angleIncrease;
    float distance;

    int vertexIndex;
    int triangleIndex;

    float distanceFar;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().sortingOrder = 10; // making sure the mesh is over the sprites
        fov = 90f;
        origin = Vector3.zero;
        distanceFar = 3f * (Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)) - Camera.main.ViewportToWorldPoint(new Vector3(1, 1, 0))).magnitude;
    }

    private void Update()
    {
        // getting data from player
        origin = player.transform.position;
        setAimDirection(player.getMousePos() - origin);

        vertices = new Vector3[2 * (frontalConeRayCount + surroundRayCount + 2) + 1000];
        uv = new Vector2[vertices.Length];
        triangles = new int[(frontalConeRayCount + surroundRayCount + 2) * 6 + 6000];

        vertexIndex = 0;
        triangleIndex = 0;

        // FRONTAL CONE
        angle = startingAngle + 10;
        angleIncrease = (fov + 20) / frontalConeRayCount;
        distance = frontalViewDistanceModifier * frontalViewDistance;
        scan(frontalConeRayCount);

        // SURROUND VIEW
        angle = startingAngle - fov;
        angleIncrease = (360 - fov) / surroundRayCount;
        distance = surroundViewDistanceModifer * surroundViewDistance;
        scan(surroundRayCount);

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(origin, Vector3.one * 1000f);
    }

    private void scan(int rayCount)
    {
        for (int i = 0; i <= rayCount; i++)
        {
            float angleRad = angle * (Mathf.PI / 180f);
            Vector3 vectorFromAngle = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            Vector3 vertex;
            Vector3 vertexFar;

            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, vectorFromAngle, distance, layerMask);
            if (raycastHit2D.collider == null)
            {
                // WE DID NOT COLLIDE
                vertex = origin + vectorFromAngle * distance;
            }
            else
            {
                // WE COLLIDED WITH AN OBSTACLE
                if ((raycastHit2D.collider.transform.position - origin).magnitude <= distance)
                    vertex = raycastHit2D.collider.transform.position;
                else
                    vertex = raycastHit2D.point;
                // paint minimap
                Vector3 pos = raycastHit2D.collider.gameObject.transform.position;
                UI.paintMinimap((int)pos.x, (int)pos.y, raycastHit2D.collider.gameObject.tag);
            }
            // THE VERTEX OUTSIDE OF THE SCREEN BOUNDS
            vertexFar = origin + vectorFromAngle * distanceFar;
            vertexFar.z = 100;
            vertex.z = 100;
            vertices[vertexIndex] = vertex;
            vertices[vertexIndex + 1] = vertexFar;

            // making the triangles
            if (i > 0)
            {
                triangles[triangleIndex + 0] = vertexIndex + 1;
                triangles[triangleIndex + 1] = vertexIndex;
                triangles[triangleIndex + 2] = vertexIndex - 1;

                triangles[triangleIndex + 3] = vertexIndex - 1;
                triangles[triangleIndex + 4] = vertexIndex;
                triangles[triangleIndex + 5] = vertexIndex - 2;

                triangleIndex += 6;
            }

            vertexIndex += 2;
            angle -= angleIncrease;
        }
    }

    private void setAimDirection(Vector3 aimDirection)
    {
        aimDirection = aimDirection.normalized;
        startingAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        if (startingAngle < 0)
            startingAngle += 360;
        startingAngle -= fov / 2f - 90;
    }

    public void setFrontalModifier(float modifier)
    {
        frontalViewDistanceModifier = modifier;
    }

    public void setSurroundModifier(float modifier)
    {
        surroundViewDistanceModifer = modifier;
    }
}
