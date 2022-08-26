using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Script by Brandon Dines, Proton Fox 
public class Breakable : MonoBehaviour
{
    #region Variables
    public bool broken;
    public float breakForce;
    [SerializeField] float objectHealth;
    [SerializeField] List<GameObject> items;

    private float timer = 0;
    private bool startFade;
    private bool lootDropped;
    private ObjectFade objectFade;

    #endregion

    #region Methods
    void Start() // Sets default values and references.
    {
        breakForce = Random.Range(0.25f, 2f);
        objectFade = GetComponent<ObjectFade>();
    }

    void Update()
    {
        Broken();
    }


    private void Broken() // Breaks the object into pieces with force. Drops loot if object cotains loot. Object fades until it disappears.
    {
        if (broken)
        {
            GetComponent<Collider>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            transform.GetChild(0).gameObject.SetActive(true);
            AddForce();
            DropLoot();
            timer += Time.deltaTime;


            if (objectFade)
            {
                if (!startFade)
                {
                    foreach (MeshRenderer mesh in transform.GetComponentsInChildren<MeshRenderer>())
                    {
                        for (int i = 0; i < mesh.materials.Length; i++)
                        {
                            GetComponent<ObjectFade>().StartCoroutine(GetComponent<ObjectFade>().FadeOut(mesh.materials[i]));
                        }
                    }
                    startFade = true;
                }

                if (GetComponent<MeshRenderer>().materials[0].color.a <= 0)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                if (timer >= 5)
                {
                    Destroy(gameObject);
                }
            }
        }
    }

    private void AddForce() // Adds force to each rigidbody attached to the object.
    {
        if (timer < 0.5f)
        {
            foreach (Rigidbody rb in transform.GetChild(0).GetComponentsInChildren<Rigidbody>())
            {
                Vector3 force = (rb.transform.position - transform.position).normalized * breakForce;
                rb.AddForce(force);
            }
        }
    }

    public void DropLoot() // Drops loot attached to the object when called.
    {
        if (items.Count > 0)
        {
            if (!lootDropped)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    GameObject loot = Instantiate(items[i], transform.position, Quaternion.identity, null);
                    loot.transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);

                    Vector3 velocity = Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)) *
                        new Vector3(Random.Range(0.5f, 2.0f), Random.Range(5.0f, 15.0f), 0);

                    LootArc.Arc(loot, velocity);

                }
                lootDropped = true;


            }
        }
    }

    #endregion
}
