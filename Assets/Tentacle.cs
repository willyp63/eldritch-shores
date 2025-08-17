using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    void Start()
    {
        // Set animation frame to random offset
        GetComponent<Animator>().Play(0, 0, Random.value);
    }
}
