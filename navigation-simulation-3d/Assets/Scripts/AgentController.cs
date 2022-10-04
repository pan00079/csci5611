using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    // Start is called before the first frame update
    int agentSpeed = 120;
    int maxForce = 600;
    float separationCoef = 1000f;
    float alignmentCoef = 1.5f;
    float attractionCoef = 3f;
    float k_goal = 5f;
    float k_avoid = 300f;

    [HideInInspector]
    public Color color;
    [HideInInspector]
    public int id;
    [HideInInspector]
    public Vector3 startPos;
    [HideInInspector]
    public Vector3 goalPos;
    [HideInInspector]
    public List<int> path;
    [HideInInspector]
    public List<Vector3> nodePos;
    [HideInInspector]
    public Vector3 velocity;

    GameObject model;
    RuntimeAnimatorController controller;

    bool moving = false;
    bool followersMoving = false;
    // bool reachedGoal = false;
    Quaternion prevRot;
    PRM prm;
    Animator animator;
    float followerSizeVal;
    List<GameObject> agentFollowers;
    List<Vector3> followerVel;
    List<Vector3> followerAcc;
    List<bool> followerStop;
    GameObject followerParent;
    List<GameObject> obstacleList;

    void Start()
    {
        prm = FindObjectOfType<PRM>();
        model = FindObjectOfType<GlobalController>().model;
        controller = FindObjectOfType<GlobalController>().controller;
        obstacleList = FindObjectOfType<GlobalController>().obstacleList;
        obstacleList.Remove(gameObject);
        animator = GetComponentInChildren<Animator>();
        agentFollowers = new List<GameObject>();
        followerVel = new List<Vector3>();
        followerAcc = new List<Vector3>();
        followerStop = new List<bool>();
        followerParent = new GameObject("Agent #" + id + " Follower Container");
        createFollowerAgents();
    }

    void createFollowerAgents()
    {
        int num = Random.Range(2, 10);
        for (int i = 0; i < num; i++)
        {
            GameObject follower;
            if (model != null && controller != null)
            {
                follower = Instantiate(model);
                follower.AddComponent<SphereCollider>();
                follower.AddComponent<MeshRenderer>();
                follower.AddComponent<Animator>().runtimeAnimatorController = controller;
                follower.GetComponent<Animator>().enabled = false;
                follower.GetComponentInChildren<SkinnedMeshRenderer>().material.color = color;
            }
            else
            {
                follower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                follower.GetComponent<Renderer>().material.color = color;
            }
            //follower.GetComponent<SphereCollider>().enabled = false;
            followerSizeVal = (transform.localScale.x / 2.0f);
            Vector3 followerSize = (transform.localScale / 2.0f);
            follower.name = "Follower #" + i;
            follower.transform.localScale = followerSize;
            follower.GetComponent<SphereCollider>().radius = 0.7f;
            follower.transform.parent = followerParent.transform;

            int xNegRange = (int)(transform.position.x - (transform.localScale.x * 0.3f));
            int xPosRange = (int)(transform.position.x + (transform.localScale.x * 0.3f));
            int zNegRange = (int)(transform.position.z - (transform.localScale.z * 0.3f));
            int zPosRange = (int)(transform.position.z + (transform.localScale.z * 0.3f));
            int randX = Random.Range(xNegRange, xPosRange);
            int randZ = Random.Range(zNegRange, zPosRange);
            Vector3 newPos = new Vector3(randX, 1, randZ);

            follower.transform.position = newPos;
            agentFollowers.Add(follower);
            followerVel.Add(Vector3.zero);
            followerAcc.Add(Vector3.zero);
            followerStop.Add(false);
        }
    }

    void moveAgentLeader()
    {
        prevRot = transform.localRotation;

        Vector3 curPos = transform.position;
        Vector3 dir = (goalPos - curPos).normalized;
        float distBetween = Vector3.Distance(curPos, goalPos);

        if (distBetween < 0.3f)
        {
            moving = false;
            // reachedGoal = true;
            velocity = Vector3.zero;
            Debug.Log(name + " reached its goal!");
        }

        LayerMask floorMask = ~LayerMask.GetMask("Floor");
        LayerMask goalMask = floorMask & ~LayerMask.GetMask("Goals");
        if (!Physics.Raycast(curPos, dir, distBetween, goalMask))
        {
            velocity = (goalPos - curPos).normalized * agentSpeed;
            transform.position += (velocity * Time.deltaTime);
            Quaternion newRot = Quaternion.LookRotation(dir);
            transform.localRotation = Quaternion.Lerp(prevRot, newRot, Time.deltaTime * agentSpeed);
            return;

        }

        if (path.Count > 0)
        {
            if (path[0] == -1)
            {
                // call plan path
                startPos = transform.position;
                prm.recreatePath(this);
                return;
            }

            if (path.Count > 1)
            {
                Vector3 secondNode = nodePos[path[1]];
                dir = (secondNode - curPos).normalized;
                distBetween = Vector3.Distance(curPos, secondNode);

                if (!Physics.Raycast(curPos, dir, distBetween))
                {
                    path.RemoveAt(0);
                }
            }

            Vector3 firstNode = nodePos[path[0]];
            dir = (firstNode - curPos).normalized;
            distBetween = Vector3.Distance(curPos, firstNode);
            if (Physics.Raycast(curPos, dir, distBetween))
            {
                // call plan path
                startPos = transform.position;
                startPos = transform.position;
                prm.recreatePath(this);
                return;

            }

            velocity = dir * agentSpeed;
            transform.position += (velocity * Time.deltaTime);
            float distToNext = Vector3.Distance(transform.position, firstNode);
            if (distToNext < 0.3f)
            {
                path.RemoveAt(0);
            }

            Quaternion newRot = Quaternion.LookRotation(dir);
            transform.localRotation = Quaternion.Lerp(prevRot, newRot, Time.deltaTime * 2.0f);
        }
    }

    void moveFollowers()
    {
        followersMoving = false;
        foreach (bool stopped in followerStop)
        {
            if (!stopped) followersMoving = true;
        }

        Quaternion newRot = prevRot;
        Vector3 leaderPosition = transform.position;

        for (int i = 0; i < agentFollowers.Count; i++)
        {
            if (followerStop[i]) continue;
            Vector3 followerPos = agentFollowers[i].transform.position;
            Vector3 acceleration = Vector3.zero;
            Vector3 averagePos = Vector3.zero;
            Vector3 averageVel = Vector3.zero;
            int count = 0;
            for (int j = 0; j < agentFollowers.Count; j++)
            {
                
                Vector3 neighborPos = agentFollowers[j].transform.position;
                float distToNeighbor = Vector3.Distance(followerPos, neighborPos);
                if (distToNeighbor < .05 || distToNeighbor > 60) continue;
                Vector3 neighborVel = (followerPos - neighborPos);
                Vector3 separationForce =
                    neighborVel.normalized * (float)(separationCoef / Mathf.Pow(distToNeighbor, 2));
                acceleration += separationForce;

                averagePos += neighborPos;
                averageVel += neighborVel;
                count += 1;
            }

            float distToLeader = Vector3.Distance(followerPos, leaderPosition);
            if (distToLeader > .05f && distToLeader < 60)
            {
                Vector3 neighborVel = (followerPos - leaderPosition);
                Vector3 separationForce =
                    neighborVel.normalized * (float)(separationCoef / Mathf.Pow(distToLeader, 2));
                acceleration += separationForce;
            }
            
            averagePos += leaderPosition;
            averageVel += velocity;
            count += 1;
            
            averagePos *= (1.0f / count);
            averageVel *= (1.0f / count);
            Vector3 attractionForce = (averagePos - followerPos).normalized;
            attractionForce *= attractionCoef;
            attractionForce = Vector3.ClampMagnitude(attractionForce, maxForce);
            Vector3 towards = (averageVel - followerVel[i]).normalized;
            Vector3 alignmentForce = towards * alignmentCoef;
            acceleration += (attractionForce + alignmentForce);
            acceleration = Vector3.ClampMagnitude(acceleration, maxForce);

            Vector3 targetVel = (leaderPosition - followerPos);
            float followerSpeed = agentSpeed;
            if (targetVel.magnitude > followerSpeed)
            {
                targetVel = targetVel.normalized * followerSpeed;
            }
            Vector3 goalSpeedForce = (targetVel - followerVel[i]) * k_goal;
            acceleration += goalSpeedForce;
            acceleration = Vector3.ClampMagnitude(acceleration, maxForce);

            followerAcc[i] = acceleration;
            float distToGoal = Vector3.Distance(followerPos, goalPos);
            if (distToGoal < 1.0f)
            {
                followerStop[i] = true;
                agentFollowers[i].GetComponent<Animator>().enabled = false;
                continue;
            }

            foreach (GameObject obstacle in obstacleList)
            {
                AgentController isAgent = obstacle.GetComponent<AgentController>();
                ObstacleController isMovingObstacle = obstacle.GetComponent<ObstacleController>();
                Vector3 obstaclePos = new Vector3(obstacle.transform.position.x, 1, obstacle.transform.position.z);
                Vector3 obstacleVel;
                float obstacleSize;
                if (isAgent != null)
                {
                    obstacleVel = isAgent.velocity;
                    obstacleSize = obstacle.transform.localScale.x * 1f;
                }
                else if (isMovingObstacle != null)
                {
                    obstacleVel = isMovingObstacle.velocity;
                    obstacleSize = obstacle.transform.localScale.x * 0.8f;
                }
                else
                {
                    obstacleVel = Vector3.zero;
                    obstacleSize = obstacle.transform.localScale.x * 0.6f;
                }
                
                float ttc = computeTTC(followerPos, followerVel[i], followerSizeVal, obstaclePos, obstacleVel, obstacleSize);
                if (ttc > 0)
                {
                    Vector3 futureID = followerPos + (followerVel[i] * ttc);
                    Vector3 futureNeighbour = obstaclePos + (obstacleVel * ttc);
                    Vector3 relativeDir = (futureID - futureNeighbour).normalized;
                    float urgency = 1f / ttc;
                    acceleration += relativeDir * urgency * k_avoid;
                }
            }

            //acceleration = Vector3.ClampMagnitude(acceleration, maxForce);
            followerAcc[i] = acceleration;
        }

        for (int i = 0; i < agentFollowers.Count; i++)
        {
            if (followerStop[i]) continue;
            agentFollowers[i].transform.position += (followerVel[i] * Time.deltaTime);
            followerVel[i] += (followerAcc[i] * Time.deltaTime);

            float followerSpeed = agentSpeed * 2.0f;

            if (followerVel[i].magnitude > followerSpeed)
            {
                followerVel[i] = (followerVel[i].normalized * followerSpeed);
            }

            if (moving) newRot = prevRot;
            else newRot = Quaternion.LookRotation(leaderPosition - agentFollowers[i].transform.position);
            
            Quaternion finalRot = Quaternion.Lerp(agentFollowers[i].transform.localRotation, newRot, Time.deltaTime * 2.0f);
            agentFollowers[i].transform.localRotation = finalRot;
        }
    }

    float computeTTC(Vector3 pos1, Vector3 vel1, float radius1, Vector3 pos2, Vector3 vel2, float radius2)
    {
        return rayCircleIntersectTime(pos2, radius1 + radius2, pos1, vel1 - vel2);
    }
    
    float rayCircleIntersectTime(Vector3 center, float r, Vector3 l_start, Vector3 l_dir)
    {
        Vector3 toCircle = center - l_start;

        float a = l_dir.magnitude * l_dir.magnitude;
        float b = -2.0f * Vector3.Dot(l_dir, toCircle);
        float c = toCircle.sqrMagnitude - (r * r);

        float d = b * b - 4 * a * c;
        if (d >= 0)
        {
            float t = (-b - Mathf.Sqrt(d)) / (2 * a);
            if (t >= 0) return t;
        }

        return -1;
    }
    

    // Update is called once per frame
    void Update()
    {
        if (moving && path != null)
        {
            animator.enabled = true;
            moveAgentLeader();
        }
        else
        {
            animator.enabled = false;
        }

        if (followersMoving)
        {
            moveFollowers();
            foreach (GameObject follower in agentFollowers)
            {
                follower.GetComponent<Animator>().enabled = true;
            }
        }
    }

    /*
    void restartAgent()
    {
        reachedGoal = false;
        prm.recreateStartAndGoal(this);
    }
    */

    public void SetMoving(bool move)
    {
        moving = move;
        followersMoving = move;
    }
}
