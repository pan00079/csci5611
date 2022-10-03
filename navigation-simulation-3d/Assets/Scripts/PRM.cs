using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PRM : MonoBehaviour
{
    // Start is called before the first frame update
    public float distanceLimit = 100;
    public int numNodes = 300;

    bool debug = false;

    GameObject goalStorage;
    GameObject nodeStorage;
    GameObject nodePrefab;

    bool[] visited;
    int[] parent;
    float[] distances;
    List<int>[] neighbors;
    List<Vector3> nodePos;

    int numAgentLeaders;
    int groundSize;
    int agentSize;
    float eps;
    List<AgentController> agents;

    bool graphMade = false;
    float timePassed;
    LayerMask floorMask;

    void Start()
    {
        //Here, we represent our graph structure as a neighbor list
        neighbors = new List<int>[numNodes];  //A list of neighbors can can be reached from a given node
        visited = new bool[numNodes]; //A list which store if a given node has been visited
        parent = new int[numNodes]; //A list which stores the best previous node on the optimal path to reach this node
        distances = new float[numNodes]; //A list which stores shortest distance from start to this node

        nodePos = new List<Vector3>();

        numAgentLeaders = FindObjectOfType<GlobalController>().numAgentLeaders;
        groundSize = FindObjectOfType<GlobalController>().groundSize;
        agentSize = FindObjectOfType<GlobalController>().agentSize;
        eps = FindObjectOfType<GlobalController>().eps;
        agents = FindObjectOfType<GlobalController>().agents;

        if (debug)
        {
            nodeStorage = new GameObject("DebugNodeStorage");
            nodePrefab = FindObjectOfType<GlobalController>().nodePrefab;
        }

        goalStorage = new GameObject("GoalStorage");
        floorMask = ~LayerMask.GetMask("Floor");

        generateRandomNodes();
        connectNeighbors();
        initiateAgentPath();
        startAgents();
        graphMade = true;

    }

    public void recreatePaths()
    {
        generateRandomNodes();
        connectNeighbors();
        foreach (AgentController agent in agents)
        {
            recreatePath(agent);
        }
    }

    public void recreatePath(AgentController agent)
    {
        List<int> path = planPath(agent.startPos, agent.goalPos);
        agent.path = path;
        agent.nodePos = nodePos;
    }

    public void recreateStartAndGoal(AgentController agent)
    {
        Vector3 newStart = agent.transform.position;
        Vector3 newGoal = generateRandomGoal(agent.id);

        agent.startPos = newStart;
        agent.goalPos = newGoal;

        recreatePath(agent);
    }

    Vector3 generateRandomGoal(int id)
    {
        int rangeSize = groundSize - agentSize;
        int randX = Random.Range(-rangeSize, rangeSize);
        int randZ = Random.Range(-rangeSize, rangeSize);
        Vector3 goalPos = new Vector3(randX, 1, randZ);

        bool resetPos = false;
        GameObject[] goals = GameObject.FindGameObjectsWithTag("Goals");
        Collider[] hitColliders = Physics.OverlapSphere(goalPos, 2f*agentSize, floorMask);

        foreach (GameObject otherGoal in goals)
        {
            if (Vector3.Distance(otherGoal.transform.position, goalPos) < (2f * agentSize))
            {
                resetPos = true;
            }
        }
        while (hitColliders.Length != 0 || resetPos)
        {
            randX = Random.Range(-rangeSize, rangeSize);
            randZ = Random.Range(-rangeSize, rangeSize);
            goalPos = new Vector3(randX, 1, randZ);
            hitColliders = Physics.OverlapSphere(goalPos, 2f * agentSize, floorMask);
            resetPos = false;
            foreach (GameObject otherGoal in goals)
            {
                if (Vector3.Distance(otherGoal.transform.position, goalPos) < (2f * agentSize))
                {
                    resetPos = true;
                }
            }
        }

        GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        goal.name = "Goal" + id;
        goal.tag = "Goals";
        goal.transform.localScale *= (agentSize * 1.4f);
        goal.GetComponent<SphereCollider>().radius += (agentSize / (agentSize * 1.5f));
        goal.GetComponent<Renderer>().material.color
            = agents[id].GetComponent<AgentController>().color;
        goal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        goal.layer = LayerMask.NameToLayer("Goals");
        goal.transform.position = goalPos;
        goal.transform.parent = goalStorage.transform;
        //Debug.Log(goal.GetComponent<SphereCollider>());

        return goalPos;
    }

    void startAgents()
    {
        foreach (AgentController agent in agents)
        {
            Vector3 dir = (agent.goalPos - agent.startPos).normalized;
            agent.transform.localRotation = Quaternion.LookRotation(dir);
            agent.SetMoving(true);
        }
    }

    IEnumerator InitiateAgent(AgentController agent)
    {
        //Debug.Log("Coroutine started");
        Vector3 goalPos = generateRandomGoal(agent.id);
        agent.goalPos = goalPos;
        List<int> path = planPath(agent.startPos, goalPos);
        agent.GetComponent<AgentController>().path = path;
        agent.GetComponent<AgentController>().nodePos = nodePos;

        //Debug.Log("Coroutine ended");
        yield return new WaitUntil(() => (goalPos != null));

    }

    void initiateAgentPath()
    {
        foreach (AgentController agent in agents)
        {
            StartCoroutine(InitiateAgent(agent));
        }
    }

    public void generateRandomNodes()
    {
        if (debug) {
            nodePos = new List<Vector3>();
            Destroy(nodeStorage);
            nodeStorage = new GameObject("DebugNodeStorage");
        }

        for (int i = 0; i < numNodes; i++)
        {
            int rangeSize = (int) (groundSize - eps);
            int randX = Random.Range(-rangeSize, rangeSize);
            int randZ = Random.Range(-rangeSize, rangeSize);
            Vector3 newPos = new Vector3(randX, 1, randZ);
            
            Collider[] hitColliders = Physics.OverlapSphere(newPos, eps, floorMask);

            while (hitColliders.Length != 0)
            {
                randX = Random.Range(-rangeSize, rangeSize);
                randZ = Random.Range(-rangeSize, rangeSize);
                newPos = new Vector3(randX, 1, randZ);
                hitColliders = Physics.OverlapSphere(newPos, eps, floorMask);
            }

            nodePos.Add(newPos);

            if (debug)
            {
                GameObject nodeMarker = Instantiate(nodePrefab);
                nodeMarker.transform.position = nodePos[i];
                nodeMarker.transform.parent = nodeStorage.transform;
            }

        }
    }
    public void connectNeighbors()
    {
        for (int i = 0; i < numNodes; i++)
        {
            neighbors[i] = new List<int>();
            for (int j = 0; j < numNodes; j++)
            {
                if (i == j) continue; // don't connect to myself
                float distBetween = Vector3.Distance(nodePos[i],nodePos[j]);
                if (distBetween > distanceLimit) continue; // don't connect if it's too far away

                Vector3 dir = (nodePos[j] - nodePos[i]).normalized;
                if (!Physics.Raycast(nodePos[i], dir, distBetween, floorMask) &&
                    !Physics.Raycast(nodePos[j], -dir, distBetween, floorMask))
                {
                    neighbors[i].Add(j);
                }
            }
        }
    }

    int findMinDistNode(List<int> fringe)
    {
        int minDistID = -1;
        float minDist = float.MaxValue;
        foreach (int i in fringe)
        {
            float dist = distances[i];
            if (dist < minDist)
            {
                minDistID = i;
                minDist = dist;
            }
        }
        return minDistID;
    }
    
    List<int> runUCS(Vector3 startPos, Vector3 goalPos)
    {
        List<int> fringe = new List<int>();  //New empty fringe
        List<int> path = new List<int>(); //Path
        for (int i = 0; i < numNodes; i++)
        {   //Clear visit tags and parent pointers
            visited[i] = false;
            parent[i] = -1; //No parent yet
            distances[i] = float.MaxValue;
        }

        //Debug.Log("Beginning Search");

        // Getting all of the nodes that are visible from start
        for (int i = 0; i < numNodes; i++)
        {
            float distBetween = Vector3.Distance(nodePos[i], startPos);
            Vector3 dir = (nodePos[i] - startPos).normalized;
            if (!Physics.Raycast(startPos, dir, distBetween, floorMask))
            {
                visited[i] = true;
                distances[i] = distBetween;
                fringe.Add(i);
            }
        }

        int goalID = -1; // A variable to keep track of the closest node to graph
        while (fringe.Count > 0)
        {
            // Find the node with shortest distance in the fringe and remove it
            int currentNode = findMinDistNode(fringe);
            fringe.Remove(currentNode);

            // Check all neighbors of current node
            for (int i = 0; i < neighbors[currentNode].Count; i++)
            {
                int neighborNode = neighbors[currentNode][i];
                // Calculate distance from current node to the neighbor
                float dist = distances[currentNode] + Vector3.Distance(nodePos[currentNode],nodePos[i]);
                if (!visited[neighborNode] && dist < distances[neighborNode])
                {
                    visited[neighborNode] = true;
                    distances[neighborNode] = dist;
                    parent[neighborNode] = currentNode;
                    fringe.Add(neighborNode);
                    //Debug.Log("Added node" + neighborNode + "to the fringe.");
                    //Debug.Log(" Current Fringe: " + fringe);
                }
            }

            // Check if goal is visible from this node
            Vector3 dir = (goalPos-nodePos[currentNode]).normalized;
            float distBetween = Vector3.Distance(nodePos[currentNode], goalPos);

            LayerMask goalMask = floorMask & ~LayerMask.GetMask("Goals");
            // If it is - it's a potential solution
            if (!Physics.Raycast(nodePos[currentNode], dir, distBetween, goalMask))
                // &&
                //!Physics.Raycast(goalPos, -dir, distBetween, goalMask)
            {
                float goalDist = distances[currentNode] + distBetween;
                distances[currentNode] = goalDist;

                // If goalID is -1, first goal solution, update goalID
                if (goalID == -1)
                {
                    goalID = currentNode;
                }
                // Check if this node's distance is less than current closest to goal, if yes, update it
                else if (goalDist < distances[goalID])
                {
                    goalID = currentNode;
                }

                // Then, check fringe - if current distance to goal is less than everything else in the fridge = goal found!
                bool foundSmaller = false;
                foreach (int i in fringe)
                {
                    if (distances[goalID] > distances[i])
                    {
                        foundSmaller = true;
                    }
                }
                if (!foundSmaller)
                {
                    break;
                }
            }
        }


        // If fringe is empty, we found no valid path
        if (fringe.Count == 0)
        {
            //Debug.Log("No Path");
            path.Insert(0, -1);
            return path;
        }

        // Otherwise, we are reconstructing the path based on the parent[] array and returning the result
        //Debug.Log("\nReverse path: ");
        int prevNode = parent[goalID];
        path.Insert(0, goalID);
        //Debug.Log(goalID);
        while (prevNode >= 0)
        {
            //Debug.Log(prevNode);
            path.Insert(0, prevNode);
            prevNode = parent[prevNode];
        }

        return path;
    }

    public List<int> planPath(Vector3 startPos, Vector3 goalPos)
    {
        List<int> path = new List<int>();

        // Checking if there are any obstacles between start and goal
        Vector3 dir = (goalPos - startPos).normalized;
        float distBetween = Vector3.Distance(startPos, goalPos);

        // if there is, run the UCS algorithm, otherwise, simply return the direct path between the two
        if (Physics.Raycast(startPos, dir, distBetween, floorMask) ||
            Physics.Raycast(goalPos, -dir, distBetween, floorMask))
        {
            path = runUCS(startPos, goalPos);
        }

        return path;
    }

    void OnDrawGizmos()
    {
        if (graphMade)
        {
            for (int i = 0; i < numNodes; i++)
            {
                foreach (int j in neighbors[i])
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawLine(nodePos[i], nodePos[j]);
                }

            }

            foreach (AgentController agent in agents)
            {
                Gizmos.color = Color.red;
                List<int> path = agent.path;
                if (path.Count > 0 && path[0] == -1)
                {
                    continue;
                }
                if (path.Count == 0)
                {
                    Gizmos.DrawLine(agent.startPos, agent.goalPos);
                    continue;
                }

                Gizmos.DrawLine(agent.startPos, nodePos[path[0]]);
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int curNode = path[i];
                    int nextNode = path[i + 1];
                    Gizmos.DrawLine(nodePos[curNode], nodePos[nextNode]);
                }
                int last = path.Count - 1;
                Gizmos.DrawLine(agent.goalPos, nodePos[path[last]]);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //generateRandomNodes();
        //connectNeighbors();
        timePassed += Time.deltaTime;
        if (timePassed > 1.0f)
        {
            timePassed = 0.0f;
            recreatePaths();
        }
    }
}
