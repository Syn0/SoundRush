using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class moove : MonoBehaviour
{

  public GameObject player;
  public GameObject sample;
  public int speed = 0;
  public float xMin = 202.5f;
  public float xMax = 992.5f;
  private IEnumerator coroutine;
  // Start is called before the first frame update
  void Start()
    {
    coroutine = WaitAndPrint(0.5f);
    StartCoroutine(coroutine);
  }

    // Update is called once per frame
    void Update()
    {
    deplacement();
    }
  void deplacement()
  {
    if (Input.GetKey("d") && player.transform.position.x < xMax) {
      player.transform.position = new Vector3(player.transform.position.x + speed, player.transform.position.y);
      //Debug.Log(player.transform.position.x);

    }
    if (Input.GetKey("q") && player.transform.position.x > xMin) {
      player.transform.position = new Vector3(player.transform.position.x - speed, player.transform.position.y);
      //Debug.Log(player.transform.position.x);
    }
  }

  private IEnumerator WaitAndPrint(float waitTime)
  {
    while (true) {
      yield return new WaitForSeconds(waitTime);
      sample.transform.position = new Vector3(Random.Range(xMin, xMax), player.transform.position.y);
      //print("WaitAndPrint " + Time.time);
    }
  }
}
