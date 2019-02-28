using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour {

    public float speedRot = 10;

	// Update is called once per frame
	void Update () {
        transform.Rotate(transform.up, speedRot * Time.deltaTime);
	}
}
