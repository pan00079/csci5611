using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClothSimulation : MonoBehaviour
{
    // Start is called before the first frame update
    public int height = 30;
    public int width = 50;

    public GameObject obstacle;
    public float obstacleRadius = 20.0f;

    float segmentLength = 1f;

    [Range(0.0f, 25.0f)]
    public float gravity = 1.0f;
    [Range(0.0f, 10.0f)]
    public float mass = 1f;
    [Range(0.0f, 10.0f)]
    public float restLenX = 1f;
    [Range(0.0f, 10.0f)]
    public float restLenY = 1f;
    [Range(0.0f, 10000.0f)]
    public float k = 200;
    [Range(0.0f, 800.0f)]
    public float kv = 100;
    [Range(0.0f, 3.0f)]
    public float dragCoef = 5;
    public Vector3 windVel = Vector3.zero;

    public bool dragBool = true;
    public bool debug = false;
    public bool obstacleBool = true;
    public Material material;

    Vector3[] position;
    Vector3[] velocity;
    Vector3 acceleration;
    GameObject[] debugNodes;
    int[] triangles;

    Mesh clothMesh;

    void Start()
    {
        if (obstacle == null)
        {
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obstacle.transform.localScale = new Vector3(10f, 10f, 10f);
            obstacle.transform.localPosition = new Vector3(10f, 0f, 0f);
        }

        int size = height * width;
        position = new Vector3[size];
        velocity = new Vector3[size];
        acceleration = new Vector3(0f, -(gravity/10f), 0f);
        if (debug) debugNodes = new GameObject[size];
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++)
            {
                int index = i * width + j;
                position[index] = Vector3.zero;
                position[index].x = j * segmentLength;
                //position[index].z = i;
                position[index].z = i * segmentLength;
                //position[index].y = 10 - i * segmentLength;

                position[index].y = 10;
                velocity[index] = Vector3.zero;
                //drag[index] = Vector3.zero;
                if (debug)
                {
                    GameObject debugNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    debugNode.name = "Node " + i + ", " + j;
                    debugNode.transform.localPosition = position[index];
                    debugNode.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    debugNode.transform.parent = transform;
                    debugNodes[index] = debugNode;
                }
            }
        }
        //uv = new Vector2[size];
        int triangleSize = (height - 1) * (width - 1) * 6; 
        triangles = new int[triangleSize * 6];
        computeTriangles();

        clothMesh = new Mesh();
        updateMesh();
        GetComponent<MeshFilter>().mesh = clothMesh;
        GetComponent<MeshRenderer>().material = material;

        enabled = false;
    }

    private void ResetScene()
    {
        Debug.Log("Resetting the cloth.");
        clothMesh.Clear();
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int index = i * width + j;
                position[index] = Vector3.zero;
                position[index].x = j * segmentLength;
                //position[index].z = i;
                position[index].z = i * segmentLength;
                //position[index].y = 10 - i * segmentLength;

                position[index].y = 10;
                velocity[index] = Vector3.zero;
                //drag[index] = Vector3.zero;
                if (debug) debugNodes[index].transform.position = position[index];
            }
        }
        updateMesh();
    }

    void computeTriangles()
    {
        int triIndex = 0;
        int vertIndex = 0;
        for (int i = 0; i < (height - 1); i++)
        {
            for (int j = 0; j < (width - 1); j++)
            {
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 3] = triangles[triIndex + 2] = vertIndex + 1;
                triangles[triIndex + 4] = triangles[triIndex + 1] = vertIndex + (width - 1) + 1;
                triangles[triIndex + 5] = vertIndex + (width - 1) + 2;
                triIndex += 6;
                vertIndex++;
            }
            vertIndex++;
        }
    }

    void updateMesh()
    {
        clothMesh.vertices = position;
        clothMesh.triangles = triangles;
        clothMesh.RecalculateNormals();
        clothMesh.RecalculateBounds();
    }

    Vector3 computeAccel(int curInd, int nextInd, float restLen)
    {
        float ks = k;
        float kd = kv;
        Vector3 diff = position[nextInd] - position[curInd];
        float len = diff.magnitude;
        Vector3 e = diff.normalized;
        float v1 = Vector3.Dot(e, velocity[curInd]);
        float v2 = Vector3.Dot(e, velocity[nextInd]);
        float force = -ks * (restLen - len) - kd * (v1 - v2);
        Vector3 accel = force * e * (1f / mass);
        return accel;
    }

    /*
    Vector3 computeAccelRK
        (Vector3 curPos, Vector3 nextPos, Vector3 curVel, Vector3 nextVel, bool diag, float restLen)
    {
        float ks = k;
        float kd = kv;
        if (diag)
        {
            ks = kShear;
            kd = kvShear;
        }
        Vector3 diff = nextPos - curPos;
        float len = diff.magnitude;
        Vector3 e = diff.normalized;
        float v1 = Vector3.Dot(e, curVel);
        float v2 = Vector3.Dot(e, nextVel);
        float force = -ks * (restLen - len) - kd * (v1 - v2);
        Vector3 accel = force * e * (1f / mass);
        return accel;
    }
    */

    Vector3[] updateDrag(Vector3[] vel)
    {
        Vector3[] drag = new Vector3[height * width];
        for (int i = 0; i < (height - 1); i++)
        {
            for (int j = 0; j < (width - 1); j++)
            {
                int topLeft = i * width + j;
                int topRight = topLeft + 1;
                int bottomLeft = topLeft + width;

                Vector3 averageVel = ((vel[topLeft] + vel[topRight] + vel[bottomLeft]) / 3.0f) - windVel;
                Vector3 v1 = vel[bottomLeft] - vel[topLeft];
                Vector3 v2 = vel[topRight] - vel[topLeft];
                Vector3 normal = Vector3.Cross(v1, v2);

                Vector3 dragForce;

                if (normal.magnitude <= 0)
                {
                    dragForce = Vector3.zero;
                }
                else
                {
                    dragForce = (-dragCoef * averageVel.magnitude * Vector3.Dot(averageVel, normal) / (2 * normal.magnitude)) * normal.normalized;
                }

                drag[topLeft] += dragForce / 3f;
                drag[topRight] += dragForce / 3f;
                drag[bottomLeft] += dragForce / 3f;
            }

        }
        return drag;
    }

    void updateCloth() {

        float dt = Time.deltaTime;
        clothMesh.Clear();
        Vector3[] velNewMid = (Vector3[])velocity.Clone();

        // update horizontal forces (x-direction)
        for (int i = 0; i < height; i++)
            {
            for (int j = 0; j < width - 1; j++)
            {
                int curCell = i * width + j;
                int nextCol = curCell + 1;
                Vector3 accel = computeAccel(curCell, nextCol, restLenX);
                velNewMid[curCell] += accel * dt / 2f;
                velNewMid[nextCol] -= accel * dt / 2f;
            }
        }

        // update vertical forces (y-direction)
        for (int j = 0; j < width; j++)
            {
            for (int i = 0; i < height - 1; i++)
            {
                int curCell = i * width + j;
                int nextRow = curCell + width;
                Vector3 accel = computeAccel(curCell, nextRow, restLenY);
                velNewMid[curCell] += accel * dt / 2f;
                velNewMid[nextRow] -= accel * dt / 2f;
                /*
                Vector3 k1acc = computeAccel(curCell, nextRow, false, restLenY);
                Vector3 k1vel = velNewMid[curCell] + k1acc * dt;
                Vector3 k1pos = position[curCell] + k1vel * dt;
                Vector3 k1velN = velNewMid[curCell] - k1acc * dt;
                Vector3 k1posN = position[curCell] - k1vel * dt;
                //( curPos,  nextPos,  curVel,  nextVel,  diag,  restLen)
                Vector3 k2acc = computeAccelRK(k1pos, k1posN, k1vel, k1velN, false, restLenY);
                Vector3 k2vel = k1vel + k2acc * dt / 2f;
                Vector3 k2pos = k1pos + k2vel * dt / 2f;
                Vector3 k2velN = k1velN - k2acc * dt / 2f;
                Vector3 k2posN = k1pos - k2vel * dt / 2f;
                Vector3 k3acc = computeAccelRK(k2pos, k2posN, k2vel, k2velN, false, restLenY);
                Vector3 k3vel = k2vel + k3acc * dt / 2f;
                Vector3 k3pos = k2pos + k3vel * dt / 2f;
                Vector3 k3velN = k2velN - k3acc * dt / 2f;
                Vector3 k3posN = k2pos - k3vel * dt / 2f;
                Vector3 k4acc = computeAccelRK(k3pos, k3posN, k3vel, k3velN, false, restLenY);
                Vector3 accel = (k1acc + 2 * k2acc + 2 * k3acc + k4acc) / 6;
                velNewMid[curCell] += accel * dt;
                velNewMid[nextRow] -= accel * dt;
                */
            }
        }

         Vector3[] drag = updateDrag(velNewMid);

        for (int i = 1; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int curCell = i * width + j;
                velNewMid[curCell] += acceleration;
                if (dragBool) velNewMid[curCell] += drag[curCell] * dt;
                velocity[curCell] = velNewMid[curCell];
                position[curCell] += velNewMid[curCell] * dt;

                /*
                float dist = Vector3.Distance(position[curCell], obstacle.transform.localPosition);
                if (dist < 5.1f)
                {
                    Vector3 normal = -(obstacle.transform.localPosition - position[curCell]);
                    normal = normal.normalized;
                    Vector3 bounce = Vector3.Dot(normal, position[curCell]) * normal;
                    velocity[curCell] -= (1.2f * bounce);
                    position[curCell] += ((5.1f - dist) * normal);
                }
                */

                if (debug) debugNodes[curCell].transform.localPosition = position[curCell];
            }
        }

        // calculate cloth collision
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                int curCell = i * width + j;
                float dist = Vector3.Distance(position[curCell], obstacle.transform.localPosition);

                if (obstacleBool && dist < obstacleRadius + 0.5f)
                {
                    Vector3 normal = (obstacle.transform.localPosition - position[curCell]);
                    normal = normal.normalized;
                    Vector3 bounce = Vector3.Dot(normal, velocity[curCell]) * normal;
                    velocity[curCell] -= (1.4f * bounce);
                    position[curCell] += normal * (0.2f + obstacleRadius - dist);
                }

                if (debug) debugNodes[curCell].transform.localPosition = position[curCell];
            }
        }

        updateMesh();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScene();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            obstacleBool = !obstacleBool;
            if (obstacleBool)
            {
                obstacle.GetComponentInChildren<MeshRenderer>().enabled = true;
            }
            else
            {
                obstacle.GetComponentInChildren<MeshRenderer>().enabled = false;
            }

            ResetScene();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (dragBool)
            {
                windVel = Vector3.zero;
            }
            dragBool = !dragBool;
            ResetScene();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) && dragBool)
        {
            if (windVel.z > -15f)
            {
                if (windVel.z > 0)
                {
                    windVel.y -= 0.6f;
                }
                else
                {
                    windVel.y += 0.6f;
                }
                windVel.z -= 1f;
            }
            
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) && dragBool)
        {
            if (windVel.z < 15f)
            {
                if (windVel.z >= 0)
                {
                    windVel.y += 0.6f;
                }
                else
                {
                    windVel.y -= 0.6f;
                }
                windVel.z += 1f;
            }
        }

        updateCloth();
    }
}
