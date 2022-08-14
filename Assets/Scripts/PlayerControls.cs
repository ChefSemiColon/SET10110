using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class PlayerControls : MonoBehaviour
{
    private float yaw;
    private float mouseSpeedH = 1.0f;
    private float mouseSpeedV = 1.0f;
    private float pitch;
    private bool dash;

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SceneManager.LoadScene(1);
        }     
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SceneManager.LoadScene(2);
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        dash = Input.GetKey(KeyCode.LeftShift) ? true : false;

        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.up * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * Time.deltaTime * (dash ? 10.0f : 5.0f);
        }

        if (Input.GetMouseButton(1))
        {
            yaw += mouseSpeedH * Input.GetAxis("Mouse X");
            pitch -= mouseSpeedV * Input.GetAxis("Mouse Y");

            transform.eulerAngles = new Vector3(pitch*5, yaw*5, 0.0f);

        }










    }
}
