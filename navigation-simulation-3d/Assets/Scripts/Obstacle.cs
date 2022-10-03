using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public Material selectedColor;
    Material originalColor;

    // Start is called before the first frame update
    void Start()
    {
        originalColor = GetComponentInChildren<Renderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    public void setSelected(bool selected)
    {
        if (selected)
        {
            GetComponentInChildren<Renderer>().material = selectedColor;
        }
        else
        {
            GetComponentInChildren<Renderer>().material = originalColor;
        }
    }
}
