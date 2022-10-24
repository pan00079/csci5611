using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHController : MonoBehaviour
{
    // Start is called before the first frame update

    //public float simHeight = 768;
    //public float simWidth = 1366;
    public GameObject sprite;
    Animator animator;
    public GameObject particlePrefab;
    public int numParticles = 100;
    public float particleSize = 1.0f;
    GameObject[] particles;
    Vector3[] initialPos;
    List<GameObject> selectedObjects;

    [Range(0.0f, 5.0f)]
    public float k_smooth_radius = 0.035f;
    [Range(0.0f, 2000.0f)]
    public float k_stiff = 150f;
    [Range(0.0f, 5000.0f)]
    public float k_stiffN = 1000f;
    [Range(0.0f, 5.0f)]
    public float k_rest_density = 0.2f;
    [Range(0.0f, 1000.0f)]
    public float gravity = 10f;
    [Range(0.0f, 600.0f)]
    public float maxPressure = 30;
    [Range(0.0f, 3000.0f)]
    public float maxNPressure = 300;

    public float mouseRad = 4.0f;

    void Start()
    {
        // storing initial particle positions for reset 
        initialPos = new Vector3[numParticles];

        // selected objects for drag user interaction
        selectedObjects = new List<GameObject>();

        // sprite animator
        if (sprite != null)
        {
            animator = sprite.GetComponent<Animator>();
        }

        // initializing particles
        int sqrt = (int) Mathf.Sqrt(numParticles);
        int loop = sqrt / 2;
        particles = new GameObject[numParticles];
        int particleID = 0;
        for (int i = -loop; i < loop; i++)
        {
            for (int j = -loop; j < loop; j++)
            {
                GameObject particle = Instantiate(particlePrefab);
                particle.transform.parent = transform;
                float offset = particleSize * 1.5f;
                particle.transform.Translate(offset * i * Vector3.forward + offset * j * Vector3.right);
                particle.transform.Translate(offset * (i+5) * Vector3.up);
                initialPos[particleID] = particle.transform.localPosition;
                particle.GetComponent<Particle>().size = particleSize;
                particles[particleID] = particle;
                particleID++;
            }
            
        }
    }
    // SPH fluid simulation, following Clavet et al. approach
    // An attempt to code fluid simulation in Unity 
    void simulateSPH()
    {
        float dt = Time.deltaTime;
        foreach (GameObject p in particles)
        {
            Particle particle = p.GetComponent<Particle>();

            // compute velocity based on positions and gravity
            particle.velocity = (particle.position - particle.oldPosition) / dt;
            particle.velocity += new Vector2(0, -gravity) * dt;

            // checking pre-set bounds 
            // left
            if (particle.position.x < -50)
            {
                particle.position.x = -50f;
                particle.velocity.x *= -0.2f;
            }
            // top 
            if (particle.position.y > 45)
            {
                particle.position.y = 45f;
                particle.velocity.y *= -0.2f;
            }
            // bottom
            if (particle.position.y < -20)
            {
                particle.position.y = -20f;
                particle.velocity.y *= -0.2f;
            }
            // right
            if (particle.position.x > 30)
            {
                particle.position.x = 30f;
                particle.velocity.x *= -0.2f;
            }

            // add velocity towards mouse position if particle is grabbed
            if (particle.grabbed)
            {
                Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                particle.velocity += 30 * dt * ((mousePosition - particle.position) / mouseRad - particle.velocity);
            }

            // update positions and zero out densities
            particle.oldPosition = particle.position;
            particle.position += particle.velocity * dt;
            particle.density = 0.0f;
            particle.nDensity = 0.0f;

        }

        // TO-DO: try kd-trees to speed up the simulation
        // finding all pairs/neighbors for the particles
        List<ParticlePairs> pairs = new List<ParticlePairs>();
        for (int i = 0; i < numParticles-1; i++)
        {
            for (int j = i+1; j < numParticles; j++)
            {
                Particle p1 = particles[i].GetComponent<Particle>();
                Particle p2 = particles[j].GetComponent<Particle>();
                float dist = Vector2.Distance(p1.position, p2.position);
                if (dist < k_smooth_radius && i < j)
                {
                    float q = 1 - (dist / k_smooth_radius);
                    ParticlePairs pair = new ParticlePairs(p1, p2, q);
                    pairs.Add(pair);
                }
            }
        }

        // update densities
        foreach (ParticlePairs pair in pairs)
        {
            pair.p1.density += pair.q2;
            pair.p2.density += pair.q2;
            pair.p1.nDensity += pair.q3;
            pair.p2.nDensity += pair.q3;
        }

        // update pressure
        foreach (GameObject p in particles)
        {
            Particle particle = p.GetComponent<Particle>();
            particle.pressure = k_stiff * (particle.density - k_rest_density);
            particle.nPressure = k_stiffN * particle.nDensity;
            if (particle.pressure > maxPressure) particle.pressure = maxPressure;
            if (particle.nPressure > maxNPressure) particle.nPressure = maxNPressure;
        }

        // compute displacement 
        foreach (ParticlePairs pair in pairs)
        {
            Particle a = pair.p1;
            Particle b = pair.p2;
            float totalPressure = (a.pressure + b.pressure) * pair.q1 + (a.nPressure + b.nPressure) * pair.q2;
            float displace = totalPressure * (dt * dt);
            a.position += displace * (a.position - b.position).normalized;
            b.position += displace * (b.position - a.position).normalized;
        }

        // update particle positions and colors
        foreach (GameObject p in particles)
        {
            Particle particle = p.GetComponent<Particle>();
            particle.transform.localPosition = particle.position;
            float q = particle.pressure / maxPressure;
            if (particle.grabbed)
            {
                p.GetComponent<SpriteRenderer>().color = new Color(0.9f - q * 0.5f, 0.6f - q * 0.4f, 0.4f - q * 0.3f);
            }
            else
            {
                p.GetComponent<SpriteRenderer>().color = new Color(0.9f - q * 0.5f, 0.4f - q * 0.3f, 0.6f - q * 0.4f);
            }
            
        }
    }

    void ResetScene()
    {
        for (int i = 0; i < numParticles; i++)
        {
            GameObject pGO = particles[i];
            pGO.transform.localPosition = initialPos[i];
            Particle p = pGO.GetComponent<Particle>();
            p.position = p.oldPosition = initialPos[i];
            p.velocity = Vector2.zero;
            p.pressure = 0.0f;
            p.density = 0.0f;
            p.nPressure = 0.0f;
            p.nDensity = 0.0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScene();
        }

        // user interaction, using Unity's Physics2D
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(mousePosition, mouseRad);
            
            foreach (Collider2D col in colliders)
            {
                if (col.GetComponentInChildren<Particle>())
                {
                    GameObject particle = col.transform.gameObject;
                    particle.GetComponentInChildren<Particle>().grabbed = true;
                    selectedObjects.Add(particle);

                }
            }

            // change sprite animation if we grab particles
            if (selectedObjects.Count > 0)
            {
                animator.Play("attack");
            }
        }

        // release particles and reset sprite animation
        if (Input.GetMouseButtonUp(0) && selectedObjects.Count > 0)
        {
            foreach (GameObject particle in selectedObjects)
            {
                particle.GetComponentInChildren<Particle>().grabbed = false;
            }
            selectedObjects.Clear();
            animator.Play("idle");
        }

        // update SPH simulation
        simulateSPH();
    }

    // struct for pairs 
    struct ParticlePairs
    {
        public Particle p1;
        public Particle p2;
        public float q1;
        public float q2;
        public float q3;

        public ParticlePairs(Particle p1, Particle p2, float q)
        {
            this.p1 = p1;
            this.p2 = p2;
            q1 = q;
            q2 = q*q;
            q3 = q*q*q;
        }
    }
}
