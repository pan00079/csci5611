using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalController : MonoBehaviour
{
    [Header("General Settings")]
    public float eps = 1f;

    [Header("Obstacle Settings")]
    public GameObject obstacleModel;
    public int obstacleSpeed = 20;
    public int obstacleSize = 20;
    GameObject selectedObstacle;
    GameObject obstacle;
    [HideInInspector]
    public List<GameObject> obstacleList;

    [Header("Agents Settings")]
    public int numAgentLeaders = 5;
    public int agentSize = 5;
    public int groundSize = 50;
    public GameObject model;
    public RuntimeAnimatorController controller;
    [HideInInspector]
    public List<AgentController> agents;

    [Header("Camera Settings")]
    public float mouseSensitivity = 0.7f;
    Vector3 defaultCamPos;
    Quaternion defaultCamRot;
    Vector2 mouseRot;

    public GameObject nodePrefab;
    bool setup;
    PRM prmController;
    LayerMask floorMask;

    // Start is called before the first frame update
    void Start()
    {
        mouseRot = new Vector2(-45, -40);
        floorMask = ~LayerMask.GetMask("Floor");
        defaultCamPos = Camera.main.transform.position;
        defaultCamRot = Camera.main.transform.rotation;
        obstacleList = new List<GameObject>(GameObject.FindGameObjectsWithTag("UserObstacle"));

        GameObject temp;
        if (obstacleModel != null)
        {
            temp = Instantiate(obstacleModel);
        }
        else
        {
            temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        temp.AddComponent<ObstacleController>();
        temp.GetComponent<ObstacleController>().enabled = false;
        temp.transform.localScale = new Vector3(obstacleSize, obstacleSize, obstacleSize);
        temp.transform.position = new Vector3(0, (obstacleSize / 20.0f), 0);
        temp.name = "UserObstacle";
        temp.GetComponent<SphereCollider>().radius += ((float) agentSize / (2.0f * obstacleSize));
        obstacle = temp;
        obstacleList.Add(obstacle);

        agents = new List<AgentController>();
        for (int i = 0; i < numAgentLeaders; i++)
        {
            AgentController agent;

            Color color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.25f, 0.75f, 1f, 1f);
            if (model != null && controller != null)
            {
                GameObject modelObj = Instantiate(model);
                modelObj.AddComponent<SphereCollider>();
                modelObj.AddComponent<MeshRenderer>();
                modelObj.AddComponent<AgentController>();
                modelObj.AddComponent<Animator>().runtimeAnimatorController = controller;
                modelObj.GetComponentInChildren<SkinnedMeshRenderer>().material.color = color;
                agent = modelObj.GetComponent<AgentController>();
            }
            else
            {
                agent = GameObject.CreatePrimitive(PrimitiveType.Sphere).AddComponent<AgentController>();
                agent.GetComponent<Renderer>().material.color = color;
            }
            agent.name = "Agent #" + i;
            agent.transform.localScale = new Vector3(agentSize, agentSize, agentSize);
            agent.GetComponent<SphereCollider>().radius = 1.0f;
            agent.GetComponent<AgentController>().color = color;
            agent.transform.parent = transform;

            int rangeSize = groundSize - agentSize;
            int randX = Random.Range(-rangeSize, rangeSize);
            int randZ = Random.Range(-rangeSize, rangeSize);
            Vector3 newPos = new Vector3(randX, 1, randZ);

            Collider[] hitColliders = Physics.OverlapSphere(newPos, agentSize+eps, floorMask);
            while (hitColliders.Length != 0) {
                randX = Random.Range(-rangeSize, rangeSize);
                randZ = Random.Range(-rangeSize, rangeSize);
                newPos = new Vector3(randX, 1, randZ);
                hitColliders = Physics.OverlapSphere(newPos, agentSize + eps, floorMask);
            }

            agent.transform.position = newPos;
            agent.startPos = newPos;
            agent.id = i;

            agents.Add(agent);
        }
        
        foreach (AgentController agent in agents)
        {
            obstacleList.Add(agent.gameObject);
        }

        setup = true;
        prmController = new GameObject("PRMController").AddComponent<PRM>();
        prmController.enabled = false;
    }

    /*
    void RestartScene()
    {
        Debug.Log("Restarting the scene.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    */

    // Update is called once per frame
    void Update()
    {
        // press R to reset the camera to default position
        if (Input.GetKey(KeyCode.R))
        {
            Camera.main.transform.position = defaultCamPos;
            Camera.main.transform.rotation = defaultCamRot;
        }


        // move camera with WASD
        if (Input.GetKey(KeyCode.W))
        {
            Camera.main.transform.Translate(new Vector3(0, 300.0f * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.A))
        {
            Camera.main.transform.Translate(new Vector3(-300.0f * Time.deltaTime, 0, 0));
        }
        if (Input.GetKey(KeyCode.S))
        {
            Camera.main.transform.Translate(new Vector3(0, -300.0f * Time.deltaTime, 0));
        }
        if (Input.GetKey(KeyCode.D))
        {
            Camera.main.transform.Translate(new Vector3(300.0f * Time.deltaTime, 0, 0));
        }

        // rotate camera with right click
        if (Input.GetMouseButton(1))
        {
            mouseRot.x += Input.GetAxis("Mouse X") * mouseSensitivity;
            mouseRot.y += Input.GetAxis("Mouse Y") * mouseSensitivity;
            Camera.main.transform.localRotation = Quaternion.Euler(-mouseRot.y, mouseRot.x, 0);
        }


        // zoom in/out with mouse wheel
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            Camera.main.fieldOfView--;
        }
        if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            Camera.main.fieldOfView++;
        }

        // while in the setup stage
        if (setup)
        {
            // clicking left mouse button on the obstacles will highlight them and allow you to place them in the scene
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hitInfo = new RaycastHit();
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo))
                {
                    if (hitInfo.transform.gameObject.tag == "UserObstacle")
                    {
                        if (selectedObstacle != null)
                        {
                            selectedObstacle.GetComponent<Obstacle>().setSelected(false);
                        }
                        selectedObstacle = hitInfo.transform.gameObject;
                        selectedObstacle.GetComponent<Obstacle>().setSelected(true);
                    }
                    else
                    {
                        if (selectedObstacle != null)
                        {
                            selectedObstacle.transform.position = hitInfo.point;
                        }
                    }
                }
            }

            // pressing escape will un-highlight the obstacle
            if (Input.GetKey(KeyCode.Escape))
            {
                if (selectedObstacle != null)
                {
                    selectedObstacle.GetComponent<Obstacle>().setSelected(false);
                }
            }


            // pressing spacebar will start the simulation
            if (Input.GetKey(KeyCode.Space))
            {
                if (selectedObstacle != null)
                {
                    selectedObstacle.GetComponent<Obstacle>().setSelected(false);
                }
                obstacle.GetComponent<ObstacleController>().enabled = true;
                prmController.enabled = true;
                setup = false;
            }
        }
   
    }
}
