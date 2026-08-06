using UnityEngine;

namespace DuckovVP.Console;

public class IconBehaviour: MonoBehaviour
{
    private Rigidbody2D rb;
    private MeshRenderer sr;
    
    protected void Start()
    {
        gameObject.transform.position = new Vector3(0f, 0f, 5f);
        
        rb = gameObject.GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(0.1f, -0.1f).normalized * 55;
        sr = gameObject.GetComponent<MeshRenderer>();
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        sr.material.color = new Color(Random.value, Random.value, Random.value);
    }
}