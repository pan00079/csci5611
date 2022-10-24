using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    // Start is called before the first frame update
    public float size = 1f;
    public Vector2 position;
    public Vector2 oldPosition;
    public Vector2 velocity;
    public float pressure;
    public float density;
    public float nPressure;
    public float nDensity;
    public bool grabbed;
    

    void Start()
    {
        transform.localScale = new Vector2(size, size);
        position = oldPosition = transform.localPosition;
        velocity = Vector2.zero;
        pressure = 0.0f;
        density = 0.0f;
        nPressure = 0.0f;
        nDensity = 0.0f;
        grabbed = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
