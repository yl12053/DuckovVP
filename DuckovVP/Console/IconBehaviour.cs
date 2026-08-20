using UnityEngine;

namespace DuckovVP.Console;

public class IconBehaviour: MonoBehaviour
{
    private Rigidbody2D rb;
    private MeshRenderer sr;

    private int current = 0;
    
    protected void Start()
    {
        gameObject.transform.position = new Vector3(0f, 0f, 5f);
        
        rb = gameObject.GetComponent<Rigidbody2D>();
        rb.velocity = new Vector2(0.1f, -0.1f).normalized * 55;
        sr = gameObject.GetComponent<MeshRenderer>();
        sr.material.color = Color.white;
    }

    private static Color[] colors = new[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan,
        Color.white
    };
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        sr.material.color = colors[current++];
        current %= colors.Length;
    }
}