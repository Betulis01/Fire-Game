using TMPro;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Inventory))]
public class PlayerController : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Inventory inventory;
    Fuel fuel;

    public float speed = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        inventory = GetComponent<Inventory>();
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal"); // A/D or arrows
        float y = Input.GetAxisRaw("Vertical");   // W/S or arrows

        Vector3 move = new Vector3(x, y, 0f).normalized;
        transform.Translate(move * speed * Time.deltaTime);
        

        if (move.x > 0.01f) {
            spriteRenderer.flipX = false;
        } else if (move.x < -0.01f) {
            spriteRenderer.flipX = true;
        }


        // deposit wood — checked every frame, only works when near a fire
        if (fuel != null && Input.GetKeyDown(KeyCode.E))
        {
            if (inventory.Count("Wood") > 0)
            {
                inventory.Remove("Wood");
                fuel.Add(10f);
                Debug.Log("Fed the fire with 1 wood");
            }
        }
    }

        // sibling of Update — Unity calls this automatically on trigger overlap
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wood"))
        {
            inventory.Add("Wood", other.gameObject);

            Destroy(other.gameObject);

            Debug.Log("Pick up wood");
        }

        if (other.CompareTag("Campfire"))
        {
            // search parents too, in case the collider is a child of the Fuel object
            fuel = other.GetComponent<Fuel>();
            if (fuel == null)
                Debug.LogError("Campfire trigger has no Fuel component on it or its parents", other);
        }
    }
        void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Campfire"))
            fuel = null;                          // forget it when we leave
    }
}   
