using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleController : MonoBehaviour
{
    int obstacleSpeed;
    int groundSize;
    bool doubleSpeed = false;
    int obstacleSize = 22;

    [HideInInspector]
    public Vector3 velocity;
    Vector3 prevPos;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        transform.forward = new Vector3(cameraForward.x, 0, cameraForward.z);
        obstacleSpeed = FindObjectOfType<GlobalController>().obstacleSpeed;
        groundSize = FindObjectOfType<GlobalController>().groundSize;
        prevPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cameraForward = Camera.main.transform.forward;
        transform.forward = new Vector3(cameraForward.x, 0, cameraForward.z);

        // on holding shift, double the speed
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            doubleSpeed = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            doubleSpeed = false;
        }

        // move the obstacle using WASD
        CheckObstacleMovement();
        Vector3 curPos = transform.position;
        velocity = curPos - prevPos;
        prevPos = curPos;
    }

    void CheckObstacleMovement()
    {
        // bounds checking
        int bound = groundSize - obstacleSize / 2;
        float speed = obstacleSpeed;
        if (doubleSpeed)
        {
            speed *= 2;
        }
        float speedStep = speed * Time.deltaTime;
        Vector3 curPos = transform.position;

        bool moved = false;


        if (Input.GetKey(KeyCode.UpArrow) && curPos.z < bound)
        {
            // z+, forward
            transform.Translate(new Vector3(0, 0, speedStep));
            moved = true;
        }
        if (Input.GetKey(KeyCode.LeftArrow) && curPos.x > -bound)
        {
            // x-, left
            transform.Translate(new Vector3(-speedStep, 0, 0));
            moved = true;
        }
        if (Input.GetKey(KeyCode.DownArrow) && curPos.z > -bound)
        {
            // z-, backwards
            transform.Translate(new Vector3(0, 0, -speedStep));
            moved = true;
        }
        if (Input.GetKey(KeyCode.RightArrow) && curPos.x < bound)
        {
            // x+, right
            transform.Translate(new Vector3(speedStep, 0, 0));
            moved = true;
        }

        if (moved)
        {
            FindObjectOfType<PRM>().connectNeighbors();
        }
    }
}
